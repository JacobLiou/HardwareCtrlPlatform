using Device.Client;
using Device.Client.Proxies;
using Device.Contracts.Capabilities;
using Device.Contracts.Common;
using Device.Contracts.Tags;
using Device.Server.Registry;

namespace Device.Tags;

/// <summary>
/// Hard-coded Capability/Operation adapters for Tag poll/write (no invented UDL ops).
/// </summary>
public static class CapabilityTagAdapter
{
    public static void EnsureSupported(TagDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var key = (definition.Capability, definition.Operation);
        var readOk = IsReadable(key);
        var writeOk = IsWritable(key);

        if (definition.Access is TagAccess.Read or TagAccess.ReadWrite && !readOk)
        {
            throw new NotSupportedException(
                $"Tag '{definition.TagId}' Access={definition.Access} requires readable " +
                $"{definition.Capability}.{definition.Operation}.");
        }

        if (definition.Access is TagAccess.Write or TagAccess.ReadWrite && !writeOk)
        {
            throw new NotSupportedException(
                $"Tag '{definition.TagId}' Access={definition.Access} requires writable " +
                $"{definition.Capability}.{definition.Operation}.");
        }

        if (!readOk && !writeOk)
        {
            throw new NotSupportedException(
                $"Unsupported tag binding {definition.Capability}.{definition.Operation} for '{definition.TagId}'.");
        }
    }

