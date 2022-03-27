using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strategy {
    /// <summary>
    /// A data class for giving some assumptions to a Strategy
    /// that will be taken into account when executing it.
    /// </summary>
    public class ExchangeAssumptions {
        /// <summary>
        /// The fee for making a transaction (in currency not asset value)
        /// </summary>
        public double TransactionFee { get; set; }

        /// <summary>
        /// The amount of time it takes to perform a transaction in seconds.
        /// </summary>
        public double TransactionTimeS { get; set; }
    }
}
