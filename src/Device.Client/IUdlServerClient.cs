using Device.Client.Commands;
using Device.Contracts.Common;

namespace Device.Client;

public interface IUdlServerClient
{
    Task<DeviceResult> SendAsync(DeviceCommand command, CancellationToken cancellationToken);

    Task<DeviceResult<T>> SendAsync<T>(DeviceCommand command, CancellationToken cancellationToken);
}