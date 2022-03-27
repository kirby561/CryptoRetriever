﻿using CryptoRetriever.Source;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strategy {
    public class Account {
        /// <summary>
        /// How much fiat currency is in this account such as USD.
        /// </summary>
        public double CurrencyBalance { get; set; }

        /// <summary>
        /// How much of an asset we have in this account such as stock shares or eth.
        /// </summary>
        public double AssetBalance { get; set; }
    }
}
