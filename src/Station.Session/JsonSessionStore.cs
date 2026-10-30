using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Station.Session;

/// <summary>Session persistence options (bound from the <c>Session</c> config section).</summary>
public sealed class SessionOptions
{
    public string StationId { get; set; } = "STATION";

    public string? OperatorId { get; set; }

    public string? RecipeName { get; set; }

    public string? RecipeVersion { get; set; }

    /// <summary>Root directory for session files. Default: <c>{AppContext.BaseDirectory}/sessions</c>.</summary>
    public string? RootDirectory { get; set; }
}

/// <summary>Local-first session file store. Save → pending; upload ack → uploaded.</summary>
public interface ISessionStore
{
    /// <summary>Writes the record into <c>pending</c> atomically. Returns the file path.</summary>
    Task<string> SaveAsync(SessionRecord record, CancellationToken cancellationToken = default);

    /// <summary>Moves a pending session to <c>uploaded</c>. Returns false when no pending file matches.</summary>
    Task<bool> TryMarkUploadedAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Pending session file paths (oldest first).</summary>
    Task<IReadOnlyList<string>> ListPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// JSON file store: <c>{root}/pending/{sessionId}.json</c> → <c>{root}/uploaded/{sessionId}.json</c>.
/// Writes are atomic (temp file + move) so a crash never leaves a half-written session.
/// </summary>
public sealed class JsonSessionStore(IOptions<SessionOptions> options) : ISessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _root = options.Value.RootDirectory is { Length: > 0 } root
        ? root
        : Path.Combine(AppContext.BaseDirectory, "sessions");

    private string PendingDirectory => Path.Combine(_root, "pending");

    private string UploadedDirectory => Path.Combine(_root, "uploaded");

    public async Task<string> SaveAsync(SessionRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(PendingDirectory);

        var target = Path.Combine(PendingDirectory, $"{record.SessionId}.json");
        var temp = Path.Combine(PendingDirectory, $".{record.SessionId}.tmp");

        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, record, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(temp, target, overwrite: true);
        return target;
    }

    public Task<bool> TryMarkUploadedAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var source = Path.Combine(PendingDirectory, $"{sessionId}.json");
        if (!File.Exists(source))
        {
            return Task.FromResult(false);
        }

        Directory.CreateDirectory(UploadedDirectory);
        File.Move(source, Path.Combine(UploadedDirectory, $"{sessionId}.json"), overwrite: true);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<string>> ListPendingAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(PendingDirectory))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var files = Directory.EnumerateFiles(PendingDirectory, "*.json")
            .OrderBy(path => File.GetCreationTimeUtc(path))
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(files);
    }
}
