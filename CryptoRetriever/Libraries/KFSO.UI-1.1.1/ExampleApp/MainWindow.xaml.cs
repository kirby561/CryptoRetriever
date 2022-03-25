using KFSO.UI.DockablePanels;
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

namespace ExampleApp {
    /// <summary>
    /// A window for launching examples showing how to 
    /// use the various UI features in this library.
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void OnRunDockablePanelsClicked(object sender, RoutedEventArgs e) {
            DockablePanelsExample dockablePanelsExample = new DockablePanelsExample();
            dockablePanelsExample.Show();
        }
    }
}
