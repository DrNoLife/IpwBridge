namespace IpwBridge.Services;

internal static class Hex
{
    /// <summary>Formats bytes as lowercase hexadecimal, as the Metazo checksums require.</summary>
    public static string ToLower(ReadOnlySpan<byte> bytes)
    {
#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(bytes);
#else
        return Convert.ToHexString(bytes).ToLowerInvariant();
#endif
    }
}
