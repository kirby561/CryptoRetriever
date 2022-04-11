using CryptoRetriever.Source;
using System;
using System.Collections.Generic;
using System.IO;
using Utf8Json;

namespace CryptoRetriever.Data {
    class DatasetReader {
        /// <summary>
        /// Reads the given dataset file and returns it as a Dataset object if successful.
        /// </summary>
        /// <param name="filename">A path to a json dataset file.</param>
        /// <returns>Returns a result indicating if the operation was successful or not. If not, check the description for what went wrong.</returns>
        public static Result<Dataset> ReadFile(String filename) {
            // Try to read the file.
            String datasetText = null;
            try {
                datasetText = File.ReadAllText(filename);
            } catch (Exception ex) {
                return new Error<Dataset>("There was an error opening the dataset. Exception: " + ex.Message);
            }

            if (String.IsNullOrEmpty(datasetText)) {
                return new Error<Dataset>("The dataset is empty.");
            }

            Dataset dataset = null;
            try {
                dataset = JsonSerializer.Deserialize<Dataset>(datasetText);
            } catch (Exception ex) {
                return new Error<Dataset>("There was an error in the formatting of the dataset. Exception: " + ex.Message);
            }

            if (dataset == null)
                return new Error<Dataset>("There was an error in the formatting of the dataset.");
            return new Result<Dataset>(dataset);
        }
    }
}
