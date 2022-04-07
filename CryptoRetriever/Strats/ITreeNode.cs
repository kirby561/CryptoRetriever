using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// The base of all nodes in the Action and Condition editors
    /// for triggers. Nodes can have one or more children, have
    /// a unique ID that does not change, a way of generating a
    /// String Value that can change, and a way to set a specific
    /// child to a new one.
    /// </summary>
    public interface ITreeNode {
        String GetId();
        String GetStringValue(StrategyRuntimeContext context);
        ITreeNode[] GetChildren();
        void SetChild(int index, ITreeNode child);
    }
}
