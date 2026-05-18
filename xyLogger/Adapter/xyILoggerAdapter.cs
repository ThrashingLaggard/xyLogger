using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using xyLogger.Interfaces;

namespace xyLogger.Adapters
{
    // ═══════════════════════════════════════════════════════════════════
    // Direction A:  ILogging  →  ILogger<T>
    //
    // Use when third-party code expects ILogger<T> (e.g. ASP.NET, EF Core)
    // but you want all output routed into your own ILogging stack.
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Wraps an <see cref="ILogging"/> instance and exposes it as <see cref="ILogger{T}"/>,
    /// so any framework component that depends on <see cref="ILogger{T}"/> transparently
    /// routes its output into your logging stack.
    /// </summary>
    /// <typeparam name="T">The category type — matches the generic of <see cref="ILogger{T}"/>.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "<This is the way>")]
    public class xyILoggerAdapter<T>(ILogging logger_) : ILogger<T>
    {
        private readonly ILogging _logger = logger_;

        /// <summary>
        /// Minimum <see cref="LogLevel"/> that will be forwarded to the inner logger.
        /// Messages below this level are silently dropped. Default: <see cref="LogLevel.Trace"/> (everything passes).
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Trace;

        /// <summary>
        /// When <see langword="true"/>, <see cref="BeginScope{TState}"/> maintains an
        /// <see cref="AsyncLocal{T}"/> scope stack and prepends active scope data to every
        /// log message. When <see langword="false"/>, scopes are ignored and a <see cref="NullScope"/>
        /// is returned instead — zero overhead.
        /// </summary>
        public bool ScopesEnabled { get; set; } = false;

        // One stack per async context — each Task/thread gets its own scope chain.
        private readonly AsyncLocal<Stack<object?>> _scopeStack = new();

        private Stack<object?> ScopeStack
            => _scopeStack.Value ??= new Stack<object?>();

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (!ScopesEnabled) return NullScope.Instance;
            return new LogScope(ScopeStack, state);
        }

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="logLevel"/> is at or above
        /// <see cref="MinLevel"/> and is not <see cref="LogLevel.None"/>.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None && logLevel >= MinLevel;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            if (formatter is null) return;

            string message = formatter(state, exception);
            string caller = BuildCaller(eventId);

            if (ScopesEnabled && ScopeStack.Count > 0)
            {
                string scopeContext = string.Join(" > ",ScopeStack.Reverse().Select(s => s?.ToString() ?? string.Empty));
                message = $"[{scopeContext}] {message}";
            }

            IReadOnlyDictionary<string, object?> properties = ExtractProperties(state);
            string? originalTemplate = ExtractTemplate(state);

            if (exception is not null)
            {
                _logger.ExLog(exception, message, logLevel, caller);
            }
            else if (properties.Count > 0 && originalTemplate is not null)
            {
                _logger.Log(originalTemplate, logLevel, properties, caller);
            }
            else
            {
                _logger.Log(message, logLevel, caller);
            }
        }

        private static IReadOnlyDictionary<string, object?> ExtractProperties<TState>(TState state)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                Dictionary<string, object?> dict = [];
                foreach (KeyValuePair<string, object?> kv in kvps)
                    if (kv.Key != "{OriginalFormat}")
                        dict[kv.Key] = kv.Value;
                return dict;
            }
            return new Dictionary<string, object?>();
        }

        private static string? ExtractTemplate<TState>(TState state)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
                foreach (KeyValuePair<string, object?> kv in kvps)
                    if (kv.Key == "{OriginalFormat}") return kv.Value?.ToString();
            return null;
        }

        /// <summary>
        /// Builds a human-readable caller string from an <see cref="EventId"/>.
        /// Returns an empty string when the EventId carries no useful information.
        /// </summary>
        private static string BuildCaller(EventId eventId)
        {
            bool hasId = eventId.Id != 0;
            bool hasName = !string.IsNullOrEmpty(eventId.Name);

            return (hasId, hasName) switch
            {
                (true, true) => $"{eventId.Id}:{eventId.Name}",
                (true, false) => eventId.Id.ToString(),
                (false, true) => eventId.Name!,
                _ => string.Empty
            };
        }
    }


    // ═══════════════════════════════════════════════════════════════════
    // Direction B:  ILogger<T>  →  ILogging
    //
    // Use when you want an existing ILogger<T> provider (Serilog, NLog,
    // Application Insights, …) to act as the backend for your ILogging calls.
    // Register this with xyLoggerManager just like any other ILogging.
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Wraps an <see cref="ILogger{T}"/> instance and exposes it as <see cref="ILogging"/>,
    /// so your logging calls transparently flow into any MS-Logging-compatible provider
    /// such as Serilog, NLog, or Application Insights.
    /// </summary>
    /// <typeparam name="T">The category type — matches the generic of the wrapped <see cref="ILogger{T}"/>.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "<This is the way>")]
    public class xyLoggingAdapter<T>(ILogger<T> logger_) : ILogging
    {
        private readonly ILogger<T> _logger = logger_;

        /// <inheritdoc/>
        public void Log(string message, LogLevel level, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (!_logger.IsEnabled(level)) return;
            _logger.Log(level, "[{Caller}] {Message}", callerName+ " " + callerFile+ " "+ callerLine, message);
        }

        /// <inheritdoc/>
        public void Log(string template, LogLevel level, IReadOnlyDictionary<string, object?> properties, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (!_logger.IsEnabled(level)) return;


            _logger.Log(level, template, properties);
        }

        /// <inheritdoc/>
        public void ExLog(Exception ex, string? message = null, LogLevel level = LogLevel.Error, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (!_logger.IsEnabled(level)) return;
            _logger.Log(level, ex, "[{Caller}] {Message}", callerName + " " + callerFile + " " + callerLine, message ?? ex.Message);
        }

        /// <summary>
        /// No-op. <see cref="ILogger{T}"/> providers do not have a shutdown concept —
        /// their lifecycle is managed by the DI container / application host.
        /// </summary>
        public void Shutdown() { }

    }


    // ═══════════════════════════════════════════════════════════════════
    // Scope helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// A no-op <see cref="IDisposable"/> returned by <see cref="xyILoggerAdapter{T}.BeginScope{TState}"/>
    /// when <see cref="xyILoggerAdapter{T}.ScopesEnabled"/> is <see langword="false"/>.
    /// Uses a singleton to avoid any allocation.
    /// </summary>
    internal sealed class NullScope : IDisposable
    {
        /// <summary>The singleton instance — always use this instead of creating new.</summary>
        public static readonly NullScope Instance = new();
        private NullScope() { }
        public void Dispose() { }
    }

    /// <summary>
    /// A real scope that pushes <paramref name="state"/> onto the <see cref="AsyncLocal{T}"/> stack
    /// on creation and pops it on <see cref="Dispose"/>. Guards against double-dispose.
    /// </summary>
    internal sealed class LogScope : IDisposable
    {
        private readonly Stack<object?> _stack;
        private bool _disposed;

        public LogScope(Stack<object?> stack, object? state)
        {
            _stack = stack;
            _stack.Push(state);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_stack.Count > 0)
                _stack.Pop();
        }
    }
}