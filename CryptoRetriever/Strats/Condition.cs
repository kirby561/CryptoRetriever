using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// A Condition is something that can be true or false
    /// but may need to evaluate something to figure that out.
    /// </summary>
    public abstract class Condition : ITreeNode {
        public abstract bool IsTrue(StrategyRuntimeContext context);
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

        public override bool IsTrue(StrategyRuntimeContext context) {
            return _isTrue;
        }

        public override string GetId() {
            return _name;
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + IsTrue(context);
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

        public override bool IsTrue(StrategyRuntimeContext context) {
            return Operator.Compare(LeftOperand, RightOperand, context).IsTrue(context);
        }

        public override string GetId() {
            return "NumberComparison";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + IsTrue(context);
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

        public override bool IsTrue(StrategyRuntimeContext context) {
            return Operator.Compare(LeftOperand, RightOperand, context).IsTrue(context);
        }

        public override string GetId() {
            return "LogicComparison";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + IsTrue(context);
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

    /// <summary>
    /// A condition who's truth is determined by comparing 2 strings.
    /// </summary>
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

        public override bool IsTrue(StrategyRuntimeContext context) {
            return Operator.Compare(LeftOperand, RightOperand, context).IsTrue(context);
        }

        public override string GetId() {
            return "StringComparison";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + IsTrue(context);
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
}
