using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// A Transaction keeps track of the amount of money and
    /// assets transferred for a single purchase or sell for
    /// the associated account.
    /// </summary>
    public class Transaction {
        /// <summary>
        /// The datapoint index of the sample this transaction was made on.
        /// </summary>
        public int DatapointIndex { get; private set; }

        /// <summary>
        /// The exact time the purchase occurred.
        /// </summary>
        public DateTime TransactionTime { get; private set; }

        /// <summary>
        /// The account that made the transaction.
        /// </summary>
        public Account Account { get; private set; }

        /// <summary>
        /// The transaction fee charged to make this trade.
        /// This is in fiat currency and is not included in the CurrencyTransferred property.
        /// </summary>
        public double TransactionFee { get; private set; }

        /// <summary>
        /// The amount of currency that was transferred from the exchange to the account
        /// in this transaction. Note this will be positive for Sells and negative for Buys.
        /// </summary>
        public double CurrencyTransferred { get; private set; }

        /// <summary>
        /// The amount of the asset transferred from the exchange to the account in this
        /// transaction. Note this will be positive for Buys and negative for Sells.
        /// </summary>
        public double AssetTransferred { get; private set; }

        /// <summary>
        /// The amount of currency per 1 asset when this transaction was made.
        /// </summary>
        public double CurrentPrice { get; private set; }

        public Transaction(int datapointIndex, DateTime time, double fee, double currencyTransferred, double assetTransferred, Account account, double currentPrice) {
            DatapointIndex = datapointIndex;
            TransactionTime = time;
            TransactionFee = fee;
            CurrencyTransferred = currencyTransferred;
            AssetTransferred = assetTransferred;
            Account = account;
            CurrentPrice = currentPrice;
        }
    }
}
