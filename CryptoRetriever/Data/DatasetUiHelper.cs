using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CryptoRetriever.Data {
    /// <summary>
    /// Contains helpful methods for showing Dataset UI elements
    /// like the Save As dialog.
    /// </summary>
    public static class DatasetUiHelper {
        /// <summary>
        /// Shows a Save as... dialog for dataset files and returns the selected path.
        /// </summary>
        /// <param name="initialName">The initial name to show in the dialog.</param>
        /// <returns>Returns a result containing the path selected or an error if the user did not select anything.</returns>
        public static Result<String> ShowSaveDatasetAsDialog(String initialName) {
            SaveFileDialog saveFileDlg = new SaveFileDialog();

            // Initialize some defaults
            saveFileDlg.FileName = initialName;
            saveFileDlg.DefaultExt = "dataset";
            saveFileDlg.Filter = "Dataset Files (*.dataset)|*.dataset";
            saveFileDlg.AddExtension = true;

            // Show it
            Nullable<bool> result = saveFileDlg.ShowDialog();

            if (result.HasValue && result.Value) {
                return new Result<String>(saveFileDlg.FileName);
            } else {
                return new Error<String>("No file was selected.");
            }
        }

        /// <summary>
        /// Shows an open dialog for dataset files and returns the selected path.
        /// </summary>
        /// <param name="initialName">The initial name to show in the dialog. This can be null or empty if you just want the default.</param>
        /// <returns>Returns a result contianing the selected path or an error if the user did not select anything.</returns>
        public static Result<String> ShowOpenDatasetDialog(String initialName) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".dataset";
            openFileDialog.Filter = "Datasets (.dataset)|*.dataset";

            if (!String.IsNullOrEmpty(initialName))
                openFileDialog.FileName = initialName;

            // Open the dialog
            Nullable<bool> result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result.HasValue && result.Value) {
                return new Result<String>(openFileDialog.FileName);
            } else {
                return new Error<String>("The user didn't select anything");
            }
        }

        /// <summary>
        /// Attempts to open a dataset by showing the user a dialog to select one
        /// and then reading the file they select. Messages are shown to the user
        /// when errors occur but not when the open dialog is cancelled.
        /// </summary>
        /// <param name="initialName">An initial path to start from.</param>
        /// <returns>Returns the loaded dataset (Item1 of the Tuple) and the path (Item2) or details on why one was not loaded.</returns>
        public static Result<Tuple<Dataset, String>> OpenDatasetWithDialog(String initialName) {
            Result<String> openDialogResult = ShowOpenDatasetDialog(initialName);

            // Process open file dialog box results
            if (openDialogResult.Succeeded) {
                String filePath = openDialogResult.Value;

                // Check if the dataset file exists
                Result<Dataset> datasetResult = DatasetReader.ReadFile(filePath);
                if (datasetResult.Succeeded) {
                    if (datasetResult.Value.Points.Count > 0) {
                        return new Result<Tuple<Dataset, String>>(
                            new Tuple<Dataset, String>(datasetResult.Value, filePath)); // Success
                    } else {
                        MessageBox.Show("The dataset was empty.");
                        return new Error<Tuple<Dataset, String>>("The dataset was empty.");
                    }
                } else {
                    MessageBox.Show("An error occurred reading the dataset: " + datasetResult.ErrorDetails);
                    return new Error<Tuple<Dataset, String>>(datasetResult.ErrorDetails);
                }
            } else {
                // No file selected. This is fine.
                return new Error<Tuple<Dataset, String>>(openDialogResult.ErrorDetails);
            }
        }
    }
}
