using System;
using System.IO;
using System.Windows.Controls;
using GVK.GridDefender.Config;
using GVK.GridDefender.Engine;
using GVK.GridDefender.Patches;
using GVK.GridDefender.Services;
using GVK.GridDefender.Views;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.Managers.PatchManager;

namespace GVK.GridDefender
{
    public class GridDefenderPlugin : TorchPluginBase, IWpfPlugin
    {
        public static readonly ILogger Log = LogManager.GetLogger("GVK.GridDefender");

        private Persistent<GridDefenderConfig> _config;
        private GridDefenderControl _control;

        public static GridDefenderPlugin Instance { get; private set; }

        public GridDefenderConfig Config => _config?.Data;
        public DefenseStatistics Statistics { get; private set; }
        public DeformationDefenseEngine Engine { get; private set; }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;

            LoadConfig();
            Statistics = new DefenseStatistics();
            Engine = new DeformationDefenseEngine(Config, Statistics);

            RegisterPatches();

            Log.Info("[GridDefender] Plugin initialized successfully. Defense engine is active.");
        }

        private void RegisterPatches()
        {
            try
            {
                var patchManager = Torch.Managers.GetManager(typeof(PatchManager)) as PatchManager;
                if (patchManager != null)
                {
                    var ctx = patchManager.AcquireContext();
                    MyGridPhysicsPatch.Patch(ctx);
                    MotorSuspensionPatch.Patch(ctx);
                    patchManager.Commit();
                }
                else
                {
                    Log.Warn("[GridDefender] PatchManager not found. Falling back to HarmonyLib directly.");
                    var harmony = new HarmonyLib.Harmony("GVK.GridDefender");
                    harmony.PatchAll(typeof(GridDefenderPlugin).Assembly);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Error while applying patches!");
            }
        }

        public void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(StoragePath, "GridDefender.cfg");
                _config = Persistent<GridDefenderConfig>.Load(configPath);
                if (_config?.Data == null)
                {
                    _config = new Persistent<GridDefenderConfig>(configPath, new GridDefenderConfig());
                    _config.Save();
                }
                Engine?.UpdateConfig(Config);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Failed to load configuration file! Creating default config.");
                _config = new Persistent<GridDefenderConfig>(Path.Combine(StoragePath, "GridDefender.cfg"), new GridDefenderConfig());
                Engine?.UpdateConfig(Config);
            }
        }

        public void SaveConfig()
        {
            try
            {
                _config?.Save();
                Log.Info("[GridDefender] Configuration saved.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Failed to save configuration file!");
            }
        }

        public UserControl GetControl()
        {
            return _control ?? (_control = new GridDefenderControl(this));
        }

        public override void Dispose()
        {
            try
            {
                SaveConfig();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Error saving config during Dispose.");
            }

            base.Dispose();
            _control = null;
            Engine = null;
            Statistics = null;
            Instance = null;
        }
    }
}
