using Microsoft.Extensions.Logging;

namespace xyLogger.Interfaces
{
    /// <summary>
    /// Interface for Message-Formatters
    /// </summary>
    public interface IMessageFormatter
    {
        /// <summary>
        /// Format log message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="callerName"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        string FormatMessageForLogging(string message, LogLevel? level = null, string ? callerName = null, string? callerFile = null, int? callerLine = null);
    }
}
