using System;
using System.Reflection;
using HarmonyLib;
using NLog;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;
using VRage.Game.Entity;

namespace GVK.GridDefender.Patches
{
    [HarmonyPatch]
    public static class MyGridPhysicsPatch
    {
        private static readonly ILogger Log = LogManager.GetLogger("GridDefender.Patch");

        public static void Patch(PatchContext ctx)
        {
            try
            {
                var targetMethod = typeof(MyGridPhysics).GetMethod("PerformDeformation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (targetMethod == null)
                {
                    Log.Error("[GridDefender] Could not find MyGridPhysics.PerformDeformation method to patch!");
                    return;
                }

                var prefixMethod = typeof(MyGridPhysicsPatch).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                ctx.GetPattern(targetMethod).Prefixes.Add(prefixMethod);
                Log.Info("[GridDefender] Successfully registered MyGridPhysics.PerformDeformation patch with Torch PatchManager.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Failed to patch MyGridPhysics.PerformDeformation!");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MyGridPhysics), "PerformDeformation")]
        public static bool Prefix(MyGridPhysics __instance, MyEntity otherEntity, ref float separatingVelocity)
        {
            try
            {
                var plugin = GridDefenderPlugin.Instance;
                if (plugin?.Engine == null)
                {
                    return true;
                }

                return plugin.Engine.ShouldAllowDeformation(__instance, otherEntity, ref separatingVelocity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Error inside PerformDeformation prefix hook!");
                return true; // Failsafe: let original execute
            }
        }
    }
}

