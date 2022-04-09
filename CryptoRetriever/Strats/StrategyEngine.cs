using CryptoRetriever.Data;
using CryptoRetriever.Filter;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

/// <summary>
/// A StrategyEngine executes strategies and stores the result.
/// </summary>
namespace CryptoRetriever.Strats {
    public class StrategyEngine {
        public StrategyRuntimeContext RunContext { get; private set; }

        public StrategyEngine(Strategy strategy, Dataset dataset) {
            RunContext = new StrategyRuntimeContext(strategy, dataset);
        }

        /// <summary>
        /// Runs the strategy through the full dataset.
        /// 
        /// // ?? TODO: Respect the start/end datetime in the strategy if set
        /// </summary>
        public void Run() {
            // Run all the filters first
            FilterDataset();

            // Run the whole thing by stepping thru the whole
            // dataset.
            for (int i = 0; i < RunContext.Dataset.Count; i++) {
                Step();
            }
        }

        /// <summary>
        /// Runs all the filters in the strategy on the dataset.
        /// The filters are run in the order they appear in the strategy.
        /// The original dataset is unmodified.
        /// </summary>
        public void FilterDataset() {
            foreach (IFilter filter in RunContext.Strategy.Filters) {
                RunContext.Dataset = filter.Filter(RunContext.Dataset);
            }
        }

        /// <summary>
        /// Move to the next datapoint in the dataset.
        /// </summary>
        public void Step() {
            foreach (Trigger trigger in RunContext.Strategy.Triggers) {
                if (trigger.Condition.IsTrue(RunContext)) {
                    if (trigger.TrueAction != null)
                        trigger.TrueAction.Execute(RunContext);
                } else if (trigger.FalseAction != null) {
                    trigger.FalseAction.Execute(RunContext);
                }
            }

            RunContext.CurrentState = RunContext.NextState;
            RunContext.CurrentDatapointIndex++;
        }
    }

    /// <summary>
    /// This is the data that is available to actions and
    /// variables while a strategy is being run.
    /// </summary>
    public class StrategyRuntimeContext {
        private String _currentState;

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
        /// Returns the current state as controlled by the running strategy.
        /// Note that if this is set by a trigger, it will override the NextState
        /// if one has been set.
        /// </summary>
        public String CurrentState {
            get {
                return _currentState;
            }
            set {
                _currentState = value;
                
                // Set the NextState too so it doesn't flip back
                // if the state is changing while running a set
                // of triggers. This also means if a previous
                // trigger sets the next state, a following
                // trigger setting the current state will override
                // that.
                NextState = _currentState;
            }
        }
        
        /// <summary>
        /// Gets or sets the next state the context will enter after all current
        /// triggers have been run. If the CurrentState is set before this, it will
        /// be overwritten.
        /// </summary>
        public String NextState { get; set; } // The state we will enter after running all the triggers for the current event

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
            Account = strategy.Account.Copy();
            CurrentState = strategy.States[0].GetId();
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
