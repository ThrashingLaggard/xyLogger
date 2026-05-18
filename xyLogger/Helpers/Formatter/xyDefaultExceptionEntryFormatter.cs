using Microsoft.Extensions.Logging;
using System.Reflection;
using xyLogger.Interfaces;
using xyLogger.Models;


namespace xyLogger.Helpers.Formatters
{
    public class xyDefaultExceptionEntryFormatter : IExceptionEntityFormatter
    {



        public xyExceptionEntry PackAndFormatIntoEntity(Exception exception, DateTimeOffset? timestamp = null,string? message = null,  uint? id = null, string? description = null, string? callerFile = null, int callerLine = 0)
        {
            return new(exception, callerFile, callerLine);
        }



        /// <inheritdoc/>
        public string UnpackAndFormatFromEntity<T>(T entry_, string? callerName = null, LogLevel? level = LogLevel.Debug) where T : class
        {
            if (entry_ is xyExceptionEntry entry)
            {
                
                Dictionary<string, object?> dictionary = GetPropertyValuesForTarget<T>(entry_);
                string properties = Join(dictionary);
                return properties;
            }
            return "";
        }

        private static PropertyInfo[] GetPropertyInfosForTarget<T>(T obj)
        {
            try
            {
                if (obj is null)
                {
                    xyOutput.Output("Parameter is Null");
                    return [];
                }
                else
                {
                    Type type = typeof(T);
                    if (type.GetProperties() is PropertyInfo[] propertyInfos && propertyInfos.Length > 0)
                    {
                        // Falls mal wieder debuggt werden muss
                        // xyOutput.Output($"Successfully read the property infos for {type}");
                        return propertyInfos;
                    }
                }
            }
            catch (Exception ex)
            {
                xyOutput.Output(xyLogFormatter.FormatExceptionDetails(ex));
            }
            return [];
        }

        private static Dictionary<string, object?> GetPropertyValuesForTarget<T>(T obj) where T : class 
        {
            Dictionary<string, object?> propertyDictionary = [];
            PropertyInfo[] propertyInfos = GetPropertyInfosForTarget(obj);

            foreach (PropertyInfo info in propertyInfos)
            {
                object? value = info.GetValue(obj);
                if (value is null) continue;
                try
                {
                    propertyDictionary.Add(info.Name, value);
                }
                catch (Exception ex)
                {
                    xyOutput.Output(xyLogFormatter.FormatExceptionDetails(ex));
                }
            }

            return propertyDictionary;
        }


        private static string Join<T>(IEnumerable<T> values, bool? hasWhitespace = true, bool? hasSeperator = true, string? seperator = ",")
           => string.Join((hasSeperator ?? true ? seperator : string.Empty) + (((bool)hasWhitespace!) ? " " : string.Empty), values);
    }
}
