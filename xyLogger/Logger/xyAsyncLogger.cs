using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using xyLogger.Enums;
using xyLogger.Helpers.Formatters;
using xyLogger.Interfaces;
using xyLogger.Models;

namespace xyLogger.Loggers
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "<This is the way>")]
    public class xyAsyncLogger<T> : ILogging, IDisposable
    {
        private readonly Task _worker;
        private readonly StreamWriter _writer;
        private readonly IReadOnlyList<xyLogTargets> _targets;
        private readonly CancellationTokenSource _cts = new();
        private readonly BlockingCollection<string> _logQueue = [];
        private readonly List<IEntry> _entries = [];
        private readonly object _entriesLock = new();

        public IReadOnlyList<IEntry> GetEntries() { lock (_entriesLock) return _entries.ToList(); }
        public IEnumerable<xyDefaultLogEntry> GetMessageEntries() => GetEntries().OfType<xyDefaultLogEntry>();
        public IEnumerable<xyExceptionEntry> GetExceptionEntries() => GetEntries().OfType<xyExceptionEntry>();

        public IMessageFormatter? MessageFormatter { get; set; }
        public IExceptionFormatter? ExceptionFormatter { get; set; }
        public IMessageEntityFormatter<T>? MessageEntryFormatter { get; set; } 
        public IExceptionEntityFormatter? ExceptionEntryFormatter { get; set; }

        public xyAsyncLogger(string? filepath= null, IEnumerable<xyLogTargets>? logTargets = null, IMessageFormatter? messageFormatter_ =null, IExceptionFormatter? exceptionFormatter_ = null, IMessageEntityFormatter<T>? messageEntryFormatter_ = null, IExceptionEntityFormatter? exceptionEntryFormatter_ = null)
        {
            MessageFormatter = messageFormatter_?? new xyDefaultMessageFormatter();
            ExceptionFormatter = exceptionFormatter_ ?? new xyDefaultExceptionFormatter();
            ExceptionEntryFormatter = exceptionEntryFormatter_ ?? new xyDefaultExceptionEntryFormatter();
            MessageEntryFormatter = messageEntryFormatter_ ?? new xyDefaultLogEntryFormatter<T>();

            if (string.IsNullOrEmpty(filepath))
            {

                string parent = Directory.GetParent(Environment.CurrentDirectory)?.FullName
                 ?? Environment.CurrentDirectory;

                filepath = Path.Combine(parent, "logs", "app.log"); 

            }
            Directory.CreateDirectory(Path.GetDirectoryName(filepath)!);

            _targets = logTargets?.ToList()?? [xyLogTargets.StandardSystemConsole];

            // Setup the writer
            _writer = _targets.Contains(xyLogTargets.File)? new(filepath!, true) {AutoFlush = true,}:StreamWriter.Null;

            // Starting the asynchronous work
            _worker = Task.Run(() => ProcessQueue(), _cts.Token);
        }
        /// <summary>
        /// Writes an informative message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        public void Log(string message, LogLevel level, [CallerMemberName] string? callerName = null, [CallerFilePath]string? callerFile = null, [CallerLineNumber]int callerLine = 0)
        {
            // xyDefaultLogEntry? logEntry = default;
            string formattedMsg = FormatMsg(message, out xyDefaultLogEntry entry, DateTime.Now, null, null, null,level, callerName, callerFile, callerLine );// 
            lock (_entriesLock) _entries.Add(entry);
            Enqueue(formattedMsg, callerName);
        }

        /// <summary>
        /// Writes an exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="level"></param>
        /// <param name="message">Optional: additional informationen</param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        public void ExLog(Exception ex, string? message = null, LogLevel level = LogLevel.Error, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            //xyExceptionEntry? excEntry =  default;
            string exMessage = FormatEx(ex, level, out xyExceptionEntry  entry, message, callerName, callerFile, callerLine);
            lock (_entriesLock) _entries.Add(entry);
            Enqueue(exMessage, callerName);
        }


        /// <summary>
        /// Add a message to the queue for logging
        /// </summary>
        /// <param name="message"></param>
        /// <param name="callerName"></param>
        private void Enqueue(string message, [CallerMemberName] string? callerName = null)
        {
            try
            {
                _logQueue.Add(message);
            }
            catch(Exception ex)
            {
                if (ExceptionFormatter is null) ExceptionFormatter = new xyDefaultExceptionFormatter();
                Console.WriteLine(ExceptionFormatter.FormatExceptionDetails(ex,message,LogLevel.Error,callerName));
                Console.Out.Flush();
            }
        }

        /// <summary>
        /// Work through the queued log messages
        /// </summary>
        private async Task ProcessQueue()
        {
            try
            {
                foreach (string message in _logQueue.GetConsumingEnumerable(_cts.Token))
                {
                    await WriteToTarget(message);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (ExceptionFormatter is null) ExceptionFormatter = new xyDefaultExceptionFormatter();
                Console.WriteLine(ExceptionFormatter.FormatExceptionDetails(ex,"An error occured while processing the message queue" ,LogLevel.Error));
                await Console.Out.FlushAsync();
            }
        }

        /// <summary>
        /// Write the output to the target =>   0 for console,    1 for file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logTargets"></param>
        /// <returns></returns>
        private async Task WriteToTarget(string message)
        {
            if (_targets.Contains(xyLogTargets.StandardSystemConsole))
            {
                Console.WriteLine(message);
                await Console.Out.FlushAsync();
            }
            if (_targets.Contains(xyLogTargets.File)) 
            {
                await _writer.WriteLineAsync(message);
            }
            if (_targets.Contains(xyLogTargets.RemoteServer))
            {
                await xyLog.AsxLog("Logging to a remote server is not yet implemented, please wait a minute!");

                // I shall find out what to implement here eventually
            }
            if (_targets.Contains(xyLogTargets.Elsewhere))
            {
                // Yes yes, what do i do here?
            }
        }

        #region "Formatting"

        /// <summary>
        /// Formats a log message with optional caller information and log level.
        /// </summary>
        /// <remarks>The exact format of the returned string is determined by the underlying formatter
        /// implementation.</remarks>
        /// <returns>A formatted string that includes the provided message, and optionally the caller name and log level.</returns>
        private string FormatMsg(string message, out xyDefaultLogEntry logEntry, DateTime? timestamp = null, uint? id = null, string? description = null, string? comment = null,  LogLevel? level = LogLevel.Debug, string? callerName = null, string? callerFile = null, int callerLine = 0)
        {
            logEntry = FormatIntoDefaultLogEntry(callerName!, (LogLevel)level!, message, timestamp ?? DateTime.Now, id, description, comment, null, callerFile, callerLine);

            if (MessageFormatter is not null)
            {
                return MessageFormatter.FormatMessageForLogging(message, level, callerName, callerFile, callerLine);
            }
            else
            {
                string outputMessage = new xyDefaultMessageFormatter().FormatMessageForLogging(message, level, callerName, callerFile, callerLine);
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
        private string FormatEx(Exception ex, LogLevel level, out xyExceptionEntry excEntry, string? information = null, string? callerName = null, string? callerFile = null, int callerLine = 0)
        {
            excEntry = FormatIntoExceptionEntry(ex, information, callerFile, callerLine);

            if (ExceptionFormatter is not null)
            {
                return ExceptionFormatter.FormatExceptionDetails(ex, information, level, callerName, callerFile, callerLine);
            }
            else
            {
                if (ExceptionFormatter is null) ExceptionFormatter = new xyDefaultExceptionFormatter();
                string outputMessage = ExceptionFormatter.FormatExceptionDetails(ex,information ,level, callerName, callerFile, callerLine);
                return outputMessage;
            }
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
            if (MessageEntryFormatter is not null)
            {
                return MessageEntryFormatter.UnpackAndFormatFromEntity(entry_, callerName, level);
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
            if (MessageEntryFormatter is not null)
            {
                return MessageEntryFormatter.PackAndFormatIntoEntity(source, level, message, timestamp, id, description, comment, exception, callerFile, callerLine);
            }
            else    // fallback for  DI fails
            {
                xyDefaultLogEntryFormatter<T> formatter = new();
                return formatter.PackAndFormatIntoEntity(source, level, message, timestamp, id, description, comment, exception, callerFile, callerLine);
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
            ExceptionEntryFormatter ??= new xyDefaultExceptionEntryFormatter();
            return ExceptionEntryFormatter.PackAndFormatIntoEntity(exception, DateTime.Now, information,null,null, callerFile, callerLine);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        //public void Shutdown()
        //{
        //    _logQueue.CompleteAdding();
        //    _cts.Cancel();
        //    _worker.Wait();
        //}
        public void Shutdown()
        {
            _logQueue.CompleteAdding();
            try { _worker.Wait(TimeSpan.FromSeconds(5)); }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException)) { }
         }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Shutdown();
            _worker.Dispose();
            _writer.Dispose();
            _logQueue.Dispose();
            _cts.Dispose();
        }
    }
}
