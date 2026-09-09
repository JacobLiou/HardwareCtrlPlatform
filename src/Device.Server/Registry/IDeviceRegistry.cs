using Device.Contracts.Common;

namespace Device.Server.Registry;

public interface IDeviceRegistry
{
    void Register(DeviceDefinition definition, IDevice device);

    bool TryGet(string deviceId, out IDevice? device);

    bool TryGetDefinition(string deviceId, out DeviceDefinition? definition);

    IReadOnlyList<DeviceDefinition> ListDefinitions();
}