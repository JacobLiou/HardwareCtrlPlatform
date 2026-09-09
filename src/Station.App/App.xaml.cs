using System.Windows;
using Device.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Station.App.ViewModels;
using Station.Workflow;

namespace Station.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IStationWorkflow, EmptyStationWorkflow>();
                services.AddDevicePlatform(context.Configuration);
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<DeviceDebugViewModel>();
                services.AddSingleton<MainShellViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        var lifecycle = _host.Services.GetRequiredService<IDevicePlatformLifecycle>();
        await lifecycle.ConnectAllAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            var lifecycle = _host.Services.GetService<IDevicePlatformLifecycle>();
            if (lifecycle is not null)
            {
                await lifecycle.DisconnectAllAsync(forceCloseUdlSession: true);
            }

            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
