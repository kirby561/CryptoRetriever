using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace CryptoRetriever.Strats {
    public class StrategyManager {
        private ObservableCollection<Strategy> _strategies = new ObservableCollection<Strategy>();
        private String _strategyDirectory;
        private String _strategyExt = ".strat";

        public StrategyManager(String strategyDirectory) {
            _strategyDirectory = strategyDirectory;
        }

        public ObservableCollection<Strategy> GetStrategies() {
            return _strategies;
        }

        /// <summary>
        /// Adds the given strategy to the list and saves it to disk.
        /// </summary>
        /// <param name="strategy">The strategy to add.</param>
        /// <returns>Returns true if the strategy was added or false if one by that name exists already.</returns>
        public bool AddStrategy(Strategy strategy) {
            if (GetStrategyByName(strategy.Name) == null) {
                _strategies.Add(strategy);
                SaveStrategyToFile(_strategyDirectory + "/" + strategy.Name + _strategyExt, strategy);

                return true;
            }

            // Already exists
            return false;
        }

        public bool UpdateStrategy(Strategy strategy) {
            Strategy strat = GetStrategyByName(strategy.Name);

            if (strat == null)
                return false;

            _strategies.Remove(strat);
            AddStrategy(strategy);

            return true;
        }

        public void LoadAll() {
            foreach (String path in Directory.GetFiles(_strategyDirectory, "*" + _strategyExt)) {
                Strategy strat = LoadStrategyFromFile(path);
                if (strat != null)
                    _strategies.Add(strat);
            }
        }

        /// <summary>
        /// Gets the strategy with the given name if it exists.
        /// </summary>
        /// <param name="name">The name to look for.</param>
        /// <returns>Returns the strategy or null if the name does not exist.</returns>
        public Strategy GetStrategyByName(String name) {
            foreach (Strategy strategy in _strategies)
                if (name.Equals(strategy.Name))
                    return strategy;
            return null;
        }

        /// <summary>
        /// Saves the given strategy to the given filepath.
        /// </summary>
        /// <param name="filepath">The filepath to save it to.</param>
        /// <param name="strategy">The strategy to save.</param>
        public void SaveStrategyToFile(String filepath, Strategy strategy) {
            if (File.Exists(filepath))
                File.Delete(filepath);

            File.WriteAllText(filepath, strategy.ToJson().ToJsonString());
        }

        public bool DeleteStrategyByName(String name) {
            Strategy strategy = GetStrategyByName(name);
            if (strategy == null)
                return false;

            _strategies.Remove(strategy);
            File.Delete(_strategyDirectory + "/" + name + _strategyExt);
            return true;
        }

        public Strategy LoadStrategyFromFile(String filepath) {
            if (!File.Exists(filepath))
                return null;

            byte[] jsonText = File.ReadAllBytes(filepath);
            Strategy strategy = new Strategy();
            strategy.FromJson(JsonObject.FromJsonBytes(jsonText));
            return strategy;
        }
    }
}
