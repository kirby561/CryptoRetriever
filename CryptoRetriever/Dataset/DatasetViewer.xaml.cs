using CryptoRetriever.Filter;
using KFSO.UI.DockablePanels;
using Microsoft.Win32;
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
        private String _filePath;
        private GraphRenderer _renderer;
        private GraphController _graphController;

        // Keep track of the last filter that was run so we can
        // run it again if the user clicks it.
        private IFilter _lastFilter = null;

        public DatasetViewer() {
            this.InitializeComponent();

            _dockManager.UseDockManagerForTree(this);
            _dockPanelSpotLeft.ChildrenChanged += OnStationChildrenChanged;
            _dockPanelSpotRight.ChildrenChanged += OnStationChildrenChanged;
        }

        public void SetDataset(String filePath, Dataset dataset) {
            _graphCanvas.Children.Clear();
            _filePath = filePath;
            String filename = Path.GetFileName(filePath);
            _window.Title = filename;
            _dataset = dataset;
            _renderer = new GraphRenderer(_graphCanvas, _dataset);
            // Set the formatters for how to display the timestamps and values. These could be
            // configurable in the future and also could depend on the currency that is being used.
            _renderer.SetCoordinateFormatters(new TimestampToDateFormatter(), new DollarFormatter());
            _renderer.UpdateAll();

            _graphController = new GraphController(_renderer);
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

        private void OnShowGraphOptionsPanelClick(object sender, RoutedEventArgs e) {
            // Check if it's displayed already
            if (!_optionsPanel.IsShown) {
                _optionsPanel.Dock(_dockPanelSpotLeft);
            } else {
                // Try to bring it in to view
                FocusWindowOfElement(_optionsPanel);
            }
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
            bool enableSplitter = true;
            List<DockablePanel> panels = station.GetDockedPanels();
            // Make the panels auto-hide when there's no children in them.
            if (panels.Count == 0) {
                Grid parent = station.Parent as Grid;
                parent.ColumnDefinitions[Grid.GetColumn(station)].Width = new GridLength(0, GridUnitType.Auto);

                enableSplitter = false;
            }

            if (station == _dockPanelSpotLeft)
                _leftGridSplitter.IsEnabled = enableSplitter;
            else if (station == _dockPanelSpotRight)
                _rightGridSplitter.IsEnabled = enableSplitter;
        }

        private void OnAxisVisibleCheckboxChecked(object sender, RoutedEventArgs e) {
            if (_renderer == null)
                return; // The dataset has not been set yet
            
            CheckBox checkbox = sender as CheckBox;
            bool? isChecked = checkbox.IsChecked;
            _renderer.IsAxisEnabled = (isChecked.HasValue && isChecked.Value) ? true : false;
        }

        /// <summary>
        /// Focuses the window the given element is in. This can be used to bring
        /// it into view if it is behind other windows (such as a floating dockable panel
        /// that is behind the main window).
        /// </summary>
        /// <param name="element">The element to find and focus the window of</param>
        private void FocusWindowOfElement(FrameworkElement element) {
            FrameworkElement parent = element.Parent as FrameworkElement;
            while (parent != null) {
                Window window = parent as Window;
                if (window != null) {
                    window.Focus();
                    return;
                }

                parent = parent.Parent as FrameworkElement;
            }
        }

        private void OnGaussianBlurClicked(object sender, RoutedEventArgs e) {
            _lastFilter = new GaussianFilter(1);
            Dataset output = _lastFilter.Filter(_dataset);
            SetDataset(_filePath, output);
        }

        private void OnRepeatLastFilterClicked(object sender, RoutedEventArgs e) {
            if (_lastFilter != null) {
                Dataset output = _lastFilter.Filter(_dataset);
                SetDataset(_filePath, output);
            }
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e) {
            DatasetWriter writer = new DatasetWriter();
            Result result = writer.WriteFile(_dataset, _filePath);
            if (!result.Succeeded) {
                MessageBox.Show("UH OH! Couldnt write the dataset to a file: " + result.ErrorDetails);
            }
        }

        private void OnSaveAsClicked(object sender, RoutedEventArgs e) {
            Result<String> getPathResult = DatasetUiHelper.ShowSaveDatasetAsDialog(_filePath);

            if (getPathResult.Succeeded) {
                DatasetWriter writer = new DatasetWriter();
                Result writeResult = writer.WriteFile(_dataset, getPathResult.Value);
                if (!writeResult.Succeeded) {
                    MessageBox.Show("UH OH! Couldnt write the dataset to a file: " + writeResult.ErrorDetails);
                }
            }
        }

        private void OnOpenClicked(object sender, RoutedEventArgs e) {
            Result<Tuple<Dataset, String>> result = DatasetUiHelper.OpenDatasetWithDialog(_filePath);
            if (result.Succeeded) {
                SetDataset(result.Value.Item2, result.Value.Item1);
            }
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
