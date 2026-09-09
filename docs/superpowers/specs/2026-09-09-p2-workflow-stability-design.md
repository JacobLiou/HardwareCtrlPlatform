# P2 Workflow Stability Design

> Implemented 2026-09-09

## Decisions

- Self-built FSM in `Station.Workflow` (no Elsa / WorkflowCore / Stateless)
- No Polly — BCL `CancellationTokenSource` + `CancelAfter` + bounded retry loop
- User **Abort** → Idle; **Timeout / exception** → Fault; **Reset** Fault → Idle

## Types

- `StationWorkflowBase`, `WorkflowStepRunner`, `WorkflowRunOptions`, `WorkstationFaultInfo`
- `IStationWorkflow`: Start / Abort / Reset + `FaultInfo` + `StateChanged`
