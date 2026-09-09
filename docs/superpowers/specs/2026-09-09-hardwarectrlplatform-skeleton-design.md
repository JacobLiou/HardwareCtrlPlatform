# HardwareCtrlPlatform Skeleton Design

> Date: 2026-09-09  
> Status: Approved for implementation  
> Goal: Reusable station HMI skeleton — company UDL via Device capabilities + Workflow empty shell + Station App template

## Decisions

- Platform name: `HardwareCtrlPlatform`
- Keep `Device.*` (Contracts / Server / Client / Drivers.Udl / Drivers.Samples / Simulators)
- Delete all `VoaCollimator.*`, TAS, `Device.UiDemo`, product docs, strategy long-form doc
- Add `Station.Workflow` (empty kernel) + `Station.App` (copyable WPF template)
- No Data Pipeline / MES this round
- No VOA / collimator / point-test product semantics in main code or docs

## Layering

```text
Station.App → Station.Workflow
Station.App may later use Device.Client (not Drivers.Udl / Server directly)
Device.Client → Device.Server → Device.Contracts
Device.Drivers.* / Device.Simulators → Device.Server / Contracts
```

## Acceptance

```powershell
dotnet build HardwareCtrlPlatform.sln
dotnet test HardwareCtrlPlatform.sln
```

Station.App runs with EmptyStationWorkflow (Start/Abort).
