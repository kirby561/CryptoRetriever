using CryptoRetriever.Source;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public class Account {
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
    }
}
