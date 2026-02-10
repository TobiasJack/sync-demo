using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Microsoft.Extensions.Logging;
using System.Data;

namespace SyncDemo.Api.Infrastructure.OracleAQ;

/// <summary>
/// Modernized Oracle AQ Listener with native C# API
/// </summary>
public class OracleQueueListener : IOracleQueueListener, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<OracleQueueListener> _logger;
    private OracleConnection? _connection;
    private OracleAQQueue? _queue;
    private bool _isListening;
    private CancellationTokenSource? _cancellationTokenSource;

    public event EventHandler<OracleQueueMessage>? MessageReceived;

    public OracleQueueListener(string connectionString, ILogger<OracleQueueListener> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        _isListening = true;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Initialize Connection
            await InitializeConnectionAsync(cancellationToken);

            _logger.LogInformation("Oracle AQ Listener started (Native C# API)");

            // Main loop
            while (_isListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await DequeueMessageAsync(cancellationToken);

                    if (message != null)
                    {
                        _logger.LogDebug(
                            "Received AQ message: {TableName} - {Operation} (ID: {RecordId})",
                            message.TableName,
                            message.Operation,
                            message.RecordId
                        );

                        // Trigger Event
                        MessageReceived?.Invoke(this, message);
                    }
                    else
                    {
                        // No message, wait briefly
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OracleException ex) when (ex.Number == 25228)
                {
                    // ORA-25228: timeout or end-of-fetch during message dequeue
                    // Normal - keine Message in Queue
                    await Task.Delay(500, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in Oracle AQ Listener - retrying in 5 seconds");
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Oracle AQ Listener stopped (cancelled)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Oracle AQ Listener");
            throw;
        }
    }

    private async Task InitializeConnectionAsync(CancellationToken cancellationToken)
    {
        _connection = new OracleConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken);

        // Initialize Queue with native API
        _queue = new OracleAQQueue("SYNC_CHANGES_QUEUE", _connection);
        _queue.MessageType = OracleAQMessageType.Udt;
        _queue.UdtTypeName = "SYNCUSER.SYNC_CHANGE_PAYLOAD";

        _logger.LogInformation("Oracle AQ Queue initialized: {QueueName}", _queue.Name);
    }

    private async Task<OracleQueueMessage?> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        if (_queue == null || _connection?.State != ConnectionState.Open)
        {
            await InitializeConnectionAsync(cancellationToken);
        }

        // Configure Dequeue Options
        var dequeueOptions = new OracleAQDequeueOptions
        {
            Wait = 1, // Wait 1 second
            DequeueMode = OracleAQDequeueMode.Remove,
            ConsumerName = "SYNC_SERVICE" // Subscriber Name
        };

        try
        {
            // Native Dequeue with C# API
            var aqMessage = _queue!.Dequeue(dequeueOptions) as OracleAQMessage;

            if (aqMessage?.Payload == null)
            {
                return null;
            }

            // Extract Payload (Oracle UDT)
            var udtPayload = aqMessage.Payload as OracleSyncChangePayload;

            if (udtPayload == null)
            {
                _logger.LogWarning("Received message with null or invalid payload");
                return null;
            }

            // Convert to OracleQueueMessage
            return udtPayload.ToQueueMessage();
        }
        catch (OracleException ex) when (ex.Number == 25228)
        {
            // ORA-25228: timeout or end-of-fetch
            // No message available - not an error
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dequeuing message from Oracle AQ");
            throw;
        }
    }

    public Task StopListeningAsync()
    {
        _isListening = false;
        _cancellationTokenSource?.Cancel();

        _queue?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        _logger.LogInformation("Oracle AQ Listener stopped");

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopListeningAsync().GetAwaiter().GetResult();
        _cancellationTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}
