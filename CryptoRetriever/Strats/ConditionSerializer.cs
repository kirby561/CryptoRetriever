using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;

namespace CryptoRetriever.Strats {
    public class ConditionSerializer {
        public static JsonObject ToJson(Condition condition) {
            return condition.ToJson();
        }

        public static Condition ToObject(JsonObject json) {
            Dictionary<String, Condition> conditions = Conditions.GetConditions();
            Condition result = conditions[json.GetString("Id")];
            result.FromJson(json);
            return result;
        }
    }
}
