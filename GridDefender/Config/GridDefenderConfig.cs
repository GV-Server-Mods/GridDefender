using System;
using System.Xml.Serialization;
using Torch;
using Torch.Views;

namespace GVK.GridDefender.Config
{
    /// <summary>
    /// Configuration model for GVK GridDefender, containing all missile gates,
    /// crash protection toggles, anti-clang settings, and separation parameters.
    /// </summary>
    public class GridDefenderConfig : ViewModel
    {
        private bool _enabled = true;
        private bool _enableDebugLogging = false;
        private float _deformationMultiplier = 1.0f;

        // --- Missile (PMW) Allowance Settings ---
        private bool _allowMissileDamage = true;
        private int _largeGridMissileMinBlocks = 3;
        private int _largeGridMissileMaxBlocks = 50;
        private int _smallGridMissileMinBlocks = 4;
        private int _smallGridMissileMaxBlocks = 150;
        private float _missileMinVelocity = 20.0f; // m/s

        // --- Ship & Rover Driving / Ramming Protection ---
        private bool _protectShipsAgainstRamming = true;
        private bool _protectShipsAgainstVoxels = true;
        private bool _protectAgainstFloatingObjects = true;
        private bool _protectStaticGrids = true;
        private bool _protectSubgrids = true;

        // --- Voxel & Terrain Protection ---
        private bool _suppressAllVoxelExplosionDamage = true;

        // --- Anti-Clang & Phasing Prevention ---
        private bool _enableAntiClang = true;
        private float _impactVelocityDamping = 0.5f; // 0.0 = no damping, 1.0 = full stop on impact
        private int _antiClangVibrationThreshold = 8; // consecutive contact frames before aggressive damping
        private bool _stopClangSpinning = true; // stops extreme angular velocity spins

        // --- Active Separation / Push-Apart ---
        private bool _enablePushApart = true;
        private int _pushApartThreshold = 25; // consecutive frames of grinding before nudging apart (~0.4s)
        private float _pushApartDistance = 0.5f; // meters to nudge grids apart

        // --- Speed Gates & Rate Limits ---
        private float _minDrivingVelocity = 10.0f; // m/s (docking/parking protection)
        private float _maxDeformationVelocity = 110.0f; // m/s (anti-lag crash protection)
        private int _deformationCooldownFrames = 30;

        [Display(Order = 1, Name = "Enable Plugin", GroupName = "General", Description = "Master toggle for Grid Defender collision & impact damage management.")]
        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }

        [Display(Order = 2, Name = "Enable Debug Logging", GroupName = "General", Description = "Log missile hits, anti-clang events, and push-apart actions to the Torch console.")]
        public bool EnableDebugLogging
        {
            get => _enableDebugLogging;
            set => SetValue(ref _enableDebugLogging, value);
        }

        [Display(Order = 3, Name = "Allowed Impact Damage Multiplier", GroupName = "General", Description = "Scale factor when collision impact damage is allowed for missiles (1.0 = normal vanilla damage, 0.5 = 50% damage, 0.0 = zero damage).")]
        public float DeformationMultiplier
        {
            get => _deformationMultiplier;
            set => SetValue(ref _deformationMultiplier, Math.Max(0.0f, Math.Min(1.0f, value)));
        }

        // --- Missile Settings ---
        [Display(Order = 4, Name = "Allow Missile (PMW) Damage", GroupName = "Missile (PMW) Settings", Description = "Allow player-made missiles and kinetic torpedoes to deal collision impact damage.")]
        public bool AllowMissileDamage
        {
            get => _allowMissileDamage;
            set => SetValue(ref _allowMissileDamage, value);
        }

        [Display(Order = 5, Name = "Large Missile Min Blocks", GroupName = "Missile (PMW) Settings", Description = "Minimum block count for a large grid missile (filters out single loose blocks/debris). Default: 3.")]
        public int LargeGridMissileMinBlocks
        {
            get => _largeGridMissileMinBlocks;
            set
            {
                SetValue(ref _largeGridMissileMinBlocks, Math.Max(1, value));
                if (_largeGridMissileMaxBlocks < _largeGridMissileMinBlocks)
                {
                    LargeGridMissileMaxBlocks = _largeGridMissileMinBlocks;
                }
            }
        }

        [Display(Order = 6, Name = "Large Missile Max Blocks", GroupName = "Missile (PMW) Settings", Description = "Maximum block count for a large grid missile/torpedo. Grids up to this size deal missile damage. Default: 50.")]
        public int LargeGridMissileMaxBlocks
        {
            get => _largeGridMissileMaxBlocks;
            set => SetValue(ref _largeGridMissileMaxBlocks, Math.Max(_largeGridMissileMinBlocks, value));
        }

