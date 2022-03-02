﻿using StockStratMemes.DatasetView;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System;
using StockStratMemes.Source;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StockStratMemes {
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

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".dataset";
            openFileDialog.Filter = "Datasets (.dataset)|*.dataset";

            // Open the dialog
            Nullable<bool> result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result.Value) {
                string filename = openFileDialog.FileName;

                // Check if the dataset file exists
                Result<Dataset> datasetResult = DatasetReader.ReadFile(filename);
                if (datasetResult.Succeeded) {
                    _dataSetViewer = new DatasetViewer();
                    _dataSetViewer.SetDataset(datasetResult.Value);
                    _dataSetViewer.Show();
                } else {
                    MessageBox.Show(datasetResult.ErrorDetails);
                }
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
