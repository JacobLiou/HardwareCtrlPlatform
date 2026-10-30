using System.Text.Json;
using Microsoft.Extensions.Options;
using Station.Workflow;

namespace Station.Session.Tests;

internal sealed class RecordingWorkflow : StationWorkflowBase
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Steps.RunAsync("init", ct => Task.Delay(10, ct), cancellationToken: cancellationToken);
        await Steps.RunAsync(
            "measure",
            ct => Task.Delay(10, ct),
            new WorkflowStepOptions { MaxAttempts = 2, RetryDelay = TimeSpan.FromMilliseconds(5) },
            cancellationToken);
    }
}

public sealed class JsonSessionStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"hcp-sessions-{Guid.NewGuid():N}");

    private JsonSessionStore CreateStore()
    {
        var options = Options.Create(new SessionOptions { RootDirectory = _root });
        return new JsonSessionStore(options);
    }

    [Fact]
    public async Task Save_writes_pending_file_with_record_content()
    {
        var store = CreateStore();
        var record = NewRecord();

        var path = await store.SaveAsync(record);

        Assert.Equal(Path.Combine(_root, "pending", $"{record.SessionId}.json"), path);
        Assert.True(File.Exists(path));

        await using var stream = File.OpenRead(path);
        var saved = JsonSerializer.Deserialize<JsonElement>(stream);
        Assert.Equal(record.SessionId.ToString(), saved.GetProperty("SessionId").GetString());
        Assert.Equal("Completed", saved.GetProperty("Result").GetString());
        Assert.Equal("ST-01", saved.GetProperty("StationId").GetString());
        Assert.Equal(2, saved.GetProperty("Steps").GetArrayLength());
        Assert.Equal("measure", saved.GetProperty("Steps")[1].GetProperty("Name").GetString());
        Assert.Equal("Passed", saved.GetProperty("Steps")[1].GetProperty("Status").GetString());
    }

    [Fact]
    public async Task Save_is_atomic_no_temp_files_left_behind()
    {
        var store = CreateStore();

        await store.SaveAsync(NewRecord());

        var leftover = Directory.EnumerateFiles(Path.Combine(_root, "pending"), "*.tmp");
        Assert.Empty(leftover);
    }

    [Fact]
    public async Task ListPending_returns_files_oldest_first()
    {
        var store = CreateStore();
        var first = await store.SaveAsync(NewRecord());
        await Task.Delay(20);
        var second = await store.SaveAsync(NewRecord());

        var pending = await store.ListPendingAsync();

        Assert.Equal(new[] { first, second }, pending);
    }

    [Fact]
    public async Task TryMarkUploaded_moves_file_from_pending_to_uploaded()
    {
        var store = CreateStore();
        var record = NewRecord();
        var path = await store.SaveAsync(record);

        Assert.True(await store.TryMarkUploadedAsync(record.SessionId));

        Assert.False(File.Exists(path));
        Assert.True(File.Exists(Path.Combine(_root, "uploaded", $"{record.SessionId}.json")));
        Assert.Empty(await store.ListPendingAsync());
    }

    [Fact]
    public async Task TryMarkUploaded_returns_false_for_unknown_session()
    {
        var store = CreateStore();

        Assert.False(await store.TryMarkUploadedAsync(Guid.NewGuid()));
    }

    private SessionRecord NewRecord() =>
        new(
            Guid.NewGuid(),
            "ST-01",
            "op-01",
            "recipe-a",
            "1.0",
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow,
            nameof(WorkflowRunResult.Completed),
            null,
            new List<SessionStepRecord>
            {
                new("init", "Passed", 1, 1, DateTimeOffset.UtcNow, 12, null),
                new("measure", "Passed", 1, 2, DateTimeOffset.UtcNow, 8, null)
            });

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

public sealed class SessionRecorderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"hcp-recorder-{Guid.NewGuid():N}");

    [Fact]
    public async Task RunCompleted_is_persisted_as_pending_session()
    {
        var workflow = new RecordingWorkflow();
        var recorder = CreateRecorder(workflow, out var store);

        await workflow.StartAsync();
        await recorder.LastSave!;

        var pending = await store.ListPendingAsync();
        var file = Assert.Single(pending);

        await using var stream = File.OpenRead(file);
        var saved = JsonSerializer.Deserialize<JsonElement>(stream);
        Assert.Equal(workflow.RunId.ToString(), saved.GetProperty("SessionId").GetString());
        Assert.Equal("op-02", saved.GetProperty("OperatorId").GetString());
        Assert.Equal(2, saved.GetProperty("Steps").GetArrayLength());
        Assert.Equal("ST-07", saved.GetProperty("StationId").GetString());
    }

    [Fact]
    public async Task MeasurementsProvider_values_land_in_record()
    {
        var workflow = new RecordingWorkflow();
        var recorder = CreateRecorder(workflow, out _);
        recorder.MeasurementsProvider = () => new Dictionary<string, object?> { ["PM-01.Power"] = -3.25 };

        await workflow.StartAsync();
        await recorder.LastSave!;

        var saved = ReadLastSaved(_root);
        var measurements = saved.GetProperty("Measurements");
        Assert.Equal(-3.25, measurements.GetProperty("PM-01.Power").GetDouble());
    }

    [Fact]
    public async Task Broken_MeasurementsProvider_does_not_block_save()
    {
        var workflow = new RecordingWorkflow();
        var recorder = CreateRecorder(workflow, out var store);
        recorder.MeasurementsProvider = () => throw new InvalidOperationException("provider broken");

        SessionSavedEventArgs? args = null;
        recorder.SessionSaved += (_, e) => args = e;

        await workflow.StartAsync();
        await recorder.LastSave!;

        Assert.NotNull(args);
        Assert.True(args!.Success);
        Assert.Null(args.Error);
        Assert.Single(await store.ListPendingAsync());
    }

    [Fact]
    public async Task Faulted_run_records_fault_reason()
    {
        var workflow = new ThrowingWorkflow();
        var recorder = CreateRecorder(workflow, out _);

        await workflow.StartAsync();
        await recorder.LastSave!;

        var saved = ReadLastSaved(_root);
        Assert.Equal(nameof(WorkflowRunResult.Faulted), saved.GetProperty("Result").GetString());
        Assert.Equal("boom", saved.GetProperty("FaultReason").GetString());
    }

    private SessionRecorder CreateRecorder(StationWorkflowBase workflow, out JsonSessionStore store)
    {
        var options = Options.Create(new SessionOptions
        {
            RootDirectory = _root,
            StationId = "ST-07",
            OperatorId = "op-02",
            RecipeName = "recipe-x",
            RecipeVersion = "2.1"
        });
        store = new JsonSessionStore(options);
        return new SessionRecorder(workflow, store, options);
    }

    private static JsonElement ReadLastSaved(string root)
    {
        var file = Directory.EnumerateFiles(Path.Combine(root, "pending"), "*.json").Single();
        using var stream = File.OpenRead(file);
        return JsonSerializer.Deserialize<JsonElement>(stream);
    }

    internal sealed class ThrowingWorkflow : StationWorkflowBase
    {
        protected override Task ExecuteAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("boom");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
