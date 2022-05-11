using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public static class ActionType {
        public static String MultiAction = "MultiAction";
        public static String SimpleAction = "SimpleAction";
    }

    public static class ActionId {
        public static readonly String BuyMax = "BuyMax";
        public static readonly String BuySmall = "BuySmall";
        public static readonly String SellMax = "SellMax";
        public static readonly String SellSmall = "SellSmall";
        public static readonly String ChangeVariableTo = "ChangeVariableTo";
        public static readonly String DoNothing = "DoNothing";
        public static readonly String NotSet = "NotSet";
        public static readonly String MultiAction = "MultiAction";
    }

    public abstract class StratAction : ITreeNode {
        public abstract void Execute(StrategyRuntimeContext context);
        public abstract String GetStringValue(StrategyRuntimeContext context);
        public abstract String GetId();
        public abstract String GetDescription();
        public abstract String GetLabel();
        public abstract StratAction Clone();

        public abstract JsonObject ToJson();
        public abstract void FromJson(JsonObject json);

        public virtual ITreeNode[] GetChildren() {
            // No Children by default
            return null;
        }

        public virtual void SetChild(int index, ITreeNode child) {
            // No children by default
            throw new InvalidOperationException("No children.");
        }
    }

    public class MultiAction : StratAction {
        public StratAction Action1 { get; set; }
        public StratAction Action2 { get; set; }

        public MultiAction(StratAction action1, StratAction action2) {
            Action1 = action1;
            Action2 = action2;
        }

        public override void Execute(StrategyRuntimeContext context) {
            if (Action1 != null)
                Action1.Execute(context);
            if (Action2 != null)
                Action2.Execute(context);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            String action1Str = "Action1";
            String action2Str = "Action2";
            if (Action1 != null) action1Str = Action1.GetStringValue(context);
            if (Action2 != null) action2Str = Action2.GetStringValue(context);
            return action1Str + " and " + action2Str;
        }

        public override string GetId() {
            return ActionType.MultiAction;
        }

        public override String GetDescription() {
            return "Runs 2 actions.";
        }

        public override String GetLabel() {
            return GetId();
        }

        public override StratAction Clone() {
            return new MultiAction((StratAction)Action1.Clone(), (StratAction)Action2.Clone());
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { Action1, Action2 };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                Action1 = child as StratAction;
            else if (index == 1)
                Action2 = child as StratAction;
            else
                throw new ArgumentOutOfRangeException("This action only has 2 children. Tried to set child at index " + index);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Action1", Action1.ToJson());
            obj.Put("Action2", Action2.ToJson());
            return obj;
        }

        public override void FromJson(JsonObject json) {
            Dictionary<String, StratAction> actions = Actions.GetActions();
            JsonObject action1Json = json.GetObject("Action1");
            JsonObject action2Json = json.GetObject("Action2");
            StratAction action1 = actions[action1Json.GetString("Id")].Clone();
            action1.FromJson(action1Json);
            Action1 = action1;
            StratAction action2 = actions[action2Json.GetString("Id")].Clone();
            action2.FromJson(action2Json);
            Action2 = action2;
        }
    }

    /// <summary>
    /// A SimpleAction does one thing and does not need a parameters.
    /// </summary>
    public class SimpleAction : StratAction {
        private String _description;
        private String _id;

        public override string GetId() {
            return _id;
        }

        /// <summary>
        /// Describes this action.
        /// </summary>
        public override String GetDescription() {
            return _description;
        }

        public override StratAction Clone() {
            return new SimpleAction(_id, _description, ActionMethod);
        }

        /// <summary>
        /// A method that takes in the strategy run context and performs
        /// and action.
        /// </summary>
        public Action<StrategyRuntimeContext> ActionMethod { get; private set; }

        public SimpleAction(String id, String description, Action<StrategyRuntimeContext> actionMethod) {
            _id = id;
            _description = description;
            ActionMethod = actionMethod;
        }

        public override void Execute(StrategyRuntimeContext context) {
            ActionMethod.Invoke(context);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            // No associated value
            return "";
        }

        public override String GetLabel() {
            return GetId();
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            return obj;
        }

        public override void FromJson(JsonObject json) {
            // Nothing to do, the ID is inherent.
        }
    }

    /// <summary>
    /// A ValueAction needs a Value selected from the user.
    /// </summary>
    public class ValueChanger : StratAction {
        private String _description;
        private ToOperator _operator = new ToOperator(); // Just to indicate the change in the tree

        public override string GetId() {
            return "ValueChanger";
        }

        /// <summary>
        /// Describes this action.
        /// </summary>
        public override String GetDescription() {
            return _description;
        }

        /// <summary>
        /// The variable that will be changed.
        /// </summary>
        public IValue OriginalValue { get; set; } = null;

        /// <summary>
        /// The value to change to
        /// </summary>
        public IValue TargetValue { get; set; } = null;

        public ValueChanger(String description, IValue originalValue) {
            _description = description;
            OriginalValue = originalValue;

            if (originalValue.GetValueType() == ValueType.Number)
                TargetValue = new SimpleNumberValue(0);
            else if (originalValue.GetValueType() == ValueType.String)
                TargetValue = new SimpleStringValue();
            else
                throw new NotSupportedException("Type not supported: " + originalValue.GetValueType());
        }

        public ValueChanger(String description, IValue originalValue, IValue targetValue) {
            _description = description;
            OriginalValue = originalValue;
            TargetValue = targetValue;
        }

        public ValueType GetValueType() {
            return OriginalValue.GetValueType();
        }

        public override void Execute(StrategyRuntimeContext context) {
            IValue target = TargetValue;
            // If the original is a uservar, use the context version
            IVariable targetVar = TargetValue as IVariable;
            if (targetVar != null) {
                if (context.UserVars.ContainsKey(targetVar.GetVariableName()))
                    target = context.UserVars[targetVar.GetVariableName()];
            }

            // Use the context's version since the one we were given could
            // be a template not the current one.
            IVariable originalVar = OriginalValue as IVariable;
            context.UserVars[originalVar.GetVariableName()].SetFromValue(context, target);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return ""; // No value makes sense here
        }

        public override String GetLabel() {
            String result = "Change " + OriginalValue.GetLabel();
            return result;
        }

        public override StratAction Clone() {
            if (TargetValue == null)
                return new ValueChanger(_description, OriginalValue.Clone());
            return new ValueChanger(_description, OriginalValue.Clone(), TargetValue.Clone());
        }

        public override ITreeNode[] GetChildren() {
            return new ITreeNode[] { OriginalValue, _operator, TargetValue };
        }

        public override void SetChild(int index, ITreeNode child) {
            if (index == 0)
                OriginalValue = child as IValue;
            else if (index == 1)
                _operator = child as ToOperator;
            else if (index == 2)
                TargetValue = child as IValue;
            else
                throw new ArgumentOutOfRangeException("Index out of range: " + index);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Description", GetDescription());
            obj.Put("OriginalValue", OriginalValue.ToJson());
            if (TargetValue != null)
                obj.Put("TargetValue", TargetValue.ToJson());
            return obj;
        }

        public override void FromJson(JsonObject json) {
            JsonObject originalJson = json.GetObject("OriginalValue");
            OriginalValue = Values.GetValues()[originalJson.GetString("Id")].Clone();
            OriginalValue.FromJson(originalJson);

            JsonObject targetJson = json.GetObject("TargetValue");
            if (targetJson != null) {
                TargetValue = Values.GetValues()[targetJson.GetString("Id")].Clone();
                TargetValue.FromJson(targetJson);
            }

            _description = json.GetString("Description");
        }
    }
}