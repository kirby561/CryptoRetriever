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
        /// <returns>
        /// Gets the ID of the node. This is the ID of the purpose
        /// of the node rather than the physical type of it. For
        /// example a StratAction might "Do Nothing" so its and its 
        /// ID would be "DoNothing"
        /// </returns>
        String GetId();

        /// <returns>
        /// This returns a description of what the node is for
        /// or what the node does. This is informational and
        /// should not be used for decisions.
        /// </returns>
        String GetDescription();

        /// <returns>
        /// Returns a short label that can be displayed when representing the node somewhere.
        /// </returns>
        String GetLabel();

        /// <summary>
        /// This gets the current value of the node as a string given the
        /// current context.
        /// </summary>
        /// <param name="context">The runtime context which may or may not be needed to get the value.</param>
        /// <returns>Returns the current value as a string.</returns>
        String GetStringValue(StrategyRuntimeContext context);

        /// <returns>
        /// Returns an array of the child nodes of this node.
        /// </returns>
        ITreeNode[] GetChildren();

        /// <summary>
        /// Sets the child at the given index to the new child.
        /// This is an index into the array returned by GetChildren().
        /// </summary>
        /// <param name="index">The index to set.</param>
        /// <param name="child">The new child.</param>
        void SetChild(int index, ITreeNode child);
    }
}
