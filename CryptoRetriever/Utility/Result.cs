using System;

namespace CryptoRetriever {
    /// <summary>
    /// A class that can be used to return a value or an error
    /// and description if there was an error. Note that there
    /// is an Error() subclass that should be used in most cases
    /// if there was an error.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with this result.</typeparam>
    public class Result<T> {
        public T Value { get; set; }
        public bool Succeeded { get; set; }
        public String ErrorDetails { get; set; }

        /// <summary>
        /// This can be used as an error case but Error() should be used
        /// instead because it is more clear. This is public so it can be
        /// used to construct a result prior to it being known if it will
        /// be an error or not.
        /// </summary>
        public Result() {
            // No value is an error
            Value = default(T);
            Succeeded = false;
            ErrorDetails = "Unknown";
        }

        /// <summary>
        /// Creates a successful result with the given value.
        /// </summary>
        /// <param name="value">The value associated with this result.</param>
        public Result(T value) {
            Value = value;
            Succeeded = true;
            ErrorDetails = "";
        }
    }

    /// <summary>
    /// Error subclasses Result for convenience in constructing
    /// errors without having to set Succeeded. If the base class
    /// has a Result(String errorDetails) constructor then it is
    /// the same signature as Result(T value) when T is a String.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Error<T> : Result<T> {
        /// <summary>
        /// Creates an unsuccessful result.
        /// </summary>
        public Error() : base() {
            // The base class is an error
        }

        /// <summary>
        /// Creates an unsuccessful result with the given
        /// details describing what went wrong.
        /// </summary>
        /// <param name="errorDetails"></param>
        public Error(String errorDetails) {
            Value = default(T);
            Succeeded = false;
            ErrorDetails = errorDetails;
        }
    }

    /// <summary>
    /// A class to store whether something succeeded or failed
    /// without a specific value associated with it.
    /// </summary>
    public class Result {
        public bool Succeeded { get; set; }
        public String ErrorDetails { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// Note: This defaults to SUCCESSFUL rather than unsuccessful.
        /// This is different then the default value for Result<T>() because
        /// in that case it is assumed that if you don't have a value you did
        /// not succeed since that is the more common case.
        /// </summary>
        public Result() {
            Succeeded = true;
            ErrorDetails = "";
        }

        /// <summary>
        /// Creates an unsuccessful result with the given
        /// details about what went wrong.
        /// </summary>
        /// <param name="errorDetails">The details about what went wrong.</param>
        public Result(String errorDetails) {
            Succeeded = false;
            ErrorDetails = errorDetails;
        }
    }
}
