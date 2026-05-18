using Microsoft.Extensions.Logging;
using xyLogger.Models;

namespace xyLogger.Interfaces;

public interface ILogSink : IDisposable
{
    LogLevel MinimumLevel { get; set; }
    bool IsEnabled(LogLevel level) => level >= MinimumLevel;  // Default Method

    Task WriteAsync(xyDefaultLogEntry entry, CancellationToken ct = default);
    Task FlushAsync(CancellationToken ct = default);
}