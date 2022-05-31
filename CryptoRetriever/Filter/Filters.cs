using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Filter {
    /// <summary>
    /// Contains the list of possible filter types.
    /// </summary>
    public class Filters {
        /// <returns>
        /// Returns a map of filter type to a new instance of that filter.
        /// </returns>
        public static Dictionary<String, IFilter> GetFilters() {
            var filters = new Dictionary<String, IFilter>();

            // Filter types
            AddFilter(filters, new AverageFilter());
            AddFilter(filters, new DerivativeFilter());
            AddFilter(filters, new GaussianFilter());
            AddFilter(filters, new LeftGaussianFilter());
            AddFilter(filters, new ResamplerFilter(60));

            return filters;
        }

        private static void AddFilter<T>(Dictionary<String, T> dict, T filter) where T : IFilter {
            dict.Add(filter.GetType().Name, filter);
        }
    }
}
