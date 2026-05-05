using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using WorkerService_Test.Models;
using WorkerService_Test.Repository;
using WorkerService_Test.Services;

namespace WorkerService_Test.Worker
{
    public class StatisticsWorker : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<StatisticsWorker> _logger;
        private readonly SegmentService _segmentService;
        private readonly StatisticsRepository _repository;

        private IConnection? _connection;
        private IChannel? _channel;

        public StatisticsWorker(IConfiguration config, ILogger<StatisticsWorker> logger, SegmentService segmentService, StatisticsRepository repository)
        {
            _config = config;
            _logger = logger;
            _segmentService = segmentService;
            _repository = repository;
        }


        private async Task InitializeRabbitMqAsync()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _config["RabbitMQ:Host"],
                UserName = _config["RabbitMQ:Username"],
                Password = _config["RabbitMQ:Password"],
                VirtualHost = _config["RabbitMQ:VirtualHost"]
            };

            _connection = await factory.CreateConnectionAsync();
            Console.WriteLine("Connected!");

            _channel = await _connection.CreateChannelAsync();
            Console.WriteLine("Channel created!");

            await _channel.QueueDeclareAsync(
                queue: "statistics_queue",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            Console.WriteLine("✅ Queue declared!");

            await _channel.QueueBindAsync(
                queue: "statistics_queue",
                exchange: _config["RabbitMQ:Exchange"],
                routingKey: "b6.transaction.create"
            );

            await _channel.QueueBindAsync(
                queue: "statistics_queue",
                exchange: _config["RabbitMQ:Exchange"],
                routingKey: "b6.transaction.delete"
            );
            Console.WriteLine("Queue bound!");
        }

        private async Task HandleMessageAsync(string routingKey, string message)
        {

            Console.WriteLine($"Message მოვიდა! RoutingKey: {routingKey}");
            var doc = JsonSerializer.Deserialize<DocumentMessage>(message);

            Console.WriteLine($"DebitCustomerId:  {doc.DebitCustomerId}");
            Console.WriteLine($"CreditCustomerId: {doc.CreditCustomerId}");

            if (doc.DebitCustomerId == null && doc.CreditCustomerId == null) return;

            var debitSegment = doc.DebitCustomerId != null
                ? await _segmentService.GetQuery(doc.DebitCustomerId.Value)
                : "N/A";

            var creditSegment = doc.CreditCustomerId != null
                ? await _segmentService.GetQuery(doc.CreditCustomerId.Value)
                : "N/A";

            Console.WriteLine($"Debit Segment:  {debitSegment}");
            Console.WriteLine($"Credit Segment: {creditSegment}");

            if (debitSegment == "N/A" && creditSegment == "N/A") return;

            int count = routingKey == "b6.transaction.create" ? 1 : -1;

            if (count == -1)
            {
                var exists = await _repository.ExistsAsync(debitSegment, creditSegment, doc.ChannelId, doc.Date);
                if (!exists)
                {
                    Console.WriteLine("row არ არსებობს — გამოტოვება!");
                    return;
                }
            }
            await _repository.UpdateStatisticsAsync(
                debitSegment,
                creditSegment,
                doc.ChannelId,
                doc.Date,
                count
            );

            Console.WriteLine($"ჩაიწერა! Count: {count}");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitializeRabbitMqAsync();
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                Console.WriteLine("📨 Consumer-მა დაიჭირა message!");
                var routingKey = ea.RoutingKey;
                var rawMessage = Encoding.UTF8.GetString(ea.Body.ToArray()); 

                var jsonStart = rawMessage.IndexOf('{');
                var message = jsonStart >= 0 ? rawMessage.Substring(jsonStart) : rawMessage;

                Console.WriteLine($"JSON: {message}");
                try
                {
                    await HandleMessageAsync(routingKey, message);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message with routing key: {RoutingKey}", routingKey);
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await _channel!.BasicConsumeAsync(
                queue: "statistics_queue",
                autoAck: false,
                consumer: consumer
            );

            var tcs = new TaskCompletionSource();
            stoppingToken.Register(() => tcs.SetResult());
            await tcs.Task;
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StatisticsWorker stopping...");

            if (_channel is not null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
            await base.StopAsync(cancellationToken);

        }

    }
}
