using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;

namespace StockStratMemes.DatasetView {
    /// <summary>
    /// Used to view datasets and manipulate or export them.
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

        private void OnExportClicked(object sender, RoutedEventArgs e) {
            ExportDatasetOptionsDialog exportOptionsWindow = new ExportDatasetOptionsDialog();
            exportOptionsWindow.SetDataset(_dataset);
            bool? result = exportOptionsWindow.ShowDialog();
            if (result == true) {
                ExportDatasetOptions options = exportOptionsWindow.GetOptions();
                ExportDataset(_dataset, options);
            }
        }

        private void ExportDataset(Dataset dataset, ExportDatasetOptions options) {
            using (StreamWriter stream = new StreamWriter(options.FilePath)) {
                stream.WriteLine(options.GetDateHeader() + ", Price ($)");
                for (int i = 0; i < dataset.Points.Count; i++) {
                    stream.WriteLine(options.FormatDateString(dataset.Points[i].X) + ", " + dataset.Points[i].Y);
                }
            }
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
