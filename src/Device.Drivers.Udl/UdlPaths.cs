namespace Device.Drivers.Udl;

public static class UdlPaths
{
    public const string ConfigRelativePath = @"set\UDLConfig.xml";

    public static string ResolveConfigFullPath() =>
        Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ConfigRelativePath));
}
