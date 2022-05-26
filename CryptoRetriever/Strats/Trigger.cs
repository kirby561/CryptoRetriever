using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CryptoRetriever.Strats {
    public class Trigger : IJsonable {
        public String Name { get; set; }

        public String Summary {
            get {
                return Name;
            }
        }

        public Condition Condition { get; set; } = null;

        public StratAction TrueAction { get; set; } = null;

        public StratAction FalseAction { get; set; } = null;

        public Trigger(String name = "") {
            Name = name;
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Name", Name);
            obj.Put("Condition", ConditionSerializer.ToJson(Condition));
            obj.Put("TrueAction", ActionSerializer.ToJson(TrueAction));
            obj.Put("FalseAction", ActionSerializer.ToJson(FalseAction));
            return obj;
        }

        public void FromJson(JsonObject json) {
            Name = json.GetString("Name");
            Condition = ConditionSerializer.ToObject(json.GetObject("Condition"));
            TrueAction = ActionSerializer.ToObject(json.GetObject("TrueAction"));
            FalseAction = ActionSerializer.ToObject(json.GetObject("FalseAction"));
        }

        /// <returns>
        /// Creates and returns a deep copy of this Trigger.
        /// </returns>
        public Trigger Clone() {
            Trigger result = new Trigger();
            result.FromJson(ToJson());
            return result;
        }
    }
}
