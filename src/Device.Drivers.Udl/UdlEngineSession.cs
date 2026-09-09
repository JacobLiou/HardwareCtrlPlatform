using System.Runtime.InteropServices;
using System.Text;
using UDL2_ServerLib;

namespace Device.Drivers.Udl;

/// <summary>
/// Process-wide UDL2 Engine session. Config path is fixed to <see cref="UdlPaths.ConfigRelativePath"/>.
/// </summary>
public sealed class UdlEngineSession : IDisposable
{
    private static readonly object Sync = new();
    private static UdlEngineSession? _shared;

    private readonly UDL2_Engine _engine;
    private bool _disposed;

    private UdlEngineSession(UDL2_Engine engine, UDL2_OPM opm, UDL2_TLS tls, UDL2_OSW osw, string configFullPath)
    {
        _engine = engine;
        Opm = opm;
        Tls = tls;
        Osw = osw;
        ConfigFullPath = configFullPath;
    }

    public string ConfigFullPath { get; }

    public UDL2_OPM Opm { get; }

    public UDL2_TLS Tls { get; }

    public UDL2_OSW Osw { get; }

    public bool IsOpen => !_disposed;

    /// <summary>Open once (idempotent). Throws if config missing or UDL reports an error.</summary>
    public static UdlEngineSession OpenShared()
    {
        lock (Sync)
        {
            if (_shared is { IsOpen: true })
            {
                return _shared;
            }

            _shared?.Dispose();
            _shared = CreateNew();
            return _shared;
        }
    }

    public static bool TryOpenShared(out UdlEngineSession? session, out string error)
    {
        try
        {
            session = OpenShared();
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            session = null;
            error = ex.Message;
            return false;
        }
    }

    private static UdlEngineSession CreateNew()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("UDL2 COM requires Windows.");
        }

        var configPath = UdlPaths.ResolveConfigFullPath();
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                $"UDL config not found: '{configPath}'. Place {UdlPaths.ConfigRelativePath} under the process working directory.",
                configPath);
        }

        var engine = new UDL2_Engine();
        try
        {
            try
            {
                engine.SetDebugLogFile(Path.Combine(Environment.CurrentDirectory, "UDLlog.txt"));
            }
            catch
            {
                // Optional; do not block Open.
            }

            engine.LoadConfiguration(configPath);
            EnsureOk(engine, $"LoadConfiguration('{configPath}')");

            engine.OpenEngine();
            EnsureOk(engine, "OpenEngine");

            var opm = new UDL2_OPM();
            var tls = new UDL2_TLS();
            var osw = new UDL2_OSW();
            return new UdlEngineSession(engine, opm, tls, osw, configPath);
        }
        catch
        {
            ReleaseCom(engine);
            throw;
        }
    }

    public bool TryGetLastError(out string message) => TryReadLastError(_engine, out message);

    public void EnsureOk(string operation) => EnsureOk(_engine, operation);

    private static void EnsureOk(UDL2_Engine engine, string operation)
    {
        if (TryReadLastError(engine, out var message))
        {
            return;
        }

        throw new InvalidOperationException($"UDL {operation} failed: {message}");
    }

    /// <summary>Returns true when UDL reports NO ERROR (aligned with DeviceHandle.GetUDLMessage).</summary>
    private static bool TryReadLastError(UDL2_Engine engine, out string message)
    {
        try
        {
            var sbMsg = new sbyte[1024];
            engine.GetLastErrorMessage(out sbMsg[0], 1024);
            var bytes = new byte[1024];
            for (var i = 0; i < 1024; i++)
            {
                bytes[i] = (byte)sbMsg[i];
            }

            var result = UdlErrorMessageDecoder.Decode(bytes);

            if (result.Length >= 8 && result.StartsWith("NO ERROR", StringComparison.Ordinal))
            {
                message = string.Empty;
                return true;
            }

            message = result;
            return false;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    public static void ForceCloseShared()
    {
        lock (Sync)
        {
            _shared?.Dispose();
            _shared = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ReleaseCom(Osw);
        ReleaseCom(Tls);
        ReleaseCom(Opm);
        ReleaseCom(_engine);

        lock (Sync)
        {
            if (ReferenceEquals(_shared, this))
            {
                _shared = null;
            }
        }
    }

    private static void ReleaseCom(object? com)
    {
        if (com is not null && OperatingSystem.IsWindows() && Marshal.IsComObject(com))
        {
            Marshal.FinalReleaseComObject(com);
        }
    }
}
