using System;
using System.Collections.Generic;

namespace CryptoRetriever.Strats {
    public static class Values {
        public static Dictionary<String, IValue> GetValues() {
            var values = new Dictionary<String, IValue>();

            // Non-variable types
            foreach (KeyValuePair<String, IValue> pair in GetNonVariableTypes())
                values.Add(pair.Key, pair.Value);
            
            // Variable types
            AddVal(values, new VariableStringValue());
            AddVal(values, new VariableNumberValue());
            AddVal(values, new UserNumberVariable("UserNumberVariable", 0));
            AddVal(values, new UserStringVariable("UserStringVariable", ""));

            return values;
        }

        public static Dictionary<String, IValue> GetNonVariableTypes() {
            var values = new Dictionary<String, IValue>();

            AddVal(values, new SimpleStringValue(""));
            AddVal(values, new SimpleNumberValue());
            AddVal(values, new MathValue(new SimpleNumberValue(), new AdditionOperator(), new SimpleNumberValue()));

            return values;
        }

        private static void AddVal<T>(Dictionary<String, T> dict, T val) where T : IValue {
            dict.Add(val.GetId(), val);
        }
    }
}
