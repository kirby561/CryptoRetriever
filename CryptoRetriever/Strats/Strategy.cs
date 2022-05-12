using CryptoRetriever.Data;
using CryptoRetriever.Filter;
using CryptoRetriever.UI;
using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// A Strategy contains information on when to buy
    /// and sell an asset that can be used to simulate
    /// a series of transactions according to the rules
    /// contained within in order to see how they will
    /// perform on a historical dataset.
    /// </summary>
    public class Strategy {
        public String Name { get; set; } = "";
        public Account Account { get; set; } = new Account();
        public ExchangeAssumptions ExchangeAssumptions { get; set; } = new ExchangeAssumptions();
        public ObservableCollection<IFilter> Filters { get; set; } = new ObservableCollection<IFilter>();
        public ObservableCollection<IValue> UserVars { get; set; } = new ObservableCollection<IValue>();
        public ObservableCollection<Trigger> Triggers { get; set; } = new ObservableCollection<Trigger>();
        public ObservableCollection<VariableRunner> VariableRunners { get; set; } = new ObservableCollection<VariableRunner>();
        public DateTime Start { get; set; } = DateTime.MinValue; // Min means use the dataset's start
        public DateTime End { get; set; } = DateTime.MinValue; // Min means use the dataset's end

        /// <summary>
        /// Used for displaying in list views.
        /// </summary>
        public String Summary {
            get {
                return Name;
            }
        }

        /// <returns>
        /// Gets the current list of actions that apply to this Strategy.
        /// This includes actions that are always applicable and actions that
        /// depend on strategy parameters like UserVars.
        /// </returns>
        public Dictionary<String, StratAction> GetActions() {
            var result = new Dictionary<String, StratAction>();
            foreach (KeyValuePair<String, StratAction> action in Actions.GetActions())
                result.Add(action.Key, action.Value);

            // Add an action to change each user var
            foreach (IValue userVarValue in UserVars) {
                IVariable var = userVarValue as IVariable;
                ValueChanger valAction = new ValueChanger(
                    "Change " + var.GetVariableName(),
                    userVarValue);
                result.Add("Change" + var.GetVariableName(), valAction);
            }

            return result;
        }

        /// <returns>
        /// Gets the current list of values that apply to this Strategy.
        /// This includes values that are always applicable and values that
        /// depend on strategy parameters like UserVars.
        /// </returns>
        public Dictionary<String, IValue> GetValuesOfType(ValueType type) {
            var result = new Dictionary<String, IValue>();

            foreach (KeyValuePair<String, IValue> pair in Values.GetNonVariableTypes())
                if (type.Equals(pair.Value.GetValueType()))
                    result.Add(pair.Key, pair.Value);

            foreach (KeyValuePair<String, IValue> pair in Variables.GetReadOnlyVariablesOfType(type))
                if (type.Equals(pair.Value.GetValueType()))
                    result.Add(pair.Key, pair.Value);

            foreach (IValue userVarValue in UserVars) {
                IUserVariable userVar = (IUserVariable)userVarValue;
                if (type.Equals(userVarValue.GetValueType()))
                    result.Add(userVar.GetVariableName(), userVarValue);
            }

            return result;
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Name", Name)
                .Put("Account", Account.ToJson())
                .Put("ExchangeAssumptions", ExchangeAssumptions.ToJson())
                .Put("Filters", Filters)
                .Put("UserVars", UserVars)
                .Put("Triggers", Triggers)
                .Put("VariableRunners", VariableRunners)
                .Put("Start", Start)
                .Put("End", End);
            return obj;
        }

        public void FromJson(JsonObject obj) {
            Name = obj.GetString("Name");
            Account.FromJson(obj.GetObject("Account"));
            ExchangeAssumptions.FromJson(obj.GetObject("ExchangeAssumptions"));

            List<JsonObject> filters = obj.GetObjectArray("Filters");
            if (filters != null) {
                foreach (JsonObject filterObj in filters) {
                    String filterType = filterObj.GetString("Type");
                    bool found = false;
                    // ?? TODO: Make this not necessary - both the for loop and the UI dependency
                    foreach (IFilter filter in FilterUi.GetFilterUiMap().Keys) {
                        if (filter.GetType().Name.Equals(filterType)) {
                            filter.FromJson(filterObj);
                            Filters.Add(filter);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        throw new InvalidOperationException("Unsupported filter type in strategy: " + filterType);
                }
            }

            List<JsonObject> userVars = obj.GetObjectArray("UserVars");
            if (userVars != null) {
                foreach (JsonObject userVarObj in userVars) {
                    IValue userVar = Variables.GetUserVariableTypes()[userVarObj.GetString("Id")].Clone();
                    userVar.FromJson(userVarObj);
                    UserVars.Add(userVar);
                }
            }

            List<JsonObject> triggers = obj.GetObjectArray("Triggers");
            if (triggers != null) {
                foreach (JsonObject triggerObj in triggers) {
                    Trigger trigger = new Trigger();
                    trigger.FromJson(triggerObj);
                    Triggers.Add(trigger);
                }
            }

            List<JsonObject> runners = obj.GetObjectArray("VariableRunners");
            if (runners != null) {
                foreach (JsonObject runnerObj in runners) {
                    VariableRunner runner = new VariableRunner();
                    runner.FromJson(runnerObj);
                    VariableRunners.Add(runner);
                }
            }

            Start = obj.GetDateTime("Start");
            End = obj.GetDateTime("End");
        }
    }

    public class ExampleStrategy : Strategy {
        public ExampleStrategy() {
            Name = "ExampleStrategy";
            UserVars.Add(new UserStringVariable("Default", "None"));
        }
    }

    public class ExampleDataset : Dataset {
        public ExampleDataset() {
            Points.Add(new Point(1, 10));
            Points.Add(new Point(2, 20));
            Points.Add(new Point(3, 30));
            Points.Add(new Point(4, 40));
            Points.Add(new Point(5, 50));
        }
    }

    //public class ExampleStrategyRunParams : StrategyRuntimeContext {
    //    public ExampleStrategyRunParams() : base(new ExampleStrategy(), new ExampleDataset()) {

    //    }
    //}
}
