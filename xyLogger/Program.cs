using Microsoft.Extensions.Logging;
using xyLogger.Adapters;
using xyLogger.Enums;
using xyLogger.Helpers.Formatters;
using xyLogger.Loggers;
using xyLogger.Managers;

// ═══════════════════════════════════════════════════════════════════════
//  xyLogger — Wiring Examples
//
//  All four scenarios run without a DI container.
//  At the end of each section there is a comment showing how the same
//  wiring would look with Microsoft.Extensions.DependencyInjection.
// ═══════════════════════════════════════════════════════════════════════

namespace xyLogger
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("=== xyLogger Wiring Examples ===\n");

            Example1_Minimal();
            Example2_Extended();
            Example3_AdapterDirectionA();
            await Example4_AdapterDirectionBAsync();

            Console.WriteLine("\n=== Fertig ===");
        }


        // ───────────────────────────────────────────────────────────────
        // Example 1 — Minimal Setup
        // Manager + ConsoleLogger, no formatters injected (defaults apply)
        // ───────────────────────────────────────────────────────────────
        private static void Example1_Minimal()
        {
            Console.WriteLine("--- Example 1: Minimal ---");

            // Create logger — defaults handle formatting
            var consoleLogger = new xyConsoleLogger<Program>(new xyDefaultMessageFormatter(), new xyDefaultExceptionFormatter());

            // Set up manager and register logger
            var manager = new xyLoggerManager();
            manager.RegisterLogger(consoleLogger);

            // Logging
            manager.Log("Minimal setup works.", LogLevel.Information);

            try
            {
                throw new InvalidOperationException("Test error Example 1");
            }
            catch (Exception ex)
            {
                manager.ExLog(ex, "Exception in minimal setup");
            }

            manager.Shutdown();
            Console.WriteLine();

            // ── With DI ─────────────────────────────────────────────────
            // services.AddSingleton<ILogging, xyConsoleLogger<Program>>();
            // services.AddSingleton<ILoggerManager>(sp =>
            // {
            //     var mgr = new xyLoggerManager();
            //     mgr.RegisterLogger(sp.GetRequiredService<ILogging>());
            //     return mgr;
            // });
        }


        // ───────────────────────────────────────────────────────────────
        // Example 2 — Extended Setup
        // Manager + ConsoleLogger + AsyncLogger with File target
        // ───────────────────────────────────────────────────────────────
        private static void Example2_Extended()
        {
            Console.WriteLine("--- Example 2: Extended ---");

            // Ensure the directory exists (consumer's responsibility)
            string logDir  = Path.Combine(AppContext.BaseDirectory, "logs");
            string logFile = Path.Combine(logDir, "app.log");
            Directory.CreateDirectory(logDir);

            // Two loggers — Console and File
            var consoleLogger = new xyConsoleLogger<Program>(new xyDefaultMessageFormatter(), new xyDefaultExceptionFormatter());
            var asyncLogger   = new xyAsyncLogger<Program>(
                filepath:   logFile,
                logTargets: [xyLogTargets.StandardSystemConsole, xyLogTargets.File]
            );

            // Manager fans out to both
            var manager = new xyLoggerManager();
            manager.RegisterLogger(consoleLogger, asyncLogger);

            Console.WriteLine($"Active loggers: {manager.Count}");

            manager.Log("Message goes to Console AND file.", LogLevel.Information);

            try
            {
                int zero = 0;
                _ = 1 / zero;
            }
            catch (Exception ex)
            {
                manager.ExLog(ex, "Division by zero in Example 2");
            }

            // AsyncLogger needs a clean Shutdown to drain the queue
            manager.Shutdown();
            Console.WriteLine($"Log written to: {logFile}");
            Console.WriteLine();

            // ── With DI ─────────────────────────────────────────────────
            // services.AddSingleton<xyConsoleLogger<Program>>();
            // services.AddSingleton<xyAsyncLogger<Program>>(sp =>
            //     new xyAsyncLogger<Program>(logFile, [xyLogTargets.StandardSystemConsole, xyLogTargets.File]));
            // services.AddSingleton<ILoggerManager>(sp =>
            // {
            //     var mgr = new xyLoggerManager();
            //     mgr.RegisterLogger(
            //         sp.GetRequiredService<xyConsoleLogger<Program>>(),
            //         sp.GetRequiredService<xyAsyncLogger<Program>>());
            //     return mgr;
            // });
        }


        // ───────────────────────────────────────────────────────────────
        // Example 3 — Adapter Direction A
        // xyILoggerAdapter: ILogging → ILogger<T>
        //
        // Scenario: a third-party class expects ILogger<T>.
        // Your manager is injected as the backend.
        // ───────────────────────────────────────────────────────────────
        private static void Example3_AdapterDirectionA()
        {
            Console.WriteLine("--- Example 3: Adapter Direction A (ILogging → ILogger<T>) ---");

            // Standard setup
            var consoleLogger = new xyConsoleLogger<ThirdPartyService>(new xyDefaultMessageFormatter(), new xyDefaultExceptionFormatter());
            var manager       = new xyLoggerManager();
            manager.RegisterLogger(consoleLogger);

            // Adapter wraps the manager → exposes ILogger<T>
            var adapter = new xyILoggerAdapter<ThirdPartyService>(manager)
            {
                MinLevel      = LogLevel.Debug,
                ScopesEnabled = true              // Enable scopes for this example
            };

            // Third-party class receives ILogger<T> — knows nothing about xyLogger
            var service = new ThirdPartyService(adapter);
            service.DoWork();

            manager.Shutdown();
            Console.WriteLine();

            // ── With DI ─────────────────────────────────────────────────
            // services.AddSingleton<ILogging, xyConsoleLogger<ThirdPartyService>>();
            // services.AddSingleton<ILoggerManager>(sp =>
            // {
            //     var mgr = new xyLoggerManager();
            //     mgr.RegisterLogger(sp.GetRequiredService<ILogging>());
            //     return mgr;
            // });
            // services.AddSingleton<ILogger<ThirdPartyService>>(sp =>
            //     new xyILoggerAdapter<ThirdPartyService>(sp.GetRequiredService<ILoggerManager>())
            //     {
            //         MinLevel      = LogLevel.Debug,
            //         ScopesEnabled = true
            //     });
            // services.AddSingleton<ThirdPartyService>();
        }


        // ───────────────────────────────────────────────────────────────
        // Example 4 — Adapter Direction B
        // xyLoggingAdapter: ILogger<T> → ILogging
        //
        // Scenario: you want an existing ILogger<T> provider
        // (here: Microsoft.Extensions.Logging.Console as a stand-in) to
        // act as the backend for your ILogging system.
        // In practice this would be e.g. a Serilog or NLog ILogger<T>.
        // ───────────────────────────────────────────────────────────────
        private static async Task Example4_AdapterDirectionBAsync()
        {
            Console.WriteLine("--- Example 4: Adapter Direction B (ILogger<T> → ILogging) ---");

            // Statt LoggerFactory: ein einfacher Stub der ILogger<T> implementiert
            // — repräsentiert in der Praxis z.B. einen Serilog-Logger
            var msLogger = new DummyLogger<Program>();

            // Adapter turns ILogger<T> into ILogging
            var adapter = new xyLoggingAdapter<Program>(msLogger);

            // Register adapter with the manager — from here on all
            // xyLogger calls flow through the MS Console provider
            var manager = new xyLoggerManager();
            manager.RegisterLogger(adapter);

            manager.Log("This message goes through xyLoggingAdapter into the MS logger.", LogLevel.Information);

            try
            {
                throw new NotSupportedException("Test error Example 4");
            }
            catch (Exception ex)
            {
                manager.ExLog(ex, "Exception through xyLoggingAdapter");
            }

            manager.Shutdown();

            // Brief delay so the MS Console provider can flush its internal buffer
            await Task.Delay(100);
            Console.WriteLine();

            // ── With DI ─────────────────────────────────────────────────
            // services.AddLogging(builder => builder.AddConsole());  // or Serilog, NLog, etc.
            // services.AddSingleton<ILogging>(sp =>
            //     new xyLoggingAdapter<Program>(sp.GetRequiredService<ILogger<Program>>()));
            // services.AddSingleton<ILoggerManager>(sp =>
            // {
            //     var mgr = new xyLoggerManager();
            //     mgr.RegisterLogger(sp.GetRequiredService<ILogging>());
            //     return mgr;
            // });
        }
    }


    // ───────────────────────────────────────────────────────────────────
    // Helper class for Example 3
    // Simulates a third-party class that expects ILogger<T> and has no
    // knowledge of xyLogger.
    // ───────────────────────────────────────────────────────────────────
    internal class ThirdPartyService(ILogger<ThirdPartyService> logger)
    {
        private readonly ILogger<ThirdPartyService> _logger = logger;

        public void DoWork()
        {
            // Scope demonstration: all log entries inside the using block
            // are enriched with "ThirdPartyService.DoWork"
            using (_logger.BeginScope("ThirdPartyService.DoWork"))
            {
                _logger.LogInformation("Third-party service starting work.");

                try
                {
                    throw new TimeoutException("Connection to server lost.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ThirdPartyService.");
                }

                _logger.LogInformation("Third-party service finished work.");
            }
        }
    }
    
    internal class DummyLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Console.WriteLine($"[DummyLogger] [{logLevel}] {formatter(state, exception)}");
    }
}