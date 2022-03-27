using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strategy {
    /// <summary>
    /// A Transaction keeps track of the amount of money and
    /// assets transferred for a single purchase or sell for
    /// the associated account.
    /// </summary>
    public class Transaction {
        /// <summary>
        /// The account that made the transaction.
        /// </summary>
        public Account Account { get; set; }

        /// <summary>
        /// The amount of currency that was transferred from the account to the exchange
        /// in this transaction. Note this will be negative for Sells and positive for Buys.
        /// </summary>
        public double CurrencyTransferred { get; set; }

        /// <summary>
        /// The amount of the asset transferred from the account to the exchange in this
        /// transaction. Note this will be negative for Buys and positive for Sells.
        /// </summary>
        public double AssetTransferred { get; set; }
    }
}
