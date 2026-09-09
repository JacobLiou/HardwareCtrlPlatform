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
│  WorkstationState · StateTransitionGuard · IStationWorkflow        │
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
| `Device.Simulators` | Simulator devices + `SimulatorPlatformFactory` |
| `Station.Workflow` | Empty workflow shell |
| `Station.App` | WPF station template |

## Important types

| Type | Where | Role |
|------|-------|------|
| `IOpticalPowerMeter` / `ILaserSource` / `IOpticalSwitch` | `Device.Contracts` | Capability contracts |
| `InMemoryUdlServerClient` | `Device.Client` | In-proc bridge to runtime |
| `SimulatorPlatformFactory` | `Device.Simulators` | Spin up simulator stack |
| `IStationWorkflow` | `Station.Workflow` | Station use-case surface |
| `EmptyStationWorkflow` | `Station.Workflow` | Template implementation |
| `MainViewModel` | `Station.App` | Start/Abort bound to workflow |

## Suggested flow when building a real station

1. Copy `Station.App`, rename
2. Expand states + `IStationWorkflow` implementation
3. Register Simulator devices; swap to UDL resolver for real hardware
4. Add persistence / MES adapters only when required

## Docs

- `docs/Device-Capability-Catalog.md`
- `docs/UDL-Device-Platform-README.md`
- `docs/Error-Code-Catalog.md`
- `docs/superpowers/specs/2026-09-09-hardwarectrlplatform-skeleton-design.md`
