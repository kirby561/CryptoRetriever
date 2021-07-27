using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockStratMemes.Source {
    public interface ISource {
        String GetName();
        List<int> GetGranularityOptions();
        Task<AssetListResult> GetAssetsAsync();
        Task<DataSetResult> GetPriceHistoryAsync(Asset asset, DateRange range, int secondsPerSample = 86400);
        Task<DataSetResult> GetPriceHistoryAsync(Asset asset, DateTime start, int secondsPerSample = 86400);
    }

    public class DateRange {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public DateRange(DateTime start, DateTime end) {
            Start = start;
            End = end;
        }
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
    
    public class Result<T> {
        public T Value { get; set; }
        public bool Succeeded { get; set; }
        public String ErrorDetails { get; set; }

        public Result() {
            Value = default(T);
            Succeeded = false;
            ErrorDetails = "Unknown";
        }

        public Result(T value) {
            Value = value;
            Succeeded = true;
            ErrorDetails = "";
        }

        public Result(String errorDetails) {
            Value = default(T);
            Succeeded = false;
            ErrorDetails = errorDetails;
        }
    }
    
    public class AssetListResult : Result<List<Asset>> { }
    public class DataSetResult : Result<DataSet> { }
}
