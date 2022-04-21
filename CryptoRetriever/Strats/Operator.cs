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
        public abstract String GetDescription();
        public abstract String GetStringValue(StrategyRuntimeContext context);
        public abstract Operator Clone();
        
        public virtual String GetLabel() {
            return GetStringValue(null);
        }

        public virtual ITreeNode[] GetChildren() {
            // By default, no children
            return null;
        }

        public virtual void SetChild(int index, ITreeNode child) {
            // By default, no children
            throw new Exception("This node has no children");
        }

        /// <returns>
        /// Gets the available operators for this operator type indexed in a map
        /// by ID..
        /// </returns>
        public abstract Dictionary<String, Operator> GetOptions();
    }

    /// <summary>
    /// Does a math operation on 2 numbers.
    /// </summary>
    public abstract class MathOperator : Operator {
        public abstract NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context);
        public abstract override String GetStringValue(StrategyRuntimeContext context);
        public abstract override String GetDescription();
        public abstract override Operator Clone();
        public override String GetId() {
            return "MathOperator";
        }
        public override Dictionary<String, Operator> GetOptions() {
            return Operators.GetMathOperators();
        }
    }

    public class AdditionOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(num1.GetValue(context) + num2.GetValue(context));
        }

        public override String GetId() {
            return "AdditionOperator";
        }

        public override String GetDescription() {
            return "Adds two numbers together.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "+";
        }

        public override Operator Clone() {
            return new AdditionOperator();
        }
    }

    public class SubtractionOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(num1.GetValue(context) - num2.GetValue(context));
        }

        public override String GetId() {
            return "SubtractionOperator";
        }

        public override String GetDescription() {
            return "Subtracts the second number from the first.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "-";
        }

        public override Operator Clone() {
            return new SubtractionOperator();
        }
    }

    public class MultiplicationOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(num1.GetValue(context) * num2.GetValue(context));
        }

        public override String GetId() {
            return "MultiplicationOperator";
        }

        public override String GetDescription() {
            return "Multiplies two numbers together.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "x";
        }

        public override Operator Clone() {
            return new MultiplicationOperator();
        }
    }

    public class DivisionOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(num1.GetValue(context) / num2.GetValue(context));
        }

        public override String GetId() {
            return "DivisionOperator";
        }

        public override String GetDescription() {
            return "Divides the first number by the second.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "/";
        }

        public override Operator Clone() {
            return new DivisionOperator();
        }
    }

    public class ExponentOperator : MathOperator {
        public override NumberValue Operate(NumberValue num1, NumberValue num2, StrategyRuntimeContext context) {
            return new NumberValue(Math.Pow(num1.GetValue(context), num2.GetValue(context)));
        }

        public override String GetId() {
            return "ExponentOperator";
        }

        public override String GetDescription() {
            return "Raises the first number to the second number power.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "^";
        }

        public override Operator Clone() {
            return new ExponentOperator();
        }
    }

    /// <summary>
    /// The options for comparing 2 strings
    /// </summary>
    public abstract class StringComparisonOperator : Operator {
        public abstract Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context);
        public abstract override String GetStringValue(StrategyRuntimeContext context);
        public abstract override String GetDescription();
        public override String GetId() {
            return "StringComparisonOperator";
        }

        public override Dictionary<String, Operator> GetOptions() {
            return Operators.GetStringComparisonOperators();
        }
    }

    public class LessThanStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context).CompareTo(right.GetValue(context)) < 0);
        }

        public override String GetId() {
            return "LessThanStringOperator";
        }

        public override String GetDescription() {
            return "Indicates if the first string is less than the second (alphabetically).";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "<";
        }

        public override Operator Clone() {
            return new LessThanStringOperator();
        }
    }

    public class GreaterThanStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context).CompareTo(right.GetValue(context)) > 0);
        }

        public override String GetId() {
            return "GreaterThanStringOperator";
        }

        public override String GetDescription() {
            return "Indicates if the first string is greater than the second (alphabetically).";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return ">";
        }

        public override Operator Clone() {
            return new GreaterThanStringOperator();
        }
    }

    public class EqualsStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context).CompareTo(right.GetValue(context)) == 0);
        }

        public override String GetId() {
            return "EqualsStringOperator";
        }

        public override String GetDescription() {
            return "Indicates if the first string has the same content as the second.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "==";
        }

        public override Operator Clone() {
            return new EqualsStringOperator();
        }
    }

    public class DoesNotEqualStringOperator : StringComparisonOperator {
        public override Condition Compare(StringValue left, StringValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context).CompareTo(right.GetValue(context)) != 0);
        }

        public override String GetId() {
            return "DoesNotEqualStringOperator";
        }

        public override String GetDescription() {
            return "Indicates if the first string has different content than the second string.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "!=";
        }

        public override Operator Clone() {
            return new DoesNotEqualStringOperator();
        }
    }

    /// <summary>
    /// The options for comparing 2 numbers
    /// </summary>
    public abstract class NumberComparisonOperator : Operator {
        public abstract Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context);
        public abstract override String GetStringValue(StrategyRuntimeContext context);
        public abstract override String GetDescription();
        public override String GetId() {
            return "NumberComparisonOperator";
        }

        public override Dictionary<String, Operator> GetOptions() {
            return Operators.GetNumberComparisonOperators();
        }
    }

    public class LessThanNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context) < right.GetValue(context));
        }

        public override String GetId() {
            return "LessThanNumberOperator";
        }

        public override String GetDescription() {
            return "Indicates if the first number is less than the second number.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "<";
        }

        public override Operator Clone() {
            return new LessThanNumberOperator();
        }
    }

    public class GreaterThanNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context) > right.GetValue(context));
        }

        public override String GetId() {
            return "GreaterThanNumberOperator";
        }

        public override String GetDescription() {
            return "Indicates if the first number is greater than the second number.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return ">";
        }

        public override Operator Clone() {
            return new GreaterThanNumberOperator();
        }
    }

    public class EqualsNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context) == right.GetValue(context));
        }

        public override String GetId() {
            return "EqualsNumberOperator";
        }

        public override String GetDescription() {
            return "Indicates if the two numbers are equal.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "==";
        }

        public override Operator Clone() {
            return new EqualsNumberOperator();
        }
    }

    public class DoesNotEqualNumberOperator : NumberComparisonOperator {
        public override Condition Compare(NumberValue left, NumberValue right, StrategyRuntimeContext context) {
            return new BoolCondition(left.GetValue(context) != right.GetValue(context));
        }

        public override String GetId() {
            return "DoesNotEqualNumberOperator";
        }

        public override String GetDescription() {
            return "Indicates if the two numbers are not equal.";
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "!=";
        }

        public override Operator Clone() {
            return new DoesNotEqualNumberOperator();
        }
    }

    /// <summary>
    /// The options for comparing 2 Conditions.
    /// </summary>
    public abstract class LogicOperator : Operator {
        public abstract Condition Compare(Condition left, Condition right, StrategyRuntimeContext context);
        public abstract override String GetStringValue(StrategyRuntimeContext context);
        public abstract override String GetDescription();
        public override String GetId() {
            return "LogicOperator";
        }

        public override Dictionary<String, Operator> GetOptions() {
            return Operators.GetLogicOperators();
        }
    }

    public class AndLogicOperator : LogicOperator {
        public override Condition Compare(Condition left, Condition right, StrategyRuntimeContext context) {
            return new BoolCondition(left.IsTrue(context) && right.IsTrue(context));
        }

        public override String GetId() {
            return "AndLogicOperator";
        }

        public override String GetDescription() {
            return "Indicates if the first and second conditions are both true.";
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return "&&";
        }

        public override Operator Clone() {
            return new AndLogicOperator();
        }
    }

    public class OrLogicOperator : LogicOperator {
        public override Condition Compare(Condition left, Condition right, StrategyRuntimeContext context) {
            return new BoolCondition(left.IsTrue(context) || right.IsTrue(context));
        }

        public override String GetId() {
            return "OrLogicOperator";
        }

        public override String GetDescription() {
            return "Indicates if the first condition is true or the second condition is true.";
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return "||";
        }

        public override Operator Clone() {
            return new OrLogicOperator();
        }
    }
}
