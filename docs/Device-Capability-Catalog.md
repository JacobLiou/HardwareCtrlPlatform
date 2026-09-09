# Device Capability Catalog

Capabilities modeled in `Device.Contracts.Capabilities`.

## IOpticalPowerMeter

| Operation | Parameters | Returns |
|-----------|------------|---------|
| `ReadPowerAsync` | `channel` (int, >=0) | `OpticalPower(Value, Unit, WavelengthNm?)` |
| `SetWavelengthAsync` | `wavelengthNm` (double, >0) | `DeviceResult` |

Default Simulator unit: `dBm`.

## ILaserSource

| Operation | Parameters | Returns |
|-----------|------------|---------|
| `SetWavelengthAsync` | `wavelengthNm` | `DeviceResult` |
| `SetOutputAsync` | `enabled` | `DeviceResult` |

## IOpticalSwitch

| Operation | Parameters | Returns |
|-----------|------------|---------|
| `SwitchToAsync` | `inputPort`, `outputPort` (positive ints) | `DeviceResult` |

## IDevice (common)

| Operation | Returns |
|-----------|---------|
| `GetHealthAsync` | `DeviceHealth(State, IsHealthy, Message?, CheckedAt?)` |

## Extension guidance

Add new capabilities as interfaces under `Device.Contracts.Capabilities`, then implement Simulator + UDL/Native drivers. Do not leak COM types into Contracts or Station layers.
