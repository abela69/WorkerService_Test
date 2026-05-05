using Serilog;
using WorkerService_Test.Logging;
using WorkerService_Test.Repository;
using WorkerService_Test.Services;
using WorkerService_Test.Worker;

Console.OutputEncoding = System.Text.Encoding.UTF8;
LoggingConfiguration.Configure();

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddSingleton<SegmentService>();
builder.Services.AddSingleton<StatisticsRepository>();
builder.Services.AddHostedService<StatisticsWorker>();

var host = builder.Build();
host.Run();