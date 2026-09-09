using System.Windows;
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

    protected override void OnStartup(StartupEventArgs e)
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
                // TODO: register Device.Client / Simulator stack when the station needs hardware.
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
