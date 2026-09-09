using Device.Client.Commands;
using Device.Contracts.Common;

namespace Device.Client;

/// <summary>
/// Placeholder for the real UDLServer transport. Do not invent protocol fields here.
/// </summary>
public sealed class UdlServerClient : IUdlServerClient
{
    public Task<DeviceResult> SendAsync(DeviceCommand command, CancellationToken cancellationToken) =>
        throw CreateNotReady();

    public Task<DeviceResult<T>> SendAsync<T>(DeviceCommand command, CancellationToken cancellationToken) =>
        throw CreateNotReady();

    private static NotSupportedException CreateNotReady() =>
        new(
            "TODO: real UDLServer transport is not confirmed yet. " +
            "Use InMemoryUdlServerClient for development and tests.");
}