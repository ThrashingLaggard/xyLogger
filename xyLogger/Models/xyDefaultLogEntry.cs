using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using xyLogger.Interfaces;


namespace xyLogger.Models
{
    /// <summary>
    /// Bundled information for a log message
    /// </summary>
    public class xyDefaultLogEntry :  IEntry
    {
        /// <summary>
        /// For easy administration
        /// </summary>
        public uint ID { get; set; }

        /// <summary>
        /// Add interesting information
        /// </summary>
        public string? Description { get; set; } = default!;

        /// <summary>
        /// Additional information
        /// </summary>
        public string? Comment { get; set; } = default!;

        /// <summary>
        /// Time of logging
        /// </summary>
        public required DateTimeOffset Timestamp { get; init; }

        public string MessageTemplate { get; init; } = string.Empty;

        public IReadOnlyDictionary<string, object?> Properties { get; init; }
            = new Dictionary<string, object?>();

        /// <summary>
        /// Where it was logged --> callername
        /// </summary>
        public required string Source { get; init; }

        public string? CallerFile { get; init; }

        public int CallerLine { get; init; }

        /// <summary>
        /// The level of "severity"
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// The logging message
        /// </summary>
        public required string Message { get; init; }

        /// <summary>
        /// The exception connected to the log
        /// </summary>
        public Exception? Exception { get; set; }

        public xyExceptionEntry? ExceptionEntry { get; set; }

        [JsonConstructor]
        [SetsRequiredMembers]
        public xyDefaultLogEntry(string source_, LogLevel level_, string message_, DateTimeOffset timestamp_, Exception? exception_ = null, string? callerFile_ = null, int callerLine_ = 0)
        {
            Timestamp = timestamp_;
            CallerFile = callerFile_;
            CallerLine = callerLine_;
            Source = source_;
            Level = level_;
            Message = message_;
            Exception = exception_ ?? default!;
            ExceptionEntry = exception_ is not null ? new xyExceptionEntry(exception_:exception_, callerFile_, callerLine_) : default!;
        }




    }
}
