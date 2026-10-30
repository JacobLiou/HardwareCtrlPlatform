using System.Windows;
using Device.Contracts.Tags;
using Device.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Station.App.ViewModels;
using Station.App.Workflow;
using Station.Session;
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
                services.AddSingleton<IWorkflowInterlock, TagsReadyInterlock>();
                services.AddSingleton<IStationWorkflow, TemplateStationWorkflow>();
                services.AddDevicePlatform(context.Configuration);
                services.AddStationSession(context.Configuration);
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<DeviceDebugViewModel>();
                services.AddSingleton<MainShellViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        var lifecycle = _host.Services.GetRequiredService<IDevicePlatformLifecycle>();
        await lifecycle.ConnectAllAsync();

        WireSessionMeasurements(_host.Services);

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    /// <summary>Session measurements = Good-quality tag values at run completion (data plane snapshot).</summary>
    private static void WireSessionMeasurements(IServiceProvider services)
    {
        var recorder = services.GetRequiredService<SessionRecorder>();
        if (services.GetService<ITagStore>() is not { } tagStore)
        {
            return;
        }

        recorder.MeasurementsProvider = () => tagStore.Snapshot()
            .Where(t => t.Quality == TagQuality.Good)
            .ToDictionary(t => t.TagId, t => t.Value);
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
