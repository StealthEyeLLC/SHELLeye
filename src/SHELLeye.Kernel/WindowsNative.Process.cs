using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SHELLeye;

public static partial class WindowsNative
{
    public const uint PROCESS_TERMINATE = 0x0001;
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    public const uint SYNCHRONIZE = 0x00100000;
    public const uint STILL_ACTIVE = 259;
    private const int SystemBasicProcessInformation = 252; // empirically verified on target 26100 build against Microsoft-documented compact layout
    private const int ProcessTelemetryIdInformation = 64; // Microsoft-documented PROCESSINFOCLASS value
    private const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

    [StructLayout(LayoutKind.Sequential)] public struct FILETIME_NATIVE { public uint Low; public uint High; public readonly long ToLong() => ((long)High << 32) | Low; }
    [StructLayout(LayoutKind.Sequential)] private struct PROCESS_MEMORY_COUNTERS_EX
    {
        public uint cb, PageFaultCount; public nuint PeakWorkingSetSize, WorkingSetSize, QuotaPeakPagedPoolUsage, QuotaPagedPoolUsage, QuotaPeakNonPagedPoolUsage, QuotaNonPagedPoolUsage, PagefileUsage, PeakPagefileUsage, PrivateUsage;
    }
    [StructLayout(LayoutKind.Sequential)] private struct IO_COUNTERS { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }
    [StructLayout(LayoutKind.Sequential)] private struct PROCESS_TELEMETRY_ID_INFORMATION
    {
        public uint HeaderSize;
        public uint ProcessId;
        public ulong ProcessStartKey;
        public ulong CreateTime;
        public ulong CreateInterruptTime;
        public ulong CreateUnbiasedInterruptTime;
        public ulong ProcessSequenceNumber;
        public ulong SessionCreateTime;
        public uint SessionId;
        public uint BootId;
        public uint ImageChecksum;
        public uint ImageTimeDateStamp;
        public uint UserSidOffset;
        public uint ImagePathOffset;
        public uint PackageNameOffset;
        public uint RelativeAppNameOffset;
        public uint CommandLineOffset;
    }

