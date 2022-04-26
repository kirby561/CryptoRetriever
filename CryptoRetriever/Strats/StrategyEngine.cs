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
                RunContext.FilteredDataset = filter.Filter(RunContext.FilteredDataset);
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

            RunContext.CurrentDatapointIndex++;
        }
    }
}