    public static async Task PollAsync(
        TagDefinition definition,
        IUdlServerClient client,
        IReadOnlyList<DeviceDefinition> devices,
        ITagStore store,
        CancellationToken cancellationToken)
    {
        if (definition.Access == TagAccess.Write)
        {
            return;
        }

        try
        {
            var identity = ResolveIdentity(definition, devices);
            var now = DateTimeOffset.UtcNow;

            switch (definition.Capability, definition.Operation)
            {
                case ("IOpticalPowerMeter", "ReadPower"):
                {
                    var meter = new UdlPowerMeterProxy(client, identity);
                    var channel = GetInt(definition.Args, "channel", 0);
                    var result = await meter.ReadPowerAsync(channel, cancellationToken).ConfigureAwait(false);
                    PublishResult(
                        store,
                        definition,
                        result.Success ? result.Data?.Value : null,
                        result.Success ? (definition.Unit ?? result.Data?.Unit) : definition.Unit,
                        result,
                        now);
                    break;
                }
                case ("IDevice", "GetHealth"):
                {
                    var device = CreateHealthDevice(client, identity);
                    var result = await device.GetHealthAsync(cancellationToken).ConfigureAwait(false);
                    object? value = null;
                    if (result.Success && result.Data is not null)
                    {
                        var field = GetString(definition.Args, "field", "State");
                        value = field.ToLowerInvariant() switch
                        {
                            "state" => result.Data.State.ToString(),
                            "ishealthy" => result.Data.IsHealthy,
                            "message" => result.Data.Message,
                            _ => result.Data.State.ToString()
                        };
                    }

                    PublishResult(store, definition, value, definition.Unit, result, now);
                    break;
                }
                default:
                    store.Publish(new TagSnapshot(
                        definition.TagId,
                        null,
                        definition.Unit,
                        TagQuality.Bad,
                        now,
                        $"Unsupported poll {definition.Capability}.{definition.Operation}"));
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            store.Publish(new TagSnapshot(
                definition.TagId,
                null,
                definition.Unit,
                TagQuality.Bad,
                DateTimeOffset.UtcNow,
                ex.Message));
        }
    }

    public static async Task<DeviceResult> WriteAsync(
        TagDefinition definition,
        IUdlServerClient client,
        IReadOnlyList<DeviceDefinition> devices,
        object? value,
        CancellationToken cancellationToken)
    {
        if (definition.Access == TagAccess.Read)
        {
            return DeviceResult.Fail(
                DeviceErrorCode.NotSupported,
                $"Tag '{definition.TagId}' is read-only.",
                definition.DeviceId);
        }

        var identity = ResolveIdentity(definition, devices);

        return (definition.Capability, definition.Operation) switch
        {
            ("IOpticalPowerMeter", "SetWavelength") =>
                await new UdlPowerMeterProxy(client, identity)
                    .SetWavelengthAsync(Convert.ToDouble(value), cancellationToken)
                    .ConfigureAwait(false),

            ("ILaserSource", "SetWavelength") =>
                await new UdlLaserSourceProxy(client, identity)
                    .SetWavelengthAsync(Convert.ToDouble(value), cancellationToken)
                    .ConfigureAwait(false),

            ("ILaserSource", "SetOutput") =>
                await new UdlLaserSourceProxy(client, identity)
                    .SetOutputAsync(Convert.ToBoolean(value), cancellationToken)
                    .ConfigureAwait(false),

            ("IOpticalSwitch", "SwitchTo") => await WriteSwitchAsync(
                definition,
                client,
                identity,
                value,
                cancellationToken).ConfigureAwait(false),

            _ => DeviceResult.Fail(
                DeviceErrorCode.NotSupported,
                $"Unsupported write {definition.Capability}.{definition.Operation}.",
                definition.DeviceId)
        };
    }

    private static async Task<DeviceResult> WriteSwitchAsync(
        TagDefinition definition,
        IUdlServerClient client,
        DeviceIdentity identity,
        object? value,
        CancellationToken cancellationToken)
    {
        var input = GetInt(definition.Args, "inputPort", 1);
        var output = GetInt(definition.Args, "outputPort", 1);

        if (value is string text && text.Contains(','))
        {
            var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                input = int.Parse(parts[0]);
                output = int.Parse(parts[1]);
            }
        }
        else if (value is not null)
        {
            // Primary field override: output port when a single numeric value is supplied.
            output = Convert.ToInt32(value);
        }

        return await new UdlOpticalSwitchProxy(client, identity)
            .SwitchToAsync(input, output, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void PublishResult(
        ITagStore store,
        TagDefinition definition,
        object? value,
        string? unit,
        DeviceResult result,
        DateTimeOffset now)
    {
        if (result.Success)
        {
            store.Publish(new TagSnapshot(
                definition.TagId,
                value,
                unit,
                TagQuality.Good,
                now,
                result.Message));
        }
        else
        {
            store.Publish(new TagSnapshot(
                definition.TagId,
                value,
                unit,
                TagQuality.Bad,
                now,
                $"{result.ErrorCode}: {result.Message}"));
        }
    }

    private static DeviceIdentity ResolveIdentity(
        TagDefinition definition,
        IReadOnlyList<DeviceDefinition> devices)
    {
        var match = devices.FirstOrDefault(d =>
            string.Equals(d.DeviceId, definition.DeviceId, StringComparison.OrdinalIgnoreCase));

        var deviceType = definition.DeviceType
            ?? match?.DeviceType
            ?? throw new InvalidOperationException(
                $"Device '{definition.DeviceId}' not found for tag '{definition.TagId}'.");

        return new DeviceIdentity(definition.DeviceId, deviceType, match?.DisplayName);
    }

    private static IDevice CreateHealthDevice(IUdlServerClient client, DeviceIdentity identity)
    {
        if (CapabilityProxyFactory.TryCreate(client, identity, out var device, out _) && device is not null)
        {
            return device;
        }

        // Fallback: power-meter proxy still exposes GetHealth via IDevice.
        return new UdlPowerMeterProxy(client, identity);
    }

    private static bool IsReadable((string Capability, string Operation) key) =>
        key is ("IOpticalPowerMeter", "ReadPower") or ("IDevice", "GetHealth");

    private static bool IsWritable((string Capability, string Operation) key) =>
        key is ("IOpticalPowerMeter", "SetWavelength")
            or ("ILaserSource", "SetWavelength")
            or ("ILaserSource", "SetOutput")
            or ("IOpticalSwitch", "SwitchTo");

    private static int GetInt(IReadOnlyDictionary<string, object?> args, string key, int defaultValue)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return Convert.ToInt32(value);
    }

    private static string GetString(IReadOnlyDictionary<string, object?> args, string key, string defaultValue)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return Convert.ToString(value) ?? defaultValue;
    }
}
