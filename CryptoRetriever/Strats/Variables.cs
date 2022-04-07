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
            return result;
        }

        /// <returns>
        /// Returns the list of number variables that can be used by 
        /// triggers in this strategy.
        /// </returns>
        public static List<NumberVariable> GetNumberVariables() {
            var result = new List<NumberVariable>();
            result.Add(new NumberVariable(
                "CurrentTimestampSecs",
                (context) => { return new NumberValue(context.GetCurrentTimestamp()); }
            ));
            return result;
        }
    }
}
