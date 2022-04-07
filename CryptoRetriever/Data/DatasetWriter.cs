using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CryptoRetriever.Data {
    /// <summary>
    /// Responsible for writing datasets to files.
    /// </summary>
    public class DatasetWriter {
        /// <summary>
        /// Writes the given Dataset to a file as JSON.
        /// </summary>
        /// <param name="dataset">The dataset to write.</param>
        /// <param name="filePath">The file path to write it to.</param>
        /// <returns>Returns a result indicating if it was successful and containing details on what happened if not.</returns>
        public Result WriteFile(Dataset dataset, String filePath) {
            try {
                String json = JsonConvert.SerializeObject(dataset, Formatting.Indented);
                File.WriteAllText(filePath, json);
            } catch (Exception ex) {
                return new Result("Failed to write the file. Exception: " + ex.Message);
            }
            return new Result();
        }
    }
}
