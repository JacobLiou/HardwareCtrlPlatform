namespace Device.Drivers.Udl;

internal static class UdlChannel
{
    public static int ParseResourceId(string? resourceId, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return defaultValue;
        }

        return int.TryParse(resourceId.Trim(), out var value) ? value : defaultValue;
    }
}
