using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Infrastructure.RabbitMQ;

public interface IMessagePublisher
{
    Task PublishUpdateAsync(string deviceId, UpdateMessage message);
}
