using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Represents a value that can be used by conditions, operators and actions.
    /// </summary>
    public abstract class Value : ITreeNode {
        public abstract String GetId();
        public abstract string GetDescription();
        public abstract String GetStringValue(StrategyRuntimeContext context);
        public virtual String GetLabel() {
            return GetId() + ": " + GetStringValue(new ExampleStrategyRunParams());
        }
        public abstract Value Clone();
        public abstract JsonObject ToJson();
        public abstract void FromJson(JsonObject json);
        public virtual ITreeNode[] GetChildren() {
            // By default, no children
            return null;
        }
        public virtual void SetChild(int index, ITreeNode child) {
            // By default, no children
            throw new Exception("This node has no children");
        }

        public override string ToString() {
            return GetId();
        }
    }

    /// <summary>
    /// Represents a constant String value.
    /// </summary>
    public class StringValue : Value {
        private String _value = "";

        public virtual String GetValue(StrategyRuntimeContext context) {
            return _value;
        }

        protected StringValue() { }

        public StringValue(String value) {
            _value = value;
        }

        public override String GetId() {
            return "StringValue";
        }

        public override String GetDescription() {
            return "Represents some String value.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return GetValue(context);
        }

        public override Value Clone() {
            return new StringValue(_value);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", GetStringValue(null));
            return obj;
        }

        public override void FromJson(JsonObject json) {
            _value = json.GetString("Value");
        }
    }

    /// <summary>
    /// Represents a string that is retrieved from a variable.
    /// </summary>
    public class VariableStringValue : StringValue {
        private StringVariable _variable;

        public override String GetValue(StrategyRuntimeContext context) {
            return _variable.VariableRetrievalMethod.Invoke(context).GetValue(context);
        }

        public VariableStringValue(StringVariable variable) {
            _variable = variable;
        }

        public override String GetId() {
            return "VariableStringValue";
        }

        public override String GetDescription() {
            return "Represents a value that will be retrieved from a String variable when evaluated.";
        }

        public override String GetLabel() {
            return _variable.Id + ": " + GetStringValue(new ExampleStrategyRunParams());
        }

        public override Value Clone() {
            return new VariableStringValue((StringVariable)_variable.Clone());
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", _variable.Id);
            return obj;
        }

        public override void FromJson(JsonObject json) {
            String varId = json.GetString("Value");
            _variable = (StringVariable)Variables.GetStringVariables()[varId].Clone();
        }
    }

    /// <summary>
    /// Represents a constant Number value (double).
    /// </summary>
    public class NumberValue : Value {
        private double _value = 0;

        public NumberValue() {
            _value = 0;
        }

        public NumberValue(double value) {
            _value = value;
        }

        public virtual double GetValue(StrategyRuntimeContext context) {
            return _value;
        }

        public override String GetId() {
            return "NumberValue";
        }

        public override String GetDescription() {
            return "Represents some Number value.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + GetValue(context);
        }

        public override Value Clone() {
            return new NumberValue(_value);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", _value);
            return obj;
        }

        public override void FromJson(JsonObject json) {
            _value = json.GetDouble("Value");
        }
    }

    /// <summary>
    /// Represents a number that is retrieved from a variable.
    /// </summary>
    public class VariableNumberValue : NumberValue {
        private NumberVariable _variable;

        public override double GetValue(StrategyRuntimeContext context) {
            return _variable.VariableRetrievalMethod.Invoke(context).GetValue(context);
        }

        public VariableNumberValue(NumberVariable variable) {
            _variable = variable;
        }

        public override String GetId() {
            return "VariableNumberValue";
        }

        public override String GetDescription() {
            return "Represents a number that will be retrieved from a variable when evaluated.";
        }

        public override String GetLabel() {
            return _variable.Id + ": " + GetStringValue(new ExampleStrategyRunParams());
        }

        public override Value Clone() {
            return new VariableNumberValue((NumberVariable)_variable.Clone());
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", _variable.Id);
            return obj;
        }

        public override void FromJson(JsonObject json) {
            String varId = json.GetString("Value");
            _variable = (NumberVariable)Variables.GetNumberVariables()[varId].Clone();
        }
    }

    /// <summary>
    /// A Number that calculates its value using 2 other numbers
    /// and a MathOperator.
    /// </summary>
    public class MathValue : NumberValue {
        public NumberValue LeftOperand { get; private set; }
        public MathOperator Operator { get; private set; }
        public NumberValue RightOperand { get; private set; }

        public MathValue(NumberValue left, MathOperator op, NumberValue right) {
            LeftOperand = left;
            Operator = op;
            RightOperand = right;
        }

        public override double GetValue(StrategyRuntimeContext context) {
            return Operator.Operate(LeftOperand, RightOperand, context).GetValue(context);
        }

        public override string GetId() {
            return "MathValue";
        }

        public override String GetDescription() {
            return "Represents a Number value that is calculated from 2 other Number values using a math operator.";
        }

        public override Value Clone() {
            return new MathValue((NumberValue)LeftOperand.Clone(), (MathOperator)Operator.Clone(), (NumberValue)RightOperand.Clone());
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { LeftOperand, Operator, RightOperand };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                LeftOperand = child as NumberValue;
            else if (index == 1)
                Operator = child as MathOperator;
            else if (index == 2)
                RightOperand = child as NumberValue;
            else
                throw new ArgumentOutOfRangeException("Index out of range: " + index);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("LeftOperand", LeftOperand.ToJson());
            obj.Put("Operator", Operator.GetId());
            obj.Put("RightOperand", RightOperand.ToJson());
            return obj;
        }

        public override void FromJson(JsonObject json) {
            Dictionary<String, Value> values = Values.GetValues();
            JsonObject leftOperandObj = json.GetObject("LeftOperand");
            LeftOperand = (NumberValue)values[leftOperandObj.GetString("Id")].Clone();
            LeftOperand.FromJson(leftOperandObj);
            Operator = (MathOperator)Operators.GetMathOperators()[json.GetString("Operator")].Clone();
            JsonObject rightOperandObj = json.GetObject("RightOperand");
            RightOperand = (NumberValue)values[rightOperandObj.GetString("Id")].Clone();
            RightOperand.FromJson(rightOperandObj);
        }
    }
}
