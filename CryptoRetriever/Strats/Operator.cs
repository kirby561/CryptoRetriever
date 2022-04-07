using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// An Operator is a node that performs an operation or compares
    /// another node or nodes to get a value. Operators have a fixed
    /// number of operations they can perform which can be retrieved 
    /// via the GetOptions method.
    /// </summary>
    public abstract class Operator : ITreeNode {
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
        public abstract Operator[] GetOptions();
    }

    /// <summary>
    /// Does a math operation on 2 numbers.
    /// </summary>
    public abstract class MathOperator : Operator {
        public abstract NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context);
        public abstract override String GetStringValue(StrategyRuntimeContext context);
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
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(num1.GetValue(context) + num2.GetValue(context));
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "+";
        }
    }

    public class SubtractionOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(num1.GetValue(context) - num2.GetValue(context));
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "-";
        }
    }

    public class MultiplicationOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(num1.GetValue(context) * num2.GetValue(context));
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "x";
        }
    }

    public class DivisionOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(num1.GetValue(context) / num2.GetValue(context));
        }
        public override String GetStringValue(StrategyRuntimeContext context) {
            return "/";
        }
    }

    public class ExponentOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(Math.Pow(num1.GetValue(context), num2.GetValue(context)));
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "^";
        }
    }

    /// <summary>
    /// The options for comparing 2 strings
    /// </summary>
    public abstract class StringComparisonOperator : Operator {
        public abstract Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context);
        public abstract override String GetStringValue(StrategyRuntimeContext context);
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
        public override Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context).CompareTo(right.GetValue(context)) < 0);
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "<";
        }
    }

    public class GreaterThanStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context).CompareTo(right.GetValue(context)) > 0);
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return ">";
        }
    }

    public class EqualsStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context).CompareTo(right.GetValue(context)) == 0);
        }
        public override String GetStringValue(StrategyRuntimeContext context) {
            return "==";
        }
    }

    public class DoesNotEqualStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context).CompareTo(right.GetValue(context)) != 0);
        }
        public override String GetStringValue(StrategyRuntimeContext context) {
            return "!=";
        }
    }

    /// <summary>
    /// The options for comparing 2 numbers
    /// </summary>
    public abstract class NumberComparisonOperator : Operator {
        public abstract Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context);
        public abstract override String GetStringValue(StrategyRuntimeContext context);
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
        public override Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context) < right.GetValue(context));
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "<";
        }
    }

    public class GreaterThanNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context) > right.GetValue(context));
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return ">";
        }
    }

    public class EqualsNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context) == right.GetValue(context));
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "==";
        }
    }

    public class DoesNotEqualNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context) != right.GetValue(context));
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "!=";
        }
    }

    /// <summary>
    /// The options for comparing 2 Conditions.
    /// </summary>
    public abstract class LogicOperator : Operator {
        public abstract Condition Compare(Condition left, Condition right, StrategyRuntimeContext context);
        public abstract override String GetStringValue(StrategyRuntimeContext context);
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
        public override Condition Compare(Condition left, Condition right, StrategyRuntimeContext context) {
            return new BoolCondition(left.IsTrue(context) && right.IsTrue(context));
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return "&&";
        }
    }

    public class OrLogicOperator : LogicOperator {
        public override Condition Compare(Condition left, Condition right, StrategyRuntimeContext context) {
            return new BoolCondition(left.IsTrue(context) || right.IsTrue(context));
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return "||";
        }
    }
}
