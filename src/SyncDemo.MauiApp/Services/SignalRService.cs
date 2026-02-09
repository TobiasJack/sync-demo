using Microsoft.AspNetCore.SignalR.Client;
using SyncDemo.Shared.Models;

namespace SyncDemo.MauiApp.Services;

public interface ISignalRService
{
    Task StartAsync();
    Task StopAsync();
    Task SendSyncUpdateAsync(SyncMessage message);
    event Action<SyncMessage>? OnSyncUpdateReceived;
    bool IsConnected { get; }
}

public class SignalRService : ISignalRService
{
    private HubConnection? _hubConnection;
    private readonly string _hubUrl = "http://localhost:5000/synchub";

    public event Action<SyncMessage>? OnSyncUpdateReceived;
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task StartAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<SyncMessage>("ReceiveSyncUpdate", (message) =>
        {
            OnSyncUpdateReceived?.Invoke(message);
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SignalR connection error: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task SendSyncUpdateAsync(SyncMessage message)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("SendSyncUpdate", message);
        }
    }
}
