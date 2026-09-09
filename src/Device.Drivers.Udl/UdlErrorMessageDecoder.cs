using System.Text;

namespace Device.Drivers.Udl;

public static class UdlErrorMessageDecoder
{
    static UdlErrorMessageDecoder()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static string Decode(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        var trimmed = bytes;
        var nullIndex = Array.IndexOf(bytes, (byte)0);
        if (nullIndex >= 0)
        {
            trimmed = bytes[..nullIndex];
        }

        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        foreach (var encoding in GetCandidateEncodings())
        {
            try
            {
                var text = encoding.GetString(trimmed);
                var candidate = text.TrimEnd('\0', ' ', '\r', '\n');
                if (!string.IsNullOrEmpty(candidate))
                {
                    return candidate;
                }
            }
            catch (DecoderFallbackException)
            {
                // Try the next encoding.
            }
        }

        return Encoding.Default.GetString(trimmed);
    }

    private static IEnumerable<Encoding> GetCandidateEncodings()
    {
        yield return Encoding.GetEncoding(936);
        yield return Encoding.GetEncoding("GBK");
        yield return Encoding.GetEncoding("GB2312");
        yield return Encoding.UTF8;
        yield return Encoding.Default;
    }
}
