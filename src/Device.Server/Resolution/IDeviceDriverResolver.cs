using Device.Contracts.Common;

namespace Device.Server.Resolution;

public interface IDeviceDriverResolver
{
    IDevice Resolve(Device.Server.Registry.DeviceDefinition definition);
}