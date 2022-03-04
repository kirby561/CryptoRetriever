using System;

namespace StockStratMemes {
    public class DateRange {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public DateRange(DateTime start, DateTime end) {
            Start = start;
            End = end;
        }
    }
}
