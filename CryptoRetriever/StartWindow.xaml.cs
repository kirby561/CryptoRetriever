using CryptoRetriever.DatasetView;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System;
using CryptoRetriever.Source;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoRetriever {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartWindow : Window {
        private static Color _buttonHoverColor = Color.FromArgb(0xff, 0x34, 0x65, 0xa4);
        private static SolidColorBrush _buttonHoverBackground = new SolidColorBrush(_buttonHoverColor);
        private static Color _buttonColor = Color.FromArgb(0xff, 0x72, 0x9f, 0xcf);
        private static SolidColorBrush _buttonBackground = new SolidColorBrush(_buttonColor);
        private static Color _buttonPressedColor = Color.FromArgb(0xff, 0xad, 0xd8, 0xe6);
        private static SolidColorBrush _buttonPressedBackground = new SolidColorBrush(_buttonPressedColor);

        private DatasetViewer _dataSetViewer;
        private Window _dataSetCreator;

        public StartWindow() {
            InitializeComponent();
        }

        
        private void OnButtonPointerEntered(object sender, MouseEventArgs e) {
            Panel button = sender as Panel;
            button.Background = _buttonHoverBackground;
        }

        private void OnButtonPointerLeft(object sender, MouseEventArgs e) {
            Panel button = sender as Panel;
            button.Background = _buttonBackground;
        }

        private void OnCreateDatasetPressed(object sender, MouseButtonEventArgs e) {
            _createDatasetButton.Background = _buttonPressedBackground;
        }

        private void OnCreateDatasetReleased(object sender, MouseButtonEventArgs e) {
            _createDatasetButton.Background = _buttonBackground;
            _dataSetCreator = new DatasetCreator();
            _dataSetCreator.Show();
        }

        private void OnViewDatasetButtonPressed(object sender, MouseButtonEventArgs e) {
            _viewDatasetButton.Background = _buttonPressedBackground;

            Result<Tuple<Dataset, String>> result = DatasetUiHelper.OpenDatasetWithDialog("");
            if (result.Succeeded) {
                _dataSetViewer = new DatasetViewer();
                _dataSetViewer.SetDataset(result.Value.Item2, result.Value.Item1);
                _dataSetViewer.Show();
            }
        }

        private void OnViewDatasetButtonReleased(object sender, MouseButtonEventArgs e) {
            _viewDatasetButton.Background = _buttonBackground;
        }

        private void OnExitButtonPressed(object sender, MouseButtonEventArgs e) {
            _quitButton.Background = _buttonPressedBackground;
        }

        private void OnExitButtonReleased(object sender, MouseButtonEventArgs e) {
            _quitButton.Background = _buttonBackground;
            Application.Current.Shutdown();
        }
    }
}
