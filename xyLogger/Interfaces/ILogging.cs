using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using xyLogger.Helpers;
namespace xyLogger.Interfaces
{
    /// <summary>
    /// Interface for my own loggers like the console or the async logger
    /// </summary>
    public interface ILogging
    {
        /// <summary>
        /// Write a message 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        void Log(string message, LogLevel level, [CallerMemberName] string? callerName = null,[CallerFilePath]   string? callerFile = null, [CallerLineNumber] int callerLine = 0);

        void Log(string template,LogLevel level,IReadOnlyDictionary<string, object?> properties,[CallerMemberName] string? callerName = null,[CallerFilePath] string? callerFile = null,[CallerLineNumber] int callerLine = 0)
        {
            string rendered = xyLogTemplate.Render(template, properties);
            Log(rendered, level, callerName, callerFile, callerLine);
        }

        /// <summary>
        /// Write an exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        void ExLog(Exception ex, string? message = null, LogLevel level = LogLevel.Error, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0);
        
        /// <summary>
        /// Set reference to null
        /// </summary>
        void Shutdown();
    }
}