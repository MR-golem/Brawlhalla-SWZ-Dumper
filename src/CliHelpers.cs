// ===
// File:         CliHelpers.cs
// Purpose:      Shared CLI/GUI helpers (argument parsing).
// ===

using System.Globalization;

namespace BrawlhallaSWZTool;

public static class CliHelpers
{
    /// <summary>
    /// Accepts: decimal ("827161004"), 0x-prefixed hex ("0x314FA36C"), bare hex ("314FA36C").
    /// </summary>
    public static uint ParseUInt32Arg(string s)
    {
        var span = s.AsSpan();
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.Parse(span[2..], NumberStyles.HexNumber);
        if (uint.TryParse(span, NumberStyles.None, null, out var dec))
            return dec;
        return uint.Parse(span, NumberStyles.HexNumber);
    }
}
