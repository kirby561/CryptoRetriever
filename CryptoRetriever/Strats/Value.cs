using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public enum ValueType {
        String,
        Number
    }

    /// <summary>
    /// Represents a value that can be used by conditions, operators and actions.
    /// </summary>
    public interface IValue : ITreeNode, IJsonable {
        /// <summary>
        /// This gets the current value of the node as a string given the
        /// current context.
        /// </summary>
        /// <param name="context">The runtime context which may or may not be needed to get the value.</param>
        /// <returns>Returns the current value as a string.</returns>
        String GetStringValue(StrategyRuntimeContext context);

        void SetFromValue(StrategyRuntimeContext context, IValue otherValue);
        void SetValueFromString(StrategyRuntimeContext context, String val);

        IValue Clone();
        ValueType GetValueType();
    }

    public abstract class Value : IValue {
        public abstract String GetId();
        public abstract String GetDescription();
        public abstract String GetStringValue(StrategyRuntimeContext context);
        public abstract void SetFromValue(StrategyRuntimeContext context, IValue otherValue);
        public abstract void SetValueFromString(StrategyRuntimeContext context, String val);
        public abstract IValue Clone();
        public abstract ValueType GetValueType();
        public abstract JsonObject ToJson();
        public abstract void FromJson(JsonObject json);

        public virtual String GetLabel() {
            String result = GetId();
            return result;
        }

        public virtual ITreeNode[] GetChildren() {
            // By default, no children
            return null;
        }

        public virtual void SetChild(int index, ITreeNode child) {
            // By default, no children
            throw new Exception("This node has no children");
        }

        public String Summary {
            get {
                return GetLabel();
            }
        }

        public override string ToString() {
            return GetId();
        }
    }

    public abstract class TypeValue<T> : Value {
        public override String GetId() {
            return GetValueType().ToString() + "Value";
        }

        public override String GetDescription() {
            return "Represents some " + GetValueType().ToString() + " value.";
        }

        public override void SetFromValue(StrategyRuntimeContext context, IValue otherValue) {
            SetValueFromString(context, otherValue.GetStringValue(context));
        }

        public abstract T GetValue(StrategyRuntimeContext context);
    }

    public interface INumberValue : IValue {
        public double GetValue(StrategyRuntimeContext context);
    }

    public interface IStringValue : IValue {
        public String GetValue(StrategyRuntimeContext context);
    }

    /// <summary>
    /// Simple values are typed values that just have a member for their value.
    /// </summary>
    /// <typeparam name="T">The type of this value.</typeparam>
    public abstract class SimpleValue<T> : TypeValue<T> {
        protected T _value = default;

        protected SimpleValue() { }

        public SimpleValue(T value) {
            _value = value;
        }

        public override T GetValue(StrategyRuntimeContext context) {
            return _value;
        }

        public override String GetId() {
            return "Simple" + GetValueType().ToString() + "Value";
        }

        public override string GetLabel() {
            return GetId() + " (" + _value + ")";
        }

        public override String GetDescription() {
            return "Represents some " + GetValueType().ToString() + " value.";
        }

        public override void SetFromValue(StrategyRuntimeContext context, IValue otherValue) {
            SetValueFromString(context, otherValue.GetStringValue(context));
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", GetStringValue(null));
            return obj;
        }

        public override void FromJson(JsonObject json) {
            SetValueFromString(null, json.GetString("Value"));
        }
    }

    /// <summary>
    /// Represents a constant String value.
    /// </summary>
    public class SimpleStringValue : SimpleValue<String>, IStringValue {
        public SimpleStringValue() { }
        public SimpleStringValue(String val) : base(val) { }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return GetValue(context);
        }

        public override void SetValueFromString(StrategyRuntimeContext context, string val) {
            _value = val;
        }

        public override ValueType GetValueType() {
            return ValueType.String;
        }

        public override IValue Clone() {
            return new SimpleStringValue(_value);
        }
    }

    /// <summary>
    /// Represents a constant String value.
    /// </summary>
    public class SimpleNumberValue : SimpleValue<double>, INumberValue {
        public SimpleNumberValue() : base(0) { }
        public SimpleNumberValue(double val) : base(val) { }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + GetValue(context);
        }

        public override void SetValueFromString(StrategyRuntimeContext context, string val) {
            _value = Double.Parse(val);
        }

        public override ValueType GetValueType() {
            return ValueType.Number;
        }

        public override IValue Clone() {
            return new SimpleNumberValue(_value);
        }
    }

    /// <summary>
    /// A Number that calculates its value using 2 other numbers
    /// and a MathOperator.
    /// </summary>
    public class MathValue : TypeValue<double>, INumberValue {
        public INumberValue LeftOperand { get; private set; }
        public MathOperator Operator { get; private set; }
        public INumberValue RightOperand { get; private set; }

        public MathValue(INumberValue left, MathOperator op, INumberValue right) {
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

        public override ValueType GetValueType() {
            return ValueType.Number;
        }

        public override IValue Clone() {
            return new MathValue((INumberValue)LeftOperand.Clone(), (MathOperator)Operator.Clone(), (INumberValue)RightOperand.Clone());
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return "" + GetValue(context);
        }

        public override void SetValueFromString(StrategyRuntimeContext context, string val) {
            throw new NotSupportedException();
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { LeftOperand, Operator, RightOperand };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                LeftOperand = (INumberValue)child;
            else if (index == 1)
                Operator = (MathOperator)child;
            else if (index == 2)
                RightOperand = (INumberValue)child;
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
            Dictionary<String, IValue> values = Values.GetValues();
            JsonObject leftOperandObj = json.GetObject("LeftOperand");
            LeftOperand = (INumberValue)values[leftOperandObj.GetString("Id")].Clone();
            LeftOperand.FromJson(leftOperandObj);
            Operator = (MathOperator)Operators.GetMathOperators()[json.GetString("Operator")].Clone();
            JsonObject rightOperandObj = json.GetObject("RightOperand");
            RightOperand = (INumberValue)values[rightOperandObj.GetString("Id")].Clone();
            RightOperand.FromJson(rightOperandObj);
        }
    }
}
