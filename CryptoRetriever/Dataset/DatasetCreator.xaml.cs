using Microsoft.Win32;
using CryptoRetriever.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoRetriever.DatasetView {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DatasetCreator : Window {

        private Dictionary<String, ISource> _sources = new Dictionary<String, ISource>();
        private ISource _currentSource = null;
        private int _selectedGranularity = -1;

        public DatasetCreator() {
            InitializeComponent();

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

            EndlessProgressDialog dialog = new EndlessProgressDialog();
            if (_currentSource != null) {
                int granularity = _selectedGranularity;
                Asset currentAsset = _currencySelection.SelectedItem as Asset;
                _currentSource.GetPriceHistoryAsync(currentAsset, new DateRange(startDate, endDate), granularity).ContinueWith((Task<DatasetResult> taskResult) => {
                    dialog.Dispatcher.InvokeAsync(() => {
                        DatasetResult result = taskResult.Result;

                        if (result.Succeeded) {
                            JavaScriptSerializer serializer = new JavaScriptSerializer();
                            try {
                                String filePath = _selectedOutput.Text;
                                String dir = Path.GetDirectoryName(filePath);
                                Directory.CreateDirectory(dir);
                                String json = serializer.Serialize(result.Value);
                                json = JsonUtil.PoorMansJsonFormat(json);
                                File.WriteAllText(filePath, json);
                                dialog.Close();
                                Close();

                                // Open a dataset viewer
                                DatasetViewer viewer = new DatasetViewer();
                                viewer.SetDataset(result.Value);
                                viewer.Show();
                            } catch (Exception ex) {
                                dialog.Close();
                                MessageBox.Show("UH OH! Couldnt write the dataset to a file: " + ex.Message);
                            }
                        } else {
                            dialog.Close();
                            MessageBox.Show("UH OH! " + result.ErrorDetails);
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
            SaveFileDialog saveFileDlg = new SaveFileDialog();

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

            // Initialize some defaults
            saveFileDlg.FileName = name + ".dataset";
            saveFileDlg.DefaultExt = "dataset";
            saveFileDlg.Filter = "Dataset Files (*.dataset)|*.dataset";
            saveFileDlg.AddExtension = true;

            // Show it
            Nullable<bool> result = saveFileDlg.ShowDialog();

            if (result.HasValue && result.Value) {
                _selectedOutput.Text = saveFileDlg.FileName;
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
