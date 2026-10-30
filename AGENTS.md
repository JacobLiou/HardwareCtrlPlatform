# HardwareCtrlPlatform — agent / developer constraints

Personal reusable platform for **quickly scaffolding station HMI / workstation control software**.

## Positioning

- Reuse company **UDL** behind capability interfaces (`Device.Contracts` + `Device.Drivers.Udl`).
- Provide a **Workflow empty shell** (`Station.Workflow`) and a **copyable WPF template** (`Station.App`).
- No product-specific process (no invented MES fields, no fake device commands).
- Prefer Simulator for development; pin **x86** when using UDL2 COM.
- Workflow stability (BCL only, **no Polly**): Abort cancels cooperatively → Idle; step/run timeout or exception → Fault; Reset clears Fault → Idle. Use `StationWorkflowBase` + `WorkflowStepRunner`.
- Run observability (P5): `WorkflowStepTrace`/`WorkflowRunTrace` + `StepTraceChanged`/`RunCompleted` events; interlocks via `IWorkflowInterlock` (Start rejected → `WorkflowStartRejectedException`, state stays Idle). See `docs/Workflow-Trace-And-Session.md`.
- Session persistence (P5): `SessionRecorder` subscribes `RunCompleted` → `SessionRecord` → JSON pending/uploaded (`Station.Session`, local save before any upload). No Polly for device IO in this layer.
- Device runtime (P3): `IDeviceConnection` lifecycle, `ResourceId ?? DeviceId` serial scheduling, command audit at client dispatch, Simulator `Devices:FaultInjection`. See `docs/Device-Runtime.md`. No Polly for device IO in this layer.
- Tag data plane (P4): config Tags → `ITagStore` / `ITagWriter` + Hosting poller. See `docs/Tag-Data-Plane.md`.

## Layering

```text
Station.App -> Station.Workflow / Station.Session
Station.App -> Device.Hosting
Station.Session -> Station.Workflow
Device.Hosting -> Device.Client / Simulators / Drivers.Udl / Drivers.Samples / Server / Contracts
```

- View / ViewModel: display + call `IStationWorkflow`. No COM / UDL ProgID in UI.
- Device wiring: `AddDevicePlatform` + `Devices:Entries` (`Simulator` | `Udl` | `Native`).
- When UDL is incomplete: implement a **Native** driver (see `docs/Adding-Native-Driver.md`).
- Workflow: explicit states; cancel / timeout / Fault via `StationWorkflowBase` (no Polly).
- Session: `AddStationSession` + `SessionRecorder` (records every run; pending → uploaded).
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
