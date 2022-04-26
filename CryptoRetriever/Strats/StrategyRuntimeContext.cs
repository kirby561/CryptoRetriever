using CryptoRetriever.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// This is the data that is available to actions and
    /// variables while a strategy is being run.
    /// </summary>
    public class StrategyRuntimeContext {
        /// <summary>
        /// Gets the list of transactions that were made while running
        /// a strategy.
        /// </summary>
        public List<Transaction> Transactions { get; protected set; } = new List<Transaction>();

        /// <summary>
        /// Gets a list of errors that occurred while running a Strategy
        /// </summary>
        public List<StrategyError> Errors { get; protected set; } = new List<StrategyError>();

        /// <summary>
        /// Gets the Strategy being run.
        /// </summary>
        public Strategy Strategy { get; protected set; }

        /// <summary>
        /// Gets the state of the Account at any point during running the strategy.
        /// </summary>
        public Account Account { get; protected set; }

        /// <summary>
        /// Gets the dataset being used.
        /// </summary>
        public Dataset Dataset { get; set; }

        /// <summary>
        /// Gets the filtered dataset (after all filters have been applied).
        /// The original dataset is returned if the dataset has not been filtered.
        /// </summary>
        public Dataset FilteredDataset { get; set; }

        /// <summary>
        /// A dictionary of user variables and their current values.
        /// The dictionary is indexed by Variable Name.
        /// </summary>
        public Dictionary<String, IValue> UserVars { get; protected set; }

        /// <summary>
        /// The current datapoint index in Dataset.
        /// </summary>
        public int CurrentDatapointIndex { get; set; } = 0;

        /// <summary>
        /// Gets the next datapoint index in Dataset.
        /// </summary>
        public int NextDatapointIndex {
            get {
                return CurrentDatapointIndex + 1;
            }
        }

        /// <summary>
        /// Gets the current DateTime in the dataset being looked at.
        /// </summary>
        public DateTime CurrentDateTime {
            get {
                return DateTime.UnixEpoch + TimeSpan.FromSeconds(Dataset.Points[CurrentDatapointIndex].X);
            }
        }

        /// <returns>
        /// Returns the timestamp (seconds since 1/1/1970) of the
        /// current datapoint in the dataset.
        /// </returns>
        public double GetCurrentTimestamp() {
            if (Dataset.Count <= CurrentDatapointIndex)
                return Dataset.Points[Dataset.Count - 1].X;
            return Dataset.Points[CurrentDatapointIndex].X;
        }

        /// <summary>
        /// Purchases as much of the asset as possible with 
        /// the current amount of currency in the account.
        /// </summary>
        public void PurchaseMax() {
            // Get the current price
            double fee = Strategy.ExchangeAssumptions.TransactionFee;
            double price = Dataset.Points[CurrentDatapointIndex].Y;
            double maxPurchaseAmount = (Account.CurrencyBalance - fee) / price;

            if (maxPurchaseAmount > 0) {
                if (LastTransactionCompleted()) {
                    double purchaseCost = maxPurchaseAmount * price;
                    Transaction transaction = new Transaction(CurrentDateTime, fee, -purchaseCost, maxPurchaseAmount, Account);
                    PerformTransaction(transaction);
                } else {
                    Errors.Add(new StrategyError(
                        StrategyErrorCode.NotEnoughTimeSinceLastTransaction,
                        "A purchase was made before the minimum transaction time has passed (See ExchangeAssumptions.TransactionTimeS: " + Strategy.ExchangeAssumptions.TransactionTimeS + ")."));
                }
            } else {
                Errors.Add(new StrategyError(
                    StrategyErrorCode.NotEnoughMoneyToMakePurchase,
                    "Max purchase amount was " + maxPurchaseAmount + " so there was not enough money in the account to make a purchase."));
            }
        }

        /// <summary>
        /// Sells all of the currently held asset for currency.
        /// </summary>
        public void SellMax() {
            double fee = Strategy.ExchangeAssumptions.TransactionFee;
            double price = Dataset.Points[CurrentDatapointIndex].Y;
            double maxSellAmount = Account.AssetBalance - (fee / price);

            if (maxSellAmount > 0) {
                if (LastTransactionCompleted()) {
                    double sellRevenue = maxSellAmount * price;
                    Transaction transaction = new Transaction(CurrentDateTime, fee, sellRevenue, -maxSellAmount, Account);
                    PerformTransaction(transaction);
                } else {
                    Errors.Add(new StrategyError(
                        StrategyErrorCode.NotEnoughTimeSinceLastTransaction,
                        "A purchase was made before the minimum transaction time has passed (See ExchangeAssumptions.TransactionTimeS: " + Strategy.ExchangeAssumptions.TransactionTimeS + ")."));
                }
            } else {
                Errors.Add(new StrategyError(
                    StrategyErrorCode.NotEnoughMoneyToMakePurchase,
                    "Max sell amount was " + maxSellAmount + " so there was not enough assets to be worth more than the transaction fee."));
            }
        }

        public void PerformTransaction(Transaction transaction) {
            transaction.Account.CurrencyBalance += transaction.CurrencyTransferred;
            transaction.Account.CurrencyBalance -= transaction.TransactionFee;
            transaction.Account.AssetBalance += transaction.AssetTransferred;
            Transactions.Add(transaction);
        }

        public StrategyRuntimeContext(Strategy strategy, Dataset dataset) {
            Strategy = strategy;
            Dataset = dataset;
            FilteredDataset = dataset;
            Account = strategy.Account.Copy();

            UserVars = new Dictionary<String, IValue>();
            foreach (IValue userVar in strategy.UserVars) {
                IUserVariable var = (IUserVariable)userVar;
                UserVars.Add(var.GetVariableName(), var.CreateInstance());
            }
        }

        /// <returns>
        /// Returns true if enough time has passed since the last transaction to
        /// satisfy the transaction time constraint in the ExchangeAssumptions.
        /// </returns>
        private bool LastTransactionCompleted() {
            if (Transactions.Count == 0)
                return true; // No transactiosn so no transaction minimum time

            TimeSpan timeElapsed = CurrentDateTime - Transactions[Transactions.Count - 1].TransactionTime;
            TimeSpan minimumTimespan = TimeSpan.FromSeconds(Strategy.ExchangeAssumptions.TransactionTimeS);

            return timeElapsed > minimumTimespan;
        }
    }
}
