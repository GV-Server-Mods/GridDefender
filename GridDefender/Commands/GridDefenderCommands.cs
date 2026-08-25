using System;
using System.Globalization;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace GVK.GridDefender.Commands
{
    [Category("defender")]
    public class GridDefenderCommands : CommandModule
    {
        private GridDefenderPlugin Plugin => GridDefenderPlugin.Instance;

        [Command("rules", "Explains current missile vs ship protection rules.")]
        [Permission(MyPromoteLevel.None)]
        public void Rules()
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("GridDefender is not initialized.");
                return;
            }

            var cfg = Plugin.Config;
            var sb = new StringBuilder();
            sb.AppendLine("=== Grid Defender Rules ===");
            sb.AppendLine($"Status: {(cfg.Enabled ? "ENABLED" : "DISABLED")}");
            sb.AppendLine();
            sb.AppendLine("🚀 MISSILES (Damage Allowed):");
            sb.AppendLine($"• Large Grid Missiles: {cfg.LargeGridMissileMinBlocks} to {cfg.LargeGridMissileMaxBlocks} blocks @ >={cfg.MissileMinVelocity:F1} m/s");
            sb.AppendLine($"• Small Grid Missiles: {cfg.SmallGridMissileMinBlocks} to {cfg.SmallGridMissileMaxBlocks} blocks @ >={cfg.MissileMinVelocity:F1} m/s");
            sb.AppendLine();
            sb.AppendLine("🛡️ SHIPS & ROVERS (Damage Blocked / Protected):");
            sb.AppendLine($"• Ships > {cfg.LargeGridMissileMaxBlocks} Large / > {cfg.SmallGridMissileMaxBlocks} Small blocks are protected against crash & ramming damage.");
            sb.AppendLine($"• Safe Docking: All collisions < {cfg.MinDrivingVelocity:F1} m/s are protected.");
            sb.AppendLine($"• Anti-Ramming: Ship-on-Ship ramming is {(cfg.ProtectShipsAgainstRamming ? "PROTECTED" : "Allowed")}.");
            sb.AppendLine($"• Terrain Crashes: Voxel collision damage is {(cfg.ProtectShipsAgainstVoxels ? "PROTECTED" : "Allowed")}.");
            sb.AppendLine($"• Static Grids (Stations): Station damage is {(cfg.ProtectStaticGrids ? "PROTECTED" : "Allowed")}.");
            sb.AppendLine($"• Subgrids (Rotors/Pistons): Mechanical self-damage is {(cfg.ProtectSubgrids ? "PROTECTED" : "Allowed")}.");

            Context.Respond(sb.ToString());
        }

        [Command("check", "Tests if a grid size and speed count as a missile or a protected ship. Usage: !defender check <large/small> <blockCount> <speed>")]
        [Permission(MyPromoteLevel.None)]
        public void Check(string size, int blocks, float speed)
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("GridDefender is not initialized.");
                return;
            }

            var cfg = Plugin.Config;
            bool isLarge = size.StartsWith("l", StringComparison.OrdinalIgnoreCase);
            bool isSmall = size.StartsWith("s", StringComparison.OrdinalIgnoreCase);

            if (!isLarge && !isSmall)
            {
                Context.Respond("Invalid grid size. Use 'large' or 'small'. Example: !defender check small 25 80");
                return;
            }

            if (speed < cfg.MinDrivingVelocity)
            {
                Context.Respond($"[GridDefender] Result: 🛡️ PROTECTED (Safe Docking). Speed {speed:F1} m/s is below {cfg.MinDrivingVelocity:F1} m/s min driving speed.");
                return;
            }

            bool isMissile = false;
            if (isLarge)
            {
                isMissile = blocks >= cfg.LargeGridMissileMinBlocks && blocks <= cfg.LargeGridMissileMaxBlocks && speed >= cfg.MissileMinVelocity;
            }
            else
            {
                isMissile = blocks >= cfg.SmallGridMissileMinBlocks && blocks <= cfg.SmallGridMissileMaxBlocks && speed >= cfg.MissileMinVelocity;
            }

            if (isMissile)
            {
                Context.Respond($"[GridDefender] Result: 🚀 MISSILE (Damage Allowed!). {blocks} blocks @ {speed:F1} m/s meets missile criteria and will deal impact damage.");
            }
            else
            {
                Context.Respond($"[GridDefender] Result: 🛡️ SHIP (Protected from Crash/Ramming). {blocks} blocks does not match missile criteria, so ramming/crash damage is blocked.");
            }
        }

        [Command("status", "Shows the current GridDefender status and configuration summary.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Status()
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("GridDefender plugin is not initialized.");
                return;
            }

            var cfg = Plugin.Config;
            var sb = new StringBuilder();
            sb.AppendLine("=== Grid Defender Status ===");
            sb.AppendLine($"Enabled: {cfg.Enabled}");
            sb.AppendLine($"Allow Missile Damage: {cfg.AllowMissileDamage}");
            sb.AppendLine($"Large Missile Blocks: {cfg.LargeGridMissileMinBlocks} - {cfg.LargeGridMissileMaxBlocks}");
            sb.AppendLine($"Small Missile Blocks: {cfg.SmallGridMissileMinBlocks} - {cfg.SmallGridMissileMaxBlocks}");
            sb.AppendLine($"Missile Min Velocity: {cfg.MissileMinVelocity:F1} m/s");
            sb.AppendLine($"Protect Ramming: {cfg.ProtectShipsAgainstRamming} | Protect Voxels: {cfg.ProtectShipsAgainstVoxels}");
            sb.AppendLine($"Protect Stations: {cfg.ProtectStaticGrids} | Protect Subgrids: {cfg.ProtectSubgrids}");
            sb.AppendLine($"Protect Floating Debris: {cfg.ProtectAgainstFloatingObjects}");
            sb.AppendLine($"Min Driving Speed (Safe Docking): {cfg.MinDrivingVelocity:F1} m/s");
            sb.AppendLine($"Max Velocity Limit: {cfg.MaxDeformationVelocity:F1} m/s");
            sb.AppendLine($"Anti-Clang: {cfg.EnableAntiClang} (Damping: {cfg.ImpactVelocityDamping:F2}, Threshold: {cfg.AntiClangVibrationThreshold} frames)");
            sb.AppendLine($"Push-Apart: {cfg.EnablePushApart} (Distance: {cfg.PushApartDistance:F2}m, Threshold: {cfg.PushApartThreshold} frames)");
            sb.AppendLine($"Wheel Broadphase Optimizer: {cfg.EnableWheelOptimization}");

            Context.Respond(sb.ToString());
        }

        [Command("stats", "Displays real-time collision and missile statistics.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Stats()
        {
            if (Plugin?.Statistics == null)
            {
                Context.Respond("GridDefender statistics are not available.");
                return;
            }

            var stats = Plugin.Statistics;
            var sb = new StringBuilder();
            sb.AppendLine("=== Grid Defender Telemetry ===");
            sb.AppendLine($"Total Evaluated: {stats.TotalEvaluated:N0}");
            sb.AppendLine($"Crashes Blocked: {stats.TotalBlocked:N0} ({stats.BlockRatio:F1}%)");
            sb.AppendLine($"🚀 Missile Impacts Allowed: {stats.MissileHitsAllowed:N0}");
            sb.AppendLine($"⚡ Clang Vibrations Arrested: {stats.ClangVibrationsArrested:N0}");
            sb.AppendLine($"🧲 Grids Separated (Push-Apart): {stats.GridsSeparated:N0}");
            sb.AppendLine($"🛡️ Ship Ramming Blocked: {stats.RammingBlocked:N0}");
            sb.AppendLine($"🏔️ Voxel Crashes Blocked: {stats.VoxelCrashesBlocked:N0}");
            sb.AppendLine($"⚙️ Subgrid Collisions Blocked: {stats.SubgridCollisionsBlocked:N0}");
            sb.AppendLine($"⏱️ Cooldown Throttled: {stats.CooldownThrottled:N0}");

            Context.Respond(sb.ToString());
        }

        [Command("resetstats", "Resets the defense statistics counters.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ResetStats()
        {
            Plugin?.Statistics?.Reset();
            Context.Respond("GridDefender statistics have been reset to zero.");
        }

        [Command("toggle", "Toggles the GridDefender plugin on or off.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Toggle()
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("GridDefender is not initialized.");
                return;
            }

            Plugin.Config.Enabled = !Plugin.Config.Enabled;
            Plugin.SaveConfig();
            Context.Respond($"GridDefender is now {(Plugin.Config.Enabled ? "ENABLED" : "DISABLED")}.");
        }

        [Command("togglevoxels", "Toggles protection against voxel and asteroid collision damage.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ToggleVoxels()
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("GridDefender is not initialized.");
                return;
            }

            Plugin.Config.ProtectShipsAgainstVoxels = !Plugin.Config.ProtectShipsAgainstVoxels;
            Plugin.SaveConfig();
            Context.Respond($"Voxel Collision Protection is now {(Plugin.Config.ProtectShipsAgainstVoxels ? "ENABLED (Ships will NOT deform on voxels/asteroids)" : "DISABLED (Ships WILL take collision damage on voxels/asteroids)")}.");
        }

        [Command("reload", "Reloads the configuration from file.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            if (Plugin == null)
            {
                Context.Respond("GridDefender is not initialized.");
                return;
            }

            Plugin.LoadConfig();
            Context.Respond("GridDefender configuration reloaded from disk.");
        }

        [Command("set", "Changes a configuration setting on the fly. Usage: !defender set <property> <value>")]
        [Permission(MyPromoteLevel.Admin)]
        public void Set(string property, string value)
        {
            if (Plugin?.Config == null)
            {
                Context.Respond("GridDefender is not initialized.");
                return;
            }

            var cfg = Plugin.Config;
            try
            {
                switch (property.ToLowerInvariant())
                {
                    case "enabled":
                        cfg.Enabled = bool.Parse(value);
                        break;
                    case "debug":
                    case "enabledebuglogging":
                        cfg.EnableDebugLogging = bool.Parse(value);
                        break;
                    case "multiplier":
                    case "deformationmultiplier":
                        cfg.DeformationMultiplier = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "allowmissiles":
                    case "allowmissiledamage":
                        cfg.AllowMissileDamage = bool.Parse(value);
                        break;
                    case "largemin":
                    case "largemissilemin":
                        cfg.LargeGridMissileMinBlocks = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "largemax":
                    case "largemissilemax":
                        cfg.LargeGridMissileMaxBlocks = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "smallmin":
                    case "smallmissilemin":
                        cfg.SmallGridMissileMinBlocks = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "smallmax":
                    case "smallmissilemax":
                        cfg.SmallGridMissileMaxBlocks = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "missilespeed":
                    case "missileminvelocity":
                        cfg.MissileMinVelocity = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "protectramming":
                    case "ramming":
                        cfg.ProtectShipsAgainstRamming = bool.Parse(value);
                        break;
                    case "protectvoxels":
                    case "voxels":
                        cfg.ProtectShipsAgainstVoxels = bool.Parse(value);
                        break;
                    case "protectstations":
                    case "protectstaticgrids":
                    case "stations":
                        cfg.ProtectStaticGrids = bool.Parse(value);
                        break;
                    case "protectsubgrids":
                    case "subgrids":
                        cfg.ProtectSubgrids = bool.Parse(value);
                        break;
                    case "protectfloating":
                    case "protectagainstfloatingobjects":
                    case "floating":
                        cfg.ProtectAgainstFloatingObjects = bool.Parse(value);
                        break;
                    case "mindriving":
                    case "mindrivingspeed":
                        cfg.MinDrivingVelocity = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "maxvelocity":
                        cfg.MaxDeformationVelocity = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "cooldown":
                    case "deformationcooldownframes":
                        cfg.DeformationCooldownFrames = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "anticlang":
                    case "enableanticlang":
                        cfg.EnableAntiClang = bool.Parse(value);
                        break;
                    case "damping":
                    case "impactdamping":
                        cfg.ImpactVelocityDamping = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "vibrationthreshold":
                    case "anticlangthreshold":
                        cfg.AntiClangVibrationThreshold = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "stopspin":
                    case "stopclangspinning":
                        cfg.StopClangSpinning = bool.Parse(value);
                        break;
                    case "pushapart":
                    case "enablepushapart":
                        cfg.EnablePushApart = bool.Parse(value);
                        break;
                    case "pushdistance":
                    case "pushapartdistance":
                        cfg.PushApartDistance = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "pushthreshold":
                    case "pushapartthreshold":
                        cfg.PushApartThreshold = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "wheelopt":
                    case "wheeloptimization":
                    case "enablewheeloptimization":
                        cfg.EnableWheelOptimization = bool.Parse(value);
                        break;
                    default:
                        Context.Respond($"Unknown setting '{property}'. Valid options: enabled, debug, multiplier, allowmissiles, largemin, largemax, smallmin, smallmax, missilespeed, protectramming, protectvoxels, protectstations, protectsubgrids, protectfloating, mindriving, maxvelocity, cooldown, anticlang, damping, vibrationthreshold, stopspin, pushapart, pushdistance, pushthreshold, wheelopt.");
                        return;
                }

                Plugin.SaveConfig();
                Context.Respond($"GridDefender: Set '{property}' to '{value}'. Configuration saved.");
            }
            catch (Exception ex)
            {
                Context.Respond($"Error setting '{property}': {ex.Message}");
            }
        }
    }
}

