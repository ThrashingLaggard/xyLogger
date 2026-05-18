using Microsoft.Extensions.Logging;
using xyLogger.Interfaces;

namespace xyLogger.Helpers.Formatters
{
    /// <summary>
    /// Format exceptions for structured logging
    /// </summary>
    public class xyDefaultExceptionFormatter : IExceptionFormatter
    {
        /// <summary>
        /// Read all relevant details from the exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public string FormatExceptionDetails(Exception ex, string? message = null, LogLevel level = LogLevel.Error, string? callerName = null, string? callerFile = null, int callerLine = 0, int depth = 1)    =>xyLogFormatter.FormatExceptionDetails(ex, level, message, callerName, callerFile, callerLine, depth);
        
    }
}

