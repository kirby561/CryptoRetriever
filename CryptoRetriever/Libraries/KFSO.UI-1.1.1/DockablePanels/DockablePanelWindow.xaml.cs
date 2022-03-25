using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KFSO.UI.DockablePanels {
    /// <summary>
    /// A Window that hosts a DockablePanel when it is floating
    /// and not docked into any DockStation.
    /// </summary>
    public partial class DockablePanelWindow : Window {
        private DockablePanel _hostedPanel;

        public DockablePanelWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the dockable panel that is hosted
        /// by this window.
        /// </summary>
        public DockablePanel HostedPanel {
            get {
                return _hostedPanel;
            }
            set {
                if (_hostedPanel != null)
                    _contentHost.Children.Remove(_hostedPanel);
                _hostedPanel = value;

                if (_hostedPanel != null) {
                    _contentHost.Children.Add(_hostedPanel);
                    Width = _hostedPanel.ActualWidth;
                    Height = _hostedPanel.ActualHeight;
                }
            }
        }
    }
}
