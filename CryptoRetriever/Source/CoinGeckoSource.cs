using CryptoRetriever.Data;
using CryptoRetriever.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Utf8Json;

namespace CryptoRetriever.Source {
    class CoinGeckoSource : ISource {
        // GoinGecko only supports daily 
        private List<int> _granularityOptions = new int[] { 86400 }.ToList<int>();

        // Keep a queue of request times. If the queue has > 40 requests younger than a minute,
        // chill. This queue should only be accessed on task thread.
        private Queue<long> _requestQueue = new Queue<long>();
        private long _requestsPerMinuteLimit = 40;

        public string GetName() {
            return "CoinGecko";
        }

        public string GetNote() {
            return "Note: Large date ranges can take several minutes due to rate limiting.";
        }

        public List<int> GetGranularityOptions() {
            return _granularityOptions;
        }

        public Task<AssetListResult> GetAssetsAsync() {
            Task<AssetListResult> listResultTask = new Task<AssetListResult>(() => {
                var listResult = new AssetListResult();

                String url = "https://api.coingecko.com/api/v3/coins/list";
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                request.UserAgent = "CoinGeckoClient/1.0";

                CheckRateLimit();
                request.GetResponseAsync().ContinueWith((action) => {
                    if (action.IsFaulted) {
                        listResult.Succeeded = false;
                        listResult.ErrorDetails = action.ToString();
                    } else {
                        Stream webStream = action.Result.GetResponseStream();
                        var reader = new StreamReader(webStream);
                        String data = reader.ReadToEnd();

                        List<Asset> assetList = ParseCoinGeckoJsonToAssetList(data);

                        // Fill out the result
                        listResult.Succeeded = true;
                        listResult.Value = assetList;
                    }
                }).Wait();

                return listResult;
            });

            listResultTask.Start();
            return listResultTask;
        }

        public Task<DatasetResult> GetPriceHistoryAsync(Asset asset, DateRange rangeLocal, int secondsPerSample = 86400, ProgressListener progressListener = null) {
            Task<DatasetResult> result = new Task<DatasetResult>(() => {
                // CoinGecko only supports 1 day at a time
                DatasetResult datasetResult = new DatasetResult();
                DateTime startUtc = rangeLocal.Start.ToUniversalTime();
                DateTime endUtc = rangeLocal.End.ToUniversalTime();
                TimeSpan span = endUtc - startUtc;
                int numDays = span.Days + 1;

                ReportProgress(progressListener, 0, numDays);

                bool succeeded = true;
                String error = "";
                Dataset dataset = new Dataset(numDays);
                for (int i = 0; i < numDays; i++) {
                    CheckRateLimit();

                    DateTime time = startUtc.AddDays(i);
                    String dateStringForDay = time.ToString("dd-MM-yyyy");
                    String currencyId = asset.Id;
                    String url = "https://api.coingecko.com/api/v3/coins/" + currencyId + "/history?date=" + dateStringForDay + "&localization=false";

                    Console.WriteLine("Fetching day " + i + " " + dateStringForDay);
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.Method = "GET";
                    request.UserAgent = "CoinGeckoClient/1.0";
                                        
                    request.GetResponseAsync().ContinueWith((action) => {
                        if (action.IsFaulted) {
                            succeeded = false;
                            error = action.ToString();
                        } else {
                            Stream webStream = action.Result.GetResponseStream();
                            var reader = new StreamReader(webStream);
                            String data = reader.ReadToEnd();

                            double priceUsd = ParseUsdPriceFromJson(data);
                            double timestamp = (time - DateTimeConstant.UnixStart).TotalSeconds;
                            dataset.Insert(new Point(timestamp, priceUsd));
                        }
                    }).Wait();

                    // If any of the requests fail, stop and report the error.
                    if (!succeeded)
                        break;

                    ReportProgress(progressListener, i + 1, numDays);
                }

                if (succeeded) {
                    // Fill out the result
                    datasetResult.Succeeded = true;
                    datasetResult.Value = dataset;
                } else {
                    datasetResult.Succeeded = false;
                    datasetResult.ErrorDetails = error;
                }

                return datasetResult;
            });
            result.Start();
            return result;
        }

