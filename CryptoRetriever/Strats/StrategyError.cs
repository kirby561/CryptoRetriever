using System;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// When a problem occurs executing a strategy,
    /// they are tracked and this class stores what
    /// happened.
    /// </summary>
    public class StrategyError {
        public StrategyErrorCode Code { get; set; }
        public String Description { get; set; }

        public StrategyError(StrategyErrorCode code, String description) {
            Code = code;
            Description = description;
        }
    }

    public enum StrategyErrorCode {
        NotEnoughMoneyToMakePurchase,
        NotEnoughAssetsToMakePurchase,
        NoAssetsToCoverTransactionFee,
        NotEnoughTimeSinceLastTransaction,
        FilterError
    }
}
