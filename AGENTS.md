# HardwareCtrlPlatform — agent / developer constraints

Personal reusable platform for **quickly scaffolding station HMI / workstation control software**.

## Positioning

- Reuse company **UDL** behind capability interfaces (`Device.Contracts` + `Device.Drivers.Udl`).
- Provide a **Workflow empty shell** (`Station.Workflow`) and a **copyable WPF template** (`Station.App`).
- No product-specific process (no invented MES fields, no fake device commands).
- Prefer Simulator for development; pin **x86** when using UDL2 COM.
- Workflow stability (BCL only, **no Polly**): Abort cancels cooperatively → Idle; step/run timeout or exception → Fault; Reset clears Fault → Idle. Use `StationWorkflowBase` + `WorkflowStepRunner`.
- Device runtime (P3): `IDeviceConnection` lifecycle, `ResourceId ?? DeviceId` serial scheduling, command audit at client dispatch, Simulator `Devices:FaultInjection`. See `docs/Device-Runtime.md`. No Polly for device IO in this layer.

## Layering

```text
Station.App -> Station.Workflow
Station.App -> Device.Hosting
Device.Hosting -> Device.Client / Simulators / Drivers.Udl / Drivers.Samples / Server / Contracts
```

- View / ViewModel: display + call `IStationWorkflow`. No COM / UDL ProgID in UI.
- Device wiring: `AddDevicePlatform` + `Devices:Entries` (`Simulator` | `Udl` | `Native`).
- When UDL is incomplete: implement a **Native** driver (see `docs/Adding-Native-Driver.md`).
- Workflow: explicit states; cancel / timeout / Fault via `StationWorkflowBase` (no Polly).
- Drivers and COM stay under `Device.Drivers.*` / Hosting composition only.

## Stack

- .NET 8, WPF (`net8.0-windows`) for `Station.App`
- CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting
- Default host architecture: **x86** (`PlatformTarget=x86`, publish RID `win-x86`) because UDL2 COM is win32

- Reliability baseline (when you add real stations): Spec outside UI; local save before upload; bounded retries; never invent unknown UDL/MES contracts.

## Verify

```powershell
dotnet build HardwareCtrlPlatform.sln
dotnet test HardwareCtrlPlatform.sln
```
