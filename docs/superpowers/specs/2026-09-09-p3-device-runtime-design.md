# P3 Device Runtime design

Date: 2026-09-09

## Goal

Deepen Device Runtime without new business capabilities: connection lifecycle, ResourceId/UDL session lifecycle, command audit, per-resource serial scheduling documentation, Simulator fault injection.

## Decisions

| Topic | Choice |
|-------|--------|
| New capabilities | Out of scope (choice A) |
| Connection API | `IDeviceConnection` optional on devices |
| UDL Disconnect | Logical Offline only; shared session closed on host shutdown |
| Audit boundary | `InMemoryUdlServerClient.DispatchAsync` |
| Scheduler | Keep serial-per-ResourceId; document; defer wait-timeout metrics |
| Fault injection | `Devices:FaultInjection` + `SimulatorFaultInjector` on Simulator devices |
| Workflow retries | Still no Polly |

## Surfaces

- Contracts: `IDeviceConnection`, `IDeviceCommandAuditor`, `DeviceCommandAuditEntry`
- Server: `InMemoryDeviceCommandAuditor`, `DeviceSchedulerOptions`
- Hosting: `IDevicePlatformLifecycle`, FaultInjection options, ConnectAll/DisconnectAll
- Client: Connect/Disconnect/Reconnect commands + audit recording
- App: start/exit lifecycle; Debug Console buttons + Audit panel
- Docs: `docs/Device-Runtime.md`
