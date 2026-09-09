# UDL Device Platform (Contracts)

Typed device **capabilities** and common result/health types.

- Business / station code depends on this project + `Device.Client`
- Do not put COM ProgIDs, native handles, or UDL config parsing here

Solution: `HardwareCtrlPlatform.sln` → `01-Device`.

```powershell
dotnet build HardwareCtrlPlatform.sln
dotnet test HardwareCtrlPlatform.sln --filter "FullyQualifiedName~Device."
```

Catalog: `docs/Device-Capability-Catalog.md`.
