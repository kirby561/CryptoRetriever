using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Manages a list of strategies and keeps them in sync with files
    /// on disk that can be loaded on startup.
    /// </summary>
    public class StrategyManager {
        private ObservableCollection<Strategy> _strategies = new ObservableCollection<Strategy>();
        private String _strategyDirectory;
        private String _strategyExt = ".strat";

        // Strategies can specify which engine they want to run them by a string ID of the engine.
        // It is possible for an ID to not match any engines if they are not loaded or don't exist.
        // "Default" is the ID of the default engine.
        private Dictionary<String, IStrategyEngineFactory> _engines = new Dictionary<String, IStrategyEngineFactory>();
        public static String DefaultEngineId = "Default";

        public StrategyManager(String strategyDirectory) {
            _strategyDirectory = strategyDirectory;

            // We always have the default engine
            _engines.Add(DefaultEngineId, new DefaultStrategyEngineFactory());
        }

        /// <returns>
        /// Returns the list of engine factories that have been loaded.
        /// </returns>
        public List<IStrategyEngineFactory> GetEngines() {
            return _engines.Values.ToList();
        }

        /// <summary>
        /// Gets a factory that creates engines with the given ID.
        /// </summary>
        /// <param name="id">The ID of the engine to check for.</param>
        /// <returns>Returns the engine factory or null if no engine by that ID is loaded.</returns>
        public IStrategyEngineFactory GetEngineFactoryById(String id) {
            if (!_engines.ContainsKey(id))
                return null;
            return _engines[id];
        }

        /// <returns>
        /// Returns the list of strategies.
        /// </returns>
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

        /// <summary>
        /// Updates the given strategy on disk.
        /// </summary>
        /// <param name="strategy">The strategy to update.</param>
        /// <returns>Returns true if the strategy was updated successfullly or false otherwise.</returns>
        public bool UpdateStrategy(Strategy newStrategy, Strategy oldStrategy) {
            Strategy strat = GetStrategyByName(oldStrategy.Name);

            if (strat == null)
                return false;

            // Backup the old strategy in case the save fails
            String path = GetStrategyPath(oldStrategy.Name);
            String backupPath = GenerateUniqueBackupPath(path);
            if (File.Exists(path)) {
                File.Move(path, backupPath);
            }

            // Delete the existing one so if it was renamed
            // the old file gets removed.
            DeleteStrategy(oldStrategy);
            if (AddStrategy(newStrategy)) {
                // Remove the backup since everything succeeded
                File.Delete(backupPath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads all the strategies in the default strategy directory.
        /// </summary>
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

        /// <summary>
        /// Deletes the strategy with the given name. This includes
        /// deleting the file on disk as well as removing it from the list.
        /// </summary>
        /// <param name="name">The name of the strategy to remove.</param>
        public void DeleteStrategyByName(String name) {
            Strategy strategy = GetStrategyByName(name);
            if (strategy == null) {
                // Check if there's a strategy on disk by that name
                // and delete it if there is
                String path = GetStrategyPath(name);
                if (File.Exists(path))
                    File.Delete(path);

                return;
            }

            DeleteStrategy(strategy);
        }

        /// <summary>
        /// Deletes the given strategy from the list of strategies and
        /// from disk.
        /// </summary>
        /// <param name="strategy">The strategy to delete.</param>
        public void DeleteStrategy(Strategy strategy) {
            _strategies.Remove(strategy);
            String strategyPath = GetStrategyPath(strategy);
            if (File.Exists(strategyPath))
                File.Delete(strategyPath);
        }

        /// <summary>
        /// Loads the strategy at the given filepath.
        /// </summary>
        /// <param name="filepath">A path to a strategy file.</param>
        /// <returns>Returns the loaded strategy or null if the file does not exist.</returns>
        public Strategy LoadStrategyFromFile(String filepath) {
            if (!File.Exists(filepath))
                return null;

            byte[] jsonText = File.ReadAllBytes(filepath);
            Strategy strategy = new Strategy();
            strategy.FromJson(JsonObject.FromJsonBytes(jsonText));
            return strategy;
        }

        /// <summary>
        /// Returns the default path on disk to the given strategy.
        /// </summary>
        /// <param name="strategy">The strategy.</param>
        /// <returns>A path to the strategy on disk.</returns>
        private String GetStrategyPath(Strategy strategy) {
            return GetStrategyPath(strategy.Name);
        }

        /// <summary>
        /// Returns the default path on disk to the given strategy name.
        /// </summary>
        /// <param name="strategyName">The name of the strategy.</param>
        /// <returns>A path to the strategy on disk.</returns>
        private String GetStrategyPath(String strategyName) {
            return _strategyDirectory + "/" + strategyName + _strategyExt;
        }

        /// <summary>
        /// Generates a unique filename that can be used as a backup for the given path.
        /// </summary>
        /// <param name="originalPath">The original path.</param>
        /// <returns>Returns a unique filename based on the input.</returns>
        private String GenerateUniqueBackupPath(String originalPath) {
            long fileTime = DateTime.Now.ToFileTimeUtc();
            String newPath = originalPath + "." + fileTime + ".backup";
            int i = 0;
            while (File.Exists(newPath)) {
                newPath = originalPath + "." + fileTime + "." + i + ".backup";
            }
            return newPath;
        }
    }
}
