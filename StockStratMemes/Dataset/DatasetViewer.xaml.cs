using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StockStratMemes.DatasetView {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DatasetViewer : Window {
        private Dataset _dataset;
        private GraphRenderer _renderer;

        public DatasetViewer() {
            this.InitializeComponent();
        }

        public void SetDataset(Dataset dataset) {
            _dataset = dataset;
            _renderer = new GraphRenderer(_graphCanvas, _dataset);
            _renderer.Draw();
        }

        private void OnGraphSizeChanged(object sender, SizeChangedEventArgs e) {

        }
    }
}
