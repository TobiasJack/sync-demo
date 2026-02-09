using Microsoft.AspNetCore.SignalR;
using SyncDemo.Api.Data;
using SyncDemo.Api.Hubs;
using SyncDemo.Api.Infrastructure.OracleAQ;
using SyncDemo.Api.Infrastructure.RabbitMQ;
using SyncDemo.Api.Infrastructure.SignalR;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Services;

public class OracleQueueService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OracleQueueService> _logger;
    private IOracleQueueListener? _queueListener;

    public OracleQueueService(
        IServiceProvider serviceProvider,
        ILogger<OracleQueueService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OracleQueueService starting - Event-Driven Architecture");

        using var scope = _serviceProvider.CreateScope();
        
        var connectionString = scope.ServiceProvider
            .GetRequiredService<IConfiguration>()
            .GetConnectionString("OracleConnection");
        
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var listenerLogger = loggerFactory.CreateLogger<OracleQueueListener>();

        _queueListener = new OracleQueueListener(connectionString!, listenerLogger);
        
        // Subscribe to messages
        _queueListener.MessageReceived += OnMessageReceived;

        try
        {
            await _queueListener.StartListeningAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OracleQueueService stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OracleQueueService failed");
        }
    }

    private async void OnMessageReceived(object? sender, OracleQueueMessage message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var connectionManager = scope.ServiceProvider.GetRequiredService<IConnectionManager>();
            var permissionRepo = scope.ServiceProvider.GetRequiredService<IDevicePermissionRepository>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<SyncHub>>();

            var updateMessage = new UpdateMessage
            {
                Id = Guid.NewGuid(),
                EntityType = message.TableName,
                Operation = message.Operation,
                Timestamp = message.ChangeTimestamp,
                DataJson = message.DataJson
            };

            _logger.LogInformation($"Processing change: {message.TableName} ({message.Operation}) - Record ID: {message.RecordId}");

            // Get authorized devices
            var authorizedDevices = await permissionRepo.GetAuthorizedDevicesForEntityAsync(
                message.TableName, 
                message.RecordId
            );

            _logger.LogInformation($"Broadcasting to {authorizedDevices.Count} authorized devices");

            foreach (var deviceId in authorizedDevices)
            {
                if (await connectionManager.IsDeviceOnlineAsync(deviceId))
                {
                    // Online: Send via SignalR
                    var connectionId = await connectionManager.GetConnectionIdAsync(deviceId);
                    if (connectionId != null)
                    {
                        await hubContext.Clients.Client(connectionId)
                            .SendAsync("ReceiveUpdate", updateMessage);
                        
                        _logger.LogDebug($"Sent update to online device {deviceId} via SignalR");
                    }
                }
                else
                {
                    // Offline: Queue in RabbitMQ
                    await messagePublisher.PublishUpdateAsync(deviceId, updateMessage);
                    
                    _logger.LogDebug($"Queued update for offline device {deviceId} in RabbitMQ");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Oracle AQ message");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_queueListener != null)
        {
            await _queueListener.StopListeningAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
