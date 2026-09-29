using Device.Client;
using Device.Contracts.Common;
using Device.Contracts.Tags;
using Device.Server.Audit;
using Device.Server.Execution;
using Device.Server.Registry;
using Device.Server.Scheduling;
using Device.Simulators;
using Device.Tags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Device.Hosting;

public static class DevicePlatformServiceCollectionExtensions
{
    /// <summary>
    /// Builds an in-process device runtime from the <c>Devices</c> configuration section
    /// and registers <see cref="IUdlServerClient"/> for station apps.
    /// Also registers the Tag data plane from the <c>Tags</c> section.
    /// </summary>
    public static IServiceCollection AddDevicePlatform(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DevicePlatformOptions>(configuration.GetSection(DevicePlatformOptions.SectionName));
        AddDevicePlatformCore(services);
        services.AddTagDataPlane(configuration);
        return services;
    }

    /// <summary>
    /// Same as config-based registration, but options are supplied in code (tests / custom hosts).
    /// Does not register Tags; call <c>AddTagDataPlane</c> when needed.
    /// </summary>
    public static IServiceCollection AddDevicePlatform(
        this IServiceCollection services,
        Action<DevicePlatformOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        AddDevicePlatformCore(services);
        return services;
    }

    private static IServiceCollection AddDevicePlatformCore(IServiceCollection services)
    {
        services.AddSingleton<IDeviceCommandAuditor, InMemoryDeviceCommandAuditor>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DevicePlatformOptions>>().Value;
            var injector = new SimulatorFaultInjector();
            injector.Configure(options.FaultInjection ?? new FaultInjectionOptions());
            return injector;
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DevicePlatformOptions>>().Value;
            var definitions = options.ToDefinitions();
            var injector = sp.GetRequiredService<SimulatorFaultInjector>();
            var resolver = new CompositeDriverResolver(faultInjector: injector);
            var registry = new DeviceRegistry();
            foreach (var definition in definitions)
            {
                registry.Register(definition, resolver.Resolve(definition));
            }

            var executor = new DeviceCommandExecutor();
            return new InProcessDeviceRuntime(registry, executor, resolver);
        });

        services.AddSingleton<IUdlServerClient>(sp =>
            new InMemoryUdlServerClient(
                sp.GetRequiredService<InProcessDeviceRuntime>(),
                sp.GetRequiredService<IDeviceCommandAuditor>()));

        services.AddSingleton<IDevicePlatformLifecycle, DevicePlatformLifecycle>();

        services.AddSingleton<IReadOnlyList<DeviceDefinition>>(sp =>
            sp.GetRequiredService<InProcessDeviceRuntime>().Registry.ListDefinitions());

        services.AddSingleton<IDeviceRegistrationSummary>(sp =>
            new DeviceRegistrationSummary(sp.GetRequiredService<IReadOnlyList<DeviceDefinition>>().Count));

        return services;
    }
}
