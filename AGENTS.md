# HardwareCtrlPlatform — agent / developer constraints

Personal reusable platform for **quickly scaffolding station HMI / workstation control software**.

## Positioning

- Reuse company **UDL** behind capability interfaces (`Device.Contracts` + `Device.Drivers.Udl`).
- Provide a **Workflow empty shell** (`Station.Workflow`) and a **copyable WPF template** (`Station.App`).
- No product-specific process (no invented MES fields, no fake device commands).
- Prefer Simulator for development; pin **x86** when talking to UDL2 COM.

## Layering

```text
Station.App -> Station.Workflow
Station.App -> Device.Hosting
Device.Hosting -> Device.Client / Simulators / Drivers.Udl / Drivers.Samples / Server / Contracts
```

- View / ViewModel: display + call `IStationWorkflow`. No COM / UDL ProgID in UI.
- Device wiring: `AddDevicePlatform` + `Devices:Entries` (`Simulator` | `Udl` | `Native`).
- When UDL is incomplete: implement a **Native** driver (see `docs/Adding-Native-Driver.md`).
- Workflow: explicit states; no P/Invoke.
- Drivers and COM stay under `Device.Drivers.*` / Hosting composition only.

## Stack

- .NET 8, WPF (`net8.0-windows`) for `Station.App`
- CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting
- Default host architecture: **x86** (`PlatformTarget=x86`, publish RID `win-x86`) because UDL2 COM is win32

## Reliability baseline (when you add real stations)

- Explicit state machine (no boolean spaghetti)
- Spec / thresholds outside UI
- Local persistence before external upload (when data pipeline exists)
- Bounded retries; never invent unknown UDL/MES contracts

## Verify

```powershell
dotnet build HardwareCtrlPlatform.sln
dotnet test HardwareCtrlPlatform.sln
```
