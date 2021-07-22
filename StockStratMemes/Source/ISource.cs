using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockStratMemes.Source {
    public interface ISource {
        String GetName();
        SourceType GetType();
        Task<AssetListResult> GetAssetsAsync();
    }

    public enum SourceType {
        Static,
        Dynamic
    }

    public class Asset : IComparable<Asset> {
        public String Name { get; set; }
        public decimal Value { get; set; }
        public String Currency { get; set; }

        public Asset(String name, decimal value, String currency) {
            Name = name;
            Value = value;
            Currency = currency;
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

    public class AssetListResult {
        public List<Asset> Result { get; set; }
        public bool Succeeded { get; set; }
        public String ErrorDetails { get; set; }

        public AssetListResult() {
            Result = null;
            Succeeded = false;
            ErrorDetails = "Unknown";
        }
    }
}
