using CryptoRetriever.Data;
using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CryptoRetriever.Filter {
    /// <summary>
    /// Averages the past requested number of samples together.
    /// The number of samples is the KernelSize.
    /// </summary>
    public class AverageFilter : IFilter {
        /// <summary>
        /// The number of samples to average together.
        /// They will be averaged on the left side of the current point.
        /// </summary>
        public int KernelSize { get; set; }

        /// <summary>
        /// Returns a string summary of this filter.
        /// </summary>
        public String Summary {
            get {
                return "Average Filter (KernelSize = " + KernelSize + ")";
            }
        }

        /// <summary>
        /// Creates a new AverageFilter with the given kernel size.
        /// </summary>
        /// <param name="kernelSize">The number of samples to average together.</param>
        public AverageFilter(int kernelSize = 9) {
            KernelSize = kernelSize;
        }

        /// <summary>
        /// Filters the given input by taking the average of the previous KernelSize samples.
        /// </summary>
        /// <param name="input">The input to filter.</param>
        /// <returns>Returns a new dataset with the filtered result.</returns>
        public Result<Dataset> Filter(Dataset input) {
            if (input.Count < 1)
                return new Result<Dataset>(new Dataset());

            Dataset result = new Dataset(input.Count);

            // For the first KernelSize samples, average that number together (since there aren't KernelSize samples yet)
            result.Points.Add(new Point(input.Points[0].X, input.Points[0].Y));
            for (int i = 1; i < Math.Min(KernelSize, input.Count); i++) {
                double sum = 0;
                for (int y = 0; y < i; y++)
                    sum += input.Points[y].Y;
                result.Points.Add(new Point(input.Points[i].X, sum / i));
            }

            // For the rest of the samples, take the average of the last KernelSize samples
            // Note that if the number of samples is less than KernelSize this does nothing.
            for (int i = KernelSize; i < input.Count; i++) {
                double sum = 0;
                for (int y = 0; y < KernelSize; y++)
                    sum += input.Points[i - y].Y;
                result.Points.Add(new Point(input.Points[i].X, sum / KernelSize));
            }

            return new Result<Dataset>(result);
        }

        public void FromJson(JsonObject json) {
            KernelSize = json.GetInt("KernelSize");
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Type", "AverageFilter");
            obj.Put("KernelSize", KernelSize);
            return obj;
        }
    }
}
