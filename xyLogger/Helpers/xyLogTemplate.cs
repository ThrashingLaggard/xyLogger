using System.Text;

namespace xyLogger.Helpers
{
    public static class xyLogTemplate
    {
        public static string Render(string template, IReadOnlyDictionary<string, object?> properties)
        {
            if (string.IsNullOrEmpty(template)) return template;
            if (properties is null || properties.Count == 0) return template;

            StringBuilder sb = new(template.Length + 64);
            int i = 0;

            while (i < template.Length)
            {
                char c = template[i];

                // Escaped braces: {{ → {  und  }} → }
                if (c == '{' && i + 1 < template.Length && template[i + 1] == '{')
                {
                    sb.Append('{');
                    i += 2;
                    continue;
                }
                if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
                {
                    sb.Append('}');
                    i += 2;
                    continue;
                }

                if (c == '{')
                {
                    int close = template.IndexOf('}', i + 1);
                    if (close > i)
                    {
                        // Hole kann Formatspecifier enthalten: {Name:format}
                        string hole = template.Substring(i + 1, close - i - 1);
                        int colonIndex = hole.IndexOf(':');
                        string name = colonIndex >= 0 ? hole.Substring(0, colonIndex) : hole;
                        string format = colonIndex >= 0 ? hole.Substring(colonIndex + 1) : string.Empty;

                        if (!string.IsNullOrWhiteSpace(name) && properties.TryGetValue(name, out object? value))
                        {
                            if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
                                sb.Append(formattable.ToString(format, null));
                            else
                                sb.Append(value?.ToString() ?? "null");

                            i = close + 1;
                            continue;
                        }
                    }
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        public static IReadOnlyList<string> ExtractPropertyNames(string template)
        {
            if (string.IsNullOrEmpty(template)) return [];

            List<string> names = [];
            int i = 0;

            while (i < template.Length)
            {
                if (template[i] == '{' && i + 1 < template.Length && template[i + 1] == '{') { i += 2; continue; }
                if (template[i] == '}' && i + 1 < template.Length && template[i + 1] == '}') { i += 2; continue; }

                if (template[i] == '{')
                {
                    int close = template.IndexOf('}', i + 1);
                    if (close > i)
                    {
                        string hole = template.Substring(i + 1, close - i - 1);
                        int colon = hole.IndexOf(':');
                        string name = colon >= 0 ? hole.Substring(0, colon) : hole;

                        if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                            names.Add(name);

                        i = close + 1;
                        continue;
                    }
                }
                i++;
            }

            return names;
        }

        public static IReadOnlyDictionary<string, object?> BuildProperties(string[] names, object?[] values)
        {
            Dictionary<string, object?> dict = new(names.Length);
            int count = Math.Min(names.Length, values.Length);
            for (int i = 0; i < count; i++)
                dict[names[i]] = values[i];
            return dict;
        }
    }
}