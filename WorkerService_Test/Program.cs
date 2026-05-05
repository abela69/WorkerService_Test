using RabbitMQ.Client;
using WorkerService_Test;
using WorkerService_Test.Repository;
using WorkerService_Test.Services;
using WorkerService_Test.Worker;




//var factory = new ConnectionFactory()
//{
//    HostName = "test-rabbit-srv.bsb.ge",
//    UserName = "test",
//    Password = "Qwer@1234",
//    VirtualHost = "B6dev"
//};

//Console.Write("connect");
//var connection = await factory.CreateConnectionAsync();
//Console.WriteLine("Connected to RabbitMQ! ✅");

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<SegmentService>();
builder.Services.AddSingleton<StatisticsRepository>();
builder.Services.AddHostedService<StatisticsWorker>();

var host = builder.Build();

var segmentService = host.Services.GetRequiredService<SegmentService>();
var result = await segmentService.GetQuery(11111);
Console.WriteLine(result);

host.Run();