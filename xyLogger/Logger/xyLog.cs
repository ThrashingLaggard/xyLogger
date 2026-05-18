using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using xyLogger.Enums;
using xyLogger.Helpers;
using xyLogger.Helpers.Archiver;
using xyLogger.Helpers.Formatters;

namespace xyLogger.Loggers
{

    /// <summary>
    /// Provides centralised, static logging functionality including plain-text and JSON output,
    /// synchronous and asynchronous variants, file persistence, and event-based log forwarding.
    /// </summary>
    /// <remarks>
    /// Basically this is my "quick and dirty" fallback for when i dont want to configure stuff but just need to log a bit asap
    /// <para>
    /// Subscribers can react to log output via <see cref="LogMessageSent"/> for standard messages
    /// and <see cref="ExLogMessageSent"/> for exception messages.
    /// </para>
    /// <para>
    /// Log output is written to the console and optionally persisted to a rolling log file.
    /// File-write methods (<see cref="WriteLog"/>, <see cref="WriteExLog"/>, etc.) are thread-safe;
    /// access is serialised via an internal lock.
    /// Console-only methods (<see cref="Log"/>, <see cref="ExLog"/>, etc.) do not acquire the lock.
    /// </para>
    /// <para>
    /// Log files are automatically archived when they exceed the configured size limit.
    /// File paths and the maximum file size are hardcoded; external configuration is not yet supported.
    /// </para>
    /// <para>
    /// <b>Performance note:</b> Suitable for low-to-medium logging frequency.
    /// Synchronous file-write methods block the calling thread while the lock is held.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "<Because i said so!>")]
    public static class xyLog
    {
        /// <summary>
        /// Raised after a standard log message has been written to the output.
        /// </summary>
        /// <remarks>
        /// The first argument is the fully formatted message string, the second is used for the caller name.
        /// Can be raised via <see cref="OnMessageSent"/>.
        /// </remarks>
        public static event Action<string, string>? LogMessageSent;

        /// <summary>
        /// Raised after an exception log message has been written to the output.
        /// </summary>
        /// <remarks>
        /// The first argument is the formatted exception details, the second is the caller name.
        /// Can be raised via <see cref="OnExMessageSent"/>.
        /// </remarks>
        public static event Action<string, string>? ExLogMessageSent;


        /// <summary>
        /// Raises <see cref="LogMessageSent"/> with the supplied message.
        /// </summary>
        /// <param name="message">The message to broadcast via the event.</param>
        /// <param name="callerName">Automatically filled attribute</param>
        public static void OnMessageSent(string message, [CallerMemberName] string? callerName = "Event - Unknown caller")
        {
            LogMessageSent?.Invoke(message, callerName!);
        }

        /// <summary>
        /// Raises <see cref="ExLogMessageSent"/> with the supplied message.
        /// </summary>
        /// <param name="message">The exception-related message to broadcast via the event.</param>
        /// <param name="callerName">Automatically filled attribute</param>
        public static void OnExMessageSent(string message, [CallerMemberName] string? callerName = "Event - Unknown caller")
        {
            ExLogMessageSent?.Invoke(message, callerName!);
        }

        // Default configuration and internal state
        private static string _logFilePath = "logs/app.log";
        private static string _exLogFilePath = "logs/exceptions.log";
        private static readonly long _maxLogFileSize = 10485760;
        private static readonly object _threadSafetyLock = new();
        private readonly static xyLogArchiver _archiver = new(_maxLogFileSize);
        // private static IEnumerable<xyLogTargets> _eTargets = new List<xyLogTargets>();

        /// <summary>
        /// The lowest acceptable level for logging ops
        /// </summary>
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        public static void Configure(string logDir = "logs")
        {
            _logFilePath = Path.Combine(logDir, "app.log");
            _exLogFilePath = Path.Combine(logDir, "exceptions.log");
            Directory.CreateDirectory(logDir);
        }


        #region Logging

        /// <summary>
        /// Formats and writes a plain-text message to the console synchronously.
        /// </summary>
        /// <param name="message">The raw message text to log.</param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <param name="logLevel">
        /// Severity level included in the formatted output. Defaults to <see cref="LogLevel.Debug"/>.
        /// </param>
        /// <returns>The fully formatted log string that was written to the console.</returns>
        public static string Log(string message, [CallerMemberName] string? callerName = null, LogLevel? logLevel = LogLevel.Debug)
        {
            if (logLevel < MinimumLevel) return "";

            string formattedMsg = FormatMsg(message, callerName, logLevel);
            xyOutput.Output(formattedMsg);
            LogMessageSent?.Invoke(formattedMsg, callerName!);
            return formattedMsg;
        }