        [Display(Order = 7, Name = "Small Missile Min Blocks", GroupName = "Missile (PMW) Settings", Description = "Minimum block count for a small grid missile. Default: 4.")]
        public int SmallGridMissileMinBlocks
        {
            get => _smallGridMissileMinBlocks;
            set
            {
                SetValue(ref _smallGridMissileMinBlocks, Math.Max(1, value));
                if (_smallGridMissileMaxBlocks < _smallGridMissileMinBlocks)
                {
                    SmallGridMissileMaxBlocks = _smallGridMissileMinBlocks;
                }
            }
        }

        [Display(Order = 8, Name = "Small Missile Max Blocks", GroupName = "Missile (PMW) Settings", Description = "Maximum block count for a small grid missile. Grids up to this size deal missile damage. Default: 150.")]
        public int SmallGridMissileMaxBlocks
        {
            get => _smallGridMissileMaxBlocks;
            set => SetValue(ref _smallGridMissileMaxBlocks, Math.Max(_smallGridMissileMinBlocks, value));
        }

        [Display(Order = 9, Name = "Missile Min Velocity (m/s)", GroupName = "Missile (PMW) Settings", Description = "Missiles must be moving at or above this speed to deal impact deformation damage. Default: 20 m/s.")]
        public float MissileMinVelocity
        {
            get => _missileMinVelocity;
            set => SetValue(ref _missileMinVelocity, Math.Max(0.0f, value));
        }

        // --- Ship & Rover Protection Settings ---
        [Display(Order = 10, Name = "Protect Ships From Ramming", GroupName = "Ship & Rover Protection", Description = "Block collision impact damage when full-sized ships collide or ram each other.")]
        public bool ProtectShipsAgainstRamming
        {
            get => _protectShipsAgainstRamming;
            set
            {
                SetValue(ref _protectShipsAgainstRamming, value);
                OnPropertyChanged(nameof(IsSpeedGatesApplicable));
            }
        }

        [Display(Order = 11, Name = "Protect Ships From Voxels", GroupName = "Ship & Rover Protection", Description = "Block collision impact damage when ships and rovers bump planets, asteroids, or terrain.")]
        public bool ProtectShipsAgainstVoxels
        {
            get => _protectShipsAgainstVoxels;
            set
            {
                SetValue(ref _protectShipsAgainstVoxels, value);
                OnPropertyChanged(nameof(IsSpeedGatesApplicable));
            }
        }

        [Display(Order = 12, Name = "Protect Floating Objects / Debris", GroupName = "Ship & Rover Protection", Description = "Suppress impact damage from floating ores, dropped items, and detached debris.")]
        public bool ProtectAgainstFloatingObjects
        {
            get => _protectAgainstFloatingObjects;
            set => SetValue(ref _protectAgainstFloatingObjects, value);
        }

        [Display(Order = 13, Name = "Protect Static Grids (Stations)", GroupName = "Ship & Rover Protection", Description = "Suppress collision impact damage on stations when rammed by ships.")]
        public bool ProtectStaticGrids
        {
            get => _protectStaticGrids;
            set
            {
                SetValue(ref _protectStaticGrids, value);
                OnPropertyChanged(nameof(IsSpeedGatesApplicable));
            }
        }

        [Display(Order = 14, Name = "Protect Subgrids (Rotors/Pistons)", GroupName = "Ship & Rover Protection", Description = "Suppress self-collision impact damage between connected subgrids to prevent Clang explosions.")]
        public bool ProtectSubgrids
        {
            get => _protectSubgrids;
            set => SetValue(ref _protectSubgrids, value);
        }

        // --- Voxel & Terrain Protection ---
        [Display(Order = 15, Name = "Suppress Explosion Voxel Cutouts", GroupName = "Voxel & Terrain Protection", Description = "Blocks voxel crater cutouts from explosions, warheads, weapons, and crashes while leaving drill mining 100% operational.")]
        public bool SuppressAllVoxelExplosionDamage
        {
            get => _suppressAllVoxelExplosionDamage;
            set => SetValue(ref _suppressAllVoxelExplosionDamage, value);
        }

        // --- Anti-Clang & Phasing Prevention ---
        [Display(Order = 16, Name = "Enable Anti-Clang System", GroupName = "Anti-Clang & Phasing Prevention", Description = "Arrests physics vibration, continuous grinding loops, and rubberbanding when non-damaging grids collide.")]
        public bool EnableAntiClang
        {
            get => _enableAntiClang;
            set => SetValue(ref _enableAntiClang, value);
        }

