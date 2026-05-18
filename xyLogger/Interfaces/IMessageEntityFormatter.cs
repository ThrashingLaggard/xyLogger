using Microsoft.Extensions.Logging;
using xyLogger.Models;

namespace xyLogger.Interfaces
{
    /// <summary>
    /// Interface for Entity-Formatters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageEntityFormatter<T>
    {
        /// <summary>
        /// Read information from entity
        /// </summary>
        /// <param name="entry_"></param>
        /// <param name="callerName"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        string UnpackAndFormatFromEntity(T entry_, string? callerName = null, LogLevel? level = null);
        /// <summary>
        /// Pack information into entity
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
        xyDefaultLogEntry PackAndFormatIntoEntity(string source, LogLevel level, string message, DateTimeOffset timestamp, uint? id = null, string? description = null, string? comment = null, Exception? exception = null, string? callerFile = null, int callerLine = 0);
    }
}
