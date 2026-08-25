using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GVK.GridDefender.Views
{
    /// <summary>
    /// Interaction logic for GridDefenderControl.xaml WPF interface in Torch.
    /// Provides real-time telemetry polling and configuration persistence controls.
    /// </summary>
    public partial class GridDefenderControl : UserControl
    {
        private GridDefenderPlugin Plugin { get; }
        private readonly DispatcherTimer _telemetryTimer;

        /// <summary>
        /// Default constructor for WPF designer support.
        /// </summary>
        public GridDefenderControl()
        {
            InitializeComponent();

            _telemetryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _telemetryTimer.Tick += (s, e) =>
            {
                Plugin?.Statistics?.NotifyAll();
            };

            Loaded += (s, e) => _telemetryTimer.Start();
            Unloaded += (s, e) => _telemetryTimer.Stop();
        }

        /// <summary>
        /// Initializes the control bound to the active plugin instance.
        /// </summary>
        /// <param name="plugin">Active GridDefenderPlugin instance.</param>
        public GridDefenderControl(GridDefenderPlugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Plugin?.SaveConfig();
                MessageBox.Show("GridDefender configuration saved successfully!", "GridDefender", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetStatsButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin?.Statistics?.Reset();
        }
    }
}

