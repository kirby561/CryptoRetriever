using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public class State {
        private String _stateId;

        public String Summary {
            get {
                return _stateId;
            }
        }

        public State(String stateId) {
            _stateId = stateId;
        }
    }
}
