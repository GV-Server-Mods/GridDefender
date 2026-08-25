using System;
using System.Reflection;
using HarmonyLib;
using Havok;
using NLog;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;

namespace GVK.GridDefender.Patches
{
    [HarmonyPatch]
    public static class MotorSuspensionPatch
    {
        private static readonly ILogger Log = LogManager.GetLogger("GridDefender.WheelPatch");

        public static void Patch(PatchContext ctx)
        {
            try
            {
                var targetMethod = typeof(MyMotorSuspension).GetMethod("CreateConstraint", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (targetMethod != null)
                {
                    var postfixMethod = typeof(MotorSuspensionPatch).GetMethod(nameof(CreateConstraintPostfix), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    ctx.GetPattern(targetMethod).Suffixes.Add(postfixMethod);
                    Log.Info("[GridDefender] Successfully registered MyMotorSuspension.CreateConstraint patch with Torch PatchManager.");
                }

                var physicsChangedMethod = typeof(MyMotorSuspension).GetMethod("CubeGrid_OnPhysicsChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (physicsChangedMethod != null)
                {
                    var postfixMethod = typeof(MotorSuspensionPatch).GetMethod(nameof(PhysicsChangedPostfix), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    ctx.GetPattern(physicsChangedMethod).Suffixes.Add(postfixMethod);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Failed to patch MyMotorSuspension!");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyMotorSuspension), "CreateConstraint")]
        public static void CreateConstraintPostfix(MyMotorSuspension __instance, bool __result)
        {
            if (!__result || __instance == null) return;
            OptimizeWheelCollisionFilter(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MyMotorSuspension), "CubeGrid_OnPhysicsChanged")]
        public static void PhysicsChangedPostfix(MyMotorSuspension __instance)
        {
            if (__instance == null) return;
            OptimizeWheelCollisionFilter(__instance);
        }

        private static void OptimizeWheelCollisionFilter(MyMotorSuspension suspension)
        {
            try
            {
                var plugin = GridDefenderPlugin.Instance;
                if (plugin?.Config == null || !plugin.Config.EnableWheelOptimization)
                {
                    return;
                }

                var topGrid = suspension.TopGrid;
                var cubeGrid = suspension.CubeGrid;
                if (topGrid?.Physics?.RigidBody == null || cubeGrid?.Physics?.RigidBody == null)
                {
                    return;
                }

                var wheelBody = topGrid.Physics.RigidBody;
                var chassisBody = cubeGrid.Physics.RigidBody;
                int systemId = cubeGrid.Physics.HavokCollisionSystemID;

                // Symmetrical sub-system masking:
                // Keen's bug: Wheel was set to (subSystemId: 1, subSystemDontCollideWith: 1), but chassis is subSystemId: 0.
                // In Havok's bitmask filter, subSystemDontCollideWith = 3 (bits 0 and 1) tells Havok to ignore BOTH sub-system 0 (chassis) and 1 (wheel).
                // This eliminates the redundant AABB compound shape queries against wheel well armor blocks!
                uint wheelFilter = HkGroupFilter.CalcFilterInfo(wheelBody.Layer, systemId, 1, 3);
                wheelBody.SetCollisionFilterInfo(wheelFilter);

                MyPhysics.RefreshCollisionFilter(topGrid.Physics);
                MyPhysics.RefreshCollisionFilter(cubeGrid.Physics);

                if (plugin.Config.EnableDebugLogging)
                {
                    Log.Debug($"[GridDefender] Optimized wheel broadphase collision filter on '{cubeGrid.DisplayName}' / wheel '{topGrid.DisplayName}'.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Error optimizing wheel collision filter!");
            }
        }
    }
}
