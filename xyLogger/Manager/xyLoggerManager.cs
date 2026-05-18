using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using xyLogger.Helpers;
using xyLogger.Helpers.Formatters;
using xyLogger.Interfaces;
using xyLogger.Loggers;
using xyLogger.Models;

namespace xyLogger.Managers
{
    /// <summary>
    /// Manages a collection of loggers (ILogging) and provides methods for logging messages and exceptions.
    /// 
    ///                                                                                                                  Stuff for Eventhandlers is planned!
    /// </summary>
    public class xyLoggerManager : ILoggerManager
    {
        /// <summary>
        /// Add useful information
        /// </summary>
        public string Description { get; set; } = "Your advertisements here!";

        private readonly object _writeLock = new();

        private volatile ILogging[] _loggers = [];

        public ushort Count => (ushort)_loggers.Length;

        public event EventHandler<xyLogEventArgs>? LogWritten;
        public event EventHandler<xyLogEventArgs>? ExLogWritten;

        /// <summary>
        /// Registers new loggers to the logging system.
        /// </summary>
        /// <remarks>This method adds the specified logger(s) to the internal collection of loggers.  The
        /// registered logger will be used for logging operations performed by the system.</remarks>
        /// <param name="loggers">The logger instance(s) to be registered. Cannot be null.</param>
        public void RegisterLogger(params ILogging[] loggers)
        {
            if (loggers is null || loggers.Length == 0) return;

            ILogging[] valid = loggers.Where(l => l is not null).ToArray();
            
            try 
            {
                    lock (_writeLock)
                    {
                        if (valid.Length == 0) return;

                        ILogging[] current = _loggers;
                        ILogging[] updated = new ILogging[current.Length + valid.Length];
                        current.CopyTo(updated, 0);
                        valid.CopyTo(updated, current.Length);

                        _loggers = updated;
                    }
            }
            catch (Exception ex)
            {
                xyLog.ExLog(ex);
            }
        }

    
        /// <summary>
        /// Unregisters the specified logger from the logging system.
        /// </summary>
        /// <remarks>This method removes the specified logger from the collection of active loggers. After
        /// calling this method, the specified logger will no longer receive log messages.</remarks>
        /// <param name="target">The logger instance to be unregistered. Must not be null.</param>
        public void UnregisterLogger(ILogging target)
        {
            lock (_writeLock)
            {
                ILogging[] updated = _loggers.Where(l => l != target).ToArray();
                _loggers = updated;
            }
        }


        /// <summary>
        /// Logs a message with the specified log level to all registered loggers.
        /// </summary>
        /// <remarks>This method iterates through all configured loggers and forwards the message to each
        /// one. Ensure that at least one logger is configured to avoid the message being discarded.</remarks>
        /// <param name="message">The message to log. Cannot be null or empty.</param>
        /// <param name="level">The severity level of the log message. Defaults to <see cref="LogLevel.Debug"/> if not specified.</param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        public void Log(string message, LogLevel level = LogLevel.Debug, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            ILogging[] snapshot = _loggers;
            foreach (ILogging logger in snapshot)
            {
                logger.Log(message, level, callerName, callerFile, callerLine);
            }
            RaiseLogWritten(message, level, callerName, callerFile, callerLine);

        }


        public void Log(string template, LogLevel level,IReadOnlyDictionary<string, object?> properties,[CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            ILogging[] snapshot = _loggers;
            foreach (ILogging logger in snapshot)
                logger.Log(template, level, properties, callerName, callerFile, callerLine);

            string rendered = xyLogTemplate.Render(template, properties);
            RaiseLogWritten(rendered, level, callerName, callerFile, callerLine,template, properties);
        }
        /// <summary>
        /// Logs the specified exception at the given log level using all registered loggers.
        /// </summary>
        /// <param name="ex">The exception to log. Cannot be <see langword="null"/>.</param>
        /// <param name="message"></param>
        /// <param name="level">The severity level of the log entry. Defaults to <see cref="LogLevel.Error"/>.</param>
        /// <param name="callerName"></param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        public void ExLog(Exception ex, string? message = null, LogLevel level = LogLevel.Error, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            ILogging[] snapshot = _loggers;
            foreach (ILogging logger in snapshot)
            {
                logger.ExLog(ex, message, level, callerName, callerFile, callerLine);
            }
            RaiseExLogWritten(ex, message, level, callerName, callerFile, callerLine);
        }


        private void RaiseLogWritten(string message, LogLevel level,string? callerName, string? callerFile, int callerLine,string? template = null,IReadOnlyDictionary<string, object?>? properties = null)
        {
            if (LogWritten is null) return;  // ← kein Entry bauen wenn niemand zuhört

            xyDefaultLogEntry entry = new(callerName ?? string.Empty, level, message,DateTimeOffset.Now, null, callerFile, callerLine)
            {
                MessageTemplate = template ?? string.Empty,
                Properties = properties ?? new Dictionary<string, object?>(),
            };

            LogWritten.Invoke(this, new xyLogEventArgs(entry));
        }

        private void RaiseExLogWritten(Exception ex, string? message, LogLevel level,string? callerName, string? callerFile, int callerLine)
        {
            if (ExLogWritten is null) return;

            xyExceptionEntry excEntry = new(ex, callerFile, callerLine) { Exception = ex};
            xyDefaultLogEntry entry = new(callerName ?? string.Empty, level,message ?? ex.Message, DateTimeOffset.Now,ex, callerFile, callerLine)
            {
                ExceptionEntry = excEntry,
            };

            ExLogWritten.Invoke(this, new xyLogEventArgs(entry));
        }

        public void Shutdown()
        {
            ILogging[] snapshot = _loggers;
            foreach (ILogging logger in snapshot) logger.Shutdown();
        }
    }
}
