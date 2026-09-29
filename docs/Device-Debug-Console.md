# Device Debug Console

Hsl-style capability debug tab inside `Station.App` (**Devices**).

## Features

- Device list from `Devices:Entries`
- Panels by `DeviceType`: `OpticalPowerMeter` / `LaserSource` / `OpticalSwitch`
- Lifecycle: Connect / Disconnect / Reconnect (`IDeviceConnection`)
- Live Tags: periodic uplink + write downlink (`ITagStore` / `ITagWriter`)
- Downlink: SetWavelength, Output, SwitchTo
- Uplink: Health, ReadPower (point read; no auto-poll on capability panel)
- Audit: recent command rows from `IDeviceCommandAuditor`
- All IO via `CapabilityProxyFactory` + `Device.Client` proxies (Tags go through same client)

## Run

```powershell
dotnet run --project src/Station.App/Station.App.csproj -p:PlatformTarget=x86
```

See also `docs/Device-Runtime.md`, `docs/Tag-Data-Plane.md`.
