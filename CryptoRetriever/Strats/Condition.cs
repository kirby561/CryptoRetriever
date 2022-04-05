using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public interface ITreeNode {
        String GetId();
        String GetStringValue();
        ITreeNode[] GetChildren();
        void SetChild(int index, ITreeNode child);
    }

    /// <summary>
    /// A Condition is something that can be true or false
    /// but may need to evaluate something to figure that out.
    /// </summary>
    public abstract class Condition : ITreeNode {
        public abstract bool IsTrue();
        public abstract String GetId();
        public abstract String GetStringValue();
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

        public Condition[] GetOptions() {
            return new Condition[] {
                new BoolCondition(true),
                new BoolCondition(false),
                new NumberComparison(),
                new LogicComparison(),
                new StringComparison()
            };
        }
    }

    public abstract class Value : ITreeNode {
        public abstract String GetId();
        public abstract String GetStringValue();
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

    public abstract class Operator : ITreeNode {
        public abstract String GetId();
        public abstract String GetStringValue();
        public virtual ITreeNode[] GetChildren() {
            // By default, no children
            return null;
        }
        public virtual void SetChild(int index, ITreeNode child) {
            // By default, no children
            throw new Exception("This node has no children");
        }
        public abstract Operator[] GetOptions();
        public override string ToString() {
            return GetStringValue();
        }
    }

    /// <summary>
    /// A BoolCondition is a Condition constant that 
    /// is always true or always false.
    /// </summary>
    public class BoolCondition : Condition {
        private bool _isTrue;
        private String _name = "BoolCondition";

        public BoolCondition(bool isTrue) {
            _isTrue = isTrue;
        }

        public BoolCondition(String name, bool isTrue) {
            _isTrue = isTrue;
            _name = name;
        }

        public override bool IsTrue() {
            return _isTrue;
        }

        public override string GetId() {
            return _name;
        }

        public override String GetStringValue() {
            return "" + IsTrue();
        }
    }

    /// <summary>
    /// A Condition that compares 2 numbers and is true if
    /// the condition is true.
    /// </summary>
    public class NumberComparison : Condition {
        public NumberValue LeftOperand { get; set; }
        public NumberComparisonOperator Operator { get; set; }
        public NumberValue RightOperand { get; set; }

        public NumberComparison() {
            // Some defaults
            LeftOperand = new NumberValue(0);
            Operator = new LessThanNumberOperator();
            RightOperand = new NumberValue(0);
        }

        public NumberComparison(NumberValue left, NumberComparisonOperator op, NumberValue right) {
            LeftOperand = left;
            Operator = op;
            RightOperand = right;
        }

        public override bool IsTrue() {
            return Operator.Compare(LeftOperand, RightOperand).IsTrue();
        }

        public override string GetId() {
            return "NumberComparison";
        }

        public override String GetStringValue() {
            return "" + IsTrue();
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { LeftOperand, Operator, RightOperand };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                LeftOperand = child as NumberValue;
            else if (index == 1)
                Operator = child as NumberComparisonOperator;
            else if (index == 2)
                RightOperand = child as NumberValue;
            else
                throw new ArgumentOutOfRangeException("Index out of range: " + index);
        }
    }

    /// <summary>
    /// A Condition that compares 2 other Conditions
    /// and indicates if they are both true or if either
    /// are true depending on th LogicOperator used.
    /// </summary>
    public class LogicComparison : Condition {
        public Condition LeftOperand { get; private set; }
        public LogicOperator Operator { get; private set; }
        public Condition RightOperand { get; private set; }

        public LogicComparison() {
            // Some defaults
            LeftOperand = new BoolCondition(false);
            Operator = new OrLogicOperator();
            RightOperand = new BoolCondition(false);
        }

        public LogicComparison(Condition left, LogicOperator op, Condition right) {
            LeftOperand = left;
            Operator = op;
            RightOperand = right;
        }

        public override bool IsTrue() {
            return Operator.Compare(LeftOperand, RightOperand).IsTrue();
        }

        public override string GetId() {
            return "LogicComparison";
        }

        public override String GetStringValue() {
            return "" + IsTrue();
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { LeftOperand, Operator, RightOperand };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                LeftOperand = child as Condition;
            else if (index == 1)
                Operator = child as LogicOperator;
            else if (index == 2)
                RightOperand = child as Condition;
            else
                throw new ArgumentOutOfRangeException("Index out of range: " + index);
        }
    }

    public class StringComparison : Condition {
        public StringValue LeftOperand { get; private set; }
        public StringComparisonOperator Operator { get; private set; }
        public StringValue RightOperand { get; private set; }

        public StringComparison() {
            // Some defaults
            LeftOperand = new StringValue("String1");
            Operator = new EqualsStringOperator();
            RightOperand = new StringValue("String2");
        }

        public StringComparison(StringValue left, StringComparisonOperator op, StringValue right) {
            LeftOperand = left;
            Operator = op;
            RightOperand = right;
        }

        public override bool IsTrue() {
            return Operator.Compare(LeftOperand, RightOperand).IsTrue();
        }

        public override string GetId() {
            return "StringComparison";
        }

        public override String GetStringValue() {
            return "" + IsTrue();
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { LeftOperand, Operator, RightOperand };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                LeftOperand = child as StringValue;
            else if (index == 1)
                Operator = child as StringComparisonOperator;
            else if (index == 2)
                RightOperand = child as StringValue;
            else
                throw new ArgumentOutOfRangeException("Index out of range: " + index);
        }
    }

    /// <summary>
    /// Represents a constant String value.
    /// </summary>
    public class StringValue : Value {
        public virtual String Value { get; protected set; }

        protected StringValue() { }

        public StringValue(String value) {
            Value = value;
        }

        public override String GetId() {
            return "StringValue";
        }

        public override String GetStringValue() {
            return Value;
        }
    }

    public class VariableStringValue : StringValue {
        private StringVariable _variable;
        private StrategyRunParams _context;

        public override String Value {
            get {
                return _variable.VariableRetrievalMethod.Invoke(_context).Value;
            }
        }

        public VariableStringValue(StrategyRunParams context, StringVariable variable) {
            _context = context;
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
        public virtual double Value { get; private set; }

        public NumberValue() {
            Value = 0;
        }

        public override String GetId() {
            return "NumberValue";
        }

        public override String GetStringValue() {
            return "" + Value;
        }

        public NumberValue(double value) {
            Value = value;
        }
    }

    public class VariableNumberValue : NumberValue {
        private NumberVariable _variable;
        private StrategyRunParams _context;

        public override double Value {
            get {
                return _variable.VariableRetrievalMethod.Invoke(_context).Value;
            }
        }

        public VariableNumberValue(StrategyRunParams context, NumberVariable variable) {
            _context = context;
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

        public override double Value {
            get {
                return Operator.Operate(LeftOperand, RightOperand).Value;
            }
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

    /// <summary>
    /// Does a math operation on 2 numbers.
    /// </summary>
    public abstract class MathOperator : Operator {
        public abstract NumberValue Operate(NumberValue num1, NumberValue num2);
        public abstract override String GetStringValue();
        public override String GetId() {
            return "MathOperator";
        }
        public override Operator[] GetOptions() {
            return new Operator[] {
                new AdditionOperator(),
                new SubtractionOperator(),
                new MultiplicationOperator(),
                new DivisionOperator(),
                new ExponentOperator()
            };
        }
    }

    public class AdditionOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2) {
            return new NumberValue(num1.Value + num2.Value);
        }

        public override String GetStringValue() {
            return "+"; 
        }
    }

    public class SubtractionOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2) {
            return new NumberValue(num1.Value - num2.Value);
        }

        public override String GetStringValue() {
            return "-";
        }
    }

    public class MultiplicationOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2) {
            return new NumberValue(num1.Value * num2.Value);
        }

        public override String GetStringValue() {
            return "x";
        }
    }

    public class DivisionOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2) {
            return new NumberValue(num1.Value / num2.Value);
        }
        public override String GetStringValue() {
            return "/";
        }
    }

    public class ExponentOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2) {
            return new NumberValue(Math.Pow(num1.Value, num2.Value));
        }

        public override String GetStringValue() {
            return "^";
        }
    }

    /// <summary>
    /// The options for comparing 2 strings
    /// </summary>
    public abstract class StringComparisonOperator : Operator {
        public abstract Condition Compare(StringValue left, StringValue right);
        public abstract override String GetStringValue();
        public override String GetId() {
            return "StringComparisonOperator";
        }

        public override Operator[] GetOptions() {
            return new Operator[] {
                new LessThanStringOperator(),
                new GreaterThanStringOperator(),
                new EqualsStringOperator(),
                new DoesNotEqualStringOperator()
            };
        }
    }

    public class LessThanStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right) {
            return new BoolCondition(left.Value.CompareTo(right.Value) < 0);
        }

        public override String GetStringValue() {
            return "<";
        }
    }

    public class GreaterThanStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right) {
            return new BoolCondition(left.Value.CompareTo(right.Value) > 0);
        }

        public override String GetStringValue() {
            return ">";
        }
    }

    public class EqualsStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right) {
            return new BoolCondition(left.Value.CompareTo(right.Value) == 0);
        }
        public override String GetStringValue() {
            return "==";
        }
    }

    public class DoesNotEqualStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right) {
            return new BoolCondition(left.Value.CompareTo(right.Value) != 0);
        }
        public override String GetStringValue() {
            return "!=";
        }
    }

    /// <summary>
    /// The options for comparing 2 numbers
    /// </summary>
    public abstract class NumberComparisonOperator : Operator {
        public abstract Condition Compare(NumberValue left, NumberValue right);
        public abstract override String GetStringValue();
        public override String GetId() {
            return "NumberComparisonOperator";
        }

        public override Operator[] GetOptions() {
            return new Operator[] {
                new LessThanNumberOperator(),
                new GreaterThanNumberOperator(),
                new EqualsNumberOperator(),
                new DoesNotEqualNumberOperator()
            };
        }
    }

    public class LessThanNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right) {
            return new BoolCondition(left.Value < right.Value);
        }

        public override String GetStringValue() {
            return "<";
        }
    }

    public class GreaterThanNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right) {
            return new BoolCondition(left.Value > right.Value);
        }

        public override String GetStringValue() {
            return ">";
        }
    }

    public class EqualsNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right) {
            return new BoolCondition(left.Value == right.Value);
        }

        public override String GetStringValue() {
            return "==";
        }
    }

    public class DoesNotEqualNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right) {
            return new BoolCondition(left.Value != right.Value);
        }

        public override String GetStringValue() {
            return "!=";
        }
    }

    /// <summary>
    /// The options for comparing 2 Conditions.
    /// </summary>
    public abstract class LogicOperator : Operator {
        public abstract Condition Compare(Condition left, Condition right);
        public abstract override String GetStringValue();
        public override String GetId() {
            return "LogicOperator";
        }

        public override Operator[] GetOptions() {
            return new Operator[] {
                new AndLogicOperator(),
                new OrLogicOperator()
            };
        }
    }

    public class AndLogicOperator : LogicOperator {
        public override Condition Compare(Condition left, Condition right) {
            return new BoolCondition(left.IsTrue() && right.IsTrue());
        }

        public override string GetStringValue() {
            return "&&";
        }
    }

    public class OrLogicOperator : LogicOperator {
        public override Condition Compare(Condition left, Condition right) {
            return new BoolCondition(left.IsTrue() || right.IsTrue());
        }

        public override string GetStringValue() {
            return "||";
        }
    }
}
