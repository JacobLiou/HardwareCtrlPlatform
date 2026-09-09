# P1 Device Hosting Skeleton Design

> Approved 2026-09-09 — integration skeleton only (no read-power UI)

## Scope

- New `Device.Hosting`: options, `CompositeDriverResolver`, `AddDevicePlatform`
- Providers first-class: Simulator | Udl | Native
- `Station.App` wires hosting; UI unchanged except optional registered-device count
- Doc: `docs/Adding-Native-Driver.md`

## Non-goals

Read-power demo, workflow changes, MES, inventing new capabilities.
