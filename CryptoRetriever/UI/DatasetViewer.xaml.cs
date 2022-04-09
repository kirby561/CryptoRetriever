using CryptoRetriever.Data;
using CryptoRetriever.Filter;
using CryptoRetriever.Strats;
using CryptoRetriever.UI;
using KFSO.UI.DockablePanels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;

namespace CryptoRetriever.UI {
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

        // Strategy list view
        // ?? TODO: This is set in its OnLoad even because of this error:
        //      "Cannot set Name attribute value '_strategiesListView' on element 'ListView'. 'ListView' is under the scope of element 'DockablePanel'...
        //      Need to do more reading about this and probably need to fix KFSO.DockablePanels.
        //      For the moment, this works fine though.
        private ListView _strategiesListView;

        // Keep track of the last filter that was run so we can
        // run it again if the user clicks it.
        private IFilter _lastFilter = null;

        /// <summary>
        /// A manager that provides the list of strategies that can be run on
        /// the dataset being viewed. This can be set prior to launching the window
        /// in order to specify a different manager (one shared by the application for
        /// example).
        /// </summary>
        public StrategyManager StrategyManager { get; set; } = new StrategyManager();

        public DatasetViewer() {
            this.InitializeComponent();

            _dockManager.UseDockManagerForTree(this);
            _dockPanelSpotLeft.ChildrenChanged += OnStationChildrenChanged;
            _dockPanelSpotRight.ChildrenChanged += OnStationChildrenChanged;
        }

        public void SetDataset(String filePath, Dataset dataset) {
            // Detach the current renderer if any
            if (_renderer != null) {
                _renderer.Cleanup();
                _renderer = null;
                _graphController = null;
            }

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
            ShowDockPanel(_optionsPanel);
        }

        private void OnShowStrategiesPanelClick(object sender, RoutedEventArgs e) {
            ShowDockPanel(_strategyPanel);
        }

        private void ShowDockPanel(DockablePanel panel) {
            // Check if it's displayed already
            if (!panel.IsShown) {
                panel.Dock(_dockPanelSpotLeft);
            } else {
                // Try to bring it in to view
                FocusWindowOfElement(panel);
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

        private void OnAddStrategyClicked(object sender, RoutedEventArgs e) {
            StrategyEditorWindow window = new StrategyEditorWindow();
            UiHelper.CenterWindowInWindow(window, this);
            window.ShowDialog();

            if (window.Strategy != null) {
                StrategyManager.AddStrategy(window.Strategy);
            }
        }

        private void OnRemoveStrategyClicked(object sender, RoutedEventArgs e) {
            UiHelper.RemoveSelectedItemsFromListBox(StrategyManager.GetStrategies(), _strategiesListView);
        }

        private void OnStrategiesListDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (_strategiesListView.SelectedIndex >= 0) {
                StrategyEditorWindow strategyEditorWindow = new StrategyEditorWindow();
                UiHelper.CenterWindowInWindow(strategyEditorWindow, this);

                // ?? TODO: This should be a deep copy but there's no way to save conditions
                // right now so it will be a reference until that is implemented.
                strategyEditorWindow.SetWorkingStrategy(StrategyManager.GetStrategies()[_strategiesListView.SelectedIndex]);
                strategyEditorWindow.ShowDialog();

                if (strategyEditorWindow.Strategy != null) {
                    StrategyManager.GetStrategies()[_strategiesListView.SelectedIndex] = strategyEditorWindow.Strategy;
                }
            }
        }

        private void OnStrategyListViewLoaded(object sender, RoutedEventArgs e) {
            _strategiesListView = sender as ListView;
            _strategiesListView.ItemsSource = StrategyManager.GetStrategies();
        }

        private void RunStrategyClicked(object sender, RoutedEventArgs e) {
            if (_strategiesListView.SelectedIndex < 0) {
                return; // Nothing is selected
            }

            Strategy strategy = StrategyManager.GetStrategies()[_strategiesListView.SelectedIndex];
            StrategyEngine engine = new StrategyEngine(strategy, _dataset);
            engine.Run();

            MessageBox.Show(engine.RunContext.ToString());
        }
    }
}
