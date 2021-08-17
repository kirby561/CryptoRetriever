using Coinbase;
using Coinbase.Models;
using Microsoft.Win32;
using StockStratMemes.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StockStratMemes.DataSetView {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DataSetCreator : Window {

        private Dictionary<String, ISource> _sources = new Dictionary<String, ISource>();
        private ISource _currentSource = null;
        private int _selectedGranularity = -1;

        public DataSetCreator() {
            InitializeComponent();

            // Add all the sources
            CoinbaseSource coinbaseSource = new CoinbaseSource();
            _sources[coinbaseSource.GetName()] = coinbaseSource;

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
            
            foreach (int granularity in _currentSource.GetGranularityOptions()) {
                RadioButton granularityButton = new RadioButton();
                granularityButton.Content = "" + granularity;
                granularityButton.Template = Resources["GranularitySelectionTemplate"] as ControlTemplate;
                _granularityPanel.Children.Add(granularityButton);
            }
        }

        private void OnCreateClicked(object sender, RoutedEventArgs e) {
            //MessageBox.Show("" + DataSet.Test());

            if (!_startDatePicker.SelectedDate.HasValue) {
                MessageBox.Show("Please select a start date.");
                return;
            }

            if (String.IsNullOrEmpty(_selectedOutput.Text)) {
                MessageBox.Show("Please select an output file.");
                return;
            }

            DateTime startDate = _startDatePicker.SelectedDate.Value;
            DateTime endDate = DateTime.Now;

            if (_endDatePicker.SelectedDate.HasValue) {
                endDate = _endDatePicker.SelectedDate.Value;
            }

            EndlessProgressDialog dialog = new EndlessProgressDialog();
            if (_currentSource != null) {
                int granularity = _selectedGranularity;
                Asset currentAsset = _currencySelection.SelectedItem as Asset;
                _currentSource.GetPriceHistoryAsync(currentAsset, new DateRange(startDate, endDate), granularity).ContinueWith((Task<DataSetResult> taskResult) => {
                    dialog.Dispatcher.InvokeAsync(() => {
                        DataSetResult result = taskResult.Result;

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
            RadioButton element = sender as RadioButton;
            String selectedGranularityStr = (element.Content as TextBlock).Text;
            int granularityInSeconds = int.Parse(selectedGranularityStr);
            _selectedGranularity = granularityInSeconds;
        }

        private void OnSourceSelected(object sender, RoutedEventArgs e) {
            RadioButton element = sender as RadioButton;
            String sourceName = (element.Content as TextBlock).Text;
            
            ISource source = _sources[sourceName];
            _currentSource = source;
            _currencySelection.IsEnabled = false;
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
    }
}
