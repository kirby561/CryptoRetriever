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
        public StrategyRunParams RunParameters { get; private set; }

        public StrategyEngine(Strategy strategy, Dataset dataset) {
            RunParameters = new StrategyRunParams(strategy, dataset);
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
            for (int i = 0; i < RunParameters.Dataset.Count; i++) {
                Step();
            }
        }

        /// <summary>
        /// Runs all the filters in the strategy on the dataset.
        /// The filters are run in the order they appear in the strategy.
        /// The original dataset is unmodified.
        /// </summary>
        public void FilterDataset() {
            foreach (IFilter filter in RunParameters.Strategy.Filters) {
                RunParameters.Dataset = filter.Filter(RunParameters.Dataset);
            }
        }

        /// <summary>
        /// Move to the next datapoint in the dataset.
        /// </summary>
        public void Step() {
            foreach (Trigger trigger in RunParameters.Strategy.Triggers) {
                if (trigger.Condition.IsTrue()) {
                    if (trigger.TrueAction != null)
                        trigger.TrueAction.Execute();
                } else if (trigger.ElseAction != null) {
                    trigger.ElseAction.Execute();
                }
            }

            RunParameters.CurrentDatapointIndex++;
        }
    }

    /// <summary>
    /// This is the data that is available to actions and
    /// variables while a strategy is being run.
    /// </summary>
    public class StrategyRunParams {
        public Strategy Strategy { get; protected set; }
        public Account Account { get; set; }
        public Dataset Dataset { get; set; }
        public String CurrentState { get; set; }
        public int CurrentDatapointIndex { get; set; } = 0;
        public int NextDatapointIndex {
            get {
                return CurrentDatapointIndex + 1;
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

        public StrategyRunParams(Strategy strategy, Dataset dataset) {
            Strategy = strategy;
            Dataset = dataset;
            Account = strategy.Account.Copy();
            CurrentState = strategy.States[0].GetId();
        }
    }
}
