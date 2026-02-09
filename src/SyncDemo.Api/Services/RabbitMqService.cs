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
}

public class RabbitMqService : IMessageQueueService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string QueueName = "sync-queue";
    private EventingBasicConsumer? _consumer;

    public RabbitMqService(string hostName, int port, string userName, string password)
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public void PublishMessage(SyncMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "",
            routingKey: QueueName,
            basicProperties: properties,
            body: body);
    }

    public void StartConsuming(Action<SyncMessage> messageHandler)
    {
        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<SyncMessage>(json);
            
            if (message != null)
            {
                messageHandler(message);
            }

            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: _consumer);
    }

    public void StopConsuming()
    {
        if (_consumer != null)
        {
            _channel.BasicCancel(_consumer.ConsumerTags.FirstOrDefault() ?? "");
        }
    }

    public void Dispose()
    {
        StopConsuming();
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
