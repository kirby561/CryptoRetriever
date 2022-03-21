using KFSO.UI.DockablePanels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;

namespace CryptoRetriever.DatasetView {
    /// <summary>
    /// Used to view datasets and manipulate or export them.
    /// </summary>
    public sealed partial class DatasetViewer : Window {
        // Allow any panel to dock anywhere in this window
        private DockManager _dockManager = new DockManager();
        private Dataset _dataset;
        private GraphRenderer _renderer;
        private GraphController _graphController;

        public DatasetViewer() {
            this.InitializeComponent();

            _dockManager.UseDockManagerForTree(this);
            _dockPanelSpotLeft.ChildrenChanged += OnStationChildrenChanged;
            _dockPanelSpotRight.ChildrenChanged += OnStationChildrenChanged;
        }

        public void SetDataset(String name, Dataset dataset) {
            _window.Title = name;
            _dataset = dataset;
            _renderer = new GraphRenderer(_graphCanvas, _dataset);
            // Set the formatters for how to display the timestamps and values. These could be
            // configurable in the future and also could depend on the currency that is being used.
            _renderer.SetCoordinateFormatters(new TimestampToDateFormatter(), new DollarFormatter());
            _renderer.UpdateAll();

            _graphController = new GraphController(_renderer);
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

        private void OnMouseEntered(object sender, System.Windows.Input.MouseEventArgs e) {
            _graphController.OnMouseEntered();
        }

        private void OnMouseLeft(object sender, System.Windows.Input.MouseEventArgs e) {
            _graphController.OnMouseLeft();
        }

        private void OnMouseMoved(object sender, System.Windows.Input.MouseEventArgs e) {
            _graphController.OnMouseMoved(e.GetPosition(_graphCanvas));
        }

        private void OnMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
            _graphController.OnMouseWheel(e.Delta, e.GetPosition(_graphCanvas));
        }

        private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            _graphController.OnMouseUp(e.GetPosition(_graphCanvas));
        }

        private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            _graphController.OnMouseDown(e.GetPosition(_graphCanvas));
        }

        private void OnStationChildrenChanged(DockStation station) {
            List<DockablePanel> panels = station.GetDockedPanels();
            // Make the panels auto-hide when there's no children in them.
            if (panels.Count == 0) {
                Grid parent = station.Parent as Grid;
                parent.ColumnDefinitions[Grid.GetColumn(station)].Width = new GridLength(0, GridUnitType.Auto);
            }
        }

        private void OnAxisVisibleCheckboxChecked(object sender, RoutedEventArgs e) {
            if (_renderer == null)
                return; // The dataset has not been set yet
            
            CheckBox checkbox = sender as CheckBox;
            bool? isChecked = checkbox.IsChecked;
            _renderer.IsAxisEnabled = (isChecked.HasValue && isChecked.Value) ? true : false;
        }
    }

    class DollarFormatter : ICoordinateFormatter {
        public string Format(double coordinate) {
            return "$" + ((decimal)coordinate).ToString("N");
        }
    }

    class TimestampToDateFormatter : ICoordinateFormatter {
        public string Format(double coordinate) {
            long utcTimestampSeconds = (long)Math.Round(coordinate);
            DateTime unixStart = DateTimeConstant.UnixStart;
            DateTime localDateTime = unixStart.AddSeconds(utcTimestampSeconds).ToLocalTime();
            return localDateTime.ToString("G");
        }
    }
}
