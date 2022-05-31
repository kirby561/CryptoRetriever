using System;

namespace CryptoRetriever.Strats {
    public interface IStrategyEngineFactory {
        /// <returns>
        /// Creates a new instance of the type of StrategyEngine identified via GetId() and returns it.
        /// </returns>
        StrategyEngine CreateInstance();

        /// <returns>
        /// Returns the ID of the type of engine this factory creates.
        /// </returns>
        String GetId();
    }

    public class DefaultStrategyEngineFactory : IStrategyEngineFactory {
        public StrategyEngine CreateInstance() {
            return new StrategyEngine();
        }

        public String GetId() {
            return StrategyManager.DefaultEngineId;
        }
    }
}
