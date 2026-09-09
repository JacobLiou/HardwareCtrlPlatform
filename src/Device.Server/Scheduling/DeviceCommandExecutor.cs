using Device.Contracts.Common;
using System.Collections.Concurrent;

namespace Device.Server.Scheduling;

/// <summary>
/// Default scheduler: one SemaphoreSlim per resource id (serial), independent across ids (parallel).
/// </summary>
public sealed class DeviceCommandExecutor : IDeviceCommandExecutor
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    public async Task<DeviceResult<T>> ExecuteAsync<T>(
        string resourceId,
        Func<CancellationToken, Task<DeviceResult<T>>> action,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(action);

        var gate = _locks.GetOrAdd(resourceId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return DeviceResult<T>.Fail(DeviceErrorCode.Cancelled, "Device operation was cancelled.", resourceId);
        }
        catch (Exception ex)
        {
            return DeviceResult<T>.Fail(DeviceErrorCode.UnexpectedError, ex.Message, resourceId);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DeviceResult> ExecuteAsync(
        string resourceId,
        Func<CancellationToken, Task<DeviceResult>> action,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(action);

        var gate = _locks.GetOrAdd(resourceId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return DeviceResult.Fail(DeviceErrorCode.Cancelled, "Device operation was cancelled.", resourceId);
        }
        catch (Exception ex)
        {
            return DeviceResult.Fail(DeviceErrorCode.UnexpectedError, ex.Message, resourceId);
        }
        finally
        {
            gate.Release();
        }
    }
}