using CryptoRetriever.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CryptoRetriever.Data;
using System.Windows.Media;
using Utf8Json;
using CryptoRetriever.Strats;
using CryptoRetriever.Filter;
using CryptoRetriever.UI.GenericDialogs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoRetriever.UI {
    /// <summary>
    /// A window that lets users grab a dataset from a list of sources that are displayed
    /// for the selected currency in a selected date range and spaced at a selected granularity.
    /// </summary>
    public sealed partial class DatasetCreator : Window {

        private Dictionary<String, ISource> _sources = new Dictionary<String, ISource>();
        private ISource _currentSource = null;
        private int _selectedGranularity = -1;
        private StrategyManager _strategyManager;

        public DatasetCreator(StrategyManager manager) {
            InitializeComponent();

            _strategyManager = manager;

            // Add all the sources
            CoinbaseSource coinbaseSource = new CoinbaseSource();
            _sources[coinbaseSource.GetName()] = coinbaseSource;
            CoinGeckoSource coinGeckoSource = new CoinGeckoSource();
            _sources[coinGeckoSource.GetName()] = coinGeckoSource;

            // Fill out the sources list
            foreach (ISource source in _sources.Values) {
                RadioButton sourceButton = new RadioButton();
                sourceButton.Content = source.GetName();
                sourceButton.Template = Resources["SourceSelectionTemplate"] as ControlTemplate;
                _sourcePanel.Children.Add(sourceButton);
            }
        }

        private void UpdateGranularityOptions() {
            _granularityPanel.Children.Clear();
            _selectedGranularity = -1;

            if (_currentSource == null)
                return;

            RadioButton oneDayGranularity = null;
            foreach (int granularity in _currentSource.GetGranularityOptions()) {
                RadioButton granularityButton = new RadioButton();
                granularityButton.Margin = new Thickness(10, 0, 0, 10);
                granularityButton.VerticalContentAlignment = VerticalAlignment.Center;
                granularityButton.Checked += OnGranularitySelected;

                TextBlock granularityContent = new TextBlock();
                granularityContent.Foreground = new SolidColorBrush(Colors.White);
                granularityContent.FontSize = 20;
                granularityContent.FontFamily = new FontFamily("Arial");
                granularityContent.Text = GetUserGranularityString(granularity);
                granularityButton.Content = granularityContent;
                granularityButton.Tag = granularity;
                _granularityPanel.Children.Add(granularityButton);

                int oneDayInSeconds = 86400;
                if (granularity == oneDayInSeconds)
                    oneDayGranularity = granularityButton;
            }

            // Select one day by default if available.
            // Otherwise just pick the first one.
            if (oneDayGranularity != null)
                oneDayGranularity.IsChecked = true;
            else
                (_granularityPanel.Children[0] as RadioButton).IsChecked = true;
        }

        private void OnCreateClicked(object sender, RoutedEventArgs e) {
            if (!_startDatePicker.SelectedDate.HasValue) {
                MessageBox.Show("Please select a start date.");
                return;
            }

            if (String.IsNullOrEmpty(_selectedOutput.Text)) {
                MessageBox.Show("Please select an output file.");
                return;
            }

            DateTime pickedStartDate = _startDatePicker.SelectedDate.Value;
            DateTime startDate = new DateTime(pickedStartDate.Year, pickedStartDate.Month, pickedStartDate.Day, 0, 0, 0, DateTimeKind.Local);
            DateTime endDate = DateTime.Now;

            if (_endDatePicker.SelectedDate.HasValue) {
                DateTime pickedEndDate = _endDatePicker.SelectedDate.Value;
                endDate = new DateTime(pickedEndDate.Year, pickedEndDate.Month, pickedEndDate.Day, 0, 0, 0, DateTimeKind.Local);
            }

            if (startDate > endDate) {
                MessageBox.Show("The start date must be before the end date.");
                return;
            }

            if (startDate > DateTime.Now) {
                MessageBox.Show("The start date cannot be in the future.");
                return;
            }

            if (endDate > DateTime.Now) {
                MessageBox.Show("The end date cannot be in the future.");
                return;
            }

            ProgressDialog dialog = new ProgressDialog();
            dialog.CurrentProgress = 0;
            dialog.Label = "Saving...";
            if (_currentSource != null) {
                int granularity = _selectedGranularity;
                Asset currentAsset = _currencySelection.SelectedItem as Asset;
                _currentSource.GetPriceHistoryAsync(currentAsset, new DateRange(startDate, endDate), granularity, new DialogProgressListener(dialog)).ContinueWith((Task<DatasetResult> taskResult) => {
                    dialog.Dispatcher.InvokeAsync(() => {
                        Result<Dataset> result = taskResult.Result;

                        if (result.Succeeded && result.Value.Points.Count > 0) {
                            // Check if the dataset is evenly spaced
                            if (!result.Value.IsEvenlySpaced().Item1) {
                                // If not, offer to correct it with interpolation (They may not want this)
                                MessageBoxResult userWantsEvenSpacingResponse = MessageBox.Show(
                                    "The dataset has either some missing samples, duplicates, or other irregularities. Would you like to fill them in with interpolation?",
                                    "Uneven Dataset Spacing",
                                    MessageBoxButton.OKCancel);
                                if (userWantsEvenSpacingResponse == MessageBoxResult.OK) {
                                    ResamplerFilter filter = new ResamplerFilter((long)Math.Round(result.Value.Granularity));
                                    result = filter.Filter(result.Value);
                                }
                            }

                            try {
                                String filePath = _selectedOutput.Text;
                                String dir = Path.GetDirectoryName(filePath);
                                Directory.CreateDirectory(dir);
                                byte[] json = JsonSerializer.PrettyPrintByteArray(JsonSerializer.Serialize(result.Value));
                                File.WriteAllBytes(filePath, json);
                                DatasetWriter writer = new DatasetWriter();
                                Result writeResult = writer.WriteFile(result.Value, filePath);
                                if (!writeResult.Succeeded) {
                                    MessageBox.Show("UH OH! Couldn't write the dataset to a file: " + result.ErrorDetails);
                                } else {
                                    // Open a dataset viewer
                                    DatasetViewer viewer = new DatasetViewer(_strategyManager);
                                    viewer.SetDataset(filePath, result.Value);
                                    viewer.Show();
                                }
                                dialog.Close();
                                Close();
                            } catch (Exception ex) {
                                dialog.Close();
                                MessageBox.Show("UH OH! Couldn't write the dataset to a file: " + ex.Message);
                            }
                        } else {
                            dialog.Close();

                            // If we succeeded and we're here it means there were no points in the dataset
                            if (result.Succeeded) {
                                MessageBox.Show("UH OH! The dataset did not have any points!");
                            } else {
                                // Else there was an error
                                MessageBox.Show("UH OH! " + result.ErrorDetails);
                            }
                        }
                    });
                });
            }

            // While we're saving, block the UI with a saving dialog:
            dialog.ShowDialog();
        }

        private void OnGranularitySelected(object sender, RoutedEventArgs e) {
            FrameworkElement element = sender as FrameworkElement;
            int granularityInSeconds = (int)element.Tag; // We store the granularity in the tag of each control
            _selectedGranularity = granularityInSeconds;
        }

        private void OnSourceSelected(object sender, RoutedEventArgs e) {
            RadioButton element = sender as RadioButton;
            String sourceName = (element.Content as TextBlock).Text;
            
            ISource source = _sources[sourceName];
            _currentSource = source;
            _currencySelection.IsEnabled = false;
            _sourceNoteTextBlock.Text = source.GetNote();
            Task result = source.GetAssetsAsync().ContinueWith((action) => {
                // While we're here, sort the list if it was successful
                // so we do it off the main thread.
                if (action.Result.Succeeded) {
                    action.Result.Value.Sort();
                }

                Dispatcher.InvokeAsync(() => {
                    if (_currentSource != source) {
                        return; // Old request
                    }

                    AssetListResult listResult = action.Result;

                    if (listResult.Succeeded) {
                        _currencySelection.IsEnabled = true;
                        _currencySelection.ItemsSource = listResult.Value;
                        _currencySelection.SelectedIndex = 0;
                    } else {
                        // Error
                        MessageBox.Show("An error occurred loading the source. Error: " + action.Result.ErrorDetails);
                    }
                });
            });

            // Fill out the available granularities too
            UpdateGranularityOptions();
        }

        private void OnChooseOutputButtonClicked(object sender, RoutedEventArgs e) {
            // Make a default name that is informative
            String name = "";
            if (_currentSource != null) {
                name += _currentSource.GetName();
            }

            if (_currencySelection.SelectedIndex >= 0) {
                Asset currentAsset = _currencySelection.SelectedItem as Asset;

                if (currentAsset != null) {
                    name += "_" + currentAsset.Name;
                }
            }

            if (_startDatePicker.SelectedDate.HasValue) {
                DateTime selectedDate = _startDatePicker.SelectedDate.Value;
                name += "_from_" + selectedDate.ToShortDateString().Replace("/", "-");
            }

            Result<String> result = DatasetUiHelper.ShowSaveDatasetAsDialog(name);
            if (result.Succeeded) {
                _selectedOutput.Text = result.Value;
            }
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        /// <summary>
        /// Makes a prettified string of the given number of seconds for the user.
        /// </summary>
        /// <param name="granularitySeconds">The number of seconds.</param>
        /// <returns></returns>
        private String GetUserGranularityString(int granularitySeconds) {
            TimeSpan timeSpan = new TimeSpan(0, 0, granularitySeconds);

            String result = "";
            if (timeSpan.Days > 0)
                result = ExtendGranularityString(result, timeSpan.Days, "Day");
            if (timeSpan.Hours > 0)
                result = ExtendGranularityString(result, timeSpan.Hours, "Hour");
            if (timeSpan.Minutes > 0)
                result = ExtendGranularityString(result, timeSpan.Minutes, "Minute");
            if (timeSpan.Seconds > 0)
                result = ExtendGranularityString(result, timeSpan.Seconds, "Second");

            return result;
        }

        /// <summary>
        /// Extends the granularity string adding a comma and/or an S as appropriate.
        /// </summary>
        /// <param name="existingString">The existing string.</param>
        /// <param name="count">The number of the label.</param>
        /// <param name="label">The label (Day, Hour, Minute, Second)</param>
        /// <returns>Returns the extended string.</returns>
        private String ExtendGranularityString(String existingString, int count, String label) {
            String newString = existingString;
            if (!String.IsNullOrEmpty(newString))
                newString += ", ";
            newString += count + " " + label;
            if (count != 1)
                newString += "s";
            return newString;
        }
    }
}
