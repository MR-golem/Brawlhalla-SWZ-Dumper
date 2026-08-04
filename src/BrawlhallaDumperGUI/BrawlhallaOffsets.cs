using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BrawlhallaDumperGUI;

/// <summary>
/// Holds Brawlhalla memory offsets — both hardcoded defaults and dynamically discovered ones.
/// </summary>
public static class BrawlhallaOffsets
{
    // ============================================================
    // DISCOVERY STATUS
    // ============================================================
    public static bool OffsetsDiscovered { get; private set; }
    public static string? LastDiscoveryLog { get; private set; }

    // ============================================================
    // ENTITY FIELD OFFSETS (relative to entity base pointer)
    // These are verified for the current Brawlhalla version.
    // ============================================================
    public static int Entity_X { get; private set; } = 0x140;           // double
    public static int Entity_Y { get; private set; } = 0x148;           // double (up is NEGATIVE)
    public static int Entity_LightAttackFlag { get; private set; } = 0x30;   // byte
    public static int Entity_HeavyAttackFlag { get; private set; } = 0x34;   // byte
    public static int Entity_AttackingFlag { get; private set; } = 0x50;     // byte
    public static int Entity_AttackId { get; private set; } = 0x6C;          // int (PowerID)
    public static int Entity_StorePtr { get; private set; } = 0xC0;          // pointer
    public static int Entity_Direction { get; private set; } = 0xD4;         // int (1=LEFT, 0=RIGHT)
    public static int Entity_InAir { get; private set; } = 0x108;            // int (1 if air)
    public static int Entity_Team { get; private set; } = 0x1A8;             // int
    public static int Entity_JumpsUsed { get; private set; } = 0x22C;        // int
    public static int Entity_LocalPlayerMarker { get; private set; } = 0x25C; // int
    public static int Entity_DamageTaken { get; private set; } = 0x5D8;      // double

    // ============================================================
    // CAMERA FIELD OFFSETS (relative to camera object base)
    // ============================================================
    public static int Camera_Zoom { get; private set; } = 0x20;     // double
    public static int Camera_X { get; private set; } = 0x38;        // double
    public static int Camera_Y { get; private set; } = 0x40;        // double
    public static int Camera_Center { get; private set; } = 0x48;   // double

    // ============================================================
    // G_INPUT FIELD OFFSET
    // ============================================================
    public static int GInput_Value { get; private set; } = 0x34;    // int (bitmask)

    // ============================================================
    // POINTER CHAINS (static offsets in Adobe AIR.dll)
    // These MUST match the current game version.
    // Update these with Cheat Engine if the chain fails.
    // ============================================================
    public static int[] CameraChain { get; private set; } = { 0x120E728, 0x1F8, 0x468, 0x118, 0x100, 0x690, 0x30 };
    public static int[] LocalPlayerChain { get; private set; } = { 0x01315528, 0xA4, 0x44C, 0x14, 0x98, 0x98, 0x548 };
    public static int[] GInputChain { get; private set; } = { 0x0131550C, 0x1DC, 0x18, 0x8, 0x98, 0x1F8, 0x4CC };

    // ============================================================
    // INPUT BITMASK VALUES
    // ============================================================
    public const int INPUT_QUICK_ATTACK = 640;
    public const int INPUT_HEAVY_ATTACK = 64;
    public const int INPUT_UP = 17;
    public const int INPUT_DOWN = 2;
    public const int INPUT_LEFT = 4;
    public const int INPUT_RIGHT = 8;
    public const int INPUT_DODGE = 256;
    public const int INPUT_THROW = 516;

    // ============================================================
    // OLD OFFSETS (kept for reference only)
    // ============================================================
    public const int OldEntity_X = 0x378;
    public const int OldEntity_Y = 0x370;
    public const int OldEntity_XVel = 0x328;
    public const int OldEntity_YVel = 0x320;
    public const int OldEntity_Damage = 0x418;
    public const int OldEntity_JumpCount = 0x1F0;
    public const int OldEntity_Direction = 0xD4;
    public const int OldEntity_Stun = 0x184;
    public const int OldEntity_Edging = 0x118;
    public const int OldEntity_InAnimation = 0xA0;
    public const int OldEntity_InAttack = 0x88;
    public const int OldEntity_InAir = 0x108;
    public const int OldEntity_NotGrounded = 0xF4;
    public const int OldEntity_Dodge = 0x154;
    public const int OldEntity_IncreasedGravity = 0xC4;

    // ============================================================
    // DYNAMIC OFFSET DISCOVERY
    // ============================================================

