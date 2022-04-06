using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public class Variable<T> {
        /// <summary>
        /// A unique ID for this variable.
        /// </summary>
        public String Id { get; private set; }
        
        /// <summary>
        /// A method that takes in the strategy run context and returns
        /// the value of the variable T at that point in time.
        /// </summary>
        public Func<StrategyRuntimeContext, T> VariableRetrievalMethod { get; private set; }

        public Variable(String id, Func<StrategyRuntimeContext, T> variableRetrievalMethod) {
            Id = id;
            VariableRetrievalMethod = variableRetrievalMethod;
        }
    }

    public class StringVariable : Variable<StringValue> {
        public StringVariable(String id, Func<StrategyRuntimeContext, StringValue> variableRetrievalMethod)
            : base(id, variableRetrievalMethod) { }
    }
    
    public class NumberVariable : Variable<NumberValue> {
        public NumberVariable(String id, Func<StrategyRuntimeContext, NumberValue> variableRetrievalMethod)
            : base(id, variableRetrievalMethod) { }
    }

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
