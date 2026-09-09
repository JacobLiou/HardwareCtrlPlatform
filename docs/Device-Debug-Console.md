# Device Debug Console

Hsl-style capability debug tab inside `Station.App` (**Devices**).

## Features

- Device list from `Devices:Entries`
- Panels by `DeviceType`: `OpticalPowerMeter` / `LaserSource` / `OpticalSwitch`
- Downlink: SetWavelength, Output, SwitchTo
- Uplink: Health, ReadPower (point read; no auto-poll in v1)
- All IO via `CapabilityProxyFactory` + `Device.Client` proxies

## Run

```powershell
dotnet run --project src/Station.App/Station.App.csproj -p:PlatformTarget=x86
```
