# Overview

## What this repo is

`HardwareCtrlPlatform` is a **scaffold** for station control / HMI software:

- Device capabilities over company **UDL**
- Empty **workflow** kernel
- Copyable **Station.App** template

It is intentionally free of product station business logic.

## Tech stack

| Item | Choice |
|------|--------|
| Runtime | .NET 8 |
| UI | WPF (`Station.App`, `net8.0-windows`) |
| MVVM | CommunityToolkit.Mvvm |
| Hosting | Microsoft.Extensions.Hosting |
| Device COM | UDL2 (`Device.Drivers.Udl`), process **x86** |
| Tests | xUnit |

## Architecture

```text
┌──────────────────── Station.App (WPF template) ────────────────────┐
│  MainViewModel ──► IStationWorkflow (EmptyStationWorkflow)         │
│       └── (later) Device.Client proxies for capabilities           │
└─────────────────────────────┬──────────────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────────────┐
│ Station.Workflow                                                   │
│  StationWorkflowBase · StepRunner · Fault/Reset · IStationWorkflow  │
└────────────────────────────────────────────────────────────────────┘

┌──────────────────────── Device platform ───────────────────────────┐
│ Contracts (capabilities) ← Client ← Server ← Drivers/Simulators    │
│ UDL COM lives only in Device.Drivers.Udl                           │
└────────────────────────────────────────────────────────────────────┘
```

## Projects

| Project | Responsibility |
|---------|----------------|
| `Device.Contracts` | `IDevice`, health/result/error, capability interfaces |
| `Device.Server` | Registry, command executor, in-process runtime |
| `Device.Client` | Commands + proxies + in-memory client |
| `Device.Drivers.Udl` | UDL2 COM adapters for capabilities |
| `Device.Drivers.Samples` | Native/inline sample driver |
| `Device.Hosting` | Config + composite resolver + lifecycle + Tag DI + `AddDevicePlatform` |
| `Device.Tags` | Tag registry, poller, writer adapters, in-memory store |
| `Device.Simulators` | Simulator devices + fault injection + `SimulatorPlatformFactory` |
| `Station.Workflow` | Workflow shell: cancel / timeout / Fault / Reset |
| `Station.App` | WPF station template |

## Important types

| Type | Where | Role |
|------|-------|------|
| `ITagStore` / `ITagWriter` | `Device.Contracts` | Tag uplink store / downlink writes |
| `TagPollerHostedService` | `Device.Tags` | Periodic Capability poll |
| `IDeviceConnection` | `Device.Contracts` | Connect / Disconnect / Reconnect |
| `IDeviceCommandAuditor` | `Device.Contracts` | Command audit sink |
| `IDevicePlatformLifecycle` | `Device.Hosting` | ConnectAll / DisconnectAll |
| `IOpticalPowerMeter` / `ILaserSource` / `IOpticalSwitch` | `Device.Contracts` | Capability contracts |
| `CompositeDriverResolver` | `Device.Hosting` | Simulator / Udl / Native routing |
| `AddDevicePlatform` | `Device.Hosting` | DI bootstrap from `Devices` config |
| `InMemoryUdlServerClient` | `Device.Client` | In-proc bridge to runtime (+ audit) |
| `StationWorkflowBase` | `Station.Workflow` | Abort / timeout / Fault / Reset |
| `WorkflowStepRunner` | `Station.Workflow` | Per-step timeout + bounded retry |
| `IStationWorkflow` | `Station.Workflow` | Station use-case surface |
| `EmptyStationWorkflow` | `Station.Workflow` | Template no-op implementation |
| `MainViewModel` | `Station.App` | Start / Abort / Reset |
| `DeviceDebugViewModel` | `Station.App` | Hsl-style device debug console |
| `CapabilityProxyFactory` | `Device.Client` | DeviceType → capability proxy |

## Docs

- `docs/Device-Capability-Catalog.md`
- `docs/Device-Runtime.md`
- `docs/Tag-Data-Plane.md`
- `docs/UDL-Device-Platform-README.md`
- `docs/Error-Code-Catalog.md`
- `docs/Adding-Native-Driver.md`
- `docs/Device-Debug-Console.md`
- `docs/superpowers/specs/2026-09-09-hardwarectrlplatform-skeleton-design.md`
- `docs/superpowers/specs/2026-09-09-p1-device-hosting-design.md`
- `docs/superpowers/specs/2026-09-09-p2-workflow-stability-design.md`
- `docs/superpowers/specs/2026-09-09-p3-device-runtime-design.md`
- `docs/superpowers/specs/2026-09-09-p4-tag-data-plane-design.md`
