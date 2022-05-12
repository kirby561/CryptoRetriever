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
    /// Interaction logic for LeftGaussianFilterDialog.xaml
    /// </summary>
    public partial class LeftGaussianFilterDialog : BaseFilterDialog {
        private IFilter _result = null; // Set when the create button is pressed
        private IFilter _workingFilter;

        public override IFilter GetResult() {
            return _result;
        }

        public override void SetWorkingFilter(IFilter filter) {
            _workingFilter = filter;
        }

        public LeftGaussianFilterDialog() {
            InitializeComponent();
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnSaveButtonClicked(object sender, RoutedEventArgs e) {
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

            _result = new LeftGaussianFilter(sigma, kernelSize);

            Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            if (_workingFilter != null) {
                LeftGaussianFilter filter = (LeftGaussianFilter)_workingFilter;
                _sigmaTextBox.Text = "" + filter.Sigma;
                _kernelSizeTextBox.Text = "" + filter.KernelSize;
            }
        }
    }
}
