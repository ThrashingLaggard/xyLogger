using xyLogger.Models;

namespace xyLogger.Interfaces
{
    public interface ILoggerManager : ILogging
    {
        void RegisterLogger(params ILogging[] loggers);
        void UnregisterLogger(ILogging target);
        ushort Count { get; }

        event EventHandler<xyLogEventArgs>? LogWritten;
        event EventHandler<xyLogEventArgs>? ExLogWritten;
    }
}