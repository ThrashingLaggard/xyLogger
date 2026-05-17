using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using xyLogger.Interfaces;


namespace xyLogger.Models
{
    /// <summary>
    /// Bundled information for a log message
    /// </summary>
    public class xyDefaultLogEntry : ISerializable, IEntry
    {
        /// <summary>
        /// For easy administration
        /// </summary>
        public uint ID { get; set; }

        /// <summary>
        /// Add interesting information
        /// </summary>
        public string Description { get; set; } = default!;

        /// <summary>
        /// Additional information
        /// </summary>
        public string Comment { get; set; } = default!;

        /// <summary>
        /// Time of logging
        /// </summary>
        public required DateTime Timestamp { get; init; }

        /// <summary>
        /// Where it was logged --> callername
        /// </summary>
        public required string Source { get; init; }

        public string? CallerFile { get; init; }
        public int CallerLine { get; init; }

        /// <summary>
        /// The level of "severity"
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// The logging message
        /// </summary>
        public required string Message { get; init; }

        /// <summary>
        /// The exception connected to the log
        /// </summary>
        public Exception? Exception { get; set; }

        public xyExceptionEntry ExceptionEntry { get; set; }

        [JsonConstructor]
        public xyDefaultLogEntry(string source_, LogLevel level_, string message_, DateTime timestamp_, Exception? exception_ = null, string? callerFile_ = null, int callerLine_ = 0)
        {
            Timestamp = timestamp_;
            CallerFile = callerFile_;
            CallerLine = callerLine_;
            Source = source_;
            Level = level_;
            Message = message_;
            Exception = exception_ ?? default!;
            ExceptionEntry = exception_ is not null? (new xyExceptionEntry(exception_: exception_ ) { Exception = exception_}) : default!;
        }



        /// <summary>
        /// Method from ISerializable
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(ID), ID);
            info.AddValue(nameof(Description), Description);
            info.AddValue(nameof(Comment), Comment);
            info.AddValue(nameof(Timestamp), Timestamp);
            info.AddValue(nameof(Source), Source);
            info.AddValue(nameof(Level), Level);
            info.AddValue(nameof(Message), Message);
            info.AddValue(nameof(Exception), Exception?.ToString());
        }


        /// <summary>
        /// Get relevant information for the streaming context
        /// </summary>
        /// <param name="context"></param>
        public void ReadAllStreamingContextInfo(StreamingContext context)
        {
            // Auslesen des State
#pragma warning disable SYSLIB0050 // Typ oder Element ist veraltet
            Console.WriteLine($"StreamingContext State: {context.State}");
#pragma warning restore SYSLIB0050 // Typ oder Element ist veraltet

            // Überprüfen, ob der Context zusätzliche Informationen enthält
            if (context.Context != null)
            {
                Console.WriteLine($"Additional Context Information Type: {context.Context.GetType()}");
                Console.WriteLine($"Additional Context Information: {context.Context}");

                // Wenn der Context ein anderes Objekt ist, können Sie die Eigenschaften dynamisch auslesen
                var properties = context.Context.GetType().GetProperties();
                foreach (var property in properties)
                {
                    var value = property.GetValue(context.Context);
                    Console.WriteLine($"{property.Name}: {value}");
                }
                //// Beispiel für benutzerdefinierte Informationen
                //if (context.Context is xyContext customContext)
                //{
                //    Console.WriteLine($"UserId: {customContext.UserId}");
                //    Console.WriteLine($"SessionId: {customContext.SessionId}");
                //}
                //else
                //{
                //}
            }
            else
            {
                Console.WriteLine("No additional context information available.");
            }
        }

    }
}
