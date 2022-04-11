using Coinbase;
using Coinbase.Models;
using CryptoRetriever.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Utf8Json;

namespace CryptoRetriever.Source {
    public class CoinbaseSource : ISource {
        public CoinbaseSource() {
            // Nothing to do
        }

        public string GetName() {
            return "Coinbase";
        }

        public String GetNote() {
            return "Note: The granularities for Coinbase are aligned on UTC time boundaries not local time so it's possible the edges of the date range selected will be missing because the UTC time is ahead or behind the local time.";
        }

        /// <summary>
        /// Gets the list of available assets on Coinbase asynchronously.
        /// </summary>
        /// <returns>Retuns a task that can be waited on to get the result of the request. It will either contain a list of Assets or an error.</returns>
        public Task<AssetListResult> GetAssetsAsync() {
            Task<AssetListResult> listResultTask = new Task<AssetListResult>(() => {
                var client = new CoinbaseClient();
                var listResult = new AssetListResult();

                client.Data.GetExchangeRatesAsync().ContinueWith((action) => {
                    Response<ExchangeRates> result = action.Result;
                    if (result.HasError()) {
                        listResult.Succeeded = false;
                        listResult.ErrorDetails = "Server error. ";
                        if (result.Errors != null) {
                            foreach (Error error in result.Errors) {
                                listResult.ErrorDetails += "\n\t" + error.Id + ": " + error.Message;
                            }
                        }
                    } else {
                        // Convert to a list of assets
                        List<Asset> assets = new List<Asset>(result.Data.Rates.Count);
                        String currency = result.Data.Currency;
                        foreach (KeyValuePair<string, decimal> rate in result.Data.Rates) {
                            // The value given by the coinbase API is in eth/dollar. We want dollar/eth (Or whatever the currencies are)
                            decimal value = Decimal.Divide(1, rate.Value);
                            assets.Add(new Asset(rate.Key, value, currency));
                        }

                        // Success
                        listResult.Succeeded = true;
                        listResult.Value = assets;
                    }
                }).Wait();

                return listResult;
            });

            listResultTask.Start();
            return listResultTask;
        }

        public Task<DatasetResult> GetPriceHistoryAsync(Asset asset, DateRange fullRange, int secondsPerSample) {
            Task<DatasetResult> result = new Task<DatasetResult>(() => {
                // The coinbase library being used doesn't support coinbase pro
                // which is the only way to get history. It's no problem though
                // because it's a simple GET request of the following form:
                //    https://api.pro.coinbase.com/products/BTC-USD/candles?start=2021-01-10T12:00:00&end=2021-07-15T12:00:00&granularity=86400

                // According to the API documentation, the response can contain a maximum of 300 candles (datapoints).
                // so we need to split the request up into multiple.
                const int maxSamplesPerRequest = 300; // This is defined by the API
                const double precisionErrorOffset = 0.001; // The range in seconds should always be an integer but add a bit in case of precision error in the double.
                long secondsInRange = (long)((fullRange.End - fullRange.Start).TotalSeconds + precisionErrorOffset);
                long numSamples = secondsInRange / secondsPerSample;
                long numRequests = numSamples / maxSamplesPerRequest;
                if (numSamples % maxSamplesPerRequest > 0)
                    numRequests++;

                // Create an empty dataset (but not null) and result to keep track of the combined result
                DatasetResult combinedDatasetResult = new DatasetResult(new Dataset());
                combinedDatasetResult.Value.Granularity = secondsPerSample;

                // Run a separate request for each piece of the full date range.
                for (int i = 0; i < numRequests; i++) {
                    DateTime start = fullRange.Start.AddSeconds(i * maxSamplesPerRequest * secondsPerSample);
                    DateTime end = start.AddSeconds(maxSamplesPerRequest * secondsPerSample - 1); // -1 because we don't want duplicate samples on the edges of each range

                    if (end > fullRange.End)
                        end = fullRange.End;

                    String startUtc = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
                    String endUtc = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
                    String product = asset.Name.ToUpper() + "-" + asset.Currency.ToUpper();
                    int granularity = secondsPerSample;

                    String url = "https://api.pro.coinbase.com/products/" + product + "/candles?start=" + startUtc + "&end=" + endUtc + "&granularity=" + granularity;
                    DatasetResult datasetResult = new DatasetResult();
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.Method = "GET";
                    request.UserAgent = "CoinbaseClient/1.0";

                    request.GetResponseAsync().ContinueWith((action) => {
                        if (action.IsFaulted) {
                            datasetResult.Succeeded = false;
                            datasetResult.ErrorDetails = action.ToString();
                        } else {
                            Stream webStream = action.Result.GetResponseStream();
                            var reader = new StreamReader(webStream);
                            String data = reader.ReadToEnd();

                            Dataset dataset = ParseCoinbaseJsonToDataset(data);

                            // Fill out the result
                            datasetResult.Succeeded = true;
                            datasetResult.Value = dataset;
                        }
                    }).Wait();

                    if (datasetResult.Succeeded) {
                        // The combined result starts as successful so just add these samples to it.
                        // Since the samples are increasing in time, each add should be O(1). The underlying 
                        // array was presized earlier.
                        combinedDatasetResult.Value.Add(datasetResult.Value);
                    } else {
                        // If any of the subrequests fail, the whole operation is lost. We have
                        // failed our house and our country. We should be ashamed.
                        combinedDatasetResult.Succeeded = false;
                        combinedDatasetResult.ErrorDetails = datasetResult.ErrorDetails;
                    }
                }               

                return combinedDatasetResult;
            });

            result.Start();

            return result;
        }

