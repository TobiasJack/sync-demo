namespace SyncDemo.Api.Infrastructure.OracleAQ;

public interface IOracleQueueListener
{
    event EventHandler<OracleQueueMessage>? MessageReceived;
    Task StartListeningAsync(CancellationToken cancellationToken);
    Task StopListeningAsync();
}
