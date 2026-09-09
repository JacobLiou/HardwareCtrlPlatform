# Device Debug Console

Hsl-style capability debug tab inside `Station.App` (**Devices**).

## Features

- Device list from `Devices:Entries`
- Panels by `DeviceType`: `OpticalPowerMeter` / `LaserSource` / `OpticalSwitch`
- Lifecycle: Connect / Disconnect / Reconnect (`IDeviceConnection`)
- Downlink: SetWavelength, Output, SwitchTo
- Uplink: Health, ReadPower (point read; no auto-poll in v1)
- Audit: recent command rows from `IDeviceCommandAuditor`
- All IO via `CapabilityProxyFactory` + `Device.Client` proxies

## Run

```powershell
dotnet run --project src/Station.App/Station.App.csproj -p:PlatformTarget=x86
```

See also `docs/Device-Runtime.md`.
