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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitializeRabbitMqAsync();

            if (_channel is null)
            {
                _logger.LogError("Channel ვერ შეიქმნა — სერვისი ჩერდება.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel!.BasicConsumeAsync(
                queue: "statistics_queue",
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("სერვისი გაეშვა. statistics_queue-ს ვუსმენთ.");

            await Task.Delay(Timeout.Infinite, stoppingToken)
                      .ContinueWith(_ => Task.CompletedTask);
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
            _logger.LogInformation("RabbitMQ Connected!");

            _channel = await _connection.CreateChannelAsync();
            _logger.LogInformation("Channel created!");

            await _channel.QueueDeclareAsync(
                queue: "statistics_queue",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            _logger.LogInformation("Queue declared!");

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

            _logger.LogInformation("Queue bound!");
        }

        private async Task OnMessageReceivedAsync(object model, BasicDeliverEventArgs ea)
        {
            var routingKey = ea.RoutingKey;
            var rawMessage = Encoding.UTF8.GetString(ea.Body.ToArray());

            var message = ExtractJson(rawMessage);
            if (message == null)
            {
                _logger.LogWarning("არასწორი JSON მოვიდა. RoutingKey: {RoutingKey}", routingKey);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            try
            {
                await HandleMessageAsync(routingKey, message);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "შეცდომა message-ის დამუშავებისას. RoutingKey: {RoutingKey}", routingKey);
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        }

        private string? ExtractJson(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
                return null;

            var jsonStart = rawMessage.IndexOf('{');
            if (jsonStart < 0)
                return null;

            return rawMessage.Substring(jsonStart);
        }

        private async Task HandleMessageAsync(string routingKey, string message)
        {
            _logger.LogInformation("Message მოვიდა! RoutingKey: {RoutingKey}", routingKey);

            var doc = JsonSerializer.Deserialize<DocumentMessage>(message);
            if (doc is null)
            {
                _logger.LogWarning("Deserialization ვერ მოხერხდა — გამოტოვება!");
                throw new InvalidOperationException("Deserialization returned null.");
            }

            _logger.LogInformation("DebitCustomerId: {DebitId}, CreditCustomerId: {CreditId}",
                doc.DebitCustomerId, doc.CreditCustomerId);

            if (doc.DebitCustomerId == null && doc.CreditCustomerId == null)
            {
                _logger.LogWarning("ორივე CustomerId null — გამოტოვება!");
              
            }

            var debitSegment = doc.DebitCustomerId != null
                ? await _segmentService.GetQuery(doc.DebitCustomerId.Value)
                : "N/A";

            var creditSegment = doc.CreditCustomerId != null
                ? await _segmentService.GetQuery(doc.CreditCustomerId.Value)
                : "N/A";

            _logger.LogInformation("Debit: {Debit}, Credit: {Credit}", debitSegment, creditSegment);

            if (debitSegment == "N/A" && creditSegment == "N/A")
            {
                _logger.LogWarning("ორივე სეგმენტი N/A — გამოტოვება!");
                throw new InvalidOperationException("Both segments are N/A.");
            }

            int count = routingKey == "b6.transaction.create" ? 1 : -1;

            if (count == -1)
            {
                var exists = await _repository.ExistsAsync(debitSegment, creditSegment, doc.ChannelId, doc.Date);
                if (!exists)
                {
                    _logger.LogWarning("Row არ არსებობს — გამოტოვება!");
                    throw new InvalidOperationException("Row not found for delete.");
                }
            }

            await _repository.UpdateStatisticsAsync(
                debitSegment,
                creditSegment,
                doc.ChannelId,
                doc.Date,
                count
            );

            _logger.LogInformation("ჩაიწერა! Count: {Count}", count);
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