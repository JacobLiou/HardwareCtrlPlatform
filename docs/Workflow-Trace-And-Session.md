# Workflow Trace & Session Persistence (P5)

运行轨迹可观测 + 互锁 + Session 本地落盘。让一次 Run 从「黑盒任务」变成
「有步骤、有耗时、有判定、可追溯的记录」。对应 `继续TODO.md` 的 P4（Workflow Step 模型 +
步骤 UI 绑定）与 P5（Session 本地落盘最小集）。

## 运行轨迹（Station.Workflow）

```text
WorkflowStepRunner.RunAsync("Sample", action, options, ct)
   └─ WorkflowStepTrace { Name, Status, Attempt/MaxAttempts, StartedAtUtc, Duration, Error }
        └─ StepTraceChanged 事件（每次状态变化）
             └─ StationWorkflowBase 累积 → StepTraces / CurrentStep
                  └─ RunCompleted 事件（每次 Run 结束，一次性）
                       └─ WorkflowRunTrace { RunId, Result, Fault, Steps }
```

- `WorkflowStepTrace`：**每个 `Steps.RunAsync` 调用一个实例**；重试在同一实例上更新 `Attempt`。
  对象是可变的，UI/订阅方在事件回调里读取当前值，不要事后按状态过滤旧引用。
- `WorkflowRunTrace.Result`：`Completed` / `Aborted`（用户 Abort，回到 Idle）/ `Faulted`。
- `RunId`：每次成功 Start 重新生成，随轨迹与 Session 串联。

## 互锁（Interlock）

```csharp
public interface IWorkflowInterlock
{
    string Name { get; }
    bool IsSatisfied(out string? reason);
}
```

- `StationWorkflowBase(options, interlocks)` 构造时注入；Start 前在状态锁内逐个评估。
- 任一不满足 → 抛 `WorkflowStartRejectedException`（含全部 `Reasons`），状态保持 Idle。
- 互锁必须是**快速、非阻塞**的纯检查（快照读取），不要在互锁里做 IO。
- 模板示例：`Station.App/Workflow/TagsReadyInterlock.cs`（至少一个 Good 质量 Tag 才允许 Start）。

## Session 本地落盘（Station.Session）

```text
IStationWorkflow.RunCompleted ──► SessionRecorder
                                    ├─ Metadata（StationId / Operator / Recipe，宿主可更新）
                                    ├─ MeasurementsProvider?（可选，如 Good Tag 快照）
                                    └─ SessionRecord ──► JsonSessionStore
                                                          pending/{sessionId}.json（原子写）
                                                          MarkUploaded → uploaded/
```

- **本地先存**：`{RootDirectory}/pending/{sessionId}.json`，temp 文件 + Move 原子替换，
  崩溃不会留下半截 JSON。`Session:RootDirectory` 缺省为 `{AppContext.BaseDirectory}/sessions`。
- **Pending 上传**：`ISessionStore.TryMarkUploadedAsync(sessionId)` 把文件移入 `uploaded/`，
  供将来 MES/TAS 网关消费（幂等：不存在返回 false）。
- 落盘失败**不会**打崩工位：`SessionSaved` 事件带 `Error`，UI 只提示。
- 配置（`appsettings.json` 的 `Session` 段）：

```json
"Session": {
  "StationId": "ST-TEMPLATE-01",
  "OperatorId": "operator",
  "RecipeName": "TemplateFlow",
  "RecipeVersion": "1.0"
}
```

## 分层

```text
Station.App -> Station.Workflow   (轨迹/互锁内核，BCL only)
Station.App -> Station.Session    (SessionRecord + JSON store + recorder)
Station.Session -> Station.Workflow(仅引用轨迹类型，不依赖设备面)
```

## 模板用法（Station.App）

- `TemplateStationWorkflow`：SelfTest → Sample（读 `ITagStore`，要求 ≥1 个 Good Tag，
  有界重试 3 次）→ Report。复制它改 `ExecuteAsync` 即成真实工位流程。
- `MainViewModel`：`Steps`（步骤条 + 状态色 + 尝试次数 + 耗时）、`RunLog`（时间戳运行日志，
  上限 200 行）、`RunElapsedText`（运行计时）、`LastSessionText`（落盘结果）。
- Start 被互锁拒绝时显示 `InterlockText`（橙色），不进入 Running。

## 刻意不做（后续增量）

- 上传网关实现（`IUploadGateway`）、SQLite 存储、Recipe 外置校验 —— 等有真实对接需求。
- 报警/锁存体系 —— 下一优先级（见 `继续TODO.md` P7）。
