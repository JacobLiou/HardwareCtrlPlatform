# Adding a Native Driver

Use this path when **company UDL does not cover** a device/capability. Native is first-class alongside Simulator and Udl.

## Checklist

1. **Capability** — confirm or add an interface under `Device.Contracts.Capabilities` (e.g. `IOpticalPowerMeter`). Do not put COM/serial types in Contracts.
2. **Simulator** — always add a Simulator implementation so CI and UI work without hardware.
3. **Native driver** — implement the capability in `Device.Drivers.Samples` (template) or `Device.Drivers.<YourName>` (product). Expose a public `DriverName` constant.
4. **Resolver** — register the driver in `NativeSampleDriverResolver` (or a station-specific resolver plugged into `CompositeDriverResolver`).
5. **Config** — add an entry in `Station.App` `appsettings.json`:

```json
{
  "DeviceId": "PM-CUSTOM",
  "DeviceType": "OpticalPowerMeter",
  "Provider": "Native",
  "DriverName": "CustomInlinePowerMeter"
}
```

6. **Consume** — station code uses `IUdlServerClient` / capability proxies from `Device.Client`. Do **not** reference the driver project from the WPF App; keep wiring in `Device.Hosting`.

## Provider cheat sheet

| Provider | When |
|----------|------|
| `Simulator` | No hardware / automated tests |
| `Udl` | Capability exists in company UDL2 (`UdlCom*`) |
| `Native` | Self-written protocol, EXE, serial, incomplete UDL |

## Reference sample

`CustomInlinePowerMeter` + `NativeSampleDriverResolver` demonstrate a self-implemented meter fingerprint.
