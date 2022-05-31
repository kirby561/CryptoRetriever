using CryptoRetriever.Utility.JsonObjects;
using System;

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
        /// The fee for making a transaction as a percentage of the value of the transaction.
        /// </summary>
        public double TransactionFeePercentage { get; set; }

        /// <summary>
        /// The amount of time it takes to perform a transaction in seconds.
        /// </summary>
        public double TransactionTimeS { get; set; }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject()
                .Put("TransactionFee", TransactionFee)
                .Put("TransactionFeePercentage", TransactionFeePercentage)
                .Put("TransactionTimeS", TransactionTimeS);
            return obj;
        }

        public void FromJson(JsonObject obj) {
            TransactionFee = obj.GetDouble("TransactionFee");
            TransactionTimeS = obj.GetDouble("TransactionTimeS");
            try { // Try/catch for backwards compatibility
                TransactionFeePercentage = obj.GetDouble("TransactionFeePercentage");
            } catch (Exception) {
                TransactionFeePercentage = 0;
            }
        }
    }
}
