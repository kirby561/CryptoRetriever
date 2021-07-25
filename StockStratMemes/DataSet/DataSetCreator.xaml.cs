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

        private void OnCreateClicked(object sender, RoutedEventArgs e) {
            //MessageBox.Show("" + DataSet.Test());
            if (_currentSource != null) {
                Asset currentAsset = _currencySelection.SelectedItem as Asset;
                _currentSource.GetPriceHistoryAsync(currentAsset, DateTime.Now - TimeSpan.FromDays(7));
            }
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
        }
    }
}
