using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Station.Session;

public static class SessionServiceCollectionExtensions
{
    /// <summary>
    /// Registers JSON session persistence from the <c>Session</c> config section
    /// (StationId / OperatorId / RecipeName / RecipeVersion / RootDirectory) and a
    /// <see cref="SessionRecorder"/> bound to the single registered <see cref="IStationWorkflow"/>.
    /// </summary>
    public static IServiceCollection AddStationSession(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SessionOptions>().Bind(configuration.GetSection("Session"));
        services.TryAddSingleton<ISessionStore, JsonSessionStore>();
        services.AddSingleton<SessionRecorder>();
        return services;
    }
}