    /// <summary>
    /// Scan for AOB patterns to verify/update offsets.
    /// Also tries to find entity list and player pointer.
    /// </summary>
    public static string DiscoverOffsets(
        IntPtr hProcess,
        IntPtr airDllBase, long airDllSize,
        IntPtr gameExeBase, long gameExeSize)
    {
        var log = new List<string>();
        bool anyFound = false;

        log.WriteLine("=== AOB Offset Discovery ===");

        if (airDllBase != IntPtr.Zero && airDllSize > 0)
        {
            log.WriteLine($"Scanning Adobe AIR.dll (0x{airDllBase.ToInt64():X}, {airDllSize / 1024}KB)...");

            // Verify Entity_X pattern exists
            var results = AOBScanner.ScanModule(hProcess, airDllBase, airDllSize,
                BrawlhallaPatterns.EntityXAccess.Pattern,
                BrawlhallaPatterns.EntityXAccess.Mask);

            if (results.Count > 0)
            {
                log.WriteLine($"  [FOUND] Entity_XAccess pattern at 0x{results[0].Address.ToInt64():X} — offset 0x{Entity_X:X} verified");
                anyFound = true;
            }
            else
            {
                log.WriteLine($"  [INFO] Entity_XAccess pattern not found — using default 0x{Entity_X:X}");
            }

            // Scan for entity list pointer
            results = AOBScanner.ScanModule(hProcess, airDllBase, airDllSize,
                BrawlhallaPatterns.EntityListPointer.Pattern,
                BrawlhallaPatterns.EntityListPointer.Mask);

            if (results.Count > 0)
            {
                var offsets = BrawlhallaPatterns.EntityListPointer.ExtractOffsets(results[0].MatchedBytes);
                if (offsets.TryGetValue("RIP_Displacement", out var disp) && disp.HasValue)
                {
                    var resolved = AOBScanner.ResolveRIPRelative(results[0].Address, 7, disp.Value);
                    log.WriteLine($"  [FOUND] EntityList at 0x{resolved.ToInt64():X} (from 0x{results[0].Address.ToInt64():X} + disp {disp.Value})");
                    anyFound = true;
                }
            }
            else
            {
                log.WriteLine($"  [INFO] EntityListPointer pattern not found");
            }
        }
        else
        {
            log.WriteLine("Adobe AIR.dll not found — skipping scan");
        }

        // Verify camera chain resolves
        if (airDllBase != IntPtr.Zero)
        {
            IntPtr camPtr = ProcessMemoryReader.ResolvePointer(hProcess, airDllBase, CameraChain);
            if (camPtr != IntPtr.Zero)
            {
                if (ProcessMemoryReader.ReadDouble(hProcess, IntPtr.Add(camPtr, Camera_Zoom), out double zoom))
                {
                    log.WriteLine($"  [OK] Camera chain resolved — Zoom={zoom:F4}");
                    anyFound = true;
                }
                else
                {
                    log.WriteLine($"  [WARN] Camera chain resolved but zoom read failed — chain may be stale");
                }
            }
            else
            {
                log.WriteLine($"  [WARN] Camera chain failed — pointer chain is stale for this game version");
            }

            // Verify player chain resolves
            IntPtr playerPtr = ProcessMemoryReader.ResolvePointer(hProcess, airDllBase, LocalPlayerChain);
            if (playerPtr != IntPtr.Zero)
            {
                if (ProcessMemoryReader.ReadDouble(hProcess, IntPtr.Add(playerPtr, Entity_X), out double px))
                {
                    log.WriteLine($"  [OK] Player chain resolved — X={px:F4}");
                    anyFound = true;
                }
                else
                {
                    log.WriteLine($"  [WARN] Player chain resolved but X read failed — chain may be stale");
                }
            }
            else
            {
                log.WriteLine($"  [WARN] Player chain failed — pointer chain is stale for this game version");
            }

            // Verify input chain resolves
            IntPtr inputPtr = ProcessMemoryReader.ResolvePointer(hProcess, airDllBase, GInputChain);
            if (inputPtr != IntPtr.Zero)
            {
                if (ProcessMemoryReader.ReadInt32(hProcess, IntPtr.Add(inputPtr, GInput_Value), out int inputVal))
                {
                    log.WriteLine($"  [OK] Input chain resolved — Value={inputVal}");
                    anyFound = true;
                }
                else
                {
                    log.WriteLine($"  [WARN] Input chain resolved but read failed");
                }
            }
            else
            {
                log.WriteLine($"  [WARN] Input chain failed — pointer chain is stale for this game version");
            }
        }

        OffsetsDiscovered = anyFound;
        LastDiscoveryLog = string.Join("\n", log);

        log.WriteLine(anyFound
            ? "=== Discovery complete ==="
            : "=== Discovery complete: all defaults used ===");

        return LastDiscoveryLog;
    }
}

internal static class LogExtensions
{
    public static void WriteLine(this List<string> list, string line) => list.Add(line);
}
