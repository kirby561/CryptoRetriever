using System;

namespace CryptoRetriever {
    public interface ICoordinateFormatter {
        String Format(double coordinate);
    }

    public class DollarFormatter : ICoordinateFormatter {
        public string Format(double coordinate) {
            return "$" + ((decimal)coordinate).ToString("N");
        }
    }

    public class TimestampToDateFormatter : ICoordinateFormatter {
        public string Format(double coordinate) {
            long utcTimestampSeconds = (long)Math.Round(coordinate);
            DateTime unixStart = DateTimeConstant.UnixStart;
            DateTime localDateTime = unixStart.AddSeconds(utcTimestampSeconds).ToLocalTime();
            return localDateTime.ToString("G");
        }
    }
}
