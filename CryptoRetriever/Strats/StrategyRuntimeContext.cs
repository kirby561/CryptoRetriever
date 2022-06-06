using CryptoRetriever.Data;
using System;
using System.Collections.Generic;

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
        /// A list of the user variable values by tick for debugging.
        /// </summary>
        public List<Dictionary<String, IValue>> DebugUserVars = new List<Dictionary<String, IValue>>();

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
        /// <returns>True if the purchase succeeded or false if there was an error. Errors are reported in the Errors list.</returns>
        public bool PurchaseMax() {
            double price = Dataset.Points[CurrentDatapointIndex].Y;
            double maxPurchaseAmount = GetMaxPurchaseAmount(Account, Strategy.ExchangeAssumptions, price);
            return Purchase(maxPurchaseAmount);
        }

        /// <summary>
        /// Purchases the given amount of asset or as much as possible if
        /// the amount is larger than that.
        /// </summary>
        /// <param name="amount">The amount of asset to purchase.</param>
        /// <returns>True if the purchase succeeded or false if there was an error. Errors are reported in the Errors list.</returns>
        public bool Purchase(double amount) {
            // Get the current price
            double price = Dataset.Points[CurrentDatapointIndex].Y;
            double maxPurchaseAmount = GetMaxPurchaseAmount(Account, Strategy.ExchangeAssumptions, price);
            double purchaseAmount = Math.Min(amount, maxPurchaseAmount);
            double fee = Strategy.ExchangeAssumptions.TransactionFee + (Strategy.ExchangeAssumptions.TransactionFeePercentage / 100) * purchaseAmount * price;

            if (purchaseAmount > 0) {
                if (LastTransactionCompleted()) {
                    double purchaseCost = purchaseAmount * price;
                    Transaction transaction = new Transaction(CurrentDatapointIndex, CurrentDateTime, fee, -purchaseCost, purchaseAmount, Account, price);
                    PerformTransaction(transaction);
                    return true;
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

            return false;
        }

        /// <summary>
        /// Sells all of the currently held asset for currency.
        /// </summary>
        /// <returns>True if the sell succeeded or false if there was an error. Errors are reported in the Errors list.</returns>
        public bool SellMax() {
            double price = Dataset.Points[CurrentDatapointIndex].Y;
            double maxSellAmount = GetMaxSellAmount(Account, Strategy.ExchangeAssumptions, price);
            return Sell(maxSellAmount);
        }

        /// <summary>
        /// Sells the given amount of asset or all of it if the amount remaining is less than this.
        /// </summary>
        /// <param name="amount">The amount to sell.</param>
        /// <returns>True if the sell succeeded or false if there was an error. Errors are reported in the Errors list.</returns>
        public bool Sell(double amount) {
            double price = Dataset.Points[CurrentDatapointIndex].Y;
            double maxSellAmount = GetMaxSellAmount(Account, Strategy.ExchangeAssumptions, price);
            double sellAmount = Math.Min(amount, maxSellAmount);
            double fee = Strategy.ExchangeAssumptions.TransactionFee + (Strategy.ExchangeAssumptions.TransactionFeePercentage / 100) * sellAmount * price;

            if (sellAmount > 0) {
                if (LastTransactionCompleted()) {
                    double sellRevenue = sellAmount * price;
                    Transaction transaction = new Transaction(CurrentDatapointIndex, CurrentDateTime, fee, sellRevenue, -sellAmount, Account, price);
                    PerformTransaction(transaction);
                    return true;
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

            return false;
        }

        /// <summary>
        /// Performs the given transaction by modifying the account balance, applying the transaction fees, and recording it.
        /// </summary>
        /// <param name="transaction">The transaction to perform.</param>
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

        /// <summary>
        /// Calculates the amount of an asset you can purchase with the available funds in the given account.
        /// Takes into account the fixed and variable fees in the ExchangeAssumptions as well.
        /// </summary>
        /// <param name="account">The account making the purchase.</param>
        /// <param name="assumptions">The assumptions of the exchange.</param>
        /// <param name="assetPrice">The current price of the asset per unit.</param>
        /// <returns>Returns the amount of the asset that can be purchased (NOT the amount of money that will be spent purchasing it)</returns>
        private double GetMaxPurchaseAmount(Account account, ExchangeAssumptions assumptions, double assetPrice) {
            double moneyAvailable = account.CurrencyBalance;
            double maxPurchaseAmount = (moneyAvailable - assumptions.TransactionFee) / (1 + assumptions.TransactionFeePercentage / 100);
            return maxPurchaseAmount / assetPrice;
        }

        /// <summary>
        /// Calculates the amount of an asset you can sell with the amount available in the given account.
        /// Takes into account the fixed and variable fees in the ExchangeAssumptions as well.
        /// </summary>
        /// <param name="account">The account making the purchase.</param>
        /// <param name="assumptions">The assumptions of the exchange.</param>
        /// <param name="assetPrice">The current price of the asset per unit.</param>
        /// <returns>Returns the amount of the asset that can be sold (NOT the amount of money that will be spent purchasing it)</returns>
        private double GetMaxSellAmount(Account account, ExchangeAssumptions assumptions, double assetPrice) {
            double assetValueAvailable = account.AssetBalance * assetPrice;
            double maxPurchaseAmount = (assetValueAvailable - assumptions.TransactionFee) / (1 + assumptions.TransactionFeePercentage / 100);
            double maxAssetAmount = maxPurchaseAmount / assetPrice;
            return maxAssetAmount;
        }
    }
}
