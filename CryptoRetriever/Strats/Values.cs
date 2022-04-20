using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptoRetriever.Strats {
    public static class Values {
        public static Dictionary<String, Value> GetValues() {
            var values = new Dictionary<String, Value>();

            AddVal(values, new StringValue(""));
            AddVal(values, new VariableStringValue(Variables.GetStringVariables().Values.First()));
            AddVal(values, new NumberValue());
            AddVal(values, new VariableNumberValue(Variables.GetNumberVariables().Values.First()));
            AddVal(values, new MathValue(new NumberValue(), new AdditionOperator(), new NumberValue()));

            return values;
        }

        private static void AddVal<T>(Dictionary<String, T> dict, T val) where T : Value {
            dict.Add(val.GetId(), val);
        }
    }
}
