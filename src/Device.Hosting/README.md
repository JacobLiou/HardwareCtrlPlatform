# Device.Hosting

Composition root for the in-process device platform.

## Register

```csharp
services.AddDevicePlatform(configuration); // section Devices
// or
services.AddDevicePlatform(o => { o.Entries.Add(...); });
```

Registers:

- `InProcessDeviceRuntime`
- `IUdlServerClient` (`InMemoryUdlServerClient` + auditor)
- `IDeviceCommandAuditor`
- `IDevicePlatformLifecycle`
- `IReadOnlyList<DeviceDefinition>`
- `IDeviceRegistrationSummary`

## ResourceId

Effective schedule / lock key = **`ResourceId ?? DeviceId`** (`InProcessDeviceRuntime.ResolveResourceId`).

Use the same `ResourceId` when two logical devices must serialize on one physical resource; use different ids for parallel access.

## Lifecycle

```csharp
await sp.GetRequiredService<IDevicePlatformLifecycle>().ConnectAllAsync();
// ...
await lifecycle.DisconnectAllAsync(forceCloseUdlSession: true); // app exit
```

Per-device Disconnect never tears down `UdlEngineSession`; only host shutdown should force-close the shared session.

## Fault injection

`Devices:FaultInjection` applies to Simulator devices only. See `docs/Device-Runtime.md`.

## Tags

`AddDevicePlatform(IConfiguration)` also registers `AddTagDataPlane` from the `Tags` section (`ITagStore`, `ITagWriter`, poller). See `docs/Tag-Data-Plane.md`.
