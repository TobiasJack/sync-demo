using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyncDemo.WpfApp.Services;
using SyncDemo.WpfApp.ViewModels;
using SyncDemo.WpfApp.Views;
using System.Windows;

namespace SyncDemo.WpfApp;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // HttpClient
                services.AddHttpClient();

                // Services
                services.AddSingleton<IRealmService, RealmService>();
                services.AddSingleton<ISyncService, SyncService>();

                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<SyncItemsViewModel>();

                // Views
                services.AddTransient<MainWindow>();
                services.AddTransient<SyncItemsView>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
