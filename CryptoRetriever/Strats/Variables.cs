using System;
using System.Collections.Generic;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Provides accessors for different stock variables that can
    /// be used by conditions and actions.
    /// </summary>
    public static class Variables {
        /// <returns>
        /// Returns the list of User Variable types indexed by ID (Note the ID
        /// identifies the type of the user variable, not the name of it).
        /// </returns>
        public static Dictionary<String, IValue> GetUserVariableTypes() {
            var result = new Dictionary<String, IValue>();
            result.Add("UserStringValue", new UserStringVariable("UserStringVarTemplate", ""));
            result.Add("UserNumberValue", new UserNumberVariable("UserNumberVarTemplate", 0));
            return result;
        }

        public static Dictionary<String, IValue> GetReadOnlyVariablesOfType(ValueType type) {
            var result = new Dictionary<String, IValue>();
            if (type == ValueType.String) {
                foreach (var pair in GetStringVariables())
                    result.Add(pair.Key, pair.Value);
            } else if (type == ValueType.Number) {
                foreach (var pair in GetNumberVariables())
                    result.Add(pair.Key, pair.Value);
            } else {
                throw new NotSupportedException("Unsupported type: " + type.ToString());
            }
            return result;
        }

        /// <returns>
        /// Returns the list of string variables that can be used by 
        /// triggers in this strategy.
        /// </returns>
        public static Dictionary<String, VariableStringValue> GetStringVariables() {
            var result = new Dictionary<String, VariableStringValue>();
            AddVar(result, new VariableStringValue(
                "Strategy.Name",
                (context) => { return context.Strategy.Name; }
            ));
            return result;
        }

        /// <returns>
        /// Returns the list of number variables that can be used by 
        /// triggers in this strategy.
        /// </returns>
        public static Dictionary<String, VariableNumberValue> GetNumberVariables() {
            var result = new Dictionary<String, VariableNumberValue>();
            AddVar(result, new VariableNumberValue(
                "Account.CurrencyBalance",
                (context) => { return context.Account.CurrencyBalance; }
            ));
            AddVar(result, new VariableNumberValue(
                "Account.AssetBalance",
                (context) => { return context.Account.CurrencyBalance; }
            )); 
            AddVar(result, new VariableNumberValue(
                 "Dataset.CurrentPrice",
                 (context) => { return context.Dataset.Points[context.CurrentDatapointIndex].Y; }
            ));
            AddVar(result, new VariableNumberValue(
                 "Dataset.CurrentFilteredPrice",
                 (context) => { return context.FilteredDataset.Points[context.CurrentDatapointIndex].Y; }
            ));
            AddVar(result, new VariableNumberValue(
                "CurrentTimestampSecs",
                (context) => { return context.GetCurrentTimestamp(); }
            ));
            AddVar(result, new VariableNumberValue(
                "CurrentDatapointIndex",
                (context) => { return context.CurrentDatapointIndex; }
            ));
            return result;
        }

        private static void AddVar<T>(Dictionary<String, T> dict, T var) where T : IVariable {
            dict.Add(var.GetVariableName(), var);
        }
    }
}
