using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public abstract class StratAction : ITreeNode {
        public abstract void Execute(StrategyRuntimeContext context);
        public abstract string GetStringValue(StrategyRuntimeContext context);
        public abstract string GetId();

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
            return "MultiAction";
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
    }

    /// <summary>
    /// A SimpleAction does one thing and does not need a parameters.
    /// </summary>
    public class SimpleAction : StratAction {
        /// <summary>
        /// Describes this action.
        /// </summary>
        public String Description { get; private set; }

        /// <summary>
        /// A method that takes in the strategy run context and performs
        /// and action.
        /// </summary>
        public Action<StrategyRuntimeContext> ActionMethod { get; private set; }

        public SimpleAction(String description, Action<StrategyRuntimeContext> actionMethod) {
            Description = description;
            ActionMethod = actionMethod;
        }

        public override void Execute(StrategyRuntimeContext context) {
            ActionMethod.Invoke(context);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return Description;
        }

        public override string GetId() {
            return "SimpleAction";
        }
    }

    /// <summary>
    /// A DoNothingAction is a SimpleAction that doesn't do anything.
    /// </summary>
    public class DoNothingAction : SimpleAction {
        public DoNothingAction()
            : base("Do nothing", null) { }

        public override void Execute(StrategyRuntimeContext context) {
            // Nothing to do
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            return "Do nothing";
        }
    }

    /// <summary>
    /// Can be used to initialize a multi-action
    /// and have the UI not display "Do Nothing"
    /// </summary>
    public class FutureAction : DoNothingAction {
        public override string GetStringValue(StrategyRuntimeContext context) {
            return "Action";
        }
    }

    /// <summary>
    /// A NumberAction needs a NumberVariable from the user
    /// </summary>
    public class NumberAction : StratAction {
        /// <summary>
        /// Describes this action.
        /// </summary>
        public String Description { get; private set; }

        /// <summary>
        /// The variable needed by this action.
        /// </summary>
        public NumberVariable Var { get; set; }

        /// <summary>
        /// A method that takes in the strategy run context and performs
        /// and action.
        /// </summary>
        public Action<StrategyRuntimeContext, NumberVariable> ActionMethod { get; private set; }

        public NumberAction(String description, Action<StrategyRuntimeContext, NumberVariable> actionMethod, NumberVariable variable) {
            Description = description;
            ActionMethod = actionMethod;
            Var = variable;
        }

        public override void Execute(StrategyRuntimeContext context) {
            ActionMethod.Invoke(context, Var);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            String stringValue = Description;
            if (Var != null)
                stringValue = stringValue + ": '" + Var.VariableRetrievalMethod.Invoke(context).GetValue(context) + "'";
            return stringValue;
        }

        public override string GetId() {
            return "NumberAction";
        }
    }

    /// <summary>
    /// A StringAction needs a StringVariable from the user.
    /// </summary>
    public class StringAction : StratAction {
        /// <summary>
        /// Describes this action.
        /// </summary>
        public String Description { get; private set; }

        /// <summary>
        /// The variable needed by this action.
        /// This needs to be set by the user.
        /// </summary>
        public StringVariable Var { get; set; } = null;

        /// <summary>
        /// A method that takes in the strategy run context and performs
        /// and action.
        /// </summary>
        public Action<StrategyRuntimeContext, StringVariable> ActionMethod { get; private set; }

        public StringAction(String description, Action<StrategyRuntimeContext, StringVariable> actionMethod) {
            Description = description;
            ActionMethod = actionMethod;
        }

        public override void Execute(StrategyRuntimeContext context) {
            ActionMethod.Invoke(context, Var);
        }

        public override string GetStringValue(StrategyRuntimeContext context) {
            String stringValue = Description;
            if (Var != null)
                stringValue = stringValue + ": '" + Var.VariableRetrievalMethod.Invoke(context).GetValue(context) + "'";
            return stringValue;
        }

        public override string GetId() {
            return "StringAction";
        }
    }
}
