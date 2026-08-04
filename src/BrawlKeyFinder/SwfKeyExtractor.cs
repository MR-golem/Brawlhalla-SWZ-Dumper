// ===
// File:        SwfKeyExtractor.cs
// Purpose:     Reusable key-finder logic (Steam discovery + ABC key extraction).
// Dependencies: AbcDisassembler (vendored), AbcDisassembler.Swf (vendored)
// ===

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AbcDisassembler;
using AbcDisassembler.Instructions;
using AbcDisassembler.Multinames;
using AbcDisassembler.Swf;
using AbcDisassembler.Swf.Tags;

namespace BrawlKeyFinder;

public static class SwfKeyExtractor
{
    public const string SwfName = "BrawlhallaAir.swf";
    private const string AneClass = "ANE_RawData";
    private const string InitMethod = "Init";

    public static string? LocateSwf()
    {
        foreach (string brawlDir in EnumerateBrawlhallaDirs())
        {
            string candidate = Path.Combine(brawlDir, SwfName);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    public static uint? ExtractKey(string swfPath)
    {
        DoAbcTag? tag = ReadFirstDoAbcTag(swfPath);
        if (tag is null) return null;
        return FindDecryptionKey(tag.AbcFile);
    }

    private static DoAbcTag? ReadFirstDoAbcTag(string swfPath)
    {
        using FileStream fs = new(swfPath, FileMode.Open, FileAccess.Read);
        foreach (ITag t in SwfFile.ReadTags(fs))
        {
            if (t is DoAbcTag abcTag)
                return abcTag;
        }
        return null;
    }

    private static IEnumerable<string> EnumerateBrawlhallaDirs()
    {
        string x86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string x64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        foreach (string steamRoot in new[] { x86, x64 }
            .Select(p => Path.Combine(p, "Steam"))
            .Distinct())
        {
            string vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdfPath)) continue;

            foreach (string lib in ParseLibraryFolders(vdfPath))
            {
                string dir = Path.Combine(lib, "steamapps", "common", "Brawlhalla");
                if (Directory.Exists(dir))
                    yield return dir;
            }
        }
    }

    private static IEnumerable<string> ParseLibraryFolders(string vdfPath)
    {
        foreach (string line in File.ReadLines(vdfPath))
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                continue;

            int first = trimmed.IndexOf('"', 6);
            if (first < 0) continue;
            int second = trimmed.IndexOf('"', first + 1);
            if (second < 0) continue;

            string path = trimmed.Substring(first + 1, second - first - 1)
                                .Replace("\\\\", "\\");
            if (Directory.Exists(path))
                yield return path;
        }
    }

    private static uint? FindDecryptionKey(AbcFile abc)
    {
        foreach (MethodBodyInfo mb in abc.MethodBodies)
        {
            var instructions = System.Runtime.InteropServices.CollectionsMarshal
                .AsSpan(mb.Code);

            List<int> getlexPositions = FindGetlexPositions(
                abc.ConstantPool, AneClass, instructions);

            for (int i = 0; i < getlexPositions.Count; i++)
            {
                ReadOnlySpan<Instruction> slice = getlexPositions[i] == getlexPositions[^1]
                    ? instructions[getlexPositions[i]..]
                    : instructions[getlexPositions[i]..getlexPositions[i + 1]];

                int callPos = FindCallpropvoidPos(abc.ConstantPool, InitMethod, slice);
                if (callPos != -1)
                    return FindLastPushuintArg(instructions[..callPos]);
            }
        }
        return null;
    }

    private static List<int> FindGetlexPositions(
        CPoolInfo cpool, string lexName, ReadOnlySpan<Instruction> code)
    {
        List<int> result = [];
        for (int i = 0; i < code.Length; i++)
        {
            Instruction ins = code[i];
            if (ins.Name == "getlex" &&
                ins.Args[0].Value is INamedMultiname named &&
                cpool.Strings[(int)named.Name] == lexName)
                result.Add(i);
        }
        return result;
    }

    private static int FindCallpropvoidPos(
        CPoolInfo cpool, string methodName, ReadOnlySpan<Instruction> code)
    {
        for (int i = 0; i < code.Length; i++)
        {
            Instruction ins = code[i];
            if (ins.Name == "callpropvoid" &&
                ins.Args[0].Value is INamedMultiname named &&
                cpool.Strings[(int)named.Name] == methodName)
                return i;
        }
        return -1;
    }

    private static uint? FindLastPushuintArg(ReadOnlySpan<Instruction> code)
    {
        for (int i = code.Length - 1; i >= 0; i--)
        {
            if (code[i].Name == "pushuint")
                return (uint)code[i].Args[0].Value;
        }
        return null;
    }
}

public static class ClipboardHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    public static bool SetText(string text)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        try
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");
            IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            if (hMem == IntPtr.Zero) return false;

            IntPtr ptr = GlobalLock(hMem);
            if (ptr == IntPtr.Zero) return false;
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            GlobalUnlock(hMem);

            if (!OpenClipboard(IntPtr.Zero)) return false;
            EmptyClipboard();
            SetClipboardData(CF_UNICODETEXT, hMem);
            CloseClipboard();
            return true;
        }
        catch
        {
            return false;
        }
    }
}