        public Task<DatasetResult> GetPriceHistoryAsync(Asset asset, DateTime start, int secondsPerSample) {
            DateRange range = new DateRange(start, DateTime.Now);
            return GetPriceHistoryAsync(asset, range, secondsPerSample);
        }

        /// <summary>
        /// Gets the options that can be used for granularity when getting the historic prices for an asset.
        /// </summary>
        /// <returns>A list of ints representing the possible granularities in seconds.</returns>
        public List<int> GetGranularityOptions() {
            var result = new List<int>();

            // The following is taken from the Coinbase Pro documentation:
            //  "The granularity field must be one of the following values: {60, 300, 900, 3600, 21600, 86400}"
            //  https://docs.pro.coinbase.com/#get-historic-rates
            result.AddRange(new int[] { 
                60, 
                300,
                900, 
                3600, 
                21600, 
                86400 });

            return result;
        }

        private Dataset ParseCoinbaseJsonToDataset(String json) {
            // The data is sent back as JSON in the form:
            //
            // [0] (Furthest day)
            //      [0] time bucket start time
            //      [1] low lowest price during the bucket interval
            //      [2] high highest price during the bucket interval
            //      [3] open opening price (first trade) in the bucket interval
            //      [4] close closing price (last trade) in the bucket interval
            //      [5] volume volume of trading activity during the bucket interval
            // [1] (Furthest day + granularity) ...
            //
            // See https://docs.pro.coinbase.com/#get-historic-rates
            const int TimeBucketStartTimeIndex = 0;
            const int LowestPriceIndex = 1;
            const int HighestPriceIndex = 2;
            const int OpeningPriceIndex = 3;
            const int ClosingPriceIndex = 4;
            const int TradingVolumeIndex = 5;

            Dataset dataSet = new Dataset();

            dynamic jsonArr = JsonSerializer.Deserialize<dynamic>(json);
            for (int i = 0; i < jsonArr.Count; i++) {
                double timeBucketStartTime = jsonArr[i][TimeBucketStartTimeIndex];
                double lowestPrice = jsonArr[i][LowestPriceIndex];
                double highestPrice = jsonArr[i][HighestPriceIndex];
                double openingPrice = jsonArr[i][OpeningPriceIndex];
                double closingPrice = jsonArr[i][ClosingPriceIndex];
                double tradingVolume = jsonArr[i][TradingVolumeIndex];

                dataSet.Insert(
                    new Point(
                        timeBucketStartTime, 
                        lowestPrice));
            }

            return dataSet;
        }
    }
}
