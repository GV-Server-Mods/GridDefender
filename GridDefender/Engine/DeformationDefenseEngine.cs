using System;
using System.Collections.Concurrent;
using GVK.GridDefender.Config;
using GVK.GridDefender.Services;
using GVK.GridDefender.Utils;
using NLog;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Torch;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace GVK.GridDefender.Engine
{
    public class DeformationDefenseEngine
    {
        private static readonly ILogger Log = LogManager.GetLogger("GridDefender.Engine");

        private volatile GridDefenderConfig _config;
        private readonly DefenseStatistics _stats;
        private readonly ConcurrentDictionary<long, ulong> _lastDeformationFrames = new ConcurrentDictionary<long, ulong>();
        private readonly ConcurrentDictionary<long, int> _consecutiveContactFrames = new ConcurrentDictionary<long, int>();
        private readonly ConcurrentDictionary<long, ulong> _lastContactFrameTracker = new ConcurrentDictionary<long, ulong>();

        public DeformationDefenseEngine(GridDefenderConfig config, DefenseStatistics stats)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public void UpdateConfig(GridDefenderConfig config)
        {
            if (config != null)
            {
                _config = config;
            }
        }

        public bool IsMissile(MyCubeGrid testGrid, float speed)
        {
            if (testGrid == null) return false;
            if (speed < _config.MissileMinVelocity) return false;

            int blocks = testGrid.BlocksCount;
            if (testGrid.IsLargeGrid())
            {
                return blocks >= _config.LargeGridMissileMinBlocks && blocks <= _config.LargeGridMissileMaxBlocks;
            }
            if (testGrid.IsSmallGrid())
            {
                return blocks >= _config.SmallGridMissileMinBlocks && blocks <= _config.SmallGridMissileMaxBlocks;
            }
            return false;
        }

        public bool ShouldAllowDeformation(MyGridPhysics physics, MyEntity otherEntity, ref float separatingVelocity)
        {
            if (!_config.Enabled)
            {
                return true;
            }

            var grid = physics?.Entity as MyCubeGrid;
            if (grid == null || grid.MarkedForClose)
            {
                return true;
            }

            _stats.IncrementEvaluated();

            // 1. Subgrid / Mechanicals (Pistons, Rotors, Hinges, Suspension Wheels) Protection
            if (otherEntity is MyCubeGrid otherGrid)
            {
                if (_config.ProtectSubgrids && (GridUtils.AreInSameMechanicalGroup(grid, otherGrid) || GridUtils.AreInSameLogicalGroup(grid, otherGrid)))
                {
                    if (_config.EnableDebugLogging)
                    {
                        Log.Debug($"[GridDefender] Blocked subgrid deformation between '{grid.DisplayName}' and '{otherGrid.DisplayName}'.");
                    }
                    ApplyAntiClang(grid, physics, otherEntity);
                    _stats.IncrementBlocked(isSubgrid: true);
                    return false;
                }
            }

            // 2. Floating Objects / Ores / Loose Debris Protection
            if (otherEntity is MyFloatingObject && _config.ProtectAgainstFloatingObjects)
            {
                if (_config.EnableDebugLogging)
                {
                    Log.Debug($"[GridDefender] Blocked deformation on '{grid.DisplayName}' from floating debris.");
                }
                _stats.IncrementBlocked();
                return false;
            }

            // 3. Calculate Impact Speeds
            float gridSpeed = grid.GetSpeed();
            float otherSpeed = (otherEntity as MyCubeGrid)?.GetSpeed() ?? 0f;
            float absSepVelocity = Math.Abs(separatingVelocity);
            float impactSpeed = Math.Max(absSepVelocity, Math.Max(gridSpeed, otherSpeed));

            // 4. Safe Docking, Parking, and Slow Driving Check
            if (impactSpeed < _config.MinDrivingVelocity)
            {
                if (_config.EnableDebugLogging)
                {
                    Log.Debug($"[GridDefender] Safe driving/docking: Blocked low-speed collision on '{grid.DisplayName}' ({impactSpeed:F1} m/s < {_config.MinDrivingVelocity:F1} m/s).");
                }
                ApplyAntiClang(grid, physics, otherEntity);
                _stats.IncrementBlocked();
                return false;
            }

            // 5. Extreme Velocity Anti-Freeze Limit
            if (_config.MaxDeformationVelocity > 0 && impactSpeed > _config.MaxDeformationVelocity)
            {
                if (_config.EnableDebugLogging)
                {
                    Log.Debug($"[GridDefender] Blocked extreme-speed collision on '{grid.DisplayName}' ({impactSpeed:F1} m/s > {_config.MaxDeformationVelocity:F1} m/s).");
                }
                ApplyImpactDamping(physics, grid.IsStatic);
                ApplyAntiClang(grid, physics, otherEntity);
                _stats.IncrementBlocked(isRamming: otherEntity is MyCubeGrid, isVoxel: otherEntity is MyVoxelBase);
                return false;
            }

            // 6. Missile (Player-Made Weapon / PMW) Evaluation
            bool gridIsMissile = IsMissile(grid, impactSpeed);
            bool otherIsMissile = otherEntity is MyCubeGrid oGrid && IsMissile(oGrid, impactSpeed);

            if (_config.AllowMissileDamage && (gridIsMissile || otherIsMissile))
            {
                if (_config.EnableDebugLogging)
                {
                    string missileName = gridIsMissile ? grid.DisplayName : otherEntity.DisplayName;
                    string targetName = gridIsMissile ? (otherEntity?.DisplayName ?? "Terrain") : grid.DisplayName;
                    Log.Info($"[GridDefender] 🚀 Missile Hit ALLOWED! Missile '{missileName}' struck '{targetName}' at {impactSpeed:F1} m/s.");
                }
                return AllowOrScale(grid.EntityId, ref separatingVelocity, isMissile: true);
            }

            // 7. Non-Missile Collisions (Ships, Rovers, Stations, Voxels)
            // A. Static Station Protection
            if (grid.IsStatic && _config.ProtectStaticGrids)
            {
                if (_config.EnableDebugLogging)
                {
                    Log.Debug($"[GridDefender] Blocked non-missile collision on static station '{grid.DisplayName}'.");
                }
                if (otherEntity is MyCubeGrid otherCubeGrid && !otherCubeGrid.IsStatic && otherCubeGrid.Physics != null)
                {
                    ApplyImpactDamping(otherCubeGrid.Physics as MyGridPhysics, false);
                    ApplyAntiClang(otherCubeGrid, otherCubeGrid.Physics as MyGridPhysics, grid);
                }
                _stats.IncrementBlocked(isRamming: otherEntity is MyCubeGrid, isVoxel: otherEntity is MyVoxelBase);
                return false;
            }

            // B. Ship vs Voxel (Asteroid/Planet/Terrain driving) Protection
            if (otherEntity is MyVoxelBase)
            {
                if (_config.ProtectShipsAgainstVoxels)
                {
                    if (_config.EnableDebugLogging)
                    {
                        Log.Debug($"[GridDefender] Blocked voxel terrain collision on ship '{grid.DisplayName}' ({grid.BlocksCount} blocks at {impactSpeed:F1} m/s).");
                    }
                    ApplyImpactDamping(physics, grid.IsStatic);
                    ApplyAntiClang(grid, physics, otherEntity);
                    _stats.IncrementBlocked(isVoxel: true);
                    return false;
                }
                else
                {
                    if (_config.EnableDebugLogging)
                    {
                        Log.Debug($"[GridDefender] Voxel protection disabled: Allowed terrain collision deformation on ship '{grid.DisplayName}'.");
                    }
                    return AllowOrScale(grid.EntityId, ref separatingVelocity, isMissile: false);
                }
            }

            // C. Ship vs Ship Ramming Protection
            if (otherEntity is MyCubeGrid && _config.ProtectShipsAgainstRamming)
            {
                if (_config.EnableDebugLogging)
                {
                    Log.Debug($"[GridDefender] Blocked ship-on-ship ramming between '{grid.DisplayName}' ({grid.BlocksCount} blocks) and '{otherEntity.DisplayName}' at {impactSpeed:F1} m/s.");
                }
                ApplyImpactDamping(physics, grid.IsStatic);
                ApplyAntiClang(grid, physics, otherEntity);
                _stats.IncrementBlocked(isRamming: true);
                return false;
            }

            // 8. Rate Limiting / Cooldown for any remaining allowed deformations
            ulong currentFrame = MySandboxGame.Static?.SimulationFrameCounter ?? 0;
            if (_config.DeformationCooldownFrames > 0 && currentFrame > 0)
            {
                if (_lastDeformationFrames.TryGetValue(grid.EntityId, out ulong lastFrame))
                {
                    if (currentFrame >= lastFrame && (currentFrame - lastFrame) < (ulong)_config.DeformationCooldownFrames)
                    {
                        if (_config.EnableDebugLogging)
                        {
                            Log.Debug($"[GridDefender] Cooldown throttled deformation on '{grid.DisplayName}'.");
                        }
                        _stats.IncrementBlocked(isCooldown: true);
                        return false;
                    }
                }
            }

            return AllowOrScale(grid.EntityId, ref separatingVelocity, isMissile: false);
        }

        private void ApplyImpactDamping(MyGridPhysics physics, bool isStatic)
        {
            if (isStatic || physics == null || !_config.EnableAntiClang || _config.ImpactVelocityDamping <= 0.0f)
            {
                return;
            }

            try
            {
                float factor = Math.Max(0.0f, 1.0f - (_config.ImpactVelocityDamping * 0.6f));
                physics.LinearVelocity *= factor;
            }
            catch
            {
                // Safety guard
            }
        }

        private void ApplyAntiClang(MyCubeGrid grid, MyGridPhysics physics, MyEntity otherEntity)
        {
            if (grid == null || physics == null || !_config.EnableAntiClang)
            {
                return;
            }

            long gridEntityId = grid.EntityId;
            ulong currentFrame = MySandboxGame.Static?.SimulationFrameCounter ?? 0;
            if (currentFrame == 0) return;

            int contactCount = 1;
            if (_lastContactFrameTracker.TryGetValue(gridEntityId, out ulong lastFrame))
            {
                if (currentFrame == lastFrame + 1 || currentFrame == lastFrame)
                {
                    contactCount = _consecutiveContactFrames.AddOrUpdate(gridEntityId, 1, (k, v) => v + 1);
                }
                else
                {
                    _consecutiveContactFrames[gridEntityId] = 1;
                }
            }
            _lastContactFrameTracker[gridEntityId] = currentFrame;

            // Phase 1: Vibration & Torque Arrest (Early Clang threshold)
            if (contactCount >= _config.AntiClangVibrationThreshold)
            {
                try
                {
                    physics.LinearVelocity *= 0.75f;
                    physics.AngularVelocity *= 0.2f;

                    if (_config.StopClangSpinning && physics.AngularVelocity.LengthSquared() > 16.0f)
                    {
                        physics.AngularVelocity = Vector3.Zero;
                    }

                    _stats.IncrementClangArrested();

                    if (_config.EnableDebugLogging && contactCount == _config.AntiClangVibrationThreshold)
                    {
                        Log.Warn($"[GridDefender] ⚡ Anti-Clang Activated for '{grid.DisplayName}' ({contactCount} contact frames).");
                    }
                }
                catch
                {
                    // Safety guard
                }
            }

            // Phase 2: Active Push-Apart / Separation Nudge (Persistent Sticking/Phasing threshold)
            // CRITICAL: Never push-apart grids that are mechanically or logically connected (rotors/pistons/hinges/connectors) 
            // as coordinate translation will fight Havok joint/connector constraints and create phantom forces.
            bool areConnectedSubgrids = otherEntity is MyCubeGrid otherGrid && 
                (GridUtils.AreInSameMechanicalGroup(grid, otherGrid) || GridUtils.AreInSameLogicalGroup(grid, otherGrid));
                
            if (!areConnectedSubgrids && _config.EnablePushApart && !grid.IsStatic && contactCount >= _config.PushApartThreshold)
            {
                TryPushApart(grid, otherEntity);
                _consecutiveContactFrames[gridEntityId] = 0; // Reset counter after push
            }
        }

        private void TryPushApart(MyCubeGrid grid, MyEntity otherEntity)
        {
            if (grid == null || otherEntity == null) return;

            try
            {
                Vector3D gridPos = grid.PositionComp.GetPosition();
                Vector3D separationDir;

                if (otherEntity is MyCubeGrid otherGrid)
                {
                    separationDir = gridPos - otherGrid.PositionComp.GetPosition();
                    if (separationDir.LengthSquared() < 0.01)
                    {
                        separationDir = Vector3D.Up;
                    }
                    else
                    {
                        separationDir.Normalize();
                    }
                }
                else if (otherEntity is MyVoxelBase voxel)
                {
                    // If planet with gravity, push upwards towards sky; otherwise push away from voxel center
                    if (grid.Physics != null && grid.Physics.Gravity.LengthSquared() > 0.1f)
                    {
                        separationDir = -Vector3D.Normalize(grid.Physics.Gravity);
                    }
                    else
                    {
                        separationDir = gridPos - voxel.PositionComp.GetPosition();
                        if (separationDir.LengthSquared() < 0.01)
                            separationDir = Vector3D.Up;
                        else
                            separationDir.Normalize();
                    }
                }
                else
                {
                    separationDir = Vector3D.Up;
                }

                float distance = _config.PushApartDistance;

                // Schedule thread-safe position adjustment and gentle separation impulse on next sim tick
                TorchBase.Instance?.Invoke(() =>
                {
                    if (grid.MarkedForClose || grid.Closed) return;

                    var matrix = grid.WorldMatrix;
                    matrix.Translation += separationDir * distance;
                    grid.PositionComp.SetWorldMatrix(ref matrix);

                    if (grid.Physics != null)
                    {
                        grid.Physics.LinearVelocity = (Vector3)separationDir * 0.8f;
                        grid.Physics.AngularVelocity = Vector3.Zero;
                    }
                });

                _stats.IncrementGridsSeparated();

                if (_config.EnableDebugLogging)
                {
                    Log.Info($"[GridDefender] 🧲 Separated clanging grid '{grid.DisplayName}' away from '{otherEntity.DisplayName}' by {distance:F2}m.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Error while executing push-apart separation!");
            }
        }

        private bool AllowOrScale(long gridEntityId, ref float separatingVelocity, bool isMissile)
        {
            if (_config.DeformationMultiplier <= 0.0f)
            {
                _stats.IncrementBlocked();
                return false;
            }

            if (_config.DeformationMultiplier < 1.0f)
            {
                separatingVelocity *= _config.DeformationMultiplier;
            }

            ulong currentFrame = MySandboxGame.Static?.SimulationFrameCounter ?? 0;
            _lastDeformationFrames[gridEntityId] = currentFrame;

            // Trim stale entries
            if (_lastDeformationFrames.Count > 1000)
            {
                TrimOldFrames(currentFrame);
            }

            _stats.IncrementAllowed(isMissile: isMissile);
            return true;
        }

        private void TrimOldFrames(ulong currentFrame)
        {
            try
            {
                foreach (var kvp in _lastDeformationFrames)
                {
                    if (currentFrame > kvp.Value && (currentFrame - kvp.Value) > 600) // 10 seconds
                    {
                        _lastDeformationFrames.TryRemove(kvp.Key, out _);
                        _consecutiveContactFrames.TryRemove(kvp.Key, out _);
                        _lastContactFrameTracker.TryRemove(kvp.Key, out _);
                    }
                }
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}

