# UDL Device Platform

In-process device capability + scheduling stack (Contracts, Server, Client, Simulators, UDL2 COM drivers).

Entry solution: `HardwareCtrlPlatform.sln`.

```powershell
dotnet build HardwareCtrlPlatform.sln
dotnet test HardwareCtrlPlatform.sln --filter "FullyQualifiedName~Device."
```

## Providers

| Provider | Driver | Purpose |
|----------|--------|---------|
| `Simulator` | Simulated power meter / laser / switch | No hardware |
| `Udl` | `UdlComPowerMeter` / Laser / Switch | Real `UDL2_ServerLib` |
| `Native` | Sample inline driver | Custom driver example |

Real COM requires `set\UDLConfig.xml` relative to the working directory and an **x86** host process.

## Usage sketch (Simulator)

```csharp
var (_, client) = SimulatorPlatformFactory.Create(
    new DeviceDefinition("PM-01", "OpticalPowerMeter",
        DeviceProviderKind.Simulator, SimulatorDriverResolver.PowerMeterDriver));

var meter = new UdlPowerMeterProxy(client, new DeviceIdentity("PM-01", "OpticalPowerMeter"));
var power = await meter.ReadPowerAsync(channel: 0, CancellationToken.None);
```

## Boundaries

- Station apps depend on `Device.Client` + `Device.Contracts`
- Do not reference `Device.Drivers.Udl` or `Device.Server` directly from UI projects
- Native handles / COM stay inside Drivers

See also `docs/Device-Capability-Catalog.md` and `docs/Error-Code-Catalog.md`.
