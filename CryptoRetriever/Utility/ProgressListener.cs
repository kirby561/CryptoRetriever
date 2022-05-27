using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Utility {
    public interface ProgressListener {
        void OnProgress(long currentValue, long maxProgress);
    }
}
