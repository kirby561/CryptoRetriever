using CryptoRetriever.Data;
using CryptoRetriever.Filter;
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
        public ObservableCollection<State> States { get; set; }  = new ObservableCollection<State>();
        public ObservableCollection<Trigger> Triggers { get; set; } = new ObservableCollection<Trigger>();
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

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Name", Name)
                .Put("Account", Account.ToJson())
                .Put("ExchangeAssumptions", ExchangeAssumptions.ToJson())
                .Put("Filters", Filters)
                .Put("States", States)
                .Put("Triggers", Triggers)
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
                    if ("GaussianFilter".Equals(filterType)) {
                        GaussianFilter filter = new GaussianFilter();
                        filter.FromJson(filterObj);
                        Filters.Add(filter);
                    } else {
                        throw new InvalidOperationException("Unsupported filter type in strategy: " + filterType);
                    }
                }
            }

            List<JsonObject> states = obj.GetObjectArray("States");
            if (states != null) {
                foreach (JsonObject stateObj in states) {
                    State state = new State();
                    state.FromJson(stateObj);
                    States.Add(state);
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

            Start = obj.GetDateTime("Start");
            End = obj.GetDateTime("End");
        }
    }

    public class ExampleStrategy : Strategy {
        public ExampleStrategy() {
            Name = "ExampleStrategy";
            States.Add(new State("Default"));
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

    public class ExampleStrategyRunParams : StrategyRuntimeContext {
        public ExampleStrategyRunParams() : base(new ExampleStrategy(), new ExampleDataset()) {
            CurrentState = Strategy.States[0].GetId();
        }
    }
}