        [Display(Order = 17, Name = "Impact Velocity Damping", GroupName = "Anti-Clang & Phasing Prevention", Description = "Bleeds off kinetic energy on protected collisions (0.0 = bounce freely, 0.5 = 50% speed absorption, 1.0 = full stop). Prevents phasing into terrain.")]
        public float ImpactVelocityDamping
        {
            get => _impactVelocityDamping;
            set => SetValue(ref _impactVelocityDamping, Math.Max(0.0f, Math.Min(1.0f, value)));
        }

        [Display(Order = 18, Name = "Continuous Contact Threshold (Frames)", GroupName = "Anti-Clang & Phasing Prevention", Description = "Number of consecutive contact frames before forcefully damping physics oscillations and stopping Clang.")]
        public int AntiClangVibrationThreshold
        {
            get => _antiClangVibrationThreshold;
            set => SetValue(ref _antiClangVibrationThreshold, Math.Max(1, value));
        }

        [Display(Order = 19, Name = "Stop Clang Spinning (Death Spin)", GroupName = "Anti-Clang & Phasing Prevention", Description = "Instantly zeros out extreme rotational spin caused by Havok physics overlap glitches.")]
        public bool StopClangSpinning
        {
            get => _stopClangSpinning;
            set => SetValue(ref _stopClangSpinning, value);
        }

        // --- Active Separation / Push-Apart ---
        [Display(Order = 20, Name = "Enable Push-Apart (Anti-Stuck)", GroupName = "Active Separation / Push-Apart", Description = "Automatically nudges grids apart if they get stuck or persistently phase inside terrain or each other.")]
        public bool EnablePushApart
        {
            get => _enablePushApart;
            set => SetValue(ref _enablePushApart, value);
        }

        [Display(Order = 21, Name = "Push-Apart Threshold (Frames)", GroupName = "Active Separation / Push-Apart", Description = "Consecutive contact frames before executing a push-apart separation (~25 frames = 0.4s).")]
        public int PushApartThreshold
        {
            get => _pushApartThreshold;
            set => SetValue(ref _pushApartThreshold, Math.Max(5, value));
        }

        [Display(Order = 22, Name = "Push-Apart Distance (Meters)", GroupName = "Active Separation / Push-Apart", Description = "Distance in meters to gently nudge the grid away from the collision surface (0.2 to 2.0m).")]
        public float PushApartDistance
        {
            get => _pushApartDistance;
            set => SetValue(ref _pushApartDistance, Math.Max(0.1f, Math.Min(5.0f, value)));
        }

        // --- Speed Gates & Rate Limits (Fallback Safety Net) ---
        [Display(Order = 23, Name = "Min Driving Velocity (m/s)", GroupName = "Speed Gates & Rate Limits (Fallback)", Description = "Fallback: Collisions below this speed never cause impact damage (safe docking/parking). Active only if Ramming or Voxel protection is disabled.")]
        public float MinDrivingVelocity
        {
            get => _minDrivingVelocity;
            set => SetValue(ref _minDrivingVelocity, Math.Max(0.0f, value));
        }

        [Display(Order = 24, Name = "Max Velocity Threshold (m/s)", GroupName = "Speed Gates & Rate Limits (Fallback)", Description = "Fallback: Non-missile collisions above this speed (e.g. 110 m/s) skip damage to prevent lag freezes. Active only if Ramming or Voxel protection is disabled.")]
        public float MaxDeformationVelocity
        {
            get => _maxDeformationVelocity;
            set => SetValue(ref _maxDeformationVelocity, Math.Max(0.0f, value));
        }

        [Display(Order = 25, Name = "Impact Damage Cooldown (Frames)", GroupName = "Speed Gates & Rate Limits (Fallback)", Description = "Fallback: Minimum simulation frames (60 = 1s) between damage evaluations per grid during grinding. Active only if Ramming or Voxel protection is disabled.")]
        public int DeformationCooldownFrames
        {
            get => _deformationCooldownFrames;
            set => SetValue(ref _deformationCooldownFrames, Math.Max(0, value));
        }

        /// <summary>
        /// True if fallback speed gates and rate limits are actively applicable (i.e. at least one ship, voxel, or station protection is disabled).
        /// When full ship protection is enabled, all non-missile collisions are already 100% blocked, making these fallback thresholds dormant.
        /// </summary>
        [XmlIgnore]
        public bool IsSpeedGatesApplicable => !ProtectShipsAgainstRamming || !ProtectShipsAgainstVoxels || !ProtectStaticGrids;
    }
}

