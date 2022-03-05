using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockStratMemes {
    public class ExportDatasetOptions {
        public ExportFileType FileType { get; set; } = ExportFileType.Csv;
        public ExportDateStringFormat DateStringFormat { get; set; } = ExportDateStringFormat.UtcTimestamp;
        public String FilePath { get; set; } = "";

        public String GetFileExtension() {
            switch(FileType) {
                case ExportFileType.Csv:
                    return "csv";
            }
            return null;
        }

        public String GetFileDescription() {
            switch (FileType) {
                case ExportFileType.Csv:
                    return "Comma Separated Values";
            }
            return null;
        }

        public String FormatDateString(double utcSeconds) {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            switch (DateStringFormat) {
                case ExportDateStringFormat.UtcTimestamp:
                    return "" + (long)Math.Round(utcSeconds);
                case ExportDateStringFormat.UtcDateTimeString:
                    DateTime utcDateTime = unixStart.AddSeconds(((long)Math.Round(utcSeconds))).ToUniversalTime();
                    return utcDateTime.ToString("G");
                case ExportDateStringFormat.UtcDateString:
                    DateTime utcDate = unixStart.AddSeconds(((long)Math.Round(utcSeconds))).ToUniversalTime();
                    return utcDate.ToString("d");
                case ExportDateStringFormat.LocalDateTimeString:
                    DateTime localDateTime = unixStart.AddSeconds(((long)Math.Round(utcSeconds))).ToLocalTime();
                    return localDateTime.ToString("G");
                case ExportDateStringFormat.LocalDateString:
                    DateTime localDate = unixStart.AddSeconds(((long)Math.Round(utcSeconds))).ToLocalTime();
                    return localDate.ToString("d");
            }
            return null;
        }

        public String GetDateHeader() {
            switch (DateStringFormat) {
                case ExportDateStringFormat.UtcTimestamp:
                    return "UTC Timestamp (s)";
                case ExportDateStringFormat.UtcDateTimeString:
                    return "Date/Time (UTC)";
                case ExportDateStringFormat.UtcDateString:
                    return "Date (UTC)";
                case ExportDateStringFormat.LocalDateTimeString:
                    return "Date/Time (Local)";
                case ExportDateStringFormat.LocalDateString:
                    return "Date (Local)";
            }
            return null;
        }
    }

    public enum ExportFileType {
        Csv
    }

    public enum ExportDateStringFormat {
        UtcTimestamp,
        UtcDateTimeString,
        UtcDateString,
        LocalDateTimeString,
        LocalDateString
    }
}