    [DllImport("ntdll.dll")] private static extern int NtQuerySystemInformation(int cls, IntPtr info, int length, out int returnLength);
    [DllImport("ntdll.dll")] private static extern int NtQueryInformationProcess(IntPtr processHandle, int cls, IntPtr info, int length, out int returnLength);
    [DllImport("kernel32.dll", SetLastError=true)] public static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll", SetLastError=true)] public static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool GetProcessTimes(IntPtr h, out FILETIME_NATIVE create, out FILETIME_NATIVE exit, out FILETIME_NATIVE kernel, out FILETIME_NATIVE user);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool QueryFullProcessImageNameW(IntPtr h, uint flags, StringBuilder text, ref uint size);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool ProcessIdToSessionId(uint pid, out uint sessionId);
    [DllImport("kernel32.dll", SetLastError=true)] public static extern bool TerminateProcess(IntPtr h, uint exitCode);
    [DllImport("kernel32.dll", SetLastError=true)] public static extern uint WaitForSingleObject(IntPtr h, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool GetExitCodeProcess(IntPtr h, out uint code);
    [DllImport("kernel32.dll")] public static extern ulong GetTickCount64();
    [DllImport("psapi.dll", SetLastError=true)] private static extern bool GetProcessMemoryInfo(IntPtr h, ref PROCESS_MEMORY_COUNTERS_EX counters, uint cb);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool GetProcessIoCounters(IntPtr h, out IO_COUNTERS counters);

    public static IReadOnlyList<ProcessSnapshot> EnumerateBasicProcesses()
    {
        int size = 1024 * 1024;
        IntPtr buffer = IntPtr.Zero;
        try
        {
            while (true)
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
                buffer = Marshal.AllocHGlobal(size);
                int status = NtQuerySystemInformation(SystemBasicProcessInformation, buffer, size, out int needed);
                if (status == 0) break;
                if (status != STATUS_INFO_LENGTH_MISMATCH) throw new ShellEyeException("native_error", $"NtQuerySystemInformation(SystemBasicProcessInformation) failed: 0x{status:x8}", status);
                size = Math.Max(size * 2, needed + 65536);
                if (size > 64 * 1024 * 1024) throw new ShellEyeException("native_error", "Process inventory exceeded bounded native buffer.");
            }
            var result = new List<ProcessSnapshot>();
            int offset = 0;
            long lo = buffer.ToInt64(), hi = lo + size;
            while (offset + 48 <= size)
            {
                int next = Marshal.ReadInt32(buffer, offset);
                uint pid = unchecked((uint)Marshal.ReadIntPtr(buffer, offset + 8).ToInt64());
                uint ppid = unchecked((uint)Marshal.ReadIntPtr(buffer, offset + 16).ToInt64());
                ulong sequence = unchecked((ulong)Marshal.ReadInt64(buffer, offset + 24));
                ushort nameLength = unchecked((ushort)Marshal.ReadInt16(buffer, offset + 32));
                IntPtr namePtr = Marshal.ReadIntPtr(buffer, offset + 40);
                string name = pid == 0 ? "Idle" : "";
                long np = namePtr.ToInt64();
                if (nameLength > 0 && nameLength <= 32768 && np >= lo && np + nameLength <= hi)
                    name = Marshal.PtrToStringUni(namePtr, nameLength / 2) ?? "";
                result.Add(new ProcessSnapshot(pid, ppid, sequence, name));
                if (next == 0) break;
                if (next < 48 || offset + next <= offset || offset + next > size) throw new ShellEyeException("native_error", "Invalid SystemBasicProcessInformation record chain.");
                offset += next;
            }
            return result;
        }
        finally { if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer); }
    }

    public static ProcessTelemetry? TryQueryTelemetry(IntPtr processHandle)
    {
        int length = 4096; IntPtr p = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.WriteInt32(p, Marshal.SizeOf<PROCESS_TELEMETRY_ID_INFORMATION>());
            int status = NtQueryInformationProcess(processHandle, ProcessTelemetryIdInformation, p, length, out _);
            if (status != 0) return null;
            var t = Marshal.PtrToStructure<PROCESS_TELEMETRY_ID_INFORMATION>(p);
            return new ProcessTelemetry(t.ProcessId, t.ProcessSequenceNumber, t.SessionId, t.BootId, t.ProcessStartKey, unchecked((long)t.CreateTime));
        }
        catch { return null; }
        finally { Marshal.FreeHGlobal(p); }
    }

    public static ProcessTelemetry? TryQueryCurrentTelemetry()
    {
        using var h = OpenVerifiedCurrentProcessHandle();
        return TryQueryTelemetry(h.Handle);
    }

    private static VerifiedProcessHandle OpenVerifiedCurrentProcessHandle()
    {
        uint pid = unchecked((uint)Environment.ProcessId);
        IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE, false, pid);
        if (h == IntPtr.Zero) throw Win32("inaccessible", "OpenProcess(current) failed.");
        return new VerifiedProcessHandle(h, pid);
    }

    public static long QueryCreationFileTime(IntPtr h)
    {
        if (!GetProcessTimes(h, out var c, out _, out _, out _)) throw Win32("inaccessible", "GetProcessTimes failed.");
        return c.ToLong();
    }

    public static string? QueryExecutablePath(IntPtr h)
    {
        var sb = new StringBuilder(32768); uint n = (uint)sb.Capacity;
        return QueryFullProcessImageNameW(h, 0, sb, ref n) ? sb.ToString() : null;
    }

    public static uint QuerySessionId(uint pid) => ProcessIdToSessionId(pid, out var s) ? s : uint.MaxValue;

    public static VerifiedProcessHandle OpenVerifiedProcess(ProcessWitness witness, string currentBootEpoch, uint extraAccess = 0)
    {
        if (!StringComparer.Ordinal.Equals(witness.BootEpoch, currentBootEpoch)) throw new ShellEyeException("destroyed", "Retained process belongs to a different BootEpoch.");
        uint access = PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE | extraAccess;
        IntPtr h = OpenProcess(access, false, witness.Pid);
        if (h == IntPtr.Zero)
        {
            int e = Marshal.GetLastWin32Error();
            if (e == 87) throw new ShellEyeException("destroyed", "Retained process no longer exists.", e);
            if (e == 5) throw new ShellEyeException("inaccessible", "Windows denied exact process access.", e);
            throw new ShellEyeException("native_error", "OpenProcess failed.", e);
        }
        try
        {
            long creation = QueryCreationFileTime(h);
            if (creation != witness.CreationFileTimeUtc) throw new ShellEyeException("stale", "Process creation witness no longer matches retained incarnation.");
            var row = EnumerateBasicProcesses().FirstOrDefault(x => x.Pid == witness.Pid);
            if (row is null) throw new ShellEyeException("destroyed", "Retained process is absent from current process inventory.");
            if (row.SequenceNumber != witness.SequenceNumber) throw new ShellEyeException("stale", "Process sequence witness changed; PID was reused or binding is stale.");
            var telemetry = TryQueryTelemetry(h);
            if (telemetry is not null && telemetry.SequenceNumber != 0 && telemetry.SequenceNumber != witness.SequenceNumber)
                throw new ShellEyeException("stale", "Handle-bound process sequence witness does not match retained incarnation.");
            return new VerifiedProcessHandle(h, witness.Pid);
        }
        catch { CloseHandle(h); throw; }
    }

    public static object QueryProcessResources(ProcessWitness witness, string currentBootEpoch)
    {
        using var h = OpenVerifiedProcess(witness, currentBootEpoch);
        if (!GetProcessTimes(h.Handle, out _, out _, out var kernel, out var user)) throw Win32("inaccessible", "GetProcessTimes failed.");
        var mem = new PROCESS_MEMORY_COUNTERS_EX { cb = (uint)Marshal.SizeOf<PROCESS_MEMORY_COUNTERS_EX>() };
        bool hasMem = GetProcessMemoryInfo(h.Handle, ref mem, mem.cb);
        bool hasIo = GetProcessIoCounters(h.Handle, out var io);
        return new {
            processId = witness.Id,
            pid = witness.Pid,
            kernel100ns = kernel.ToLong(), user100ns = user.ToLong(),
            workingSetBytes = hasMem ? (ulong?)mem.WorkingSetSize : null,
            privateBytes = hasMem ? (ulong?)mem.PrivateUsage : null,
            readBytes = hasIo ? (ulong?)io.ReadTransferCount : null,
            writeBytes = hasIo ? (ulong?)io.WriteTransferCount : null,
            readOps = hasIo ? (ulong?)io.ReadOperationCount : null,
            writeOps = hasIo ? (ulong?)io.WriteOperationCount : null
        };
    }

    public static uint WaitProcess(VerifiedProcessHandle h, TimeSpan timeout)
    {
        uint ms = timeout == Timeout.InfiniteTimeSpan ? 0xffffffff : checked((uint)Math.Min(uint.MaxValue - 1, Math.Max(0, timeout.TotalMilliseconds)));
        uint w = WaitForSingleObject(h.Handle, ms);
        if (w == 0x102) throw new ShellEyeException("timeout", "Process wait timed out.");
        if (w != 0) throw Win32("native_error", "WaitForSingleObject failed.");
        if (!GetExitCodeProcess(h.Handle, out uint code)) throw Win32("native_error", "GetExitCodeProcess failed.");
        return code;
    }

    public static ShellEyeException Win32(string code, string message)
    {
        int e=Marshal.GetLastWin32Error(); return new ShellEyeException(code, message+" "+new Win32Exception(e).Message, e);
    }
}

public sealed class VerifiedProcessHandle : IDisposable
{
    private IntPtr _handle; public IntPtr Handle => _handle;
    public uint Pid { get; }
    internal VerifiedProcessHandle(IntPtr handle,uint pid){_handle=handle;Pid=pid;}
    public void Dispose(){var h=Interlocked.Exchange(ref _handle,IntPtr.Zero);if(h!=IntPtr.Zero)WindowsNative.CloseHandle(h);}
}


