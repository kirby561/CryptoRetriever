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

    public class Asset {
        public String Name { get; set; }
        public decimal Value { get; set; }
        public String Currency { get; set; }

        public Asset(String name, decimal value, String currency) {
            Name = name;
            Value = value;
            Currency = currency;
        }

        public override String ToString() {
            return "Name: " + Name + " - Value: " + Value + " - Currency: " + Currency;
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
