using System;
using System.Collections.Generic;

namespace BrawlhallaDumperGUI;

/// <summary>
/// A named AOB pattern with instructions on how to extract offsets from the match.
/// </summary>
public class PatternSignature
{
    public string Name { get; }
    public string PatternStr { get; }
    public byte[] Pattern { get; }
    public byte[] Mask { get; }
    public string Description { get; }

    public Dictionary<string, (int BytePosition, OffsetType Type)> Extractors { get; } = new();

    public enum OffsetType
    {
        Displacement32,
        Int32,
        UInt32
    }

    public PatternSignature(string name, string patternStr, string description = "")
    {
        Name = name;
        PatternStr = patternStr;
        Description = description;
        (Pattern, Mask) = AOBScanner.ParsePattern(patternStr);
    }

    public PatternSignature WithOffset(string offsetName, int bytePosition, OffsetType type = OffsetType.Displacement32)
    {
        Extractors[offsetName] = (bytePosition, type);
        return this;
    }

    public Dictionary<string, int?> ExtractOffsets(byte[] matchedBytes)
    {
        var results = new Dictionary<string, int?>();
        foreach (var kv in Extractors)
        {
            int pos = kv.Value.BytePosition;
            var type = kv.Value.Type;

            if (pos + 4 > matchedBytes.Length)
            {
                results[kv.Key] = null;
                continue;
            }

            results[kv.Key] = type switch
            {
                OffsetType.Displacement32 => BitConverter.ToInt32(matchedBytes, pos),
                OffsetType.Int32 => BitConverter.ToInt32(matchedBytes, pos),
                OffsetType.UInt32 => unchecked((int)BitConverter.ToUInt32(matchedBytes, pos)),
                _ => null
            };
        }
        return results;
    }

    public override string ToString() => $"{Name}: {PatternStr} ({Description})";
}

/// <summary>
/// Known Brawlhalla AOB patterns.
/// Only patterns verified against the current game version are included.
/// </summary>
public static class BrawlhallaPatterns
{
    // =====================================================================
    // ENTITY-LIST HOOK — the key pattern for finding entities
    // AOB: F2 0F 10 8B 40 01 00 00  (movsd xmm0,[rbx+140])
    // This reads Entity_X from the entity rendering loop.
    // From this instruction we can derive:
    //   rbx = entity base pointer
    //   [rbx+140] = Entity_X (double)
    // =====================================================================
    public static readonly PatternSignature EntityXAccess = new PatternSignature(
        "EntityXAccess",
        "F2 0F 10 8B 40 01 00 00",
        "movsd xmm0, [rbx+140h] — reads Entity X position");

    // =====================================================================
    // ENTITY LIST POINTER — rip-relative access to entity array
    // Pattern: 48 8B 05 xx xx xx xx  (mov rax, [rip+disp32])
    // followed by test rax, rax and array access
    // =====================================================================
    public static readonly PatternSignature EntityListPointer = new PatternSignature(
        "EntityListPointer",
        "48 8B 05 ?? ?? ?? ?? 48 85 C0",
        "mov rax, [rip+disp32] — entity list base")
        .WithOffset("RIP_Displacement", 3, PatternSignature.OffsetType.Displacement32);

    // =====================================================================
    // ALL PATTERNS
    // =====================================================================
    public static readonly PatternSignature[] All = new[]
    {
        EntityXAccess,
        EntityListPointer
    };
}
