using Device.Client;
using Device.Server.Execution;
using Device.Server.Registry;
using Device.Server.Scheduling;

namespace Device.Simulators;

/// <summary>Bootstrap an in-memory Simulator stack for demos and tests.</summary>
public static class SimulatorPlatformFactory
{
    public static (InProcessDeviceRuntime Runtime, InMemoryUdlServerClient Client) Create(
        params DeviceDefinition[] definitions)
    {
        var registry = new DeviceRegistry();
        var executor = new DeviceCommandExecutor();
        var resolver = new SimulatorDriverResolver();
        SimulatorDriverResolver.RegisterFromDefinitions(registry, definitions, resolver);
        var runtime = new InProcessDeviceRuntime(registry, executor, resolver);
        var client = new InMemoryUdlServerClient(runtime);
        return (runtime, client);
    }
}