using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Infrastructure.RabbitMQ;

public class MessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<MessagePublisher> _logger;
    private bool _disposed;

    public MessagePublisher(string hostName, int port, string userName, string password, ILogger<MessagePublisher> logger)
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
            
            _logger.LogInformation("Successfully connected to RabbitMQ at {Host}:{Port}", hostName, port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ at {Host}:{Port}. Message publishing will be disabled.", hostName, port);
        }
    }

    public Task PublishUpdateAsync(string deviceId, UpdateMessage message)
    {
        if (_connection?.IsOpen != true || _channel?.IsOpen != true)
        {
            _logger.LogWarning("RabbitMQ is not connected. Message will not be published for device {DeviceId}", deviceId);
            return Task.CompletedTask;
        }

        try
        {
            var queueName = $"device-{deviceId}";
            
            // Declare queue for this device
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published message to queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to RabbitMQ for device {DeviceId}", deviceId);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing MessagePublisher");
        }

        _disposed = true;
    }
}
