namespace xyLogger.Interfaces
{
    public interface ILoggerManager : ILogging
    {
        void RegisterLogger(params ILogging[] loggers);
        void UnregisterLogger(ILogging target);
        ushort Count { get; }
    }
}