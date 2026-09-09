using Device.Contracts.Common;

namespace Device.Server.Scheduling;

public interface IDeviceCommandExecutor
{
    Task<DeviceResult<T>> ExecuteAsync<T>(
        string resourceId,
        Func<CancellationToken, Task<DeviceResult<T>>> action,
        CancellationToken cancellationToken);

    Task<DeviceResult> ExecuteAsync(
        string resourceId,
        Func<CancellationToken, Task<DeviceResult>> action,
        CancellationToken cancellationToken);
}