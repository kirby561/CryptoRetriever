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
        public static readonly String SellMax = "SellMax";
        public static readonly String ChangeStateTo = "ChangeStateTo";
        public static readonly String ChangeNextStateTo = "ChangeNextStateTo";
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
            return GetId() + ": " + Action1.GetId() + " and " + Action2.GetId();
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
    /// A NumberAction needs a NumberVariable from the user
    /// </summary>
    public class NumberAction : StratAction {
        private String _id;
        private String _description;

        /// <summary>
        /// Describes this action.
        /// </summary>
        public override String GetDescription() {
            return _description;
        }

        /// <summary>
        /// The variable needed by this action.
        /// </summary>
        public NumberValue Value { get; set; }

        /// <summary>
        /// A method that takes in the strategy run context and performs
        /// and action.
        /// </summary>
        public Action<StrategyRuntimeContext, NumberValue> ActionMethod { get; private set; }

        public NumberAction(String id, String description, Action<StrategyRuntimeContext, NumberValue> actionMethod, NumberValue numberValue) {
            _id = id;
            _description = description;
            ActionMethod = actionMethod;
            Value = numberValue;
        }

        public override void Execute(StrategyRuntimeContext context) {
            ActionMethod.Invoke(context, Value);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            String stringValue = "";
            if (Value != null)
                stringValue = "" + Value.GetValue(context);
            return stringValue;
        }

        public override string GetId() {
            return _id;
        }

        public override String GetLabel() {
            String result = GetId();
            String value = GetStringValue(new ExampleStrategyRunParams());
            if (!String.IsNullOrWhiteSpace(value))
                result += ": " + value;
            return result;
        }

        public override StratAction Clone() {
            return new NumberAction(_id, _description, ActionMethod, Value);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", Value.ToJson());
            return obj;
        }

        public override void FromJson(JsonObject json) {
            JsonObject val = json.GetObject("Value");
            Value = (NumberValue)Values.GetValues()[val.GetString("Id")].Clone();
            Value.FromJson(val);
        }
    }

    /// <summary>
    /// A StringAction needs a StringVariable from the user.
    /// </summary>
    public class StringAction : StratAction {
        private String _id;
        private String _description;

        public override string GetId() {
            return _id;
        }

        /// <summary>
        /// Describes this action.
        /// </summary>
        public override String GetDescription() {
            return _description;
        }

        /// <summary>
        /// The variable needed by this action.
        /// This needs to be set by the user.
        /// </summary>
        public StringValue Value { get; set; } = null;

        /// <summary>
        /// A method that takes in the strategy run context and performs
        /// and action.
        /// </summary>
        public Action<StrategyRuntimeContext, StringValue> ActionMethod { get; private set; }

        public StringAction(String id, String description, Action<StrategyRuntimeContext, StringValue> actionMethod) {
            _id = id;
            _description = description;
            ActionMethod = actionMethod;
        }

        public StringAction(String id, String description, Action<StrategyRuntimeContext, StringValue> actionMethod, StringValue stringValue) {
            _id = id;
            _description = description;
            ActionMethod = actionMethod;
            Value = stringValue;
        }

        public override void Execute(StrategyRuntimeContext context) {
            ActionMethod.Invoke(context, Value);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            String stringValue = "";
            if (Value != null)
                stringValue = Value.GetValue(context);
            return stringValue;
        }

        public override String GetLabel() {
            String result = GetId();
            String value = GetStringValue(new ExampleStrategyRunParams());
            if (!String.IsNullOrWhiteSpace(value))
                result += ": " + value;
            return result;
        }

        public override StratAction Clone() {
            return new StringAction(_id, _description, ActionMethod, Value);
        }

        public override JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Id", GetId());
            obj.Put("Value", Value.ToJson());
            return obj;
        }

        public override void FromJson(JsonObject json) {
            JsonObject val = json.GetObject("Value");
            Value = (StringValue)Values.GetValues()[val.GetString("Id")].Clone();
            Value.FromJson(val);
        }
    }
}