        public static string Log(string template, LogLevel level, IReadOnlyDictionary<string, object?> properties, [CallerMemberName] string? callerName = null)
        {
            if (level < MinimumLevel) return string.Empty;
            string rendered = xyLogTemplate.Render(template, properties);
            string formattedMsg = FormatMsg(rendered, callerName, level);
            xyOutput.Output(formattedMsg);
            LogMessageSent?.Invoke(formattedMsg, callerName!);
            return formattedMsg;
        }

        /// <summary>
        /// Formats and writes a plain-text message to the console asynchronously.
        /// </summary>
        /// <param name="message">The raw message text to log.</param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <param name="logLevel">
        /// Severity level included in the formatted output. Defaults to <see cref="LogLevel.Debug"/>.
        /// </param>
        /// <returns>A task that resolves to the fully formatted log string that was written.</returns>
        public static async Task<string> AsxLog(string message, LogLevel? logLevel = LogLevel.Debug, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (logLevel < MinimumLevel) return "";

            string formattedMsg = FormatMsg(message, callerName, logLevel, callerFile, callerLine);
            await xyOutput.OutputAsync(formattedMsg);
            LogMessageSent?.Invoke(formattedMsg, callerName!);
            return formattedMsg;
        }

        /// <summary>
        /// Formats and writes exception details to the console synchronously.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="message">An optional additional message providing context for the exception.</param>
        /// <param name="level">
        /// Severity level included in the formatted output. Defaults to <see cref="LogLevel.Error"/>.
        /// </param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        public static void ExLog(Exception ex, string? message = null, LogLevel? level = LogLevel.Error, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;

            string exMessage = FormatEx(ex, message, level, callerName, callerFile, callerLine);
            xyOutput.Output(exMessage);
            ExLogMessageSent?.Invoke(exMessage, callerName!);
        }

        /// <summary>
        /// Formats and writes exception details to the console asynchronously.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="message"></param>
        /// <param name="level">
        /// Severity level included in the formatted output. Defaults to <see cref="LogLevel.Error"/>.
        /// </param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns>A task representing the asynchronous console-write operation.</returns>
        public static async Task AsxExLog(Exception ex, string? message = null, LogLevel? level = LogLevel.Error, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;

            string exMessage = FormatEx(ex, message ?? "", level, callerName, callerFile, callerLine);
            await xyOutput.OutputAsync(exMessage);
            ExLogMessageSent?.Invoke(exMessage, callerName!);
        }

        /// <summary>
        /// Serialises an exception to JSON and writes it to the console asynchronously.
        /// </summary>
        /// <param name="ex">The exception to serialise and log.</param>
        /// <param name="level"></param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <returns>A task representing the asynchronous console-write operation.</returns>
        public static void JsonExLog(Exception ex, LogLevel level = LogLevel.Information, [CallerMemberName] string? callerName = null)
        {
            if (level < MinimumLevel) return;
            string json = xyLogFormatter.FormatExceptionAsJson(ex);
            xyOutput.Output(json);
            ExLogMessageSent?.Invoke(json, callerName!);
        }

