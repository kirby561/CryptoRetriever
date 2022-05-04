using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Contains information about a NumberVariable
    /// that should be run through a range of values
    /// when running a strategy. The variable needs
    /// to be a NumberVariable.
    /// </summary>
    public class VariableRunner : IJsonable {
        public UserNumberVariable Variable { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public double Step { get; set; }

        public String Summary {
            get {
                return Variable.GetLabel() + " (" + Start + " - " + End + ", " + Step + ")";
            }
        }

        public void FromJson(JsonObject json) {
            Variable = new UserNumberVariable();
            Variable.FromJson(json.GetObject("Variable"));
            Start = json.GetDouble("Start");
            End = json.GetDouble("End");
            Step = json.GetDouble("Step");
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Variable", Variable.ToJson());
            obj.Put("Start", Start);
            obj.Put("End", End);
            obj.Put("Step", Step);
            return obj;
        }
    }
}
