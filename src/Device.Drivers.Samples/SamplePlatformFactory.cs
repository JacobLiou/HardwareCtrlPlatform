using Device.Client;
using Device.Server.Execution;
using Device.Server.Registry;
using Device.Server.Scheduling;

namespace Device.Drivers.Samples;

public static class SamplePlatformFactory
{
    public static (InProcessDeviceRuntime Runtime, InMemoryUdlServerClient Client) Create(
        params DeviceDefinition[] definitions)
    {
        var registry = new DeviceRegistry();
        var executor = new DeviceCommandExecutor();
        var resolver = new SampleDriverResolver();
        SampleDriverResolver.Register(registry, definitions, resolver);
        var runtime = new InProcessDeviceRuntime(registry, executor, resolver);
        var client = new InMemoryUdlServerClient(runtime);
        return (runtime, client);
    }
}