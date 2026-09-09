using Device.Contracts.Common;
using Device.Server.Registry;
using Device.Server.Resolution;

namespace Device.Drivers.Udl;

public sealed class UdlComDriverResolver : IDeviceDriverResolver
{
    private readonly Func<UdlEngineSession> _sessionFactory;

    public UdlComDriverResolver(Func<UdlEngineSession>? sessionFactory = null)
    {
        _sessionFactory = sessionFactory ?? UdlEngineSession.OpenShared;
    }

    public bool CanResolve(DeviceDefinition definition) =>
        definition.Provider == DeviceProviderKind.Udl
        && definition.DriverName is
            UdlComPowerMeter.DriverName or
            UdlComLaserSource.DriverName or
            UdlComOpticalSwitch.DriverName;

    public IDevice Resolve(DeviceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!CanResolve(definition))
        {
            throw new NotSupportedException(
                $"UdlComDriverResolver cannot resolve Provider={definition.Provider}, Driver={definition.DriverName}.");
        }

        var identity = new DeviceIdentity(definition.DeviceId, definition.DeviceType, definition.DisplayName);
        var session = _sessionFactory();

        return definition.DriverName switch
        {
            UdlComPowerMeter.DriverName => new UdlComPowerMeter(identity, session, definition.ResourceId),
            UdlComLaserSource.DriverName => new UdlComLaserSource(identity, session, definition.ResourceId),
            UdlComOpticalSwitch.DriverName => new UdlComOpticalSwitch(identity, session, definition.ResourceId),
            _ => throw new NotSupportedException($"Unknown UDL COM driver '{definition.DriverName}'.")
        };
    }
}
