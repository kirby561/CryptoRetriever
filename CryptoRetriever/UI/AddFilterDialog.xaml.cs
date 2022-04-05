using CryptoRetriever.Filter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Interaction logic for AddFilterDialog.xaml
    /// </summary>
    public partial class AddFilterDialog : Window {
        private ObservableCollection<String> _filterTypes = new ObservableCollection<String>();
        private IFilter _result = null; // Set when the create button is pressed

        public IFilter Filter {
            get {
                return _result;
            }
        }

        public AddFilterDialog() {
            InitializeComponent();

            // Create the filters combo list items
            _filterTypes.Add("Gaussian");
            _filterTypesComboBox.ItemsSource = _filterTypes;
            _filterTypesComboBox.SelectedIndex = 0;
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnCreateButtonClicked(object sender, RoutedEventArgs e) {
            // Get the filter type
            String filterId = _filterTypesComboBox.SelectedValue as String;
            if (String.IsNullOrEmpty(filterId))
                return; // Nothing selected

            switch (filterId) {
                case "Gaussian":
                    String sigmaStr = _sigmaTextBox.Text;
                    String kernelSizeStr = _kernelSizeTextBox.Text;

                    int sigma = -1;
                    if (!Int32.TryParse(sigmaStr, out sigma)) {
                        MessageBox.Show("Sigma must be an integer.");
                        return;
                    }

                    int kernelSize = -1;
                    if (!Int32.TryParse(kernelSizeStr, out kernelSize) || kernelSize <= 0 || (kernelSize % 2 != 1)) {
                        MessageBox.Show("Kernel Size must be an integer > 0 and must be odd.");
                        return;
                    }

                    _result = new GaussianFilter(sigma, kernelSize);

                    break;
                default:
                    MessageBox.Show("Filter not found.");
                    break;
            }

            Close();
        }
    }
}
