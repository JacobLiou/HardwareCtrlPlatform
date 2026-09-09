using Device.Contracts.Common;
using Device.Server.Registry;
using Device.Server.Resolution;

namespace Device.Simulators;

public sealed class SimulatorDriverResolver : IDeviceDriverResolver
{
    public const string PowerMeterDriver = "SimulatedPowerMeter";
    public const string LaserDriver = "SimulatedLaserSource";
    public const string SwitchDriver = "SimulatedOpticalSwitch";

    public IDevice Resolve(DeviceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Provider != DeviceProviderKind.Simulator)
        {
            throw new NotSupportedException(
                $"Provider '{definition.Provider}' is not supported by SimulatorDriverResolver.");
        }

        var identity = new DeviceIdentity(definition.DeviceId, definition.DeviceType, definition.DisplayName);

        return definition.DriverName switch
        {
            PowerMeterDriver => new SimulatedPowerMeter(identity),
            LaserDriver => new SimulatedLaserSource(identity),
            SwitchDriver => new SimulatedOpticalSwitch(identity),
            _ => throw new NotSupportedException($"Unknown simulator driver '{definition.DriverName}'.")
        };
    }

    public static void RegisterFromDefinitions(
        IDeviceRegistry registry,
        IEnumerable<DeviceDefinition> definitions,
        SimulatorDriverResolver? resolver = null)
    {
        resolver ??= new SimulatorDriverResolver();
        foreach (var definition in definitions)
        {
            var device = resolver.Resolve(definition);
            registry.Register(definition, device);
        }
    }
}