        public Task<DatasetResult> GetPriceHistoryAsync(Asset asset, DateTime start, int secondsPerSample = 86400, ProgressListener progressListener = null) {
            DateRange range = new DateRange(start, DateTime.Now);
            return GetPriceHistoryAsync(asset, range, secondsPerSample, progressListener);
        }

        /// <summary>
        /// Converts the given JSON response into a List of Assets derived from the response.
        /// </summary>
        /// <param name="json">
        /// JSON formatted as follows:
        /// [
        ///     {
        ///         "id": "[id]",
        ///         "symbol": "[symbol]",
        ///         "name": "[name]"
        ///     },
        ///     ...
        /// ]
        /// </param>
        /// <returns></returns>
        private List<Asset> ParseCoinGeckoJsonToAssetList(String json) {
            var result = new List<Asset>();

            dynamic jsonArray = JsonSerializer.Deserialize<dynamic>(json);
            for (int i = 0; i < jsonArray.Count; i++) {
                String id = jsonArray[i]["id"];
                String symbol = jsonArray[i]["symbol"];
                String name = jsonArray[i]["name"];
                Asset asset = new Asset(name, -1, symbol, id);
                result.Add(asset);
            }

            return result;
        }

        /// <summary>
        /// Gets the USD price and time from the given json string from the /coins/[id]/history endpoint. The json should be in this format:
        ///     {
        ///         "id": "ravencoin",
        ///         "symbol": "rvn",
        ///         "name": "Ravencoin"
        ///         "image": { ... }
        ///         "market_data": {
        ///             "current_price": {
        ///                 ...
        ///                 "usd": x.yyyy,
        ///                 ...
        ///             },
        ///             "market_cap": { ... },
        ///             "total_valume": { ... }
        ///         },
        ///         "community_data": { ... }
        ///         "developer_data": { ... }
        ///         "public_interest_stats": { ... }
        ///     }
        /// 
        /// See https://www.coingecko.com/en/api/documentation? for more details.
        /// </summary>
        /// <param name="json">A JSON string returned from the history endpoint mentioned above.</param>
        /// <returns>Returns the USD price contained in the json response.</returns>
        private double ParseUsdPriceFromJson(String json) {
            dynamic jsonReader = JsonSerializer.Deserialize<dynamic>(json);
            double price = jsonReader["market_data"]["current_price"]["usd"];

            return price;
        }

        /// <summary>
        /// Waits however long is necessary before the next request to follow
        /// CoinGecko's 50 requests/minute requirement for the free version.
        /// Do not call this on the main thread.
        /// </summary>
        private void CheckRateLimit() {
            long now = DateTime.Now.ToUniversalTime().Ticks;

            // Remove all requests older than a minute
            while (_requestQueue.Count > 0) {
                long oldestTickCount = _requestQueue.Peek();
                if (new TimeSpan(now - oldestTickCount).TotalSeconds > 60.0)
                    _requestQueue.Dequeue();
                else
                    break;
            }

            // Check the queue size
            if (_requestQueue.Count >= _requestsPerMinuteLimit) {
                long oldestTickCount = _requestQueue.Peek();
                int timeToSleep = (int)Math.Round(Math.Max(60 - new TimeSpan(now - oldestTickCount).TotalSeconds + 1, 1.0));
                Thread.Sleep(timeToSleep * 1000);
            }
            _requestQueue.Enqueue(now);
        }

        /// <summary>
        /// Helper method to report progress without needing to null check everywhere.
        /// </summary>
        /// <param name="listener">The listener to report progress to.</param>
        /// <param name="currentProgress">The current progress.</param>
        /// <param name="maxProgress">The max progress.</param>
        private void ReportProgress(ProgressListener listener, long currentProgress, long maxProgress) {
            if (listener != null)
                listener.OnProgress(currentProgress, maxProgress);
        }
    }
}
