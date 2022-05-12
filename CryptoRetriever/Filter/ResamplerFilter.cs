using CryptoRetriever.Data;
using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CryptoRetriever.Filter {
    /// <summary>
    /// A simple resampler that doesn't have a fancy kernel.
    /// It will resample at the given frequency in seconds taking
    /// a linear interpolation of the nearest 2 samples. If the sampling
    /// doesn't line up with the length of the dataset exactly, the nearest
    /// sample will be used so it can be slightly smaller or slightly longer
    /// depending on how close the last sample is to the sampling frequency.
    /// 
    /// The filter does not assume equal spacing in the dataset so the operation
    /// is O(n*log n)
    /// </summary>
    public class ResamplerFilter : IFilter {
        public String Summary {
            get {
                return "Resampler: " + SampleFrequency + " (s)";
            }
        }

        /// <summary>
        /// The sample frequency in seconds.
        /// </summary>
        public long SampleFrequency { get; set; }

        public ResamplerFilter(long sampleFrequencyS) {
            SampleFrequency = sampleFrequencyS;
        }

        public Dataset Filter(Dataset input) {
            // If it's 1 or 0 length, just return a copy
            if (input.Count < 2)
                new Dataset(input);

            double timeLength = input.Points[input.Count - 1].X - input.Points[0].X;
            double startTime = input.Points[0].X;
            int numSamples = (int)Math.Round(timeLength / SampleFrequency) + 1;
            Dataset filteredData = new Dataset(numSamples);
            filteredData.Granularity = SampleFrequency;
            for (int i = 0; i < numSamples; i++) {
                double time = i * SampleFrequency + startTime;
                filteredData.Points.Add(new Point(time, input.ValueAt(time).Result));
            }

            return filteredData;
        }

        public void FromJson(JsonObject json) {
            SampleFrequency = json.GetLong("SampleFrequencyS");
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Type", "ResamplerFilter");
            obj.Put("SampleFrequencyS", SampleFrequency);
            return obj;
        }
    }
}
