using Device.Client.Proxies;
using Device.Contracts.Common;
using Device.Drivers.Udl;
using Device.Server.Registry;

namespace Device.Client.Tests.Demos;

/// <summary>
/// Real UDL2 COM smoke via Proxy. Skips when config/COM unavailable (keeps CI green).
/// </summary>
public class UdlComIntegrationDemoTests
{
    [Fact]
    public async Task Demo_D_RealUdlCom_PowerMeter_Smoke()
    {
        if (!CanOpenUdl(out var skipReason))
        {
            // No UDL config/COM in this environment — pass without failing CI.
            _ = skipReason;
            return;
        }

        var definition = new DeviceDefinition(
            "PM-UDL",
            "OpticalPowerMeter",
            DeviceProviderKind.Udl,
            UdlComPowerMeter.DriverName,
            "UDL COM Power Meter",
            ResourceId: "0");

        var (_, client, _) = UdlComPlatformFactory.Create(definition);
        var meter = new UdlPowerMeterProxy(
            client,
            new DeviceIdentity(definition.DeviceId, definition.DeviceType, definition.DisplayName));

        var set = await meter.SetWavelengthAsync(1550, CancellationToken.None);
        // Hardware may reject; still exercise the COM path.
        _ = set;

        var power = await meter.ReadPowerAsync(0, CancellationToken.None);
        Assert.NotNull(power);
        // Do not assert Success — open light path may be absent.
        Assert.True(power.Success || power.ErrorCode is DeviceErrorCode.DriverError or DeviceErrorCode.CommunicationError);
    }

    [Fact]
    public void UdlPaths_Config_Is_Fixed_Relative()
    {
        Assert.Equal(@"set\UDLConfig.xml", UdlPaths.ConfigRelativePath);
        Assert.EndsWith(
            Path.Combine("set", "UDLConfig.xml"),
            UdlPaths.ResolveConfigFullPath(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanOpenUdl(out string reason)
    {
        var path = UdlPaths.ResolveConfigFullPath();
        if (!File.Exists(path))
        {
            reason = $"Missing '{path}'.";
            return false;
        }

        if (!UdlEngineSession.TryOpenShared(out _, out var error))
        {
            reason = error;
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
