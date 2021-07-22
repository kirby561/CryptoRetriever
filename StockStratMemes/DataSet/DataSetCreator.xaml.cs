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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StockStratMemes.DataSetView {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DataSetCreator : Window {
        private ISource _currentSource = null;

        public DataSetCreator() {
            this.InitializeComponent();
        }

        private void OnCreateClicked(object sender, RoutedEventArgs e) {
          
        }

        private void OnSourceCoinbaseSelected(object sender, RoutedEventArgs e) {
            CoinbaseSource source = new CoinbaseSource(SourceType.Static);
            _currentSource = source;
            _currencySelection.IsEnabled = false;
            Task result = source.GetAssetsAsync().ContinueWith((action) => {
                // While we're here, sort the list if it was successful
                // so we do it off the main thread.
                if (action.Result.Succeeded) {
                    action.Result.Result.Sort();
                }

                Dispatcher.InvokeAsync(() => {
                    if (_currentSource != source) {
                        return; // Old request
                    }

                    AssetListResult listResult = action.Result;

                    if (listResult.Succeeded) {
                        _currencySelection.IsEnabled = true;
                        _currencySelection.ItemsSource = listResult.Result;
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
