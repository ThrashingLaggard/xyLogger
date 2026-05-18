using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using xyLogger.Helpers;
using xyLogger.Helpers.Formatters;
using xyLogger.Interfaces;
using xyLogger.Models;

namespace xyLogger.Loggers
{
    /// <summary>
    /// Log formatted MESSAGES and EXCEPTIONS to the console    (currently discards LogEntries)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "<I insist>")]
    public class xyConsoleLogger<T> : ILogging
    {

        #region "Properties"

        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;                                                                                                                             
        private IMessageFormatter _msgFormatter = new xyDefaultMessageFormatter();
        private IExceptionFormatter _excFormatter = new xyDefaultExceptionFormatter();
        #endregion



        #region "Constructors"

       /// <summary>
       /// 
       /// </summary>
       /// <remarks>
       /// I should not leave the parameters blank, the fallbacks are hardcoded
       /// </remarks>
       /// <param name="msgFormatter_"></param>
       /// <param name="excFormatter_"></param>
        public xyConsoleLogger( IMessageFormatter msgFormatter_ , IExceptionFormatter excFormatter_ )
        {
            _msgFormatter = msgFormatter_;
            _excFormatter = excFormatter_;
        }

        #endregion



        #region "Logging"
        /// <summary>
        /// Logs a formatted message with the specified log level and optional caller information.
        /// </summary>
        /// <param name="message">The message to log. Cannot be null or empty.</param>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="callerName">The name of the calling member. This is automatically populated by the compiler  if not explicitly provided.</param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        public void Log(string message, LogLevel level, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;

            string formattedMsg = FormatMsg(message,  level, callerName, callerFile, callerLine);// _ = xyDefaultLogEntry logEntry
            xyOutput.Output(formattedMsg);
        }
        public void Log(string template,LogLevel level,IReadOnlyDictionary<string, object?> properties,[CallerMemberName] string? callerName = null,[CallerFilePath] string? callerFile = null,[CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;

            string rendered = xyLogTemplate.Render(template, properties);
            string formattedMsg = FormatMsg(rendered, level, callerName, callerFile, callerLine);
            xyOutput.Output(formattedMsg);
        }

        /// <summary>
        /// Logs the details of an exception at the specified log level.
        /// </summary>
        /// <remarks>The method formats the exception details, including the caller's name, and writes the
        /// formatted message to the console.</remarks>
        /// <param name="ex">The exception to log. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="level">The severity level of the log entry.</param>
        /// <param name="message">Optional: additional informationen</param>
        /// <param name="callerName">The name of the calling member. This is automatically populated by the compiler if not explicitly provided.</param>
        public void ExLog(Exception ex, string? message = null, LogLevel level = LogLevel.Error, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;
            string exMessage = FormatEx(ex, level, message, callerName, callerFile, callerLine);  // _  = xyExceptionEntry excEntry
            xyOutput.Output(exMessage);
        }

        #endregion



        #region "Formatting"

        /// <summary>
        /// Formats a log message with optional caller information and log level.
        /// </summary>
        /// <remarks>The exact format of the returned string is determined by the underlying formatter
        /// implementation.</remarks>
        /// <returns>A formatted string that includes the provided message, and optionally the caller name and log level.</returns>
        private string FormatMsg(string message, LogLevel? level = LogLevel.Debug, string ? callerName = null, string? callerFile = null, int callerLine = 0)
        {
            if (_msgFormatter is not null)
            {
                return _msgFormatter.FormatMessageForLogging(message, level, callerName, callerFile, callerLine);
            }
            else
            {
                string outputMessage =new xyDefaultMessageFormatter().FormatMessageForLogging(message, level, callerName, callerFile, callerLine);
                return outputMessage;
            }
        }

        /// <summary>
        /// Formats the Exception´s details for consistent logging.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="level"></param>
        /// <param name="information"></param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns></returns>
        private string FormatEx(Exception ex, LogLevel level,  string? information = null,string? callerName = null, string? callerFile = null, int callerLine = 0)
        {       
            if(_excFormatter is null)
            {
                return new xyDefaultExceptionFormatter().FormatExceptionDetails(ex, information, level, callerName, callerFile, callerLine);
            }
            return _excFormatter.FormatExceptionDetails(ex, information, level, callerName, callerFile, callerLine);                     
        }

        /// <summary>
        /// No op but the interface requires it
        /// </summary>
        public void Shutdown()
        {
            xyOutput.Output("Console logger pretends to shutdown...");
        }
        #endregion

    }
}
