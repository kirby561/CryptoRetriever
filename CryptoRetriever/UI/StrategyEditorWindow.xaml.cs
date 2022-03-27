using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CryptoRetriever.Filter;
using CryptoRetriever.Strats;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Allows the user to modify or create a strategy with a UI
    /// </summary>
    public partial class StrategyEditorWindow : Window {
        private Strategy _strategy = new Strategy();

        public StrategyEditorWindow() {
            InitializeComponent();

            _strategy.GetFilters().Add(new GaussianFilter(2));
            _filtersView.ItemsSource = _strategy.GetFilters();
            _strategy.GetFilters().Add(new GaussianFilter(1));
        }
    }
}
