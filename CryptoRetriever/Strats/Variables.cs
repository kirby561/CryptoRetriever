using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Provides accessors for different stock variables that can
    /// be used by conditions and actions.
    /// </summary>
    public static class Variables {
        /// <returns>
        /// Returns the list of string variables that can be used by 
        /// triggers in this strategy.
        /// </returns>
        public static Dictionary<String, StringVariable> GetStringVariables() {
            var result = new Dictionary<String, StringVariable>();
            AddVar(result, new StringVariable(
                "Strategy.Name",
                (context) => { return new StringValue(context.Strategy.Name); }
            ));
            AddVar(result, new StringVariable(
                "CurrentState",
                (context) => { return new StringValue(context.CurrentState); }
            ));
            AddVar(result, new StringVariable(
                "NextState",
                (context) => { return new StringValue(context.NextState); }
            ));
            return result;
        }

        /// <returns>
        /// Returns the list of number variables that can be used by 
        /// triggers in this strategy.
        /// </returns>
        public static Dictionary<String, NumberVariable> GetNumberVariables() {
            var result = new Dictionary<String, NumberVariable>();
            AddVar(result, new NumberVariable(
                "Account.CurrencyBalance",
                (context) => { return new NumberValue(context.Account.CurrencyBalance); }
            ));
            AddVar(result, new NumberVariable(
                "Account.AssetBalance",
                (context) => { return new NumberValue(context.Account.CurrencyBalance); }
            ));
            AddVar(result, new NumberVariable(
                "CurrentTimestampSecs",
                (context) => { return new NumberValue(context.GetCurrentTimestamp()); }
            ));
            AddVar(result, new NumberVariable(
                "CurrentDatapointIndex",
                (context) => { return new NumberValue(context.CurrentDatapointIndex); }
            ));
            return result;
        }

        private static void AddVar<T>(Dictionary<String, T> dict, T var) where T : IVariable {
            dict.Add(var.Id, var);
        }
    }
}
