using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using xyLogger.Enums;
using xyLogger.Helpers;
using xyLogger.Helpers.Formatters;
using xyLogger.Interfaces;
using xyLogger.Models;

namespace xyLogger.Loggers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "<This is the way>")]
    public class xyAsyncLogger<T> : ILogging, IDisposable
    {
        private readonly Task _worker;
        private readonly StreamWriter _writer;
        private readonly IReadOnlyList<xyLogTargets> _targets;
        private readonly CancellationTokenSource _cts = new();
        private readonly BlockingCollection<xyDefaultLogEntry> _queue = [];
        private readonly List<IEntry> _entries = [];
        private readonly object _entriesLock = new();

        private volatile ILogSink[] _sinks = [];
        private readonly object _sinksLock = new();

        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        public IMessageFormatter? MessageFormatter { get; set; }
        public IExceptionFormatter? ExceptionFormatter { get; set; }
        public IMessageEntityFormatter<T>? MessageEntryFormatter { get; set; }
        public IExceptionEntityFormatter? ExceptionEntryFormatter { get; set; }

        public IReadOnlyList<IEntry> GetEntries()
        {
            lock (_entriesLock) return _entries.ToList();
        }
        public IEnumerable<xyDefaultLogEntry> GetMessageEntries() => GetEntries().OfType<xyDefaultLogEntry>();
        public IEnumerable<xyExceptionEntry> GetExceptionEntries() => GetEntries().OfType<xyExceptionEntry>();

        public xyAsyncLogger(
            string? filepath = null,
            IEnumerable<xyLogTargets>? logTargets = null,
            IMessageFormatter? messageFormatter_ = null,
            IExceptionFormatter? exceptionFormatter_ = null,
            IMessageEntityFormatter<T>? messageEntryFormatter_ = null,
            IExceptionEntityFormatter? exceptionEntryFormatter_ = null)
        {
            MessageFormatter = messageFormatter_ ?? new xyDefaultMessageFormatter();
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

            _targets = logTargets?.ToList() ?? [xyLogTargets.StandardSystemConsole];
            _writer = _targets.Contains(xyLogTargets.File)
                ? new StreamWriter(filepath!, true) { AutoFlush = true }
                : StreamWriter.Null;

            _worker = Task.Run(() => ProcessQueue(), _cts.Token);
        }

        // ── Sink management ───────────────────────────────────────────

        public void AddSink(ILogSink sink)
        {
            if (sink is null) return;
            lock (_sinksLock)
            {
                ILogSink[] current = _sinks;
                ILogSink[] updated = new ILogSink[current.Length + 1];
                current.CopyTo(updated, 0);
                updated[current.Length] = sink;
                _sinks = updated;
            }
        }

        public void RemoveSink(ILogSink sink)
        {
            lock (_sinksLock)
            {
                _sinks = _sinks.Where(s => s != sink).ToArray();
            }
        }

        // ── ILogging — plain ──────────────────────────────────────────

        public void Log(string message, LogLevel level,
            [CallerMemberName] string? callerName = null,
            [CallerFilePath] string? callerFile = null,
            [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;

            xyDefaultLogEntry entry = BuildMessageEntry(
                callerName ?? string.Empty, level, message,
                DateTimeOffset.Now, callerFile, callerLine);

            lock (_entriesLock) _entries.Add(entry);
            _queue.Add(entry);
        }

        public void ExLog(Exception ex, string? message = null,
            LogLevel level = LogLevel.Error,
            [CallerMemberName] string? callerName = null,
            [CallerFilePath] string? callerFile = null,
            [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;

            xyExceptionEntry excEntry = FormatIntoExceptionEntry(ex, message, callerFile, callerLine);
            xyDefaultLogEntry entry = new(
                callerName ?? string.Empty, level,
                message ?? ex.Message, DateTimeOffset.Now,
                ex, callerFile, callerLine)
            {
                ExceptionEntry = excEntry,
            };

            lock (_entriesLock) _entries.Add(entry);
            _queue.Add(entry);
        }

        // ── ILogging — structured ─────────────────────────────────────

        public void Log(string template, LogLevel level,
            IReadOnlyDictionary<string, object?> properties,
            [CallerMemberName] string? callerName = null,
            [CallerFilePath] string? callerFile = null,
            [CallerLineNumber] int callerLine = 0)
        {
            if (level < MinimumLevel) return;

            string rendered = xyLogTemplate.Render(template, properties);

            xyDefaultLogEntry entry = BuildMessageEntry(
                callerName ?? string.Empty, level, rendered,
                DateTimeOffset.Now, callerFile, callerLine,
                messageTemplate: template, properties: properties);

            lock (_entriesLock) _entries.Add(entry);
            _queue.Add(entry);
        }

        // ── Worker ────────────────────────────────────────────────────

        private async Task ProcessQueue()
        {
            try
            {
                foreach (xyDefaultLogEntry entry in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    string formatted = RenderEntry(entry);
                    await WriteStringToTargets(formatted);
                    await WriteEntryToSinks(entry);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ExceptionFormatter ??= new xyDefaultExceptionFormatter();
                xyOutput.OutputError(ExceptionFormatter.FormatExceptionDetails(ex, "Error in xyAsyncLogger worker queue", LogLevel.Error));
            }
        }

        private async Task WriteStringToTargets(string formatted)
        {
            if (_targets.Contains(xyLogTargets.StandardSystemConsole))
            {
                await xyOutput.OutputAsync(formatted);
            }
            if (_targets.Contains(xyLogTargets.File))
                await _writer.WriteLineAsync(formatted);
        }

        private async Task WriteEntryToSinks(xyDefaultLogEntry entry)
        {
            ILogSink[] snapshot = _sinks;
            foreach (ILogSink sink in snapshot)
            {
                if (sink.IsEnabled(entry.Level))
                    await sink.WriteAsync(entry, _cts.Token);
            }
        }

        // ── Rendering ─────────────────────────────────────────────────

        private string RenderEntry(xyDefaultLogEntry entry)
        {
            if (entry.Exception is not null)
            {
                ExceptionFormatter ??= new xyDefaultExceptionFormatter();
                return ExceptionFormatter.FormatExceptionDetails(
                    entry.Exception, entry.Message, entry.Level,
                    entry.Source, entry.CallerFile, entry.CallerLine);
            }

            MessageFormatter ??= new xyDefaultMessageFormatter();
            return MessageFormatter.FormatMessageForLogging(
                entry.Message, entry.Level, entry.Source,
                entry.CallerFile, entry.CallerLine);
        }

        // ── Entry builders ────────────────────────────────────────────

        private xyDefaultLogEntry BuildMessageEntry(string source, LogLevel level, string message,DateTimeOffset timestamp, string? callerFile, int callerLine,string? messageTemplate = null,IReadOnlyDictionary<string, object?>? properties = null)
        {
            return new xyDefaultLogEntry(source, level, message, timestamp, null, callerFile, callerLine)
            {
                MessageTemplate = messageTemplate ?? string.Empty,Properties = properties ?? new Dictionary<string, object?>(),
            };
        }

        private xyExceptionEntry FormatIntoExceptionEntry(Exception exception, string? information = null,string? callerFile = null, int callerLine = 0)
        {
            ExceptionEntryFormatter ??= new xyDefaultExceptionEntryFormatter();
            return ExceptionEntryFormatter.PackAndFormatIntoEntity(exception, DateTimeOffset.Now, information, null, null, callerFile, callerLine);
        }

        // ── Shutdown / Dispose ────────────────────────────────────────

        public void Shutdown()
        {
            _queue.CompleteAdding();

            if (!_worker.Wait(TimeSpan.FromSeconds(10)))
            {
                _cts.Cancel();
                _worker.Wait(TimeSpan.FromSeconds(1));
            }

            ILogSink[] snapshot = _sinks;
            foreach (ILogSink sink in snapshot)
            {
                try { sink.FlushAsync().GetAwaiter().GetResult(); }
                catch { }
            }
        }

        public void Dispose()
        {
            Shutdown();
            _writer.Flush();
            _writer.Dispose();
            _queue.Dispose();
            _worker.Dispose();
            _cts.Dispose();

            ILogSink[] snapshot = _sinks;
            foreach (ILogSink sink in snapshot)
            {
                try { sink.Dispose(); }
                catch { }
            }

            GC.SuppressFinalize(this);
        }
    }
}