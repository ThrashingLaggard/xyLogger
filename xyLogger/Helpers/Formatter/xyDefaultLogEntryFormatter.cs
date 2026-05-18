
using Microsoft.Extensions.Logging;
using xyLogger.Interfaces;
using xyLogger.Models;

namespace xyLogger.Helpers.Formatters
{
    /// <summary>
    /// Used to store log messages and exceptions in LogEntries or get the data out of them
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class xyDefaultLogEntryFormatter<T> : IMessageEntityFormatter<T>
    {
        /// <summary>
        /// Unpack the data from a LogEntry
        /// </summary>
        /// <param name="entry_"></param>
        /// <param name="callerName"></param>
        /// <param name="level_"></param>
        /// <returns></returns>
        public string UnpackAndFormatFromEntity(T entry_, string? callerName = null, LogLevel? level_ = LogLevel.Debug)
        {
            if (entry_ is xyDefaultLogEntry logEntry)
            {
                uint ID = logEntry.ID;
                string description ="Info:" +  (logEntry.Description ?? "");
                string comment = "Comment:" + logEntry.Comment??"";
                string source = logEntry.Source;
                LogLevel level = level_ ?? logEntry.Level;
                string timestamp = DateTimeOffset.Now.ToString();
                string message = logEntry.Message;
                Exception? exception = logEntry.Exception?? default;
                string callerInfo = logEntry.CallerFile + "-" + (callerName ?? "") + logEntry.CallerLine;

                string formattedMessage = $"[{ID}{timestamp}] [{level+""}] \n[{callerInfo}] \n[{source}] \n{description}\n{comment}\n{message}\n";
                
                if(exception is not null)
                {
                    string formattedExceptionInformation = new xyDefaultExceptionFormatter().FormatExceptionDetails(exception,"", level,callerName);
                    formattedMessage += formattedExceptionInformation;
                }
                return formattedMessage;
            }
            else
            {
                return "No valid instance of xyLogEntry was given!";
            }
        }

        /// <summary>
        /// Pack the data from logmessages into objects
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
        public xyDefaultLogEntry PackAndFormatIntoEntity(string source, LogLevel level, string message, DateTimeOffset timestamp,  uint? id = null, string? description = null, string? comment= null, Exception? exception = null, string? callerFile = null, int callerLine = 0)
        {
            xyDefaultLogEntry entry = new(source_: source, level_: level, message_: message, exception_: exception, timestamp_: timestamp, callerFile_: callerFile, callerLine_: callerLine)
            {
                ID = id ?? 0
            };
            return entry;
        }
    }
}
