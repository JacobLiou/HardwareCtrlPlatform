# Error Code Catalog

`Device.Contracts.Common.DeviceErrorCode`

| Code | Meaning |
|------|---------|
| `None` | Success / no error |
| `NotFound` | Device id not registered |
| `Offline` | Device reported offline |
| `Busy` | Device/command queue busy |
| `Timeout` | Operation timed out |
| `Cancelled` | Caller cancelled |
| `InvalidArgument` | Bad channel/port/value |
| `NotSupported` | Capability/op not supported by driver |
| `DriverError` | Driver-level failure |
| `CommunicationError` | Transport / COM / IO failure |
| `UnexpectedError` | Unclassified failure |

Station workflows should map these to operator-visible messages; do not invent vendor codes in Domain/UI.
