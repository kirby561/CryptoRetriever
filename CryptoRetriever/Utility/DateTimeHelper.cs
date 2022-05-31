using System;

namespace CryptoRetriever.Utility {
    public static class DateTimeHelper {
        public static double GetUnixTimestampSeconds(DateTime dateTime) {
            return (dateTime - DateTimeConstant.UnixStart).TotalSeconds;
        }
    }
}
