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
        public static List<StringVariable> GetStringVariables() {
            var result = new List<StringVariable>();
            result.Add(new StringVariable(
                "Strategy.Name",
                (context) => { return new StringValue(context.Strategy.Name); }
            ));
            result.Add(new StringVariable(
                "CurrentState",
                (context) => { return new StringValue(context.CurrentState); }
            ));
            result.Add(new StringVariable(
                "NextState",
                (context) => { return new StringValue(context.NextState); }
            ));
            return result;
        }

        /// <returns>
        /// Returns the list of number variables that can be used by 
        /// triggers in this strategy.
        /// </returns>
        public static List<NumberVariable> GetNumberVariables() {
            var result = new List<NumberVariable>();
            result.Add(new NumberVariable(
                "Account.CurrencyBalance",
                (context) => { return new NumberValue(context.Account.CurrencyBalance); }
            ));
            result.Add(new NumberVariable(
                "Account.AssetBalance",
                (context) => { return new NumberValue(context.Account.CurrencyBalance); }
            ));
            result.Add(new NumberVariable(
                "CurrentTimestampSecs",
                (context) => { return new NumberValue(context.GetCurrentTimestamp()); }
            ));
            result.Add(new NumberVariable(
                "CurrentDatapointIndex",
                (context) => { return new NumberValue(context.CurrentDatapointIndex); }
            ));
            return result;
        }
    }
}
