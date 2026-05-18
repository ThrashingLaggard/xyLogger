namespace xyLogger.Models;

public sealed class xyLogEventArgs : EventArgs
{
    public xyDefaultLogEntry Entry { get; }

    public xyLogEventArgs(xyDefaultLogEntry entry)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
    }
}