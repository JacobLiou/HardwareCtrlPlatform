using Device.Contracts.Common;
using Device.Server.Registry;
using Device.Server.Resolution;
using Device.Server.Scheduling;

namespace Device.Server.Execution;

/// <summary>
/// In-process device runtime used by InMemoryUdlServerClient.
/// </summary>
public sealed class InProcessDeviceRuntime
{
    public InProcessDeviceRuntime(
        IDeviceRegistry registry,
        IDeviceCommandExecutor executor,
        IDeviceDriverResolver? resolver = null)
    {
        Registry = registry;
        Executor = executor;
        Resolver = resolver;
    }

    public IDeviceRegistry Registry { get; }

    public IDeviceCommandExecutor Executor { get; }

    public IDeviceDriverResolver? Resolver { get; }

    public string ResolveResourceId(string deviceId)
    {
        if (Registry.TryGetDefinition(deviceId, out var definition) && definition is not null)
        {
            return string.IsNullOrWhiteSpace(definition.ResourceId)
                ? definition.DeviceId
                : definition.ResourceId!;
        }

        return deviceId;
    }

    public async Task<DeviceResult<T>> InvokeAsync<T>(
        string deviceId,
        Func<IDevice, CancellationToken, Task<DeviceResult<T>>> action,
        CancellationToken cancellationToken)
    {
        if (!Registry.TryGet(deviceId, out var device) || device is null)
        {
            return DeviceResult<T>.Fail(DeviceErrorCode.NotFound, $"Device '{deviceId}' was not registered.", deviceId);
        }

        var resourceId = ResolveResourceId(deviceId);
        return await Executor.ExecuteAsync(
            resourceId,
            ct => action(device, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<DeviceResult> InvokeAsync(
        string deviceId,
        Func<IDevice, CancellationToken, Task<DeviceResult>> action,
        CancellationToken cancellationToken)
    {
        if (!Registry.TryGet(deviceId, out var device) || device is null)
        {
            return DeviceResult.Fail(DeviceErrorCode.NotFound, $"Device '{deviceId}' was not registered.", deviceId);
        }

        var resourceId = ResolveResourceId(deviceId);
        return await Executor.ExecuteAsync(
            resourceId,
            ct => action(device, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public TCapability RequireCapability<TCapability>(IDevice device)
        where TCapability : class, IDevice
    {
        if (device is TCapability capability)
        {
            return capability;
        }

        throw new InvalidOperationException(
            $"Device '{device.Identity.DeviceId}' does not implement {typeof(TCapability).Name}.");
    }
}