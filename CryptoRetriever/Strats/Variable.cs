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
}
