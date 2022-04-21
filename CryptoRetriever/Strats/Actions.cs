using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Contains lists of stock actions that can be used by triggers.
    /// </summary>
    public static class Actions {

        private static Dictionary<String, StratAction> _actions = null;

        public static Dictionary<String, StratAction> GetActions() {
            if (_actions == null) {
                _actions = new Dictionary<String, StratAction>();
                _actions.Add(ActionId.BuyMax, new SimpleAction(
                    ActionId.BuyMax,
                    "Buys as much of the asset as possible with the available fiat currency.",
                    (context) => {
                        context.PurchaseMax();
                    }));
                _actions.Add(ActionId.SellMax, new SimpleAction(
                    ActionId.SellMax,
                    "Sells as much of the asset as possible for fiat currency.",
                    (context) => {
                        context.SellMax();
                    }));
                _actions.Add(ActionId.ChangeStateTo, new StringAction(
                     ActionId.ChangeStateTo,
                     "Changes the state to the given StringVariable immediately.",
                     (context, stringVal) => {
                         context.CurrentState = stringVal.GetValue(context);
                     }));
                _actions.Add(ActionId.ChangeNextStateTo, new StringAction(
                     ActionId.ChangeNextStateTo,
                     "Changes the next state to the given StringVariable. This will become the CurrentState after all triggers are run if another trigger doesn't change it again.",
                     (context, stringVal) => {
                         context.NextState = stringVal.GetValue(context);
                     }));
                _actions.Add(ActionId.DoNothing, new SimpleAction(
                     ActionId.DoNothing,
                     "Does nothing.",
                     (context) => {
                        // Nothing to do
                     }));
                _actions.Add(ActionId.NotSet, new SimpleAction(
                     ActionId.NotSet,
                     "Does nothing but will show as a placeholder in the UI.",
                     (context) => {
                        // Nothing to do
                     }));
                _actions.Add(ActionId.MultiAction, new MultiAction(
                    _actions[ActionId.NotSet],
                    _actions[ActionId.NotSet]
                ));
            }
            return _actions;
        }
    }
}
