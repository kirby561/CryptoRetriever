using Coinbase;
using Coinbase.Models;
using StockStratMemes.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StockStratMemes.DataSetView {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DataSetCreator : Window {
        public DataSetCreator() {
            this.InitializeComponent();
        }

        private void OnCreateClicked(object sender, RoutedEventArgs e) {
          
        }

        private void OnSourceCoinbaseSelected(object sender, RoutedEventArgs e) {
            CoinbaseSource source = new CoinbaseSource(SourceType.Static);

            
            AssetListResult result = source.GetAssetsAsync().GetAwaiter().GetResult();
            
            foreach (Asset asset in result.Result) {
                Console.WriteLine(asset.ToString());
            }
        }
    }
}
