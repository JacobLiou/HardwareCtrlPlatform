# Device Runtime

In-process device runtime used by `Station.App` via `Device.Hosting.AddDevicePlatform`.

## Connection lifecycle

- Optional capability: `IDeviceConnection` (`Connect` / `Disconnect` / `Reconnect`).
- Simulator: Connect → `Online`, Disconnect → `Offline`; ops while Offline return `DeviceErrorCode.Offline`.
- UDL COM: Connect ensures the shared `UdlEngineSession` is open; Disconnect marks the logical device Offline and **does not** dispose the process-wide session.
- Host shutdown: `IDevicePlatformLifecycle.DisconnectAllAsync(forceCloseUdlSession: true)` calls `UdlEngineSession.ForceCloseShared()`.
- App hooks: `ConnectAllAsync` after host build; `DisconnectAllAsync(..., forceCloseUdlSession: true)` on exit.

## ResourceId

Effective lock / schedule key:

```text
ResourceId ?? DeviceId
```

Configured on `Devices:Entries[].ResourceId`. Two logical devices with the same `ResourceId` share one serial lock; different ids may execute in parallel.

## Scheduler

`DeviceCommandExecutor` policy (default, no Polly):

- **Serial** for the same resource key
- **Parallel** across different resource keys

See `DeviceSchedulerOptions` (reserved for a future wait timeout).

## Command audit

- `IDeviceCommandAuditor` + `InMemoryDeviceCommandAuditor` (ring of last ~200 entries)
- Wired at `InMemoryUdlServerClient` dispatch (covers all proxies)
- Fields: Utc time, DeviceId, ResourceId, Capability, Operation, Success, ErrorCode, Message, DurationMs, RequestId
- Debug Console **Audit** panel shows recent rows

## Simulator fault injection

Config section `Devices:FaultInjection` (global for Simulator devices resolved by Hosting):

| Property | Effect |
|----------|--------|
| `Enabled` | Master switch |
| `FixedDelayMs` | Delay before each op |
| `FailNextCount` | Next N ops return `DriverError` |
| `ForceOffline` | Treat device as Offline |
| `CorruptPowerReading` | Power meter returns NaN |

Applied via `SimulatorFaultInjector` on `SimulatedDeviceBase` from `CompositeDriverResolver`.

## Scope note (P3)

This round deepens runtime only — **no new business capabilities** (power supply, temp, serial meters, Tag bus, MES). See `docs/Device-Capability-Catalog.md` for capability extension guidance.
