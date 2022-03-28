using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public class Trigger {
        private String _name;

        public String Summary {
            get {
                return _name;
            }
        }

        public Trigger(String name) {
            _name = name;
        }
    }
}
