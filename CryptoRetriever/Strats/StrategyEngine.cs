using CryptoRetriever.Data;
using CryptoRetriever.Filter;
using CryptoRetriever.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;

/// <summary>
/// A StrategyEngine executes strategies and stores the result.
/// </summary>
namespace CryptoRetriever.Strats {
    public class StrategyEngine {
        private Strategy _strategy;
        private Dataset _originalDataset;
        private Dataset _filteredDataset;

        private bool _isDebugEnabled = false;

        // Used to keep track of how many tasks are still running
        private object _runContextLock = new object();
        private int _taskCompleteCount = 0;

        /// <summary>
        /// The context with the highest value (if there are VariableRunners) is placed
        /// here after execution is complete.
        /// </summary>
        public StrategyRuntimeContext RunContext { get; private set; }

        /// <summary>
        /// Errors that occur outside of the context go here and 
        /// are combined with the context ones afterwards.
        /// </summary>
        private List<StrategyError> Errors { get; } = new List<StrategyError>();

        public StrategyEngine(Strategy strategy, Dataset dataset) {
            _strategy = strategy;
            _originalDataset = dataset;
        }

        /// <summary>
        /// Runs the strategy through the full dataset.
        /// 
        /// // ?? TODO: Respect the start/end datetime in the strategy if set
        /// </summary>
        public void Run() {
            // Run all the filters first
            FilterDataset(_strategy.Filters);

            // Run the whole thing by stepping thru the whole
            // dataset.
            if (_strategy.VariableRunners.Count > 0) {
                LinkedList<VariableRunner> runners = new LinkedList<VariableRunner>();
                foreach (VariableRunner runner in _strategy.VariableRunners)
                    runners.AddFirst(runner);

                TaskCounter taskCounter = new TaskCounter();
                PopRunnersAndRunIterations(runners, new Dictionary<String, double>(), taskCounter);

                // Wait for all of them to complete
                lock (_runContextLock) {
                    while (_taskCompleteCount < taskCounter.TaskCount) {
                        Monitor.Wait(_runContextLock);
                    }
                }
            } else {
                StrategyRuntimeContext workingRunContext = new StrategyRuntimeContext(_strategy, _originalDataset);
                workingRunContext.FilteredDataset = _filteredDataset;
                RunIteration(workingRunContext, new Dictionary<String, double>());
                RunContext = workingRunContext;
            }

            // Combine the errors the engine threw with the ones in the context (if any)
            RunContext.Errors.AddRange(Errors);
        }

