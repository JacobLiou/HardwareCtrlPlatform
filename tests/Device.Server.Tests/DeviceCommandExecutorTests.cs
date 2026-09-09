using Device.Contracts.Common;
using Device.Server.Scheduling;

namespace Device.Server.Tests;

public class DeviceCommandExecutorTests
{
    [Fact]
    public async Task SameResourceId_IsSerialized()
    {
        var executor = new DeviceCommandExecutor();
        var timeline = new List<string>();
        var gate = new object();

        async Task<DeviceResult<int>> SlowOp(string label, CancellationToken ct)
        {
            lock (gate)
            {
                timeline.Add($"{label}:start");
            }

            await Task.Delay(80, ct);

            lock (gate)
            {
                timeline.Add($"{label}:end");
            }

            return DeviceResult<int>.Ok(1, label);
        }

        var t1 = executor.ExecuteAsync("PM-01", ct => SlowOp("A", ct), CancellationToken.None);
        await Task.Delay(10);
        var t2 = executor.ExecuteAsync("PM-01", ct => SlowOp("B", ct), CancellationToken.None);

        await Task.WhenAll(t1, t2);

        Assert.Equal(["A:start", "A:end", "B:start", "B:end"], timeline);
    }

    [Fact]
    public async Task DifferentResourceIds_CanOverlap()
    {
        var executor = new DeviceCommandExecutor();
        var started = 0;
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<DeviceResult<int>> Hold(string id, CancellationToken ct)
        {
            if (Interlocked.Increment(ref started) == 2)
            {
                bothStarted.TrySetResult();
            }

            var completed = await Task.WhenAny(bothStarted.Task, Task.Delay(1000, ct));
            Assert.Same(bothStarted.Task, completed);
            return DeviceResult<int>.Ok(1, id);
        }

        var t1 = executor.ExecuteAsync("PM-01", ct => Hold("PM-01", ct), CancellationToken.None);
        var t2 = executor.ExecuteAsync("PM-02", ct => Hold("PM-02", ct), CancellationToken.None);

        await Task.WhenAll(t1, t2);
        Assert.True(bothStarted.Task.IsCompletedSuccessfully);
    }
}