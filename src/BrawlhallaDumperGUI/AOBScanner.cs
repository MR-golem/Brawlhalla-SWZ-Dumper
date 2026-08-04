using System;
using System.Collections.Generic;

namespace BrawlhallaDumperGUI;

/// <summary>
/// Array-of-Bytes pattern scanner for process memory.
/// Supports wildcard bytes (?) in patterns.
/// </summary>
public static class AOBScanner
{
    public record ScanResult(IntPtr Address, byte[] MatchedBytes);

    /// <summary>
    /// Scan a contiguous memory buffer for a byte pattern with optional wildcards.
    /// </summary>
    public static List<IntPtr> FindPatternInBuffer(byte[] buffer, long bufferBase, byte[] pattern, byte[]? mask = null)
    {
        var results = new List<IntPtr>();
        if (pattern.Length == 0 || buffer.Length < pattern.Length) return results;

        // mask: 0xFF = must match, 0x00 = wildcard
        mask ??= new byte[pattern.Length];
        for (int i = 0; i < mask.Length; i++)
            if (mask[i] == 0) mask[i] = 0xFF; // default: all bytes must match

        for (int i = 0; i <= buffer.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (mask[j] != 0xFF && (buffer[i + j] & mask[j]) != (pattern[j] & mask[j]))
                {
                    found = false;
                    break;
                }
            }
            if (found)
                results.Add(new IntPtr(bufferBase + i));
        }
        return results;
    }

    /// <summary>
    /// Scan process memory for a byte pattern. Reads memory in chunks for efficiency.
    /// </summary>
    public static List<ScanResult> ScanProcess(IntPtr hProcess, IntPtr startAddr, IntPtr endAddr, byte[] pattern, byte[]? mask = null, int chunkSize = 0x100000)
    {
        var results = new List<ScanResult>();
        if (pattern.Length == 0) return results;

        mask ??= BuildMask(pattern);

        long start = startAddr.ToInt64();
        long end = endAddr.ToInt64();
        long chunk = chunkSize;

        for (long addr = start; addr < end; addr += chunk)
        {
            int size = (int)Math.Min(chunk + pattern.Length - 1, end - addr);
            if (size <= 0) break;

            var buf = new byte[size];
            if (!ProcessMemoryReader.ReadMemory(hProcess, new IntPtr(addr), buf, size))
                continue;

            var matches = FindPatternInBuffer(buf, addr, pattern, mask);
            foreach (var matchAddr in matches)
            {
                var matchBuf = new byte[pattern.Length];
                Array.Copy(buf, matchAddr.ToInt64() - addr, matchBuf, 0, pattern.Length);
                results.Add(new ScanResult(matchAddr, matchBuf));
            }
        }
        return results;
    }

    /// <summary>
    /// Scan only a specific module's memory range.
    /// </summary>
    public static List<ScanResult> ScanModule(IntPtr hProcess, IntPtr moduleBase, long moduleSize, byte[] pattern, byte[]? mask = null)
    {
        var end = IntPtr.Add(moduleBase, (int)moduleSize);
        return ScanProcess(hProcess, moduleBase, end, pattern, mask);
    }

    /// <summary>
    /// Build a wildcard mask from a pattern where 0xCC = wildcard.
    /// </summary>
    public static byte[] BuildMask(byte[] pattern, byte wildcard = 0xCC)
    {
        var mask = new byte[pattern.Length];
        for (int i = 0; i < pattern.Length; i++)
            mask[i] = pattern[i] == wildcard ? (byte)0x00 : (byte)0xFF;
        return mask;
    }

    /// <summary>
    /// Parse a pattern string like "F2 0F 10 ?? ?? ?? ?? ??" into bytes and mask.
    /// ?? = wildcard, everything else = hex byte.
    /// </summary>
    public static (byte[] pattern, byte[] mask) ParsePattern(string patternStr)
    {
        var tokens = patternStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var pattern = new byte[tokens.Length];
        var mask = new byte[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "??" || tokens[i] == "?")
            {
                pattern[i] = 0x00;
                mask[i] = 0x00; // wildcard
            }
            else
            {
                pattern[i] = Convert.ToByte(tokens[i], 16);
                mask[i] = 0xFF; // must match
            }
        }
        return (pattern, mask);
    }

    /// <summary>
    /// Extract a 32-bit signed offset from a pattern match at a given byte position.
    /// Used to read RIP-relative offsets from x86-64 instructions.
    /// </summary>
    public static int ExtractOffset32(byte[] matchedBytes, int offsetPosition)
    {
        if (offsetPosition + 4 > matchedBytes.Length) return 0;
        return BitConverter.ToInt32(matchedBytes, offsetPosition);
    }

    /// <summary>
    /// Read a 32-bit displacement from process memory at the match address + position.
    /// </summary>
    public static bool ReadDisplacement32(IntPtr hProcess, IntPtr matchAddress, int offsetPosition, out int displacement)
    {
        displacement = 0;
        var buf = new byte[4];
        IntPtr addr = IntPtr.Add(matchAddress, offsetPosition);
        if (!ProcessMemoryReader.ReadMemory(hProcess, addr, buf, 4))
            return false;
        displacement = BitConverter.ToInt32(buf, 0);
        return true;
    }

    /// <summary>
    /// Resolve a RIP-relative address: instruction_address + instruction_length + displacement.
    /// </summary>
    public static IntPtr ResolveRIPRelative(IntPtr instructionAddr, int instructionLength, int displacement)
    {
        return IntPtr.Add(instructionAddr, instructionLength + displacement);
    }
}
