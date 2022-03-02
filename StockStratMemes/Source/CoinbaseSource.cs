using Coinbase;
using Coinbase.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;

namespace StockStratMemes.Source {
    public class CoinbaseSource : ISource {
        public CoinbaseSource() {
            // Nothing to do
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

        public Task<DatasetResult> GetPriceHistoryAsync(Asset asset, DateRange range, int secondsPerSample) {
            Task<DatasetResult> result = new Task<DatasetResult>(() => {
                // The coinbase library being used doesn't support coinbase pro
                // which is the only way to get history. It's no problem though
                // because it's a simple GET request of the following form:
                // https://api.pro.coinbase.com/products/BTC-USD/candles?start=2021-01-10T12:00:00&end=2021-07-15T12:00:00&granularity=86400
                                
                // According to the API documentation, the response can contain a maximum of 300 candles (datapoints).
                // so we need to split the request up into multiple.

                String startUtc = range.Start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
                String endUtc = range.End.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
                String product = asset.Name.ToUpper() + "-" + asset.Currency.ToUpper();
                int granularity = secondsPerSample;

                String url = "https://api.pro.coinbase.com/products/" + product + "/candles?start=" + startUtc + "&end=" + endUtc + "&granularity=" + granularity;
                DatasetResult dataSetResult = new DatasetResult();
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                request.UserAgent = "CoinbaseClient/1.0";
                
                request.GetResponseAsync().ContinueWith((action) => {
                    if (action.IsFaulted) {
                        dataSetResult.Succeeded = false;
                        dataSetResult.ErrorDetails = action.ToString();
                    } else {
                        Stream webStream = action.Result.GetResponseStream();
                        var reader = new StreamReader(webStream);
                        String data = reader.ReadToEnd();

                        Dataset dataSet = ParseCoinbaseJsonToDataset(data);

                        // Fill out the result
                        dataSetResult.Succeeded = true;
                        dataSetResult.Value = dataSet;
                    }
                }).Wait();
                
                return dataSetResult;
            });

            result.Start();

            return result;
        }

        public Task<DatasetResult> GetPriceHistoryAsync(Asset asset, DateTime start, int secondsPerSample) {
            DateRange range = new DateRange(start, DateTime.Now);
            return GetPriceHistoryAsync(asset, range, secondsPerSample);
        }
         
        public string GetName() {
            return "Coinbase";
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

            JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
            dynamic jsonArr = jsonSerializer.Deserialize<dynamic>(json);
            for (int i = 0; i < jsonArr.Length; i++) {
                decimal timeBucketStartTime = jsonArr[i][TimeBucketStartTimeIndex];
                decimal lowestPrice = jsonArr[i][LowestPriceIndex];
                decimal highestPrice = jsonArr[i][HighestPriceIndex];
                decimal openingPrice = jsonArr[i][OpeningPriceIndex];
                decimal closingPrice = jsonArr[i][ClosingPriceIndex];
                decimal tradingVolume = jsonArr[i][TradingVolumeIndex];

                dataSet.Insert(
                    new Point(
                        Decimal.ToDouble(timeBucketStartTime), 
                        Decimal.ToDouble(lowestPrice)));
            }

            return dataSet;
        }
    }
}
