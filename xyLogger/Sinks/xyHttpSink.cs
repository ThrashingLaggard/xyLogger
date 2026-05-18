using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using xyLogger.Helpers;
using xyLogger.Interfaces;
using xyLogger.Models;

namespace xyLogger.Sinks
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "<This is the way>")]
    public sealed class xyHttpSink : ILogSink
    {
        private readonly HttpClient _http;
        private readonly string _endpoint;
        private readonly bool _ownsClient;
        private bool _disposed;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxRetries { get; set; } = 1;

        public xyHttpSink(string endpoint, HttpClient? httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint must not be null or empty.", nameof(endpoint));

            _endpoint = endpoint;
            _ownsClient = httpClient is null;
            _http = httpClient ?? new HttpClient();
        }

        public async Task WriteAsync(xyDefaultLogEntry entry, CancellationToken ct = default)
        {
            if (entry is null || !((ILogSink)this).IsEnabled(entry.Level)) return;

            string json = SerializeEntry(entry);

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using CancellationTokenSource timeout = new(RequestTimeout);
                    using CancellationTokenSource linked =
                        CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
                    using StringContent content = new(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response =
                        await _http.PostAsync(_endpoint, content, linked.Token);

                    if (response.IsSuccessStatusCode) return;

                    xyOutput.OutputError($"[xyHttpSink] POST to {_endpoint} returned {(int)response.StatusCode} {response.ReasonPhrase} (attempt {attempt + 1}/{MaxRetries + 1})");
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    xyOutput.OutputError(
                        $"[xyHttpSink] Failed to send entry " +
                        $"(attempt {attempt + 1}/{MaxRetries + 1}): {ex.Message}");
                }

                if (attempt < MaxRetries)
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1)), ct);
            }
        }

        public Task FlushAsync(CancellationToken ct = default) => Task.CompletedTask;

        private static string SerializeEntry(xyDefaultLogEntry entry)
        {
            try
            {
                Dictionary<string, object?> payload = new()
                {
                    ["Timestamp"] = entry.Timestamp.ToString("o"),
                    ["Level"] = entry.Level.ToString(),
                    ["MessageTemplate"] = entry.MessageTemplate,
                    ["RenderedMessage"] = entry.Message,
                    ["Source"] = entry.Source,
                    ["CallerFile"] = entry.CallerFile,
                    ["CallerLine"] = entry.CallerLine,
                };

                foreach (KeyValuePair<string, object?> kv in entry.Properties)
                    payload[$"prop_{kv.Key}"] = kv.Value;

                if (entry.Exception is not null)
                {
                    payload["ExceptionType"] = entry.Exception.GetType().FullName;
                    payload["ExceptionMessage"] = entry.Exception.Message;
                    payload["StackTrace"] = entry.Exception.StackTrace;
                }

                return JsonSerializer.Serialize(payload, _jsonOptions);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    Timestamp = DateTimeOffset.Now.ToString("o"),
                    Level = entry.Level.ToString(),
                    Error = $"Serialization failed: {ex.Message}",
                    Raw = entry.Message,
                });
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_ownsClient) _http.Dispose();
        }
    }
}