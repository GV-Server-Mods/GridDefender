using System;
using System.IO;
using System.Threading;
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
    /// <summary>
    /// Main entry point and lifecycle manager for the GVK GridDefender Torch plugin.
    /// Manages configuration persistence, collision defense hooks, and WPF server UI integration.
    /// </summary>
    public class GridDefenderPlugin : TorchPluginBase, IWpfPlugin
    {
        public static readonly ILogger Log = LogManager.GetLogger("GVK.GridDefender");
        private readonly object _configSaveLock = new();
        private int _tickCount;

        private Persistent<GridDefenderConfig> _config;
        private GridDefenderControl _control;

        /// <summary>
        /// Singleton instance of the running plugin.
        /// </summary>
        public static GridDefenderPlugin Instance { get; private set; }

        /// <summary>
        /// Active runtime configuration model.
        /// </summary>
        public GridDefenderConfig Config => _config?.Data;

        /// <summary>
        /// Real-time collision telemetry and statistics service.
        /// </summary>
        public DefenseStatistics Statistics { get; private set; }

        /// <summary>
        /// Core collision evaluation and anti-clang engine.
        /// </summary>
        public DeformationDefenseEngine Engine { get; private set; }

        /// <summary>
        /// Initializes plugin systems, loads configuration, and registers Torch physics patches.
        /// </summary>
        /// <param name="torch">Torch base server instance.</param>
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
                if (Torch.Managers.GetManager(typeof(PatchManager)) is PatchManager patchManager)
                {
                    var ctx = patchManager.AcquireContext();
                    MyGridPhysicsPatch.Patch(ctx);
                    MyExplosionPatch.Patch(ctx);
                    patchManager.Commit();
                    Log.Info("[GridDefender] Torch patches registered successfully.");
                }
                else
                {
                    Log.Error("[GridDefender] Torch PatchManager not found! Unable to register physics patches.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GridDefender] Error while applying patches!");
            }
        }

        /// <summary>
        /// Loads configuration from disk or generates defaults if missing.
        /// </summary>
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

        /// <summary>
        /// Saves active runtime configuration to the persistent config file.
        /// When async is true, disk I/O is offloaded to the thread pool to protect sim-speed.
        /// </summary>
        public void SaveConfig(bool async = true)
        {
            if (async)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    lock (_configSaveLock)
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
                });
            }
            else
            {
                lock (_configSaveLock)
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
            }
        }

        /// <summary>
        /// Periodically executed on the Torch simulation loop.
        /// Sweeps expired cache entries on a cold stepped 1000-tick interval.
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (++_tickCount >= 1000)
            {
                _tickCount = 0;
                Engine?.SweepCaches();
            }
        }

        /// <summary>
        /// Provides the WPF UserControl tab for Torch GUI integration.
        /// </summary>
        public UserControl GetControl()
        {
            return _control ??= new GridDefenderControl(this);
        }

        /// <summary>
        /// Disposes plugin resources and saves pending configuration on server shutdown.
        /// </summary>
        public override void Dispose()
        {
            SaveConfig(async: false);
            Engine?.Dispose();
            DeformationDefenseEngine.RestoreVoxelFakes();

            _control = null;
            Engine = null;
            Statistics = null;
            Instance = null;
            base.Dispose();
        }
    }
}
