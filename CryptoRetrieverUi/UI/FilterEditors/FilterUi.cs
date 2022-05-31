using CryptoRetriever.Filter;
using System;
using System.Collections.Generic;

namespace CryptoRetriever.UI {
    public static class FilterUi {
        public static Dictionary<IFilter, BaseFilterDialog> GetFilterUiMap() {
            var result = new Dictionary<IFilter, BaseFilterDialog>();

            result[new LeftGaussianFilter()] = new LeftGaussianFilterDialog();
            result[new GaussianFilter()] = new GaussianFilterDialog();
            result[new ResamplerFilter(1)] = new ResamplerFilterDialog();
            result[new AverageFilter()] = new AverageFilterDialog();
            result[new DerivativeFilter()] = null; // No options, don't show a dialog

            // Take this opportunity to make sure that all filter types are present
            if (result.Count != Filters.GetFilters().Count)
                throw new ApplicationException("Mismatch between UI dialogs and possible filters.");

            return result;
        }

        /// <summary>
        /// Returns the dialog that should be used to edit the given filter.
        /// </summary>
        /// <param name="filter">The filter to edit.</param>
        /// <returns>Returns the dialog or null if editing does not apply to this filter.</returns>
        public static BaseFilterDialog GetDialogFor(IFilter filter) {
            KeyValuePair<IFilter, BaseFilterDialog> pair = GetDialogByName(filter.GetType().Name).Value;
            return pair.Value;
        }

        /// <summary>
        /// Returns the dialog that should be used to edit the given filter ID.
        /// </summary>
        /// <param name="name">The ID of the filter (The class name)</param>
        /// <returns>Returns the dialog and filter that should be used or null if the type is not found.</returns>
        public static KeyValuePair<IFilter, BaseFilterDialog>? GetDialogByName(String name) {
            Dictionary<IFilter, BaseFilterDialog> map = GetFilterUiMap();
            foreach (KeyValuePair<IFilter, BaseFilterDialog> pair in map) {
                if (pair.Key.GetType().Name.Equals(name))
                    return pair;
            }

            return null;
        }
    }
}
