using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    /// <summary>
    /// Serializes/deserializes Actions to and from JSON.
    /// </summary>
    public class ActionSerializer {
        public static JsonObject ToJson(StratAction action) {
            return action.ToJson();
        }

        public static StratAction ToObject(JsonObject json) {
            Dictionary<String, StratAction> availableActions = Actions.GetActions();
            String id = json.GetString("Id");
            StratAction action = availableActions[id].Clone();
            action.FromJson(json);
            return action;
        }
    }
}
