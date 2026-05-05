using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerService_Test.Logging
{
    public static class LoggingConfiguration
    {
        public static void Configure()
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logging", "logs", "log-.txt");
            Console.WriteLine($"Log path: {logPath}");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) 
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.Console()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 50
                )
                .CreateLogger();
        }
    }
}
