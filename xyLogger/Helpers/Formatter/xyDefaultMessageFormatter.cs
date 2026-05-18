using Microsoft.Extensions.Logging;
using xyLogger.Interfaces;

namespace xyLogger.Helpers.Formatters
{
    /// <summary>
    /// Provides default formatting for log messages and exception details.
    /// </summary>
    public class xyDefaultMessageFormatter : IMessageFormatter
    {


        /// <summary>
        /// Formats a log message with a timestamp, log level, and caller information.
        /// </summary>
        /// <remarks>The formatted log message includes the current timestamp in the format "yyyy-MM-dd
        /// HH:mm:ss.fff", the specified or default log level, the caller name, and the provided message.</remarks>
        /// <param name="message">The message to be logged. This value cannot be null or empty.</param>
        /// <param name="callerName">The name of the caller or source of the log message. If null or empty, a default value of " / " is used.</param>
        /// <param name=""></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <param name="level">The log level associated with the message. If null, the default log level of "Information" is used.</param>
        /// <returns>A formatted string containing the timestamp, log level, caller information, and the log message.</returns>
        public string FormatMessageForLogging(string message, LogLevel? level = null, string? callerName = null, string? callerFile = null, int callerLine = 0) =>xyLogFormatter.FormatMessageForLogging(message, callerName, level, callerFile, callerLine);
        
    }
}
