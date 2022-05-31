using CryptoRetriever.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoRetriever.Source {
    /// <summary>
    /// A source represents an interface to something that can provide asset prices for stock or crypto.
    /// The interface must provide basic information like its name, granularity of the data it will return,
    /// the list of assets it can provide price information for, and the price history of an asset itself.
    /// </summary>
    public interface ISource {
        /// <summary>
        /// The name of the source that will be displayed to the user.
        /// </summary>
        /// <returns>The name of the source as a string.</returns>
        String GetName();

        /// <summary>
        /// A note that will be displayed when the source is selected where you can add some details
        /// or caveats about how the data will be returned.
        /// </summary>
        /// <returns>A note to display as a string.</returns>
        String GetNote(); 

        /// <summary>
        /// Gets the available granularities (time between samples) for this source.
        /// </summary>
        /// <returns>Returns a list of granularities in seconds.</returns>
        List<int> GetGranularityOptions();

        /// <summary>
        /// Asynchronously fetches the list of assets for this source from the server.
        /// </summary>
        /// <returns>Returns a task to wait for the result. The task will contain a list of the assets or indicate that the request failed with details.</returns>
        Task<AssetListResult> GetAssetsAsync();

        /// <summary>
        /// Gets the price history of the given asset within the given date range.
        /// </summary>
        /// <param name="asset">The asset to request (see GetAssetsAsync)</param>
        /// <param name="range">A date range to request the assets from in local time.</param>
        /// <param name="secondsPerSample">The number of seconds between samples (Must be one returned from GetGranularityOptions)</param>
        /// <param name="progressListener">An optional listener for getting progress reports. If null, no progress is reported.</param>
        /// <returns>Returns a task that can be waited on for the result. The result will be a dataset of (UTC timestamp, Asset Value) samples.</returns>
        Task<DatasetResult> GetPriceHistoryAsync(Asset asset, DateRange range, int secondsPerSample = 86400, ProgressListener progressListener = null);

        /// <summary>
        /// Gets the price history of the given asset within the given date range.
        /// </summary>
        /// <param name="asset">The asset to request (see GetAssetsAsync)</param>
        /// <param name="start">The start of the range to get samples from. The end of the range will be DateTime.Now.</param>
        /// <param name="secondsPerSample">The number of seconds between samples (Must be one returned from GetGranularityOptions)</param>
        /// <param name="progressListener">An optional listener for getting progress reports. If null, no progress is reported.</param>
        /// <returns>Returns a task that can be waited on for the result. The result will be a dataset of (UTC timestamp, Asset Value) samples.</returns>
        Task<DatasetResult> GetPriceHistoryAsync(Asset asset, DateTime start, int secondsPerSample = 86400, ProgressListener progressListener = null);
    }
}
