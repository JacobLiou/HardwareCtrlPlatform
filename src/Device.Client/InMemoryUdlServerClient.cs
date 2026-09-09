using Device.Client.Commands;
using Device.Contracts.Capabilities;
using Device.Contracts.Common;
using Device.Server.Execution;
using System.Diagnostics;

namespace Device.Client;

/// <summary>
/// Process-in-memory stand-in for UDLServer: routes commands through registry + executor.
/// </summary>
public sealed class InMemoryUdlServerClient(
    InProcessDeviceRuntime runtime,
    IDeviceCommandAuditor? auditor = null) : IUdlServerClient
{
    public Task<DeviceResult> SendAsync(DeviceCommand command, CancellationToken cancellationToken) =>
        DispatchAsync(command, cancellationToken);

    public async Task<DeviceResult<T>> SendAsync<T>(DeviceCommand command, CancellationToken cancellationToken)
    {
        var result = await DispatchAsync(command, cancellationToken).ConfigureAwait(false);
        if (result is DeviceResult<T> typed)
        {
            return typed;
        }

        if (!result.Success)
        {
            return DeviceResult<T>.Fail(
                result.ErrorCode,
                result.Message ?? "Command failed.",
                result.DeviceId,
                result.RequestId);
        }

        return DeviceResult<T>.Fail(
            DeviceErrorCode.UnexpectedError,
            $"Command '{command.Capability}.{command.Operation}' did not return {typeof(T).Name}.",
            command.DeviceId,
            command.RequestId);
    }

    private async Task<DeviceResult> DispatchAsync(DeviceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sw = Stopwatch.StartNew();
        DeviceResult result;
        try
        {
            result = await DispatchCoreAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
        }

        if (auditor is not null)
        {
            auditor.Record(new DeviceCommandAuditEntry(
                DateTimeOffset.UtcNow,
                command.DeviceId,
                runtime.ResolveResourceId(command.DeviceId),
                command.Capability,
                command.Operation,
                result.Success,
                result.ErrorCode,
                result.Message,
                sw.Elapsed.TotalMilliseconds,
                command.RequestId));
        }

        return result;
    }

    private async Task<DeviceResult> DispatchCoreAsync(DeviceCommand command, CancellationToken cancellationToken)
    {
        return (command.Capability, command.Operation) switch
        {
            ("IDevice", "GetHealth") => await runtime.InvokeAsync(
                command.DeviceId,
                (device, ct) => device.GetHealthAsync(ct),
                cancellationToken).ConfigureAwait(false),

            ("IDeviceConnection", "Connect") => await runtime.InvokeAsync(
                command.DeviceId,
                async (device, ct) =>
                {
                    var connection = runtime.RequireCapability<IDeviceConnection>(device);
                    return await connection.ConnectAsync(ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false),

            ("IDeviceConnection", "Disconnect") => await runtime.InvokeAsync(
                command.DeviceId,
                async (device, ct) =>
                {
                    var connection = runtime.RequireCapability<IDeviceConnection>(device);
                    return await connection.DisconnectAsync(ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false),

            ("IDeviceConnection", "Reconnect") => await runtime.InvokeAsync(
                command.DeviceId,
                async (device, ct) =>
                {
                    var connection = runtime.RequireCapability<IDeviceConnection>(device);
                    return await connection.ReconnectAsync(ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false),

            ("IOpticalPowerMeter", "ReadPower") => await runtime.InvokeAsync(
                command.DeviceId,
                async (device, ct) =>
                {
                    var meter = runtime.RequireCapability<IOpticalPowerMeter>(device);
                    var channel = GetInt(command, "channel", 0);
                    return await meter.ReadPowerAsync(channel, ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false),

            ("IOpticalPowerMeter", "SetWavelength") => await runtime.InvokeAsync(
                command.DeviceId,
                async (device, ct) =>
                {
                    var meter = runtime.RequireCapability<IOpticalPowerMeter>(device);
                    var wavelength = GetDouble(command, "wavelengthNm");
                    return await meter.SetWavelengthAsync(wavelength, ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false),

            ("ILaserSource", "SetWavelength") => await runtime.InvokeAsync(
                command.DeviceId,
                async (device, ct) =>
                {
                    var laser = runtime.RequireCapability<ILaserSource>(device);
                    var wavelength = GetDouble(command, "wavelengthNm");
                    return await laser.SetWavelengthAsync(wavelength, ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false),

            ("ILaserSource", "SetOutput") => await runtime.InvokeAsync(
                command.DeviceId,
                async (device, ct) =>
                {
                    var laser = runtime.RequireCapability<ILaserSource>(device);
                    var enabled = GetBool(command, "enabled");
                    return await laser.SetOutputAsync(enabled, ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false),

            ("IOpticalSwitch", "SwitchTo") => await runtime.InvokeAsync(
                command.DeviceId,
                async (device, ct) =>
                {
                    var opticalSwitch = runtime.RequireCapability<IOpticalSwitch>(device);
                    var input = GetInt(command, "inputPort", 1);
                    var output = GetInt(command, "outputPort", 1);
                    return await opticalSwitch.SwitchToAsync(input, output, ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false),

            _ => DeviceResult.Fail(
                DeviceErrorCode.NotSupported,
                $"Unsupported command '{command.Capability}.{command.Operation}'.",
                command.DeviceId,
                command.RequestId)
        };
    }

    private static int GetInt(DeviceCommand command, string key, int defaultValue)
    {
        if (!command.Arguments.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return Convert.ToInt32(value);
    }

    private static double GetDouble(DeviceCommand command, string key)
    {
        if (!command.Arguments.TryGetValue(key, out var value) || value is null)
        {
            throw new ArgumentException($"Missing argument '{key}'.");
        }

        return Convert.ToDouble(value);
    }

    private static bool GetBool(DeviceCommand command, string key)
    {
        if (!command.Arguments.TryGetValue(key, out var value) || value is null)
        {
            throw new ArgumentException($"Missing argument '{key}'.");
        }

        return Convert.ToBoolean(value);
    }
}
