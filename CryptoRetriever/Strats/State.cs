using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public class State : IJsonable {
        private String _stateId;

        public String Summary {
            get {
                return _stateId;
            }
        }

        public String GetId() {
            return _stateId;
        }

        public State(String stateId = "") {
            _stateId = stateId;
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("StateId", _stateId);
            return obj;
        }

        public void FromJson(JsonObject json) {
            _stateId = json.GetString("StateId");
        }
    }
}
