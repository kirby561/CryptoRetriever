using CryptoRetriever.Utility.JsonObjects;
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
        public abstract String GetDescription();
        public abstract String GetLabel();
        public abstract String GetStringValue(StrategyRuntimeContext context);
        public abstract Condition Clone();

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

        public override string GetDescription() {
            return "A condition that is either always true or always false.";
        }

        public override String GetLabel() {
            return GetId();
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + IsTrue(context);
        }

        public override Condition Clone() {
            return new BoolCondition(_name, _isTrue);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", GetStringValue(null));
            return obj;
        }

        public override void FromJson(JsonObject json) {
            _name = json.GetString("Id");
            _isTrue = "True".Equals(json.GetString("Value"));
        }
    }

    /// <summary>
    /// A Condition that compares 2 numbers and is true if
    /// the condition is true.
    /// </summary>
    public class NumberComparison : Condition {
        public INumberValue LeftOperand { get; set; }
        public NumberComparisonOperator Operator { get; set; }
        public INumberValue RightOperand { get; set; }

        public NumberComparison() {
            // Some defaults
            LeftOperand = new SimpleNumberValue(0);
            Operator = new LessThanNumberOperator();
            RightOperand = new SimpleNumberValue(0);
        }

        public NumberComparison(INumberValue left, NumberComparisonOperator op, INumberValue right) {
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

        public override string GetDescription() {
            return "Compares 2 numbers with the given operator and indicates if the result is true.";
        }

        public override String GetLabel() {
            return GetId();
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + IsTrue(context);
        }

        public override Condition Clone() {
            return new NumberComparison((INumberValue)LeftOperand.Clone(), (NumberComparisonOperator)Operator.Clone(), (INumberValue)RightOperand.Clone());
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { LeftOperand, Operator, RightOperand };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                LeftOperand = (INumberValue)child;
            else if (index == 1)
                Operator = (NumberComparisonOperator)child;
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
            Operator = (NumberComparisonOperator)Operators.GetNumberComparisonOperators()[json.GetString("Operator")].Clone();
            JsonObject rightOperandObj = json.GetObject("RightOperand");
            RightOperand = (INumberValue)values[rightOperandObj.GetString("Id")].Clone();
            RightOperand.FromJson(rightOperandObj);
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
            LeftOperand = new BoolCondition("False", false);
            Operator = new OrLogicOperator();
            RightOperand = new BoolCondition("False", false);
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

        public override string GetDescription() {
            return "Compares 2 conditions with the given operator and indicates if the result is true.";
        }

        public override String GetLabel() {
            return GetId();
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + IsTrue(context);
        }

        public override Condition Clone() {
            return new LogicComparison((Condition)LeftOperand.Clone(), (LogicOperator)Operator.Clone(), (Condition)RightOperand.Clone());
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { LeftOperand, Operator, RightOperand };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                LeftOperand = (Condition)child;
            else if (index == 1)
                Operator = (LogicOperator)child;
            else if (index == 2)
                RightOperand = (Condition)child;
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
            Dictionary<String, Condition> conditions = Conditions.GetConditions();
            JsonObject leftOperandObj = json.GetObject("LeftOperand");
            LeftOperand = conditions[leftOperandObj.GetString("Id")].Clone();
            LeftOperand.FromJson(leftOperandObj);
            Operator = (LogicOperator)Operators.GetLogicOperators()[json.GetString("Operator")].Clone();
            JsonObject rightOperandObj = json.GetObject("RightOperand");
            RightOperand = conditions[rightOperandObj.GetString("Id")].Clone();
            RightOperand.FromJson(rightOperandObj);
        }
    }

    /// <summary>
    /// A condition who's truth is determined by comparing 2 strings.
    /// </summary>
    public class StringComparison : Condition {
        public IStringValue LeftOperand { get; private set; }
        public StringComparisonOperator Operator { get; private set; }
        public IStringValue RightOperand { get; private set; }

        public StringComparison() {
            // Some defaults
            LeftOperand = new SimpleStringValue("String1");
            Operator = new EqualsStringOperator();
            RightOperand = new SimpleStringValue("String2");
        }

        public StringComparison(IStringValue left, StringComparisonOperator op, IStringValue right) {
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

        public override string GetDescription() {
            return "Compares 2 strings with the given operator and indicates if the result is true.";
        }

        public override String GetLabel() {
            return GetId();
        }

        public override String GetStringValue(StrategyRuntimeContext context) {
            return "" + IsTrue(context);
        }

        public override Condition Clone() {
            return new StringComparison((IStringValue)LeftOperand.Clone(), (StringComparisonOperator)Operator.Clone(), (IStringValue)RightOperand.Clone());
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { LeftOperand, Operator, RightOperand };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                LeftOperand = (IStringValue)child;
            else if (index == 1)
                Operator = (StringComparisonOperator)child;
            else if (index == 2)
                RightOperand = (IStringValue)child;
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
            LeftOperand = (IStringValue)values[leftOperandObj.GetString("Id")].Clone();
            LeftOperand.FromJson(leftOperandObj);
            Operator = (StringComparisonOperator)Operators.GetStringComparisonOperators()[json.GetString("Operator")].Clone();
            JsonObject rightOperandObj = json.GetObject("RightOperand");
            RightOperand = (IStringValue)values[rightOperandObj.GetString("Id")].Clone();
            RightOperand.FromJson(rightOperandObj);
        }
    }
}
