using CryptoRetriever.Filter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

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
    }
}
