using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BrawlhallaDumperGUI;

public static class ProcessMemoryReader
{
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint PROCESS_VM_OPERATION = 0x0008;

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern IntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

    [DllImport("kernel32.dll")]
    private static extern bool VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, byte[] lpBuffer, int dwLength);

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    private const uint MEM_COMMIT = 0x1000;
    private const uint PAGE_READONLY = 0x02;
    private const uint PAGE_READWRITE = 0x04;
    private const uint PAGE_EXECUTE_READ = 0x20;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;
    private const uint PAGE_GUARD = 0x100;

    public struct MemoryRegion
    {
        public IntPtr BaseAddress;
        public long RegionSize;
        public bool IsReadable;
        public bool IsExecutable;
    }

    public static IEnumerable<MemoryRegion> EnumerateReadableRegions(IntPtr hProcess, IntPtr start, IntPtr end)
    {
        IntPtr current = start;
        int mbiSize = Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();

        while (current.ToInt64() < end.ToInt64())
        {
            if (VirtualQueryEx(hProcess, current, out MEMORY_BASIC_INFORMATION mbi, (IntPtr)mbiSize) == IntPtr.Zero)
                break;

            if (mbi.State == MEM_COMMIT)
            {
                uint prot = mbi.Protect;
                bool readable = (prot & (PAGE_READONLY | PAGE_READWRITE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE)) != 0;
                bool executable = (prot & (PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE)) != 0;
                bool guarded = (prot & PAGE_GUARD) != 0;

                if (readable && !guarded)
                {
                    yield return new MemoryRegion
                    {
                        BaseAddress = mbi.BaseAddress,
                        RegionSize = mbi.RegionSize.ToInt64(),
                        IsReadable = readable,
                        IsExecutable = executable
                    };
                }
            }

            long next = mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64();
            if (next <= current.ToInt64()) break;
            current = new IntPtr(next);
        }
    }

    public static (IntPtr start, IntPtr end)? GetModuleRange(IntPtr moduleBase, long moduleSize)
    {
        if (moduleBase == IntPtr.Zero || moduleSize <= 0) return null;
        return (moduleBase, IntPtr.Add(moduleBase, (int)moduleSize));
    }

    public static Process? FindBrawlhallaProcess(int pid = -1)
    {
        if (pid > 0)
        {
            try { return Process.GetProcessById(pid); }
            catch { return null; }
        }

        var processes = Process.GetProcessesByName("Brawlhalla");
        return processes.Length > 0 ? processes[0] : null;
    }

    public static IntPtr GetModuleBase(Process process, string moduleName)
    {
        foreach (ProcessModule module in process.Modules)
        {
            if (string.Equals(module.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                return module.BaseAddress;
        }
        return IntPtr.Zero;
    }

    public static bool CloseProcessHandle(IntPtr hProcess)
    {
        return CloseHandle(hProcess);
    }

    public static IntPtr OpenProcess(Process process, bool writable = false)
    {
        uint access = PROCESS_VM_READ;
        if (writable) access |= PROCESS_VM_WRITE | PROCESS_VM_OPERATION;
        return OpenProcess(access, false, process.Id);
    }

    public static bool ReadMemory(IntPtr hProcess, IntPtr address, byte[] buffer, int size)
    {
        return ReadProcessMemory(hProcess, address, buffer, size, out _);
    }

    public static bool ReadInt32(IntPtr hProcess, IntPtr address, out int value)
    {
        var buf = new byte[4];
        if (ReadProcessMemory(hProcess, address, buf, 4, out _))
        {
            value = BitConverter.ToInt32(buf, 0);
            return true;
        }
        value = 0;
        return false;
    }

    public static bool ReadUInt32(IntPtr hProcess, IntPtr address, out uint value)
    {
        var buf = new byte[4];
        if (ReadProcessMemory(hProcess, address, buf, 4, out _))
        {
            value = BitConverter.ToUInt32(buf, 0);
            return true;
        }
        value = 0;
        return false;
    }

    public static bool ReadInt64(IntPtr hProcess, IntPtr address, out long value)
    {
        var buf = new byte[8];
        if (ReadProcessMemory(hProcess, address, buf, 8, out _))
        {
            value = BitConverter.ToInt64(buf, 0);
            return true;
        }
        value = 0;
        return false;
    }

    public static bool ReadDouble(IntPtr hProcess, IntPtr address, out double value)
    {
        var buf = new byte[8];
        if (ReadProcessMemory(hProcess, address, buf, 8, out _))
        {
            value = BitConverter.ToDouble(buf, 0);
            return true;
        }
        value = 0;
        return false;
    }

    public static bool ReadByte(IntPtr hProcess, IntPtr address, out byte value)
    {
        var buf = new byte[1];
        if (ReadProcessMemory(hProcess, address, buf, 1, out _))
        {
            value = buf[0];
            return true;
        }
        value = 0;
        return false;
    }

    public static bool WriteByte(IntPtr hProcess, IntPtr address, byte value)
    {
        var buf = new byte[] { value };
        return WriteProcessMemory(hProcess, address, buf, 1, out _);
    }

    public static bool WriteUInt32(IntPtr hProcess, IntPtr address, uint value)
    {
        var buf = BitConverter.GetBytes(value);
        return WriteProcessMemory(hProcess, address, buf, 4, out _);
    }

    // Resolve a multi-level pointer chain (64-bit pointers)
    // offsets: array of offsets to follow (last offset is the final offset, not dereferenced)
    public static IntPtr ResolvePointer(IntPtr hProcess, IntPtr baseAddress, int[] offsets)
    {
        IntPtr current = baseAddress;

        for (int i = 0; i < offsets.Length; i++)
        {
            current = IntPtr.Add(current, offsets[i]);

            if (i < offsets.Length - 1)
            {
                if (!ReadInt64(hProcess, current, out long next))
                    return IntPtr.Zero;
                current = new IntPtr(next);
            }
        }

        return current;
    }

    // Read a null-terminated UTF-8 string from process memory
    public static string? ReadString(IntPtr hProcess, IntPtr address, int maxLen = 256)
    {
        var buf = new byte[maxLen];
        if (!ReadProcessMemory(hProcess, address, buf, maxLen, out IntPtr bytesRead) || bytesRead.ToInt64() == 0)
            return null;
        int end = Array.IndexOf(buf, (byte)0);
        if (end < 0) end = (int)bytesRead;
        return System.Text.Encoding.UTF8.GetString(buf, 0, end);
    }
}
