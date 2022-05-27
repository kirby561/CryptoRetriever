using CryptoRetriever.Data;
using CryptoRetriever.Filter;
using CryptoRetriever.Strats;
using CryptoRetriever.UI;
using CryptoRetriever.UI.GenericDialogs;
using KFSO.UI.DockablePanels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Used to view datasets and manipulate or export them.
    /// </summary>
    public sealed partial class DatasetViewer : Window {
        // Allow any panel to dock anywhere in this window
        private DockManager _dockManager = new DockManager();
        private Dataset _originalDataset;
        private Dataset _filteredDataset;
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
        public StrategyManager StrategyManager { get; set; }

        public DatasetViewer(StrategyManager manager) {
            this.InitializeComponent();

            // Setup the dock manager
            StrategyManager = manager;
            _dockManager.UseDockManagerForTree(this);
            _dockPanelSpotLeft.ChildrenChanged += OnStationChildrenChanged;
            _dockPanelSpotRight.ChildrenChanged += OnStationChildrenChanged;

            // Add filter options
            List<IFilter> filterOptions = FilterUi.GetFilterUiMap().Keys.ToList();
            foreach (IFilter filter in filterOptions) {
                MenuItem filterMenuItem = new MenuItem();
                filterMenuItem.Header = filter.GetType().Name;
                filterMenuItem.Click += OnFilterClicked;
                _filterListMenuItem.Items.Add(filterMenuItem);
            }

            // Add the "Repeat" button
            _filterListMenuItem.Items.Add(new Separator());
            MenuItem repeatLastItem = new MenuItem();
            repeatLastItem.Header = "Repeat Last";
            repeatLastItem.Click += OnRepeatLastFilterClicked;
            _filterListMenuItem.Items.Add(repeatLastItem);
        }

        public void SetDataset(String filePath, Dataset originalDataset) {
            SetDataset(filePath, originalDataset, null);
        }

        public void SetDataset(String filePath, Dataset originalDataset, Dataset filteredDataset) {
            bool showOriginalDataset = true;

            // Detach the current renderer if any
            if (_renderer != null) {
                showOriginalDataset = _renderer.ShowOriginalDataset; // Maintain setting
                _renderer.Cleanup();
                _renderer = null;
                _graphController = null;
            }

            _filePath = filePath;
            String filename = Path.GetFileName(filePath);
            _window.Title = filename;
            _originalDataset = originalDataset;
            _filteredDataset = filteredDataset;
            _renderer = new GraphRenderer(_graphCanvas, _originalDataset, _filteredDataset);
            // Set the formatters for how to display the timestamps and values. These could be
            // configurable in the future and also could depend on the currency that is being used.
            _renderer.SetCoordinateFormatters(new TimestampToDateFormatter(), new DollarFormatter());
            _renderer.UpdateAll();
            _renderer.ShowOriginalDataset = showOriginalDataset;

            _graphController = new GraphController(_renderer);
        }

        private void OnExportClicked(object sender, RoutedEventArgs e) {
            ExportDatasetOptionsDialog exportOptionsWindow = new ExportDatasetOptionsDialog();
            exportOptionsWindow.SetDataset(GetActiveDataset());
            bool? result = exportOptionsWindow.ShowDialog();
            if (result == true) {
                ExportDatasetOptions options = exportOptionsWindow.GetOptions();
                ExportDataset(GetActiveDataset(), options);
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

        private void OnTickmarksVisibleCheckboxChecked(object sender, RoutedEventArgs e) {
            if (_renderer == null)
                return; // The dataset has not been set yet

            CheckBox checkbox = sender as CheckBox;
            bool? isChecked = checkbox.IsChecked;
            _renderer.AreTicksEnabled = (isChecked.HasValue && isChecked.Value) ? true : false;
        }

        private void OnHorizGridlinesVisibleCheckboxChecked(object sender, RoutedEventArgs e) {
            if (_renderer == null)
                return; // The dataset has not been set yet

            CheckBox checkbox = sender as CheckBox;
            bool? isChecked = checkbox.IsChecked;
            _renderer.AreYGridlinesEnabled = (isChecked.HasValue && isChecked.Value) ? true : false;
        }

        private void OnVertGridlinesVisibleCheckboxChecked(object sender, RoutedEventArgs e) {
            if (_renderer == null)
                return; // The dataset has not been set yet

            CheckBox checkbox = sender as CheckBox;
            bool? isChecked = checkbox.IsChecked;
            _renderer.AreXGridlinesEnabled = (isChecked.HasValue && isChecked.Value) ? true : false;
        }

        private void OnStartRangeAt0CheckboxChecked(object sender, RoutedEventArgs e) {
            if (_renderer == null)
                return; // The dataset has not been set yet

            CheckBox checkbox = sender as CheckBox;
            bool? isChecked = checkbox.IsChecked;
            _renderer.ShouldStartRangeAt0 = (isChecked.HasValue && isChecked.Value) ? true : false;
        }

        private void OnShowOriginalDatasetCheckboxChecked(object sender, RoutedEventArgs e) {
            if (_renderer == null)
                return; // The dataset has not been set yet

            CheckBox checkbox = sender as CheckBox;
            bool? isChecked = checkbox.IsChecked;
            _renderer.ShowOriginalDataset = (isChecked.HasValue && isChecked.Value) ? true : false;
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

        /// <summary>
        /// This is a generic filter handler for any type of filter.
        /// For the moment it assumes the text is the ID of the filter.
        /// </summary>
        /// <param name="sender">A MenuItem with its Header a String ID of the filter.</param>
        /// <param name="e">N/A</param>
        private void OnFilterClicked(object sender, RoutedEventArgs e) {
            MenuItem menuItem = (MenuItem)sender;
            String header = (String)menuItem.Header;
            KeyValuePair<IFilter, BaseFilterDialog> filterAndDialog = FilterUi.GetDialogByName(header).Value;

            if (filterAndDialog.Value != null) {
                IFilter filter = filterAndDialog.Key;

                // As a special case, for the ResamplerFilter, set the default
                // frequency to the frequency of the dataset
                if (filter is ResamplerFilter) {
                    ResamplerFilter resampler = (ResamplerFilter)filter;
                    resampler.SampleFrequency = (long)GetActiveDataset().Granularity;
                }

                BaseFilterDialog editor = filterAndDialog.Value;
                editor.SetWorkingFilter(filter);
                editor.ShowDialog();

                if (editor.GetResult() != null) {
                    _lastFilter = editor.GetResult();
                } else {
                    // If it's null the user cancelled
                    return;
                }
            } else {
                _lastFilter = filterAndDialog.Key;
            }

            // ?? TODO: This could be checked with a method on IFilter or
            // from the range of the resulting Dataset or something instead
            if (_lastFilter is DerivativeFilter) {
                // For this filter, launch a new window because the range is going
                // to be significantly different than the original dataset. In the
                // future, maybe the renderer could support showing 2 separate ranges
                // for the original and filtered datasets.
                Result<Dataset> newDataset = _lastFilter.Filter(GetActiveDataset());
                if (!newDataset.Succeeded) {
                    MessageBox.Show("An error occurred filtering: " + newDataset.ErrorDetails);
                    return;
                }
                DatasetViewer viewer = new DatasetViewer(StrategyManager);
                viewer.SetDataset(_filePath.Replace(".dataset", "_derivative") + ".dataset", newDataset.Value);
                viewer.Show();
            } else {
                Result<Dataset> output = _lastFilter.Filter(GetActiveDataset());
                if (!output.Succeeded) {
                    MessageBox.Show("An error occurred filtering: " + output.ErrorDetails);
                    return;
                }

                SetDataset(_filePath, _originalDataset, output.Value);
            }
        }

        private void OnRepeatLastFilterClicked(object sender, RoutedEventArgs e) {
            if (_lastFilter != null) {
                Result<Dataset> output = _lastFilter.Filter(GetActiveDataset());
                if (!output.Succeeded) {
                    MessageBox.Show("An error occurred filtering: " + output.ErrorDetails);
                    return;
                }
                SetDataset(_filePath, _originalDataset, output.Value);
            }
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e) {
            DatasetWriter writer = new DatasetWriter();
            Result result = writer.WriteFile(GetActiveDataset(), _filePath);
            if (!result.Succeeded) {
                MessageBox.Show("UH OH! Couldn't write the dataset to a file: " + result.ErrorDetails);
            }
        }

        private void OnSaveAsClicked(object sender, RoutedEventArgs e) {
            Result<String> getPathResult = DatasetUiHelper.ShowSaveDatasetAsDialog(_filePath);

            if (getPathResult.Succeeded) {
                DatasetWriter writer = new DatasetWriter();
                Dataset dataset = GetActiveDataset();
                Result writeResult = writer.WriteFile(dataset, getPathResult.Value);
                if (!writeResult.Succeeded) {
                    MessageBox.Show("UH OH! Couldn't write the dataset to a file: " + writeResult.ErrorDetails);
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
            StrategyEditorWindow window = new StrategyEditorWindow(StrategyManager);
            UiHelper.CenterWindowInWindow(window, this);
            window.ShowDialog();

            if (window.Strategy != null) {
                StrategyManager.AddStrategy(window.Strategy);
            }
        }

        private void OnRemoveStrategyClicked(object sender, RoutedEventArgs e) {
            if (_strategiesListView.SelectedIndex >= 0) {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this strategy?", "Delete strategy", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes) {
                    Strategy selectedStrategy = _strategiesListView.SelectedItem as Strategy;
                    StrategyManager.DeleteStrategyByName(selectedStrategy.Name);
                    UiHelper.RemoveSelectedItemsFromListBox(StrategyManager.GetStrategies(), _strategiesListView);
                }
            }
        }

        private void OnStrategiesListDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (_strategiesListView.SelectedIndex >= 0) {
                StrategyEditorWindow strategyEditorWindow = new StrategyEditorWindow(StrategyManager);
                UiHelper.CenterWindowInWindow(strategyEditorWindow, this);

                // ?? TODO: This should be a deep copy but there's no way to save conditions
                // right now so it will be a reference until that is implemented.
                strategyEditorWindow.SetWorkingStrategy(StrategyManager.GetStrategies()[_strategiesListView.SelectedIndex]);
                strategyEditorWindow.ShowDialog();
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
            StrategyEngine engine = new StrategyEngine(strategy, _originalDataset);
            ProgressDialog dialog = new ProgressDialog();
            dialog.Label = "Running...";

            ThreadPool.QueueUserWorkItem(o => {
                engine.Run(new DialogProgressListener(dialog));

                dialog.Dispatcher.Invoke(() => {
                    SetDataset(_filePath, _originalDataset, engine.RunContext.FilteredDataset);
                    _renderer.Transactions = engine.RunContext.Transactions;

                    StrategyResultsView resultsView = new StrategyResultsView();
                    resultsView.RunContext = engine.RunContext;
                    DockablePanel panel = new DockablePanel();
                    panel.TitleText = "Run Results";
                    panel.DockManager = _dockManager;
                    panel.HostedContent = resultsView;
                    panel.Dock(_dockPanelSpotRight);
                    dialog.Close();
                });
            });

            dialog.ShowDialog();
        }

        /// <summary>
        /// Returns the latest dataset version. If the dataset was filtered,
        /// it will return that one. Otherwise it will return the original.
        /// </summary>
        /// <returns>Returns the active dataset.</returns>
        private Dataset GetActiveDataset() {
            Dataset dataset;
            if (_filteredDataset != null)
                dataset = _filteredDataset;
            else
                dataset = _originalDataset;
            return dataset;
        }
    }
}
