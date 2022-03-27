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
        private List<IFilter> _filters = new List<IFilter>();
        private ExchangeAssumptions _exchangeAssumptions;

        public List<IFilter> GetFilters() { return _filters; }
    }
}
