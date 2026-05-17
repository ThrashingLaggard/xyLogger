using Microsoft.Extensions.Logging;
using System.Reflection;
using xyLogger.Interfaces;
using xyLogger.Loggers;
using xyLogger.Models;


namespace xyLogger.Helpers.Formatters
{
    public class xyDefaultExceptionEntryFormatter : IExceptionEntityFormatter
    {



        public xyExceptionEntry PackAndFormatIntoEntity(Exception exception, DateTime? timestamp = null,string? message = null,  uint? id = null, string? description = null, string? callerFile = null, int callerLine = 0)
        {
            xyExceptionEntry entry = new(exception, callerFile, callerLine)
            {
                CallerFile = callerFile,
                CallerLine = callerLine,
                Exception = exception,
                Message = message ?? "",
                Timestamp = timestamp ?? DateTime.Now,
                Description = description ?? "",
            };
            return entry;
        }

      

        /// <inheritdoc/>
        public string UnpackAndFormatFromEntity<T, TKey, TValue>(T entry_, string? callerName = null, LogLevel? level = LogLevel.Debug)   where T : class where TKey : class
        {
            if (entry_ is xyExceptionEntry entry)
            {
                
                Dictionary<TKey,TValue> dictionary = GetPropertyValuesForTarget<TKey, TValue, T>(entry_);
                string properties = Join(dictionary);
                return properties;
            }
            return "";
        }

        public static PropertyInfo[] GetPropertyInfosForTarget<T>(T obj)
        {
            try
            {
                if (obj is null)
                {
                    xyLog.Log("Parameter is Null");
                    return [];
                }
                else
                {
                    Type type = typeof(T);
                    if (type.GetProperties() is PropertyInfo[] propertyInfos && propertyInfos.Length > 0)
                    {
                        xyLog.Log($"Successfully read the property infos for {type}");
                        return propertyInfos;
                    }
                }
            }
            catch (Exception ex)
            {
                xyLog.ExLog(ex);
            }
            return [];
        }

        public static Dictionary<TKey, TValue> GetPropertyValuesForTarget<TKey, TValue, T>(T obj) where T : class where TKey : class
        {
            PropertyInfo[] propertyInfos = GetPropertyInfosForTarget(obj);

            Dictionary<TKey, TValue> propertyDictionary = [];
            TKey key = default!;
            object? value = default;


            foreach (PropertyInfo info in propertyInfos)
            {
                value = info.GetValue(obj);
                if (value is null) continue;
                try
                {
                    key = (TKey)Convert.ChangeType(info.Name, typeof(TKey));
                    propertyDictionary.Add(key, (TValue)value!);
                }
                catch (Exception ex)
                {
                    xyLog.ExLog(ex);
                }
            }

            return propertyDictionary;
        }


        private static string Join<T>(IEnumerable<T> values, bool? hasWhitespace = true, bool? hasSeperator = true, string? seperator = ",")
           => string.Join((hasSeperator ?? true ? seperator : string.Empty) + (((bool)hasWhitespace!) ? " " : string.Empty), values);
    }
}
