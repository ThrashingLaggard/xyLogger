using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using  xyLogger.Helpers.Formatters;
using xyLogger.Interfaces;

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

        public ushort Count { get; private set; }
        
        private readonly IList<ILogging> _loggers;

        /// <summary>
        /// 
        /// </summary>
        public xyLoggerManager()
        {
            _loggers = [];
        }

        /// <summary>
        /// Registers (a) new logger(s) to the logging system.
        /// </summary>
        /// <remarks>This method adds the specified logger(s) to the internal collection of loggers.  The
        /// registered logger will be used for logging operations performed by the system.</remarks>
        /// <param name="loggers">The logger instance(s) to be registered. Cannot be null.</param>
        public void RegisterLogger(params ILogging[] loggers)
        {
            foreach (ILogging logger  in loggers)
            {
                if (logger is null)
                {
                    string output = OutputMissingLogger(logger!);     
                    Console.WriteLine(output);
                }
                else
                {
                   _loggers.Add(logger);
                    Count++;
                }
            }
        }

        private string OutputMissingLogger(ILogging logger)
        {
            xyDefaultMessageFormatter formatter = new();

            string output = $"{logger} is null!";
            string formatted = formatter.FormatMessageForLogging(output);
            string exception = new xyDefaultExceptionFormatter().FormatExceptionDetails(new ArgumentNullException(nameof(logger)), formatted,LogLevel.Error);
            return exception;
        }



        /// <summary>
        /// Unregisters the specified logger from the logging system.
        /// </summary>
        /// <remarks>This method removes the specified logger from the collection of active loggers. After
        /// calling this method, the specified logger will no longer receive log messages.</remarks>
        /// <param name="target">The logger instance to be unregistered. Must not be null.</param>
        public void UnregisterLogger(ILogging target) 
        {
            if (_loggers.Remove(target))
                Count--;
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
            foreach (ILogging logger in _loggers)
            {
                logger.Log(message, level, callerName, callerFile, callerLine);
            }
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
            foreach (ILogging logger in _loggers)
            {
                logger.ExLog(ex, message, level, callerName, callerFile, callerLine);
            }
        }

     
        public void Shutdown()
        {
            foreach (ILogging logger in _loggers) logger.Shutdown();
        }
    }
}
