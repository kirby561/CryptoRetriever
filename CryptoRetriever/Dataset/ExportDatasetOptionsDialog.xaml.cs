using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CryptoRetriever.DatasetView {
    /// <summary>
    /// Interaction logic for ExportDatasetOptions.xaml
    /// </summary>
    public partial class ExportDatasetOptionsDialog : Window {
        private Dataset _dataset;
        private ExportDatasetOptions _options = new ExportDatasetOptions();

        public ExportDatasetOptionsDialog() {
            InitializeComponent();
        }

        public void SetDataset(Dataset dataset) {
            _dataset = dataset;
        }

        public ExportDatasetOptions GetOptions() {
            return _options;
        }

        private void OnExportClicked(object sender, RoutedEventArgs e) {
            if (String.IsNullOrEmpty(_selectedFilepath.Text)) {
                MessageBox.Show("Select a file path first.");
                return;
            }

            _options.FilePath = _selectedFilepath.Text;

            int formatIndex = _dateFormatComboBox.SelectedIndex;
            _options.DateStringFormat = (ExportDateStringFormat)formatIndex;

            int fileTypeIndex = _fileTypeComboBox.SelectedIndex;
            _options.FileType = (ExportFileType)fileTypeIndex;

            DialogResult = true;
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void OnChooseFilepathButtonClicked(object sender, RoutedEventArgs e) {
            SaveFileDialog saveFileDlg = new SaveFileDialog();

            // Make a default name that is informative
            String name = "export";

            // Initialize some defaults
            String extension = _options.GetFileExtension();
            saveFileDlg.FileName = name + "." + extension;
            saveFileDlg.DefaultExt = extension;
            saveFileDlg.Filter = _options.GetFileDescription() + "(*." + extension + ")|*." + extension; // Example: "Comma Separated Values (*.csv)|*.csv";
            saveFileDlg.AddExtension = true;

            // Show it
            Nullable<bool> result = saveFileDlg.ShowDialog();

            if (result.HasValue && result.Value) {
                _selectedFilepath.Text = saveFileDlg.FileName;
            }
        }
    }
}
