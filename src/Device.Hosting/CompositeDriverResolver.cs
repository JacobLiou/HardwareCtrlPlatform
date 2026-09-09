using Device.Contracts.Common;
using Device.Drivers.Samples;
using Device.Drivers.Udl;
using Device.Server.Registry;
using Device.Server.Resolution;
using Device.Simulators;

namespace Device.Hosting;

/// <summary>
/// Routes definitions to Simulator, UDL COM, UDL stub, or Native sample drivers.
/// Native is first-class for capabilities not covered by company UDL.
/// </summary>
public sealed class CompositeDriverResolver : IDeviceDriverResolver
{
    private readonly SimulatorDriverResolver _simulator = new();
    private readonly UdlComDriverResolver _udlCom;
    private readonly NativeSampleDriverResolver _native = new();

    public CompositeDriverResolver(Func<UdlEngineSession>? udlSessionFactory = null)
    {
        _udlCom = new UdlComDriverResolver(udlSessionFactory);
    }

    public IDevice Resolve(DeviceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return definition.Provider switch
        {
            DeviceProviderKind.Simulator => _simulator.Resolve(definition),

            DeviceProviderKind.Udl when _udlCom.CanResolve(definition) => _udlCom.Resolve(definition),

            DeviceProviderKind.Udl when definition.DriverName == UdlStubPowerMeter.DriverName =>
                CreateStubPowerMeter(definition),

            DeviceProviderKind.Native => _native.Resolve(definition),

            DeviceProviderKind.LegacyUdlCom or DeviceProviderKind.LegacyDeviceControl =>
                throw new NotSupportedException(
                    $"Provider '{definition.Provider}' is reserved for a legacy adapter and is not implemented."),

            _ => throw new NotSupportedException(
                $"No driver for Provider={definition.Provider}, Driver={definition.DriverName}, DeviceId={definition.DeviceId}.")
        };
    }

    private static UdlStubPowerMeter CreateStubPowerMeter(DeviceDefinition definition)
    {
        var identity = new DeviceIdentity(definition.DeviceId, definition.DeviceType, definition.DisplayName);
        return new UdlStubPowerMeter(identity);
    }
}
