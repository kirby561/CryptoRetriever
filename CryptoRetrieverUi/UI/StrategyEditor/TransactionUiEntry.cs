using System;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Similar to Transaction but includes UI specific parameters
    /// for displaying them in a ListView.
    /// </summary>
    public class TransactionUiEntry {
        public String Currency { get; set; }
        public String Assets { get; set; }
        public String Fee { get; set; }
        public String Price { get; set; }
        public int DatapointIndex { get; set; }
        public long TimestampS { get; set; }
    }
}
