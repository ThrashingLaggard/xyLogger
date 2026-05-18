using Microsoft.Extensions.Logging;

namespace xyLogger.Interfaces
{
    /// <summary>
    /// Interface for Exception-Formatters
    /// </summary>
    public interface IExceptionFormatter
    {
        /// <summary>
        /// Format Exception details
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        string FormatExceptionDetails(Exception ex, string? message = null, LogLevel level = LogLevel.Error, string? callerName = null, string? callerFile = null, int callerLine = 0, int depth = 1);
    }
}
