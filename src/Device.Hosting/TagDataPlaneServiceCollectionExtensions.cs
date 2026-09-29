using Device.Client;
using Device.Contracts.Tags;
using Device.Server.Registry;
using Device.Tags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Device.Hosting;

public static class TagDataPlaneServiceCollectionExtensions
{
    public static IServiceCollection AddTagDataPlane(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TagPlatformOptions>(configuration.GetSection(TagPlatformOptions.SectionName));
        return AddTagDataPlaneCore(services);
    }

    public static IServiceCollection AddTagDataPlane(
        this IServiceCollection services,
        Action<TagPlatformOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return AddTagDataPlaneCore(services);
    }

    private static IServiceCollection AddTagDataPlaneCore(IServiceCollection services)
    {
        // Avoid double-registering hosted service when AddDevicePlatform(Action) + AddTagDataPlane both run.
        if (services.Any(d => d.ServiceType == typeof(ITagStore)))
        {
            return services;
        }

        services.AddSingleton<ITagStore, InMemoryTagStore>();

        services.AddSingleton<ITagRegistry>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TagPlatformOptions>>().Value;
            var devices = sp.GetRequiredService<IReadOnlyList<DeviceDefinition>>();
            var typeLookup = devices.ToDictionary(
                d => d.DeviceId,
                d => d.DeviceType,
                StringComparer.OrdinalIgnoreCase);

            var definitions = options.ToDefinitions(
                deviceId => typeLookup.TryGetValue(deviceId, out var type) ? type : null);

            return new TagRegistry(definitions);
        });

        services.AddSingleton<ITagWriter, TagWriter>();
        services.AddHostedService<TagPollerHostedService>();
        return services;
    }
}
