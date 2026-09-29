# Tag Data Plane

Thin uplink/downlink Tag layer on top of Device Runtime (P4).

## Concepts

| Direction | API | Behavior |
|-----------|-----|----------|
| Uplink | `ITagStore` | Periodic poll via Capability proxies → `TagSnapshot` |
| Downlink | `ITagWriter` | Write mapped to Capability operations |

Quality: `Good` / `Bad` / `Stale` / `Unknown`. Stale when no successful update within `StaleAfterMs` (default `2 * PeriodMs`).

## Config (`Tags` section)

See `src/Station.App/appsettings.json`. Entries declare `TagId`, `DeviceId`, `Access`, `Capability`, `Operation`, optional `Args` / `PeriodMs` / `Unit`.

### Supported adapters (v1)

**Read:** `IOpticalPowerMeter.ReadPower`, `IDevice.GetHealth` (`Args.field` = State|IsHealthy|Message)

**Write:** `IOpticalPowerMeter.SetWavelength`, `ILaserSource.SetWavelength` / `SetOutput`, `IOpticalSwitch.SwitchTo`

Unknown bindings fail fast at `TagRegistry` construction.

## Wiring

`AddDevicePlatform(configuration)` also calls `AddTagDataPlane` and starts `TagPollerHostedService`.

UI / Workflow should use `ITagStore` / `ITagWriter` only (no COM).

## Out of scope

OPC, historians, MES, trend charts, driver event push, Workflow step binding.
