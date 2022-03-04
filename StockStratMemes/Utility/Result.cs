using System;

namespace StockStratMemes {
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
}
