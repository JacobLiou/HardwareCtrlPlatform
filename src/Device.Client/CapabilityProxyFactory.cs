using Device.Client.Proxies;
using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Client;

/// <summary>
/// Creates capability proxies from registered device identity (by DeviceType).
/// </summary>
public static class CapabilityProxyFactory
{
    public const string OpticalPowerMeter = "OpticalPowerMeter";
    public const string LaserSource = "LaserSource";
    public const string OpticalSwitch = "OpticalSwitch";

    public static bool TryCreate(
        IUdlServerClient client,
        DeviceIdentity identity,
        out IDevice? device,
        out string? panelKind)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(identity);

        switch (identity.DeviceType)
        {
            case OpticalPowerMeter:
                device = new UdlPowerMeterProxy(client, identity);
                panelKind = OpticalPowerMeter;
                return true;
            case LaserSource:
                device = new UdlLaserSourceProxy(client, identity);
                panelKind = LaserSource;
                return true;
            case OpticalSwitch:
                device = new UdlOpticalSwitchProxy(client, identity);
                panelKind = OpticalSwitch;
                return true;
            default:
                device = null;
                panelKind = null;
                return false;
        }
    }

    public static IOpticalPowerMeter CreatePowerMeter(IUdlServerClient client, DeviceIdentity identity) =>
        new UdlPowerMeterProxy(client, identity);

    public static ILaserSource CreateLaser(IUdlServerClient client, DeviceIdentity identity) =>
        new UdlLaserSourceProxy(client, identity);

    public static IOpticalSwitch CreateOpticalSwitch(IUdlServerClient client, DeviceIdentity identity) =>
        new UdlOpticalSwitchProxy(client, identity);
}
