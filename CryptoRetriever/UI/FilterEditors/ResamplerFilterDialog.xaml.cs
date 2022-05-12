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
    /// A dialog for creating or editing filters.
    /// </summary>
    public partial class ResamplerFilterDialog : BaseFilterDialog {
        private IFilter _result = null; // Set when the create button is pressed
        private IFilter _workingFilter;

        public override IFilter GetResult() {
            return _result;
        }

        public override void SetWorkingFilter(IFilter filter) {
            _workingFilter = filter;
        }

        public ResamplerFilterDialog() {
            InitializeComponent();
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnSaveButtonClicked(object sender, RoutedEventArgs e) {
            long sampleFrequencySeconds = -1;
            if (!long.TryParse(_sampleFrequencySeconds.Text, out sampleFrequencySeconds) || sampleFrequencySeconds <= 0) {
                MessageBox.Show("Sample frequency must be a 64-bit integer > 0.");
                return;
            }

            _result = new ResamplerFilter(sampleFrequencySeconds);

            Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            if (_workingFilter != null) {
                ResamplerFilter filter = (ResamplerFilter)_workingFilter;
                _sampleFrequencySeconds.Text = filter.SampleFrequency + "";
            }
        }
    }
}
