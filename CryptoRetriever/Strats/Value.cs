using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Represents a value that can be used by conditions, operators and actions.
    /// </summary>
    public abstract class Value : ITreeNode {
        public abstract String GetId();
        public abstract String GetStringValue(StrategyRuntimeContext context);
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

        public override String GetStringValue(StrategyRuntimeContext context) {
            return GetValue(context);
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
            return _variable.Id;
        }
    }

    /// <summary>
    /// Represents a constant Number value (double).
    /// </summary>
    public class NumberValue : Value {
        private double _value = 0;

        public virtual double GetValue(StrategyRuntimeContext context) {
            return _value;
        }

        public NumberValue() {
            _value = 0;
        }

        public override String GetId() {
            return "NumberValue";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + GetValue(context);
        }

        public NumberValue(double value) {
            _value = value;
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
            return _variable.Id;
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
    }
}
