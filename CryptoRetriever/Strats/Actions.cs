using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Contains lists of stock actions that can be used by triggers.
    /// </summary>
    public static class Actions {
        public static List<StratAction> GetActions() {
            List<StratAction> actions = new List<StratAction>();
            actions.Add(new SimpleAction(
                "BuyMax",
                (context) => {
                    context.PurchaseMax();
                }));
            actions.Add(new SimpleAction(
                "SellMax",
                (context) => {
                    context.SellMax();
                }));
            actions.Add(new StringAction(
                 "ChangeStateTo",
                 (context, stringVar) => {
                     context.CurrentState = stringVar.VariableRetrievalMethod.Invoke(context).GetValue(context);
                 }));
            actions.Add(new DoNothingAction());
            actions.Add(new MultiAction(new FutureAction(), new FutureAction()));
            return actions;
        }
    }
}
