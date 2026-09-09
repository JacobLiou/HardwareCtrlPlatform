using Device.Contracts.Common;

namespace Device.Server.Scheduling;

/// <summary>
/// Executes device operations with a per-resource serial lock.
/// Same <paramref name="resourceId"/> is serialized; different ids may run in parallel.
/// Effective resource key is typically <c>ResourceId ?? DeviceId</c> (see InProcessDeviceRuntime).
/// </summary>
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