        /// <summary>
        /// Serialises an exception to JSON and writes it to the console asynchronously.
        /// </summary>
        /// <param name="ex">The exception to serialise and log.</param>
        /// <param name="level"></param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns>A task representing the asynchronous console-write operation.</returns>
        public static async Task AsxJsonExLog(Exception ex, LogLevel level = LogLevel.Information, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;

            string json = xyLogFormatter.FormatExceptionAsJson(ex);
            await xyOutput.OutputAsync(json);
            ExLogMessageSent?.Invoke(json, callerName!);
        }
        #endregion
        /// <summary>
        /// Writes a formatted plain-text message to both the log file and the console synchronously.
        /// Automatically archives the log file if it exceeds the configured size limit.
        /// </summary>
        /// <param name="message">The raw message text to log.</param>
        /// <param name="level"></param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns>
        /// <see langword="true"/> if the message was written successfully;
        /// <see langword="false"/> if an exception occurred during the file-write operation.
        /// </returns>
        public static bool WriteLog(string message, LogLevel level = LogLevel.Information, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return false;

            try
            {
                lock (_threadSafetyLock)
                {
                    _archiver.MoveLogToArchiveFileIfTooBig(_logFilePath);
                    string formattedMessage = FormatMsg(message, callerName);
                    File.AppendAllText(_logFilePath, formattedMessage);
                    xyOutput.Output(formattedMessage);
                    LogMessageSent?.Invoke(formattedMessage, callerName!);
                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteExLog(ex, "", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Serialises a message to JSON and writes it to both the log file and the console synchronously.
        /// Automatically archives the log file if it exceeds the configured size limit.
        /// </summary>
        /// <param name="message">The raw message text to serialise and log.</param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the JSON message was written successfully;
        /// <see langword="false"/> if an exception occurred during the file-write operation.
        /// </returns>
        public static bool WriteJsonLog(string message, [CallerMemberName] string? callerName = null)
        {
            try
            {
                lock (_threadSafetyLock)
                {
                    _archiver.MoveLogToArchiveFileIfTooBig(_logFilePath);
                    string formattedMessage = xyLogFormatter.FormatMessageAsJson(message);
                    File.AppendAllText(_logFilePath, formattedMessage);
                    xyOutput.Output(formattedMessage);
                    LogMessageSent?.Invoke(formattedMessage, callerName!);
                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteExLog(ex, message, LogLevel.Error, true,callerName);
                return false;
            }
        }

        /// <summary>
        /// Formats exception details and writes them to both the exception log file and the console synchronously.
        /// Automatically archives the exception log file if it exceeds the configured size limit.
        /// </summary>
        /// <param name="ex">The exception whose details are to be logged.</param>
        /// <param name="message"></param>
        /// <param name="level">
        /// Severity level included in the formatted output. Defaults to <see cref="LogLevel.Error"/>.
        /// </param>
        /// <param name="console"></param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns>
        /// <see langword="true"/> if the exception details were written successfully;
        /// <see langword="false"/> if a secondary exception occurred during the file-write operation.
        /// </returns>
        public static bool WriteExLog(Exception ex, string? message = null, LogLevel? level = LogLevel.Error, bool? console = true, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            try
            {
                string exceptionDetails = FormatEx(ex, message ?? "", level, callerName, callerFile, callerLine);

                lock (_threadSafetyLock)
                {
                    _archiver.MoveLogToArchiveFileIfTooBig(_exLogFilePath);

                    File.AppendAllText(_exLogFilePath, exceptionDetails);
                }

                if(console is true)
                {
                    xyOutput.Output(exceptionDetails);
                }

                ExLogMessageSent?.Invoke(exceptionDetails, callerName!);
                return true;
            }
            catch (Exception innerEx)
            {
                ExLog(innerEx, level: LogLevel.Warning, callerName: callerName);
            }
            return false;
        }


        /// <summary>
        /// Serialises exception details to JSON and writes them to both the exception log file
        /// and the console synchronously.
        /// Automatically archives the exception log file if it exceeds the configured size limit.
        /// </summary>
        /// <param name="ex">The exception to serialise and log.</param>
        /// <param name="message">An optional additional message providing context for the exception.</param>
        /// <param name="level">
        /// Severity level used if a secondary exception is caught internally.
        /// Defaults to <see cref="LogLevel.Error"/>.
        /// </param>
        /// <param name="callerName">
        /// Name of the calling member, captured automatically via <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the JSON exception details were written successfully;
        /// <see langword="false"/> if a secondary exception occurred during the file-write operation.
        /// </returns>
        public static bool WriteJsonExLog(Exception ex, string? message = default, LogLevel? level = LogLevel.Error, [CallerMemberName] string? callerName = null)
        {
            lock (_threadSafetyLock)
            {
                try
                {
                    _archiver.MoveLogToArchiveFileIfTooBig(_exLogFilePath);

                    string exceptionDetails = xyLogFormatter.FormatExceptionAsJson(ex);
                    File.AppendAllText(_exLogFilePath, exceptionDetails);
                    xyOutput.Output(exceptionDetails);
                    ExLogMessageSent?.Invoke(exceptionDetails, callerName!);
                    return true;
                }
                catch (Exception innerEx)
                {
                    ExLog(innerEx, message, level ?? LogLevel.Warning, callerName: callerName);
                }
                return false;
            }
        }


        #region Service
        /// <summary>
        /// Executes a function synchronously, logging its start, result, and any exception that occurs.
        /// </summary>
        /// <typeparam name="T">The return type of <paramref name="action"/>.</typeparam>
        /// <param name="actionName">
        /// A descriptive label for the operation, included in all log messages.
        /// </param>
        /// <param name="action">The function to execute and observe.</param>
        /// <param name="logOnError">
        /// An optional delegate invoked with the caught exception before the fallback return.
        /// When <see langword="null"/>, only the internal <see cref="ExLog"/> call is made.
        /// </param>
        /// <returns>
        /// The result of <paramref name="action"/>, or <see langword="default"/> if an exception was thrown.
        /// </returns>
        /// <example>
        /// <code>
        /// string result = xyLog.ExecuteDebugging(     "Screaming()",     () => Screaming(testText),     ex => xyLog.ExLog(ex, level: LogLevel.Warning));
        /// </code>
        /// </example>        
        public static T ExecuteDebugging<T>(string actionName, Func<T> action, Action<Exception>? logOnError = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(actionName))
                    throw new ArgumentException("ActionName invalid.", nameof(actionName));



                Log(string.Concat("Start: ", actionName));
                var sw = System.Diagnostics.Stopwatch.StartNew();
                T? result = action();
                sw.Stop();
                Log($"✓ {actionName} in {sw.ElapsedMilliseconds}ms");
                Log(result == null || result is IEnumerable<object> enumerable && !enumerable.Any() ?
                    string.Concat("Warnung: ", actionName, " hat keine Ergebnisse geliefert.") :
                    string.Concat("Erfolgreich: ", actionName, " abgeschlossen."));


                return result;
            }
            catch (Exception ex)
            {
                logOnError?.Invoke(ex);// Den im Aufruf per Lambda-Ausdruck generierten Delegaten (in diesem Fall sogar mit Parameter) ansprechen, falls dieser != null 
                ExLog(ex, "", LogLevel.Critical, actionName);  // Wenn die Exception nicht anders definiert ist, als die des Delegaten, kommt ggf zweimal die gleiche Nachricht, mit unterschiedlichen Callern
            }

            return default!;
        }






        /// <summary>
        /// Executes a function asynchronously, logging its start, result, and any exception that occurs.
        /// </summary>
        /// <typeparam name="T">The return type of <paramref name="action"/>.</typeparam>
        /// <param name="actionName">
        /// A descriptive label for the operation, included in all log messages.
        /// </param>
        /// <param name="action">The function to execute and observe.</param>
        /// <param name="logOnError">
        /// An optional delegate invoked with the caught exception before the fallback return.
        /// When <see langword="null"/>, only the internal <see cref="AsxExLog"/> call is made.
        /// </param>
        /// <returns>
        /// A task that resolves to the result of <paramref name="action"/>,
        /// or <see langword="default"/> if an exception was thrown.
        /// </returns>
        public static async Task<T> ExecuteDebuggingAsync<T>(string actionName, Func<Task<T>> action, Action<Exception>? logOnError = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(actionName))
                {
                    var argEx = new ArgumentException("ActionName invalid.", nameof(actionName));
                    await AsxExLog(argEx, "", LogLevel.Error);
                    throw argEx;
                }
                await AsxLog(string.Concat("Start: ", actionName));
                T? result = await action();

                await AsxLog(result == null || result is IEnumerable<object> enumerable && !enumerable.Any() ?
                    string.Concat("Warnung: ", actionName, " hat keine Ergebnisse geliefert.") :
                    string.Concat("Erfolgreich: ", actionName, " abgeschlossen."));
                return result;
            }
            catch (Exception ex)
            {
                logOnError?.Invoke(ex);// Den im Aufruf per Lambda-Ausdruck generierten Delegaten (in diesem Fall sogar mit Parameter) ansprechen, falls dieser != null 
                await AsxExLog(ex, "", LogLevel.Critical, actionName);  // Wenn die Exception nicht anders definiert ist, als die des Delegaten, kommt ggf zweimal die gleiche Nachricht, mit unterschiedlichen Callern
            }

            return default!;
        }


        /// <summary>
        /// Formats a plain-text message for log output by delegating to <see cref="xyLogFormatter"/>.
        /// </summary>
        /// <param name="message">The raw message text to format.</param>
        /// <param name="callerName">Optional name of the originating member.</param>
        /// <param name="level">Optional severity level to include in the formatted output.</param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns>A formatted log string including timestamp, caller, level, and message.</returns>
        public static string FormatMsg(string message, string? callerName = null, LogLevel? level = null, string? callerFile = null, int callerLine = 0) => xyLogFormatter.FormatMessageForLogging(message, callerName, level, callerFile, callerLine);

        /// <summary>
        /// Formats exception details for consistent log output by delegating to <see cref="xyLogFormatter"/>.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <param name="level">Severity level to include in the formatted output.</param>
        /// <param name="message">An optional context message to include alongside the exception details.</param>
        /// <param name="callerName">Optional name of the originating member.</param>
        /// <param name="callerFile"></param>
        /// <param name="callerLine"></param>
        /// <returns>
        /// A formatted string containing the exception type, message, and stack trace.
        /// </returns>
        private static string FormatEx(Exception ex, string? message = null, LogLevel? level = LogLevel.Error, string? callerName = null, string? callerFile = null, int callerLine = 0) => xyLogFormatter.FormatExceptionDetails(ex, level, message, callerName, callerFile, callerLine);

        #endregion

    }
}
