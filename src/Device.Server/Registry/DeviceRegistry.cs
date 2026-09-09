using Device.Contracts.Common;
using System.Collections.Concurrent;

namespace Device.Server.Registry;

public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly ConcurrentDictionary<string, IDevice> _devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DeviceDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

    public void Register(DeviceDefinition definition, IDevice device)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(device);

        if (!string.Equals(definition.DeviceId, device.Identity.DeviceId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Definition DeviceId '{definition.DeviceId}' does not match device '{device.Identity.DeviceId}'.");
        }

        _definitions[definition.DeviceId] = definition;
        _devices[definition.DeviceId] = device;
    }

    public bool TryGet(string deviceId, out IDevice? device) =>
        _devices.TryGetValue(deviceId, out device);

    public bool TryGetDefinition(string deviceId, out DeviceDefinition? definition) =>
        _definitions.TryGetValue(deviceId, out definition);

    public IReadOnlyList<DeviceDefinition> ListDefinitions() =>
        _definitions.Values.OrderBy(item => item.DeviceId, StringComparer.OrdinalIgnoreCase).ToList();
}