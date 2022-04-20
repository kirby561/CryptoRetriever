using CryptoRetriever.Source;
using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public class Account : IJsonable {
        /// <summary>
        /// How much fiat currency is in this account such as USD.
        /// For the purposes of this class, "fiat currency" is just
        /// "currency" and "crypto currency" is an "asset".
        /// </summary>
        public double CurrencyBalance { get; set; }

        /// <summary>
        /// How much of an asset we have in this account such as stock shares or eth.
        /// </summary>
        public double AssetBalance { get; set; }

        public Account() {
            CurrencyBalance = 0;
            AssetBalance = 0;
        }

        public Account(double currency, double assets) {
            CurrencyBalance = currency;
            AssetBalance = assets;
        }

        /// <summary>
        /// Maekes a deep copy of this account.
        /// </summary>
        /// <returns>A new Account instance with the same values.</returns>
        public Account Copy() {
            return new Account(CurrencyBalance, AssetBalance);
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject()
               .Put("CurrencyBalance", CurrencyBalance)
               .Put("AssetBalance", AssetBalance);
            return obj;
        }

        public void FromJson(JsonObject obj) {
            CurrencyBalance = obj.GetDouble("CurrencyBalance");
            AssetBalance = obj.GetDouble("AssetBalance");
        }
    }
}
