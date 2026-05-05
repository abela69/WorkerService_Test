using RabbitMQ.Client;
using Serilog;
using WorkerService_Test;
using WorkerService_Test.Logging;
using WorkerService_Test.Repository;
using WorkerService_Test.Services;
using WorkerService_Test.Worker;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("Before logging configure"); // ← დაამატე
LoggingConfiguration.Configure();
Console.WriteLine("After logging configure");  // ← დაამატე

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddSingleton<SegmentService>();
builder.Services.AddSingleton<StatisticsRepository>();
builder.Services.AddHostedService<StatisticsWorker>();

var host = builder.Build();

var segmentService = host.Services.GetRequiredService<SegmentService>();
var result = await segmentService.GetQuery(11111);
Console.WriteLine(result);

host.Run();