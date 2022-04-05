using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public abstract class Action {
        public abstract void Execute();
    }

    public class DoNothingAction : Action {
        public override void Execute() {
            // Nothing to do
        }
    }
}
