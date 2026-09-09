using Device.Contracts.Common;
using Device.Drivers.Udl;
using Device.Server.Registry;
using Device.Server.Resolution;
using Device.Simulators;

namespace Device.Drivers.Samples;

/// <summary>
/// Demo resolver: Simulator | Udl stub | Udl COM | Native.
/// </summary>
public sealed class SampleDriverResolver : IDeviceDriverResolver
{
    private readonly UdlComDriverResolver _udlCom = new();

    public IDevice Resolve(DeviceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var identity = new DeviceIdentity(definition.DeviceId, definition.DeviceType, definition.DisplayName);

        if (_udlCom.CanResolve(definition))
        {
            return _udlCom.Resolve(definition);
        }

        return definition.Provider switch
        {
            DeviceProviderKind.Simulator => new SimulatorDriverResolver().Resolve(definition),

            DeviceProviderKind.Udl when definition.DriverName == UdlStubPowerMeter.DriverName =>
                new UdlStubPowerMeter(identity),

            DeviceProviderKind.Native when definition.DriverName == CustomInlinePowerMeter.DriverName =>
                CreateConnectedCustom(identity),

            DeviceProviderKind.LegacyUdlCom or DeviceProviderKind.LegacyDeviceControl =>
                throw new NotSupportedException(
                    $"Provider '{definition.Provider}' is reserved for a future Legacy Adapter. Not implemented in demos."),

            _ => throw new NotSupportedException(
                $"No sample driver for Provider={definition.Provider}, Driver={definition.DriverName}.")
        };
    }

    private static CustomInlinePowerMeter CreateConnectedCustom(DeviceIdentity identity)
    {
        var meter = new CustomInlinePowerMeter(identity);
        meter.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        return meter;
    }

    public static void Register(
        IDeviceRegistry registry,
        IEnumerable<DeviceDefinition> definitions,
        SampleDriverResolver? resolver = null)
    {
        resolver ??= new SampleDriverResolver();
        foreach (var definition in definitions)
        {
            registry.Register(definition, resolver.Resolve(definition));
        }
    }
}
