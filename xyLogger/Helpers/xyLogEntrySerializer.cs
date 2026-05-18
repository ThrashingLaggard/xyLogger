using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Xml.Serialization;
using xyLogger.Helpers.Formatters;
using xyLogger.Models;


namespace xyLogger.Helpers
{
    /// <summary>
    /// Helper class to serialize the custom logentries
    /// </summary>
    public class xyLogEntrySerializer()
    {

        /// <summary>
        /// Serialize per System.Text.Json
        /// </summary>
        /// <returns></returns>
        public string ToJson(xyDefaultLogEntry entry) => JsonSerializer.Serialize(entry);

        /// <summary>
        /// Deserialize a json string into an instance of xyLogEntry
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public xyDefaultLogEntry FromJson(string json)
        {
            if (JsonSerializer.Deserialize<xyDefaultLogEntry>(json) is xyDefaultLogEntry entry)
            {
                return entry;
            }
            else return new xyDefaultLogEntry("", LogLevel.Error, "", DateTimeOffset.Now)
            {
                Source = "xyLogEntry.FromJson()",
                Message = "Deserialization from JSON failed!",
                Timestamp = DateTimeOffset.Now
            };
        }


        /// <summary>
        /// Serialize per System.Xml.Serialization
        /// </summary>
        /// <returns></returns>
        public static string ToXML<T>(T target)
        {
            try
            {
                using (StringWriter stringWriter = new())
                {

                    XmlSerializer xmlSerializer = new(typeof(T));
                    xmlSerializer.Serialize(stringWriter, target);
                    return stringWriter.ToString();
                }
                ;
            }
            catch (Exception ex)
            {
                xyOutput.Output(xyLogFormatter.FormatExceptionDetails(ex));
            }
            string msg = $"An Error occured while trying to serialize {target}";
            xyOutput.Output(msg);
            return msg;
        }





        /// <summary>
        /// Deserialize a xml string into an instance of xyLogEntry
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="outputTargetInConsole"></param>
        /// <returns></returns>
        public T FromXml<T>(string xml, bool outputTargetInConsole = false)
        {
            try
            {
                XmlSerializer deserializer = new(typeof(T));
                using StringReader reader = new StringReader(xml);
                if (deserializer.Deserialize(reader) is T target)
                {
                    xyOutput.Output($"{target} has been deserialized!");
                    if (outputTargetInConsole is true)
                    {
                        xyOutput.Output(xml);
                    }
                    return target;
                }
                else
                {
                    xyOutput.Output($"An error occured while trying to deserialize {nameof(xml)}");
                    throw new SerializationException();
                }
            }
            catch (Exception ex)
            {
                xyOutput.Output(xyLogFormatter.FormatExceptionDetails(ex));
                return default!;
            }
        }

    }
}
