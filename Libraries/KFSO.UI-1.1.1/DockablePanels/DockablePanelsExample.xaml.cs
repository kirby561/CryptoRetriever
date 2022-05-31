using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KFSO.UI.DockablePanels {
    /// <summary>
    /// This is the main window to demonstrate the docking 
    /// features and how to use them. 
    /// </summary>
    public partial class DockablePanelsExample : Window {
        // Allow any panel to dock anywhere in this window
        private DockManager _dockManager = new DockManager();

        public DockablePanelsExample() {
            InitializeComponent();

            // Make all dock stations in this window use this manager.
            _dockManager.UseDockManagerForTree(this);
            _dockPanelSpot.ChildrenChanged += OnStationChildrenChanged;
            _dockPanelSpot2.ChildrenChanged += OnStationChildrenChanged;
        }

        private void OnStationChildrenChanged(DockStation station) {
            List<DockablePanel> panels = station.GetDockedPanels();
            // Make the panels auto-hide when there's no children in them.
            if (panels.Count == 0) {
                Grid parent = station.Parent as Grid;
                parent.ColumnDefinitions[Grid.GetColumn(station)].Width = new GridLength(0, GridUnitType.Auto);
            }
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnViewControlsClicked(object sender, RoutedEventArgs e) {
            if (!_controlsPanel.IsShown) {
                _controlsPanel.Dock(_dockPanelSpot);
            }
        }

        private void OnViewOptionsClicked(object sender, RoutedEventArgs e) {
            if (!_optionsPanel.IsShown) {
                _optionsPanel.Dock(_dockPanelSpot);
            }
        }
    }
}
