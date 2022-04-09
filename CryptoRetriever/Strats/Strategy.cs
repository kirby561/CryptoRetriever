using CryptoRetriever.Data;
using CryptoRetriever.Filter;
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
        public String Name { get; set; }
        public Account Account { get; set; } = new Account();
        public ExchangeAssumptions ExchangeAssumptions { get; set; }
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
