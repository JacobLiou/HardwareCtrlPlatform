using Device.Client;
using Device.Server.Execution;
using Device.Server.Registry;
using Device.Server.Scheduling;

namespace Device.Drivers.Udl;

/// <summary>
/// Factory lives next to COM drivers but needs Client — referenced only by demos/UI (no cycle with Server).
/// </summary>
public static class UdlComPlatformFactory
{
    public static (InProcessDeviceRuntime Runtime, InMemoryUdlServerClient Client, UdlEngineSession Session) Create(
        params DeviceDefinition[] definitions)
    {
        var session = UdlEngineSession.OpenShared();
        var registry = new DeviceRegistry();
        var executor = new DeviceCommandExecutor();
        var resolver = new UdlComDriverResolver(() => session);
        foreach (var definition in definitions)
        {
            registry.Register(definition, resolver.Resolve(definition));
        }

        var runtime = new InProcessDeviceRuntime(registry, executor, resolver);
        var client = new InMemoryUdlServerClient(runtime);
        return (runtime, client, session);
    }
}
