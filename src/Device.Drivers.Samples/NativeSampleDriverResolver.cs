using Device.Contracts.Common;
using Device.Server.Registry;
using Device.Server.Resolution;

namespace Device.Drivers.Samples;

/// <summary>
/// Resolves self-implemented (Native) sample drivers when UDL does not cover a capability.
/// </summary>
public sealed class NativeSampleDriverResolver : IDeviceDriverResolver
{
    public IDevice Resolve(DeviceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Provider != DeviceProviderKind.Native)
        {
            throw new NotSupportedException(
                $"Provider '{definition.Provider}' is not supported by NativeSampleDriverResolver.");
        }

        var identity = new DeviceIdentity(definition.DeviceId, definition.DeviceType, definition.DisplayName);

        return definition.DriverName switch
        {
            CustomInlinePowerMeter.DriverName => CreateConnectedCustom(identity),
            _ => throw new NotSupportedException(
                $"Unknown Native driver '{definition.DriverName}'. Add it to NativeSampleDriverResolver (or a station-specific resolver).")
        };
    }

    private static CustomInlinePowerMeter CreateConnectedCustom(DeviceIdentity identity)
    {
        var meter = new CustomInlinePowerMeter(identity);
        meter.ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        return meter;
    }
}
