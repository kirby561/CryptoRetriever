using CryptoRetriever.Data;
using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Utf8Json;

namespace CryptoRetriever.Filter {
    public interface IFilter : IJsonable {
        /// <summary>
        /// Gets a summary of this filter and its parameters.
        /// </summary>
        /// <returns>Returns the summary.</returns>
        String Summary { get; }

        /// <summary>
        /// Filters the given input and returns a new dataset with the
        /// filtered results.
        /// </summary>
        /// <param name="input">The input to filter.</param>
        /// <returns>A new dataset with the filtered results or an error indicating why it didn't.</returns>
        Result<Dataset> Filter(Dataset input);
    }
}
