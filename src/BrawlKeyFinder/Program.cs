// ===
// File:        Program.cs
// Purpose:     CLI entry point for BrawlKeyFinder — locate BrawlhallaAir.swf,
//              extract the SWZ decryption key, copy it to clipboard, exit.
// Dependencies: AbcDisassembler (vendored), AbcDisassembler.Swf (vendored),
//               SwfKeyExtractor.cs (key logic), ClipboardHelper.cs (P/Invoke)
// ===

using System;
using System.Text;
using System.Threading;

namespace BrawlKeyFinder;

internal static class Program
{
    static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        PrintBanner();

        string? swfPath = SwfKeyExtractor.LocateSwf();
        if (swfPath is null)
        {
            Error("BrawlhallaAir.swf not found. Is Brawlhalla installed via Steam?");
            Pause();
            return 1;
        }

        Status($"Found: {swfPath}");
        Status("Parsing SWF...");

        uint? key;
        try
        {
            key = SwfKeyExtractor.ExtractKey(swfPath);
        }
        catch (Exception ex)
        {
            Error($"Parse failed: {ex.Message}");
            Pause();
            return 2;
        }

        if (key is null)
        {
            Error("Key pattern not found inside ABC bytecode.");
            Pause();
            return 3;
        }

        string keyStr = key.Value.ToString();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"  SWZ KEY  →  {keyStr}");
        Console.ResetColor();
        Console.WriteLine();

        bool copied = ClipboardHelper.SetText(keyStr);
        if (copied)
            Status("Copied to clipboard.");
        else
            Warn("Clipboard unavailable — copy manually from above.");

        Countdown(5);
        return 0;
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(@"
  ╔══════════════════════════════════════╗
  ║     B R A W L  K E Y  F I N D E R  ║
  ╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void Status(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  [*] {msg}");
        Console.ResetColor();
    }

    private static void Warn(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  [!] {msg}");
        Console.ResetColor();
    }

    private static void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [X] {msg}");
        Console.ResetColor();
    }

    private static void Countdown(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            Console.Write($"\r  Closing in {i}s... ");
            Thread.Sleep(1000);
        }
        Console.WriteLine();
    }

    private static void Pause()
    {
        Console.WriteLine("  Press any key to exit.");
        Console.ReadKey(intercept: true);
    }
}
