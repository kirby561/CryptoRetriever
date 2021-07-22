using Coinbase;
using Coinbase.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StockStratMemes.Source {
    public class CoinbaseSource : ISource {
        private SourceType _type; // Unused for now.

        public CoinbaseSource(SourceType type) {
            _type = type;
        }

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
                        listResult.Result = assets;
                    }
                }).Wait();

                return listResult;
            });

            listResultTask.Start();
            return listResultTask;
        }

        public string GetName() {
            return "Coinbase";
        }

        SourceType ISource.GetType() {
            return _type;
        }
    }
}
