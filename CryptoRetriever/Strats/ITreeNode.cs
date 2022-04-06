using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public interface ITreeNode {
        String GetId();
        String GetStringValue(StrategyRuntimeContext context);
        ITreeNode[] GetChildren();
        void SetChild(int index, ITreeNode child);
    }
}
