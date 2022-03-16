using System;
using System.Collections.Generic;

namespace CryptoRetriever.Source {
    public class Asset : IComparable<Asset> {
        public String Name { get; set; }
        public decimal Value { get; set; }
        public String Currency { get; set; }
        public String Id { get; set; } = null; // Optional

        public Asset(String name, decimal value, String currency) {
            Name = name;
            Value = value;
            Currency = currency;
        }

        public Asset(String name, decimal value, String currency, String id) {
            Name = name;
            Value = value;
            Currency = currency;
            Id = id;
        }

        public override String ToString() {
            return Name + ": " + Value.ToString("0.##") + " " + Currency;
        }

        public int CompareTo(Asset other) {
            int nameComparison = Name.CompareTo(other.Name);
            if (nameComparison == 0) {
                int currencyComparison = Currency.CompareTo(other.Currency);
                if (currencyComparison == 0) {
                    return Value.CompareTo(other.Value);
                } else {
                    return currencyComparison;
                }
            } else {
                return nameComparison;
            }
        }
    }

    public class AssetListResult : Result<List<Asset>> {
        public AssetListResult() : base() { }
        public AssetListResult(List<Asset> value) : base(value) { }
    }

    public class DatasetResult : Result<Dataset> {
        public DatasetResult() : base() { }
        public DatasetResult(Dataset value) : base(value) { }
    }
}
