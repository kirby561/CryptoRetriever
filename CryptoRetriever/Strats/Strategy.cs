using CryptoRetriever.Filter;
using System;
using System.Collections.Generic;
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
        public List<IFilter> Filters { get; set; } = new List<IFilter>();
        public List<State> States { get; set; }  = new List<State>();
        public List<Trigger> Triggers { get; set; } = new List<Trigger>();
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
