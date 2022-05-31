using CryptoRetriever.Filter;
using System.Windows;

namespace CryptoRetriever.UI {
    public abstract class BaseFilterDialog : Window {
        /// <returns>
        /// Returns the resulting filter or null if the dialog
        /// was closed by cancelling or otherwise not saving.
        /// </returns>
        public abstract IFilter GetResult();

        /// <summary>
        /// Sets a filter to work on.
        /// </summary>
        /// <param name="filter">The filter to work on.</param>
        public abstract void SetWorkingFilter(IFilter filter);
    }
}
