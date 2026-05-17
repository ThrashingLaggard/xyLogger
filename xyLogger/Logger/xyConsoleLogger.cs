using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
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

        // Almost irrelevant, but not utterly.                                                                                                                                      
        private IMessageFormatter? _msgFormatter;
        private IExceptionFormatter? _excFormatter;
        private IMessageEntityFormatter<T>? _logEntryFormatter;
        private IExceptionEntityFormatter? _excEntryFormatter;
        #endregion



        #region "Constructors"

       /// <summary>
       /// 
       /// </summary>
       /// <param name="msgFormatter_"></param>
       /// <param name="excFormatter_"></param>
       /// <param name="logEntryFormatter_"></param>
       /// <param name="exceptionEntryFormatter_"></param>
        public xyConsoleLogger( IMessageFormatter msgFormatter_ =null!, IExceptionFormatter excFormatter_ = null!, IMessageEntityFormatter<T> logEntryFormatter_ = null!, IExceptionEntityFormatter? exceptionEntryFormatter_ = null)
        {
            _msgFormatter = msgFormatter_?? null!;
            _excFormatter = excFormatter_?? null!;
            _logEntryFormatter = logEntryFormatter_?? null!;
            _excEntryFormatter = exceptionEntryFormatter_ ?? null!;
        }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="entFormatter_"></param>
            public xyConsoleLogger( IMessageEntityFormatter<T> entFormatter_)
            {
                _logEntryFormatter = entFormatter_;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="excFormatter_"></param>
            public xyConsoleLogger(IExceptionFormatter excFormatter_)
            {
                _excFormatter = excFormatter_;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="msgFormatter_"></param>
            public xyConsoleLogger(IMessageFormatter msgFormatter_)
            {
                _msgFormatter = msgFormatter_;
            }

        #endregion



        #region "Logging"
        /// <summary>
        /// Logs a formatted message with the specified log level and optional caller information.
        /// </summary>
        /// <param name="message">The message to log. Cannot be null or empty.</param>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="callerName">The name of the calling member. This is automatically populated by the compiler  if not explicitly provided.</param>
        public void Log(string message, LogLevel level, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {                                                               
            string formattedMsg = FormatMsg(message, out _, DateTime.Now, null, null, null, level, callerName, callerFile, callerLine);// _ = xyDefaultLogEntry logEntry
            Console.WriteLine(formattedMsg);
            Console.Out.Flush();
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
            string exMessage = FormatEx(ex, level,out _, message, callerName, callerFile, callerLine);  // _  = xyExceptionEntry excEntry
            Console.WriteLine(exMessage);
            Console.Out.Flush();
        }

        #endregion



        #region "Formatting"

        /// <summary>
        /// Formats a log message with optional caller information and log level.
        /// </summary>
        /// <remarks>The exact format of the returned string is determined by the underlying formatter
        /// implementation.</remarks>
        /// <returns>A formatted string that includes the provided message, and optionally the caller name and log level.</returns>
        private string FormatMsg(string message, out xyDefaultLogEntry logEntry,DateTime? timestamp = null,uint? id = null,string? description = null, string? comment = null, LogLevel? level = LogLevel.Debug, string ? callerName = null, string? callerFile = null, int callerLine = 0)
        {
            logEntry = FormatIntoDefaultLogEntry(callerName!, (LogLevel)level!, message, timestamp ?? DateTime.Now, id, description, comment, null, callerFile, callerLine);

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
        /// <param name="excEntry"></param>
        /// <param name="information"></param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns></returns>
        private string FormatEx(Exception ex, LogLevel level, out xyExceptionEntry excEntry, string? information = null,string? callerName = null, string? callerFile = null, int callerLine = 0)
        {
            excEntry = FormatIntoExceptionEntry(ex, information, callerFile, callerLine);

            if(_excFormatter is null)
            {
                return new xyDefaultExceptionFormatter().FormatExceptionDetails(ex, information, level, callerName, callerFile, callerLine);
            }
            return _excFormatter.FormatExceptionDetails(ex, information, level, callerName, callerFile, callerLine);                     
        }

        /// <summary>
        /// Format a log entity into a message
        /// </summary>
        /// <param name="entry_"></param>
        /// <param name="callerName"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public string FormatFromEntity(T entry_, string? callerName = null, LogLevel? level = null)
        {
            if(_logEntryFormatter is not null)
            {
                return _logEntryFormatter.UnpackAndFormatFromEntity(entry_, callerName, level);
            }
            else  // if DI doesnt work
            {
                xyDefaultLogEntryFormatter<T> formatter = new();
                return formatter.UnpackAndFormatFromEntity(entry_, callerName, level);
            }
        }

        /// <summary>
        /// Pack all relevant information into a xyDefaultLogEntry-instance
        /// </summary>
        /// <param name="source"></param>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="timestamp"></param>
        /// <param name="id"></param>
        /// <param name="description"></param>
        /// <param name="comment"></param>
        /// <param name="exception"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns></returns>
        public xyDefaultLogEntry FormatIntoDefaultLogEntry(string source, LogLevel level, string message, DateTime timestamp, uint? id = null, string? description = null, string? comment = null, Exception? exception = null, string? callerFile = null, int callerLine = 0)
        {
            if (_logEntryFormatter is not null)
            {
                return _logEntryFormatter.PackAndFormatIntoEntity(source, level, message, timestamp, id,description,comment,exception, callerFile, callerLine);
            }
            else    // fallback for when DI fails
            {
                xyDefaultLogEntryFormatter<T> formatter = new();
                return formatter.PackAndFormatIntoEntity(source, level, message, timestamp, id, description, comment, exception, callerFile,callerLine);
            }
        }

        /// <summary>
        ///  Pack an Exception into an ExceptionEntry for easier serialization and storage
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="information"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns></returns>
        public xyExceptionEntry FormatIntoExceptionEntry(Exception exception, string? information = null, string? callerFile = null, int callerLine = 0)
        {
            _excEntryFormatter ??= new xyDefaultExceptionEntryFormatter();
            return _excEntryFormatter.PackAndFormatIntoEntity(exception, DateTime.Now, information,null,null,callerFile, callerLine);
        }

        /// <summary>
        /// No op but the interface requires it
        /// </summary>
        public void Shutdown()
        {
            Console.WriteLine("Console logger pretends to shutdown...");
            Console.Out.Flush();
        }

        #endregion

    }
}