        /// <summary>
        /// Stacks for loops for each variable in remainingRunners and calls RunIteration for each combination of those variables.
        /// </summary>
        /// <param name="remainingRunners">The list of variables to run.</param>
        /// <param name="runnerValues">A dictionary to keep track of the current value of each. Provide a new dictionary to start.</param>
        private void PopRunnersAndRunIterations(LinkedList<VariableRunner> remainingRunners, Dictionary<String, double> runnerValues, TaskCounter taskCounter) {
            VariableRunner runner = remainingRunners.First.Value;
            remainingRunners.RemoveFirst();
            for (double val = runner.Start; val <= runner.End; val += runner.Step) {
                runnerValues[runner.Variable.GetVariableName()] = val;
                if (remainingRunners.Count > 0) {
                    PopRunnersAndRunIterations(CloneRunners(remainingRunners), runnerValues, taskCounter);
                } else {
                    taskCounter.Increment();
                    Dictionary<String, double> clonedValues = new Dictionary<String, double>();
                    foreach (KeyValuePair<String, double> pair in runnerValues)
                        clonedValues[pair.Key] = pair.Value;
                    ThreadPool.QueueUserWorkItem(o => {
                        StrategyRuntimeContext workingRunContext = new StrategyRuntimeContext(_strategy, _originalDataset);
                        workingRunContext.FilteredDataset = _filteredDataset;
                        workingRunContext.UserVars[runner.Variable.GetVariableName()].SetValueFromString(workingRunContext, "" + val);
                        RunIteration(workingRunContext, clonedValues);

                        // Check if this scenario was better than the previous best and replace it if so.
                        double accountOrOptimizationValue = GetOptimizationValueFor(workingRunContext);
                        lock (_runContextLock) {
                            if (RunContext == null || accountOrOptimizationValue > GetOptimizationValueFor(RunContext)) {
                                RunContext = workingRunContext;
                            }
                            _taskCompleteCount++;
                            Monitor.PulseAll(_runContextLock);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Strategies allow the user to optimize a specific variable instead of the overall account value.
        /// This method checks if one is set and returns the value of it for the given context if so. Otherwise
        /// it returns the account value of the given context.
        /// </summary>
        /// <param name="workingRunContext">The context to check.</param>
        /// <returns>Returns the value of the optimization variable or the value of the account if it is not set.</returns>
        private double GetOptimizationValueFor(StrategyRuntimeContext workingRunContext) {
            // If the user specified a specific variable to optimize, get the value of that
            if (workingRunContext.Strategy.OptimizationVariable != null) {
                return ((INumberValue)workingRunContext.UserVars[workingRunContext.Strategy.OptimizationVariable.GetVariableName()]).GetValue(workingRunContext);
            } else {
                // Otherwise just use the value of the account
                double accountValue = ValueOf(workingRunContext.Account, workingRunContext.Dataset.Points[workingRunContext.CurrentDatapointIndex - 1].Y);
                return accountValue;
            }
        }

        /// <summary>
        /// Creates a deep copy of the given list of VariableRunners.
        /// </summary>
        /// <param name="runners">The list to copy.</param>
        /// <returns>Returns a deep copy of the given list of VariableRunners.</returns>
        private LinkedList<VariableRunner> CloneRunners(LinkedList<VariableRunner> runners) {
            LinkedList<VariableRunner> copy = new LinkedList<VariableRunner>();
            foreach (VariableRunner runner in runners)
                copy.AddLast(runner.Clone());
            return copy;
        }

        /// <summary>
        /// Runs through the dataset executing the conditions/triggers for each point for the given context.
        /// Variable values can be passed in to change variables prior to running.
        /// </summary>
        /// <param name="workingContext">The context to use when executing.</param>
        /// <param name="runnerValues">A dictionary of variables to set in the context prior to running. The first string is the variable name, the second is its value.</param>
        private void RunIteration(StrategyRuntimeContext workingContext, Dictionary<String, double> runnerValues) {
            String debugRunners = "";
            foreach (KeyValuePair<String, double> pair in runnerValues) {
                workingContext.UserVars[pair.Key].SetValueFromString(workingContext, "" + pair.Value);
                debugRunners += pair.Key + "=" + pair.Value + " ";
            }
            Debug.WriteLine("Running iteration (Thread " + Thread.CurrentThread.ManagedThreadId + "): " + debugRunners);

            // Adjust the sample range based on the requested
            // date range if any is set
            int firstSample = 0;
            if (workingContext.Strategy.Start != DateTime.MinValue) {
                double timestamp = DateTimeHelper.GetUnixTimestampSeconds(workingContext.Strategy.Start);
                int index = workingContext.Dataset.BinarySearchForX(timestamp);
                while (index - 1 > 0 && workingContext.Dataset.Points[index - 1].X >= timestamp)
                    index--;
                firstSample = index;
            }

            int lastSample = workingContext.Dataset.Count - 1;
            if (workingContext.Strategy.End != DateTime.MinValue) {
                double timestamp = DateTimeHelper.GetUnixTimestampSeconds(workingContext.Strategy.End);
                int index = workingContext.Dataset.BinarySearchForX(timestamp);
                while (index - 1 > 0 && workingContext.Dataset.Points[index - 1].X >= timestamp)
                    index--;
                lastSample = index;
            }

            for (int i = firstSample; i <= lastSample; i++) {
                Step(workingContext);

                // Snapshot the user variables
                if (_isDebugEnabled) {
                    var dictCopy = new Dictionary<String, IValue>();
                    foreach (KeyValuePair<String, IValue> pair in workingContext.UserVars)
                        dictCopy.Add(pair.Key, pair.Value.Clone());

                    workingContext.DebugUserVars.Add(dictCopy);
                }
            }
            Debug.WriteLine("  -- iteration complete (Thread " + Thread.CurrentThread.ManagedThreadId + "). OptimizationValue: " + GetOptimizationValueFor(workingContext));
        }

        /// <summary>
        /// Runs all the filters in the strategy on the dataset.
        /// The filters are run in the order they appear in the strategy.
        /// The original dataset is unmodified.
        /// </summary>
        public void FilterDataset(ObservableCollection<IFilter> filters) {
            _filteredDataset = _originalDataset;
            foreach (IFilter filter in filters) {
                Result<Dataset> result = filter.Filter(_filteredDataset);
                if (result.Succeeded)
                    _filteredDataset = result.Value;
                else
                    Errors.Add(new StrategyError(StrategyErrorCode.FilterError, result.ErrorDetails));
            }
        }

        /// <summary>
        /// Move to the next datapoint in the dataset.
        /// </summary>
        public void Step(StrategyRuntimeContext context) {
            foreach (Trigger trigger in context.Strategy.Triggers) {
                if (trigger.Condition.IsTrue(context)) {
                    if (trigger.TrueAction != null)
                        trigger.TrueAction.Execute(context);
                } else if (trigger.FalseAction != null) {
                    trigger.FalseAction.Execute(context);
                }
            }

            context.CurrentDatapointIndex++;
        }

        /// <summary>
        /// Gets the value of the given account given the current price of the asset.
        /// </summary>
        /// <param name="account">The account to value.</param>
        /// <param name="price">The current price of the asset.</param>
        /// <returns></returns>
        private double ValueOf(Account account, double price) {
            return account.CurrencyBalance + account.AssetBalance * price;
        }

        private class TaskCounter {
            public int TaskCount { get; private set; } = 0;

            public void Increment() {
                TaskCount++;
            }
        }
    }
}
