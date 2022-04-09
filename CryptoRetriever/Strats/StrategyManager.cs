using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CryptoRetriever.Strats {
    public class StrategyManager {
        private ObservableCollection<Strategy> _strategies = new ObservableCollection<Strategy>();

        public ObservableCollection<Strategy> GetStrategies() {
            return _strategies;
        }

        public void AddStrategy(Strategy strategy) {
            _strategies.Add(strategy);
        }
    }
}
