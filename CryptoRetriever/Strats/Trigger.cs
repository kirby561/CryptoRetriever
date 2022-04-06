﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public class Trigger {
        public String Name { get; set; }

        public String Summary {
            get {
                return Name;
            }
        }

        public Condition Condition { get; set; } = null;

        public StratAction TrueAction { get; set; } = null;

        public StratAction FalseAction { get; set; } = null;

        public Trigger(String name) {
            Name = name;
        }
    }
}
