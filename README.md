# HardwareCtrlPlatform

Reusable skeleton for building **station HMI / workstation control software** on .NET 8.

Core idea:

1. **Device capabilities** — abstract hardware as typed capabilities (power meter, laser, optical switch, …)
2. **Company UDL** — real drivers live in `Device.Drivers.Udl` (UDL2 COM, **x86**)
3. **Station workflow shell** — explicit state machine surface in `Station.Workflow`
4. **Copyable App template** — `Station.App` is the starting point for a new station UI

This is not a product station and carries no product-specific process logic.

## Quick start

```powershell
dotnet build HardwareCtrlPlatform.sln
dotnet test HardwareCtrlPlatform.sln
dotnet run --project src/Station.App/Station.App.csproj -p:PlatformTarget=x86
```

## Solution map

| Area | Projects | Role |
|------|----------|------|
| Device | `Device.Contracts`, `Server`, `Client`, `Drivers.Udl`, `Drivers.Samples`, `Simulators`, `Hosting` | Capability contracts, runtime, UDL/Native/Simulator drivers, DI bootstrap |
| Station | `Station.Workflow`, `Station.App` | Workflow empty shell + WPF template |
| Tests | `Device.*.Tests`, `Station.Workflow.Tests` | Unit / smoke tests |

## How to start a new station

1. Copy `Station.App` (or branch from it) and rename namespaces
2. Replace `EmptyStationWorkflow` with your real state machine in `Station.Workflow` (or a station-specific project)
3. Open **Devices** tab for Hsl-style capability debug (Health / ReadPower / Set λ / Switch)
4. Configure devices in `appsettings.json` (`Devices:Entries`) — `Provider` = `Simulator` | `Udl` | `Native`
5. Call `services.AddDevicePlatform(configuration)` (already in the template)
6. Keep process architecture **x86** when using UDL2 COM

See `docs/overview.md`, `docs/Adding-Native-Driver.md`, and `AGENTS.md`.
