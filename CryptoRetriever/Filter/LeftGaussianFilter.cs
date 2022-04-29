﻿using CryptoRetriever.Data;
using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

namespace CryptoRetriever.Filter {
    /// <summary>
    /// Gaussian filters can be used to remove higher frequency information
    /// from a dataset (aka blur it). This can be useful for visualizing
    /// the broader trends in the set. The LeftGaussian only applies the
    /// filter using the current and past datapoints so that it can be run
    /// to smooth realtime data without requiring future information and 
    /// without lagging.
    /// </summary>
    public class LeftGaussianFilter : IFilter {
        // Constants useful for below
        private double _oneOverSqrt2Pi = 1 / Math.Sqrt(2 * Math.PI);
        private double _oneOverSigma;
        private double _twoSigmaSquared;
        private double _oneOverSigmaTimesOneOverSqrt2Pi;

        /// <summary>
        /// Returns a string summary of this filter.
        /// </summary>
        public String Summary {
            get {
                return "Left Gaussian Filter (sigma = " + Sigma + ", kernelSize = " + KernelSize + ")";
            }
        }

        /// <summary>
        /// The number of samples to use in each step.
        /// This must be odd.
        /// </summary>
        public int KernelSize { get; set; }

        /// <summary>
        /// The standard deviation to use.
        /// </summary>
        private double _sigma;
        public double Sigma {
            get {
                return _sigma;
            }
            set {
                _sigma = value;

                // Some handywork for later
                _oneOverSigma = 1 / Sigma;
                _twoSigmaSquared = 2 * Sigma * Sigma;
                _oneOverSigmaTimesOneOverSqrt2Pi = _oneOverSigma * _oneOverSqrt2Pi;
            }
        }

        /// <summary>
        /// Creates a new GaussianFilter with the given sigma value.
        /// </summary>
        /// <param name="sigma">The standard deviation to use.</param>
        /// <param name="kernelSize">
        ///     The number of samples to take into account when applying the filter.
        ///     Must be odd!
        ///     The samples to the left of 0 and right of the end will be repeated.
        ///     Note that if the samples are not evenly spaced, a resampler can be
        ///     used prior to this filter to make it even, however the dt between 
        ///     samples will still be taken into account when calculating the gaussian
        ///     function. For example, if the points in the dataset are the following:
        ///         [0] = (1, 1)
        ///         [1] = (5, 4)
        ///         [2] = (6, 3)
        ///      The (6, 3) sample will have more weight than the (1, 1) sample when calculating
        ///      the gaussian function at index 1 since 6 is closer to 5 than 1 is.
        /// </param>
        public LeftGaussianFilter(double sigma = 1, int kernelSize = 9) {
            Sigma = sigma;
            KernelSize = kernelSize;

            if (kernelSize % 2 == 0)
                throw new ArgumentException("The kernel size must be odd.");
        }

        public Dataset Filter(Dataset input) {
            // If we have 2 or less points, just return the dataset.
            if (input.Count < 3) {
                return new Dataset(input);
            }

            Tuple<bool, double> isEvenlySpacedAndSpacing = IsEvenlySpaced(input);
            bool isEvenlySpaced = isEvenlySpacedAndSpacing.Item1;
            double spacing = isEvenlySpacedAndSpacing.Item2;
            if (!isEvenlySpaced)
                throw new ArgumentException("Non-evenly spaced data is not supported yet.");

            Dataset output = new Dataset(input);
            double[] kernel = new double[KernelSize];
            ComputeKernelFixedSpacing(KernelSize, kernel);
            for (int i = 0; i < input.Count; i++) {
                ApplyKernel(kernel, input, output, i);
            }

            return output;
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Type", "LeftGaussianFilter");
            obj.Put("KernelSize", KernelSize);
            obj.Put("Sigma", Sigma);
            return obj;
        }

        public void FromJson(JsonObject json) {
            KernelSize = json.GetInt("KernelSize");
            Sigma = json.GetDouble("Sigma");
        }

