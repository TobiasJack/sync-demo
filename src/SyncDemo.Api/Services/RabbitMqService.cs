using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Services;

public interface IMessageQueueService
{
    void PublishMessage(SyncMessage message);
    void StartConsuming(Action<SyncMessage> messageHandler);
    void StopConsuming();
    bool IsConnected { get; }
}

public class RabbitMqService : IMessageQueueService, IDisposable
{
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "sync-queue";
    private EventingBasicConsumer? _consumer;
    private readonly ILogger<RabbitMqService>? _logger;

    public bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;

    public RabbitMqService(string hostName, int port, string userName, string password, ILogger<RabbitMqService>? logger = null)
    {
        _logger = logger;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = port,
                UserName = userName,
                Password = password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            _logger?.LogInformation("Successfully connected to RabbitMQ at {Host}:{Port}", hostName, port);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to connect to RabbitMQ at {Host}:{Port}. Message queue functionality will be disabled.", hostName, port);
        }
    }

    public void PublishMessage(SyncMessage message)
    {
        if (!IsConnected)
        {
            _logger?.LogWarning("RabbitMQ is not connected. Message will not be published.");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel!.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: QueueName,
                basicProperties: properties,
                body: body);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error publishing message to RabbitMQ");
        }
    }

    public void StartConsuming(Action<SyncMessage> messageHandler)
    {
        if (!IsConnected)
        {
            _logger?.LogWarning("RabbitMQ is not connected. Cannot start consuming.");
            return;
        }

        try
        {
            _consumer = new EventingBasicConsumer(_channel!);
            _consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<SyncMessage>(json);
                    
                    if (message != null)
                    {
                        messageHandler(message);
                    }

                    _channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing message from RabbitMQ");
                }
            };

            _channel!.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: _consumer);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting RabbitMQ consumer");
        }
    }

    public void StopConsuming()
    {
        if (_consumer != null && _channel?.IsOpen == true)
        {
            try
            {
                _channel.BasicCancel(_consumer.ConsumerTags.FirstOrDefault() ?? "");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping RabbitMQ consumer");
            }
        }
    }

    public void Dispose()
    {
        StopConsuming();
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
