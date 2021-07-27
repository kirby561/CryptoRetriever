using Coinbase;
using Coinbase.Models;
using StockStratMemes.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

            DateTime startDate = _startDatePicker.SelectedDate.Value;
            DateTime endDate = DateTime.Now;

            if (_endDatePicker.SelectedDate.HasValue) {
                endDate = _endDatePicker.SelectedDate.Value;
            }

            if (_currentSource != null) {
                int granularity = _selectedGranularity;
                Asset currentAsset = _currencySelection.SelectedItem as Asset;
                DataSetResult result =_currentSource.GetPriceHistoryAsync(currentAsset, new DateRange(startDate, endDate), granularity).GetAwaiter().GetResult();
                MessageBox.Show(result.Value.Points.ToArray().ToString());
            }
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
    }
}
