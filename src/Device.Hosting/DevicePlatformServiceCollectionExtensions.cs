using Device.Client;
using Device.Server.Execution;
using Device.Server.Registry;
using Device.Server.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Device.Hosting;

public static class DevicePlatformServiceCollectionExtensions
{
    /// <summary>
    /// Builds an in-process device runtime from the <c>Devices</c> configuration section
    /// and registers <see cref="IUdlServerClient"/> for station apps.
    /// </summary>
    public static IServiceCollection AddDevicePlatform(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DevicePlatformOptions>(configuration.GetSection(DevicePlatformOptions.SectionName));
        return AddDevicePlatformCore(services);
    }

    /// <summary>
    /// Same as config-based registration, but options are supplied in code (tests / custom hosts).
    /// </summary>
    public static IServiceCollection AddDevicePlatform(
        this IServiceCollection services,
        Action<DevicePlatformOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return AddDevicePlatformCore(services);
    }

    private static IServiceCollection AddDevicePlatformCore(IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DevicePlatformOptions>>().Value;
            var definitions = options.ToDefinitions();
            var resolver = new CompositeDriverResolver();
            var registry = new DeviceRegistry();
            foreach (var definition in definitions)
            {
                registry.Register(definition, resolver.Resolve(definition));
            }

            var executor = new DeviceCommandExecutor();
            return new InProcessDeviceRuntime(registry, executor, resolver);
        });

        services.AddSingleton<IUdlServerClient>(sp =>
            new InMemoryUdlServerClient(sp.GetRequiredService<InProcessDeviceRuntime>()));

        services.AddSingleton<IReadOnlyList<DeviceDefinition>>(sp =>
            sp.GetRequiredService<InProcessDeviceRuntime>().Registry.ListDefinitions());

        services.AddSingleton<IDeviceRegistrationSummary>(sp =>
            new DeviceRegistrationSummary(sp.GetRequiredService<IReadOnlyList<DeviceDefinition>>().Count));

        return services;
    }
}