        private void ApplyKernel(double[] kernel, Dataset input, Dataset output, int index) {
            double value = 0;
            for (int i = 0; i < KernelSize; i++) {
                int inputIndex = index - i;
                int kernelIndex = KernelSize - i - 1;
                if (inputIndex < 0) {
                    // If we're before the first sample in the kernel,
                    // just repeat the first one.
                    value += kernel[kernelIndex] * input.Points[0].Y;
                } else {
                    value += kernel[kernelIndex] * input.Points[inputIndex].Y;
                }
            }

            // Set the output
            output.Points[index] = new Point(input.Points[index].X, value);
        }

        private void ComputeKernelFixedSpacing(int kernelSize, double[] kernelOutput) {
            // TODO: The timescale should be calculated based on sigma
            //      to be the x value corresponding with a 99% drop or something.
            double timeScale = 2.7 / kernelSize / 2; 
            double[] gaussianAtEachDt = new double[kernelSize];
            for (int i = 0; i < kernelSize; i++) {
                // We can use the positive side of
                // the gaussian because it's symmetric
                int dt = kernelSize - i - 1;
                double gaussian = GaussianAt(dt * timeScale);
                gaussianAtEachDt[i] = gaussian;
            }

            double sum = 0;
            for (int i = 0; i < kernelSize; i++)
                sum += gaussianAtEachDt[i];

            // Assign weights that sum to 1 based on the 
            // magnitude of the gaussian at each point.
            for (int i = 0; i < kernelSize; i++)
                kernelOutput[i] = gaussianAtEachDt[i] / sum;
        }

        private void ComputeKernelDynamicSpacing(int kernelSize, int index, Dataset dataset, double[] kernelOutput) {
            // ?? TODO
        }

        /// <summary>
        /// Checks if the dataset is evenly spaced. If it is, the second return param
        /// will be the approximate spacing. Assumes the dataset is at least 2 items.
        /// </summary>
        /// <param name="dataset">The dataset to check.</param>
        /// <param name="allowedPercentDifference">The allowed percent difference from the average to be considered even. Defaults to 0.1%</param>
        /// <returns>A pair with the first being true if the dataset is evenly spaced and if so the second being the amount it is spaced by.</returns>
        private Tuple<bool, double> IsEvenlySpaced(Dataset dataset, double allowedPercentDifference = 0.1) {
            double totalSpacing = 0;
            int numSpaces = dataset.Count - 1;
            double previousX = dataset.Points[0].X;
            double largestSpace = Double.MinValue;
            double smallestSpace = Double.MaxValue;
            for (int i = 1; i < dataset.Count; i++) {
                double currentX = dataset.Points[i].X;
                double spacing = (currentX - previousX);
                totalSpacing += spacing;
                previousX = currentX;

                if (largestSpace < spacing)
                    largestSpace = spacing;
                if (smallestSpace > spacing)
                    smallestSpace = spacing;
            }

            // Check if the percent difference from the average
            // to the smallest and largest spaces are greater than
            // the allowed spacing.
            double averageSpacing = totalSpacing / numSpaces;
            if (100 * ((largestSpace - averageSpacing) / averageSpacing) > allowedPercentDifference)
                return new Tuple<bool, double>(false, averageSpacing);
            if (100 * ((averageSpacing - smallestSpace) / averageSpacing) > allowedPercentDifference)
                return new Tuple<bool, double>(false, averageSpacing);

            // We're evenly spaced within the tolerance.
            return new Tuple<bool, double>(true, averageSpacing);
        }

        /// <summary>
        /// Calculates the gaussian function at the given delta time.
        /// The gaussian function (standard deviation form) is:
        ///     g(t) = [1 / (sqrt(2pi)s)] * e^(-t^2 / (2s^2))
        ///     
        ///     Where t = the time (dt)
        ///           s = sigma
        /// </summary>
        /// <param name="dt">The time from the center of the curve</param>
        /// <returns>Returns the answer at that time.</returns>
        private double GaussianAt(double dt) {
            return _oneOverSigmaTimesOneOverSqrt2Pi * Math.Pow(Math.E, -(dt * dt) / _twoSigmaSquared);
        }
    }
}
