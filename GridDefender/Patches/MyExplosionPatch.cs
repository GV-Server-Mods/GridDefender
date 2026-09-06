using System;
using System.Reflection;
using NLog;
using Sandbox.Game;
using Torch.Managers.PatchManager;

namespace GVK.GridDefender.Patches
{
    /// <summary>
    /// Torch PatchManager hook on internal Sandbox.Game.MyExplosion to suppress explosive voxel cutouts.
    /// </summary>
    public static class MyExplosionPatch
    {
        private static readonly ILogger Log = LogManager.GetLogger("GridDefender.Patch");

        /// <summary>
        /// Registers explosion voxel cutout suppression prefixes.
        /// </summary>
        /// <param name="ctx">Torch PatchManager context.</param>
        public static void Patch(PatchContext ctx)
        {
            try
            {
                var explosionType = typeof(MyExplosions).Assembly.GetType("Sandbox.Game.MyExplosion");
                if (explosionType == null)
                {
                    Log.Error("[GridDefender] Could not find Sandbox.Game.MyExplosion type to patch!");
                    return;
                }

                var applyVoxelMethod = explosionType.GetMethod("ApplyExplosionOnVoxel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (applyVoxelMethod != null)
                {
                    var prefixApply = typeof(MyExplosionPatch).GetMethod(nameof(PrefixApplyExplosionOnVoxel), BindingFlags.Static | BindingFlags.NonPublic);
                    ctx.GetPattern(applyVoxelMethod).Prefixes.Add(prefixApply);
                }

                var cutOutMethod = explosionType.GetMethod("CutOutVoxelMap", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (cutOutMethod != null)
                {
                    var prefixCutOut = typeof(MyExplosionPatch).GetMethod(nameof(PrefixCutOutVoxelMap), BindingFlags.Static | BindingFlags.NonPublic);
                    ctx.GetPattern(cutOutMethod).Prefixes.Add(prefixCutOut);
                }

                Log.Info("[GridDefender] Registered MyExplosion voxel cutout suppression patches.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Failed to patch MyExplosion voxel cutouts!");
            }
        }

        private static bool PrefixApplyExplosionOnVoxel()
        {
            var config = GridDefenderPlugin.Instance?.Config;
            if (config != null && config.Enabled && config.SuppressAllVoxelExplosionDamage)
            {
                return false; // Skip voxel carving; drills use MyDrillBase and remain unaffected
            }
            return true;
        }

        private static bool PrefixCutOutVoxelMap()
        {
            var config = GridDefenderPlugin.Instance?.Config;
            if (config != null && config.Enabled && config.SuppressAllVoxelExplosionDamage)
            {
                return false;
            }
            return true;
        }
    }
}

