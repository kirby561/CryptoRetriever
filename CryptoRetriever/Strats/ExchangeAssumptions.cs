using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// A data class for giving some assumptions to a Strategy
    /// that will be taken into account when executing it.
    /// </summary>
    public class ExchangeAssumptions : IJsonable {
        /// <summary>
        /// The fee for making a transaction (in currency not asset value)
        /// </summary>
        public double TransactionFee { get; set; }

        /// <summary>
        /// The amount of time it takes to perform a transaction in seconds.
        /// </summary>
        public double TransactionTimeS { get; set; }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject()
                .Put("TransactionFee", TransactionFee)
                .Put("TransactionTimeS", TransactionTimeS);
            return obj;
        }

        public void FromJson(JsonObject obj) {
            TransactionFee = obj.GetDouble("TransactionFee");
            TransactionTimeS = obj.GetDouble("TransactionTimeS");
        }
    }
}
