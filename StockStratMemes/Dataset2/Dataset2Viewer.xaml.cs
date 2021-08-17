using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StockStratMemes.Dataset2View {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Dataset2Viewer : Window {
        public Dataset2Viewer() {
            this.InitializeComponent();
        }

        public void SetDataset(Dataset2 dataset) {

        }
    }
}
