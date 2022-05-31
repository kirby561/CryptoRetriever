using CryptoRetriever.Utility.JsonObjects;
using System;

namespace CryptoRetriever.Strats {
    public interface IVariable {
        String GetStringValue(StrategyRuntimeContext context);
        String GetDescription();
        String GetLabel();
        String GetVariableName();
        ValueType GetValueType();
        IValue Clone();
    }

    public interface IUserVariable : IVariable {
        /// <returns>
        /// Creates the actual storage for the variable that will be indexed
        /// under the variable's name in the runtime context.
        /// </returns>
        IValue CreateInstance();

        void SetDefaultValue(String defaultValue);

        String GetDefaultValue();
    }

    public abstract class VariableValue<T> : TypeValue<T>, IVariable {
        protected String _variableName;

        /// <summary>
        /// A method that takes in the strategy run context and returns
        /// the value of the variable T at that point in time.
        /// </summary>
        protected Func<StrategyRuntimeContext, T> _variableRetrievalMethod;

        protected VariableValue() { }

        public VariableValue(String variableName, Func<StrategyRuntimeContext, T> variableRetrievalMethod) {
            _variableName = variableName;
            _variableRetrievalMethod = variableRetrievalMethod;
        }

        public override String GetId() {
            return "Variable" + base.GetId();
        }

        public String GetVariableName() {
            return _variableName;
        }

        public override T GetValue(StrategyRuntimeContext context) {
            return _variableRetrievalMethod.Invoke(context);
        }

        public override String GetDescription() {
            return "Represents a value that will be retrieved from a " + GetValueType().ToString() + " variable when evaluated.";
        }

        public override String GetLabel() {
            return _variableName;
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", _variableName);
            return obj;
        }

        public override void FromJson(JsonObject json) {
            _variableName = json.GetString("Value");
            _variableRetrievalMethod = (Variables.GetReadOnlyVariablesOfType(GetValueType())[_variableName] as VariableValue<T>)._variableRetrievalMethod;
        }
    }

    /// <summary>
    /// Represents a string that is retrieved from a variable.
    /// </summary>
    public class VariableStringValue : VariableValue<String>, IStringValue {
        public VariableStringValue() : base() { }
        public VariableStringValue(String variableName, Func<StrategyRuntimeContext, String> variableRetrievalMethod)
            : base(variableName, variableRetrievalMethod) { }

        public override IValue Clone() {
            return new VariableStringValue(_variableName, _variableRetrievalMethod);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return GetValue(context);
        }

        public override ValueType GetValueType() {
            return ValueType.String;
        }

        public override void SetValueFromString(StrategyRuntimeContext context, string val) {
            throw new InvalidOperationException("VariableStringValue are readonly.");
        }
    }

    /// <summary>
    /// Represents a number that is retrieved from a variable.
    /// </summary>
    public class VariableNumberValue : VariableValue<double>, INumberValue {
        public VariableNumberValue() : base() { }
        public VariableNumberValue(String variableName, Func<StrategyRuntimeContext, double> variableRetrievalMethod)
            : base(variableName, variableRetrievalMethod) { }

        public override IValue Clone() {
            return new VariableNumberValue(_variableName, _variableRetrievalMethod);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return "" + GetValue(context);
        }

        public override ValueType GetValueType() {
            return ValueType.Number;
        }

        public override void SetValueFromString(StrategyRuntimeContext context, string val) {
            throw new InvalidOperationException("VariableNumberValues are readonly.");
        }
    }

    /// <summary>
    /// Holds the template for a string variable of a user.
    /// The actual value will be stored by a StringValue held in the context.
    /// </summary>
    public class UserStringVariable : TypeValue<String>, IUserVariable, IStringValue {
        private String _variableName;
        private String _defaultValue;

        public UserStringVariable() : base() { }

        public UserStringVariable(String name, String defaultValue) {
            _defaultValue = defaultValue;
            _variableName = name;
        }

        public String GetVariableName() {
            return _variableName;
        }

        public IValue CreateInstance() {
            return new SimpleStringValue(_defaultValue);
        }

        public override IValue Clone() {
            return new UserStringVariable(_variableName, _defaultValue);
        }

        public override String GetId() {
            return "User" + base.GetId();
        }

        public override String GetLabel() {
            return _variableName + " (" + GetId() + " / " + _defaultValue + ")";
        }

        public override String GetValue(StrategyRuntimeContext context) {
            SimpleStringValue instance = (SimpleStringValue)context.UserVars[_variableName];
            return instance.GetValue(context);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return GetValue(context);
        }

        public override void SetValueFromString(StrategyRuntimeContext context, String val) {
            context.UserVars[_variableName].SetValueFromString(context, val);
        }

        public override ValueType GetValueType() {
            return ValueType.String;
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("VariableName", _variableName);
            obj.Put("DefaultValue", _defaultValue);
            return obj;
        }

        public override void FromJson(JsonObject json) {
            _variableName = json.GetString("VariableName");
            _defaultValue = json.GetString("DefaultValue");
        }

        public void SetDefaultValue(string defaultValue) {
            _defaultValue = defaultValue;
        }

        public string GetDefaultValue() {
            return _defaultValue;
        }
    }

    /// <summary>
    /// Holds a number variable created by the user.
    /// </summary>
    public class UserNumberVariable : TypeValue<double>, IUserVariable, INumberValue {
        private String _variableName;
        private double _defaultValue;

        public UserNumberVariable() : base() { }

        public UserNumberVariable(String name, double defaultValue) {
            _variableName = name;
            _defaultValue = defaultValue;
        }

        public String GetVariableName() {
            return _variableName;
        }

        public IValue CreateInstance() {
            return new SimpleNumberValue(_defaultValue);
        }

        public override IValue Clone() {
            return new UserNumberVariable(_variableName, _defaultValue);
        }

        public override String GetId() {
            return "User" + base.GetId();
        }

        public override String GetLabel() {
            return _variableName + " (" + GetId() + " / " + _defaultValue + ")";
        }

        public override ValueType GetValueType() {
            return ValueType.Number;
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + GetValue(context);
        }

        public override double GetValue(StrategyRuntimeContext context) {
            SimpleNumberValue instance = (SimpleNumberValue)context.UserVars[_variableName];
            return instance.GetValue(context);
        }

        public override void SetValueFromString(StrategyRuntimeContext context, string val) {
            context.UserVars[_variableName].SetValueFromString(context, val);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("VariableName", _variableName);
            obj.Put("DefaultValue", _defaultValue);
            return obj;
        }

        public override void FromJson(JsonObject json) {
            _variableName = json.GetString("VariableName");
            _defaultValue = json.GetDouble("DefaultValue");
        }

        public void SetDefaultValue(string defaultValue) {
            _defaultValue = Double.Parse(defaultValue);
        }

        public string GetDefaultValue() {
            return "" + _defaultValue;
        }
    }
}