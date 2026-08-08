using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace SHELLeye;

public static partial class WindowsNative
{
    public const uint JOB_OBJECT_ASSIGN_PROCESS=0x0001, JOB_OBJECT_SET_ATTRIBUTES=0x0002, JOB_OBJECT_QUERY=0x0004, JOB_OBJECT_TERMINATE=0x0008;
    private const int JobObjectBasicProcessIdList=3, JobObjectAssociateCompletionPortInformation=7;
    private const uint EXTENDED_STARTUPINFO_PRESENT=0x00080000, CREATE_UNICODE_ENVIRONMENT=0x00000400, CREATE_NO_WINDOW=0x08000000;
    private const uint STARTF_USESTDHANDLES=0x00000100, DUPLICATE_SAME_ACCESS=0x00000002;
    private static readonly IntPtr PROC_THREAD_ATTRIBUTE_HANDLE_LIST=(IntPtr)0x00020002;
    private static readonly IntPtr PROC_THREAD_ATTRIBUTE_JOB_LIST=(IntPtr)0x0002000D;
    private const uint GENERIC_READ=0x80000000, FILE_APPEND_DATA=0x00000004;
    private const uint FILE_SHARE_READ=1, FILE_SHARE_WRITE=2, FILE_SHARE_DELETE=4, OPEN_EXISTING=3, OPEN_ALWAYS=4, FILE_ATTRIBUTE_NORMAL=0x80;

    [StructLayout(LayoutKind.Sequential)] private struct SECURITY_ATTRIBUTES { public int nLength; public IntPtr lpSecurityDescriptor; [MarshalAs(UnmanagedType.Bool)] public bool bInheritHandle; }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] private struct STARTUPINFO
    {
        public int cb; public string? lpReserved, lpDesktop, lpTitle; public int dwX,dwY,dwXSize,dwYSize,dwXCountChars,dwYCountChars,dwFillAttribute; public uint dwFlags; public short wShowWindow, cbReserved2; public IntPtr lpReserved2,hStdInput,hStdOutput,hStdError;
    }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] private struct STARTUPINFOEX { public STARTUPINFO StartupInfo; public IntPtr lpAttributeList; }
    [StructLayout(LayoutKind.Sequential)] private struct PROCESS_INFORMATION { public IntPtr hProcess,hThread; public uint dwProcessId,dwThreadId; }
    [StructLayout(LayoutKind.Sequential)] private struct JOBOBJECT_ASSOCIATE_COMPLETION_PORT { public IntPtr CompletionKey, CompletionPort; }

    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern IntPtr CreateJobObjectW(IntPtr attrs,string? name);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern IntPtr OpenJobObjectW(uint access,bool inherit,string name);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool SetInformationJobObject(IntPtr job,int infoClass,IntPtr info,uint length);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool QueryInformationJobObject(IntPtr job,int infoClass,IntPtr info,uint length,out uint returnLength);
    [DllImport("kernel32.dll", SetLastError=true)] public static extern bool TerminateJobObject(IntPtr job,uint exitCode);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern IntPtr CreateIoCompletionPort(IntPtr file,IntPtr existing,UIntPtr key,uint threads);
    [DllImport("kernel32.dll", SetLastError=true)] public static extern bool GetQueuedCompletionStatus(IntPtr port,out uint message,out UIntPtr key,out IntPtr overlapped,uint milliseconds);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool InitializeProcThreadAttributeList(IntPtr list,int count,uint flags,ref IntPtr size);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool UpdateProcThreadAttribute(IntPtr list,uint flags,IntPtr attribute,IntPtr value,IntPtr size,IntPtr previous,IntPtr returnSize);
    [DllImport("kernel32.dll")] private static extern void DeleteProcThreadAttributeList(IntPtr list);
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool DuplicateHandle(IntPtr sourceProcess,IntPtr sourceHandle,IntPtr targetProcess,out IntPtr targetHandle,uint desiredAccess,bool inherit,uint options);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool CreateProcessW(string? app,StringBuilder commandLine,IntPtr processAttributes,IntPtr threadAttributes,bool inheritHandles,uint creationFlags,IntPtr environment,string? currentDirectory,ref STARTUPINFOEX startupInfo,out PROCESS_INFORMATION processInformation);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern IntPtr CreateFileW(string file,uint access,uint share,ref SECURITY_ATTRIBUTES sa,uint disposition,uint flags,IntPtr template);

    public static IntPtr CreatePersistentJob(string nativeName,out IntPtr completionPort)
    {
        IntPtr job=CreateJobObjectW(IntPtr.Zero,nativeName);
        if(job==IntPtr.Zero) throw Win32("native_error","CreateJobObject failed.");
        completionPort=IntPtr.Zero;
        try
        {
            completionPort=CreateIoCompletionPort(new IntPtr(-1),IntPtr.Zero,UIntPtr.Zero,1);
            if(completionPort==IntPtr.Zero) throw Win32("native_error","CreateIoCompletionPort failed.");
            var assoc=new JOBOBJECT_ASSOCIATE_COMPLETION_PORT{CompletionKey=job,CompletionPort=completionPort};
            IntPtr p=Marshal.AllocHGlobal(Marshal.SizeOf<JOBOBJECT_ASSOCIATE_COMPLETION_PORT>());
            try { Marshal.StructureToPtr(assoc,p,false); if(!SetInformationJobObject(job,JobObjectAssociateCompletionPortInformation,p,(uint)Marshal.SizeOf<JOBOBJECT_ASSOCIATE_COMPLETION_PORT>())) throw Win32("native_error","Job completion-port association failed."); }
            finally{Marshal.FreeHGlobal(p);}
            return job;
        }
        catch { if(completionPort!=IntPtr.Zero)CloseHandle(completionPort);CloseHandle(job);throw; }
    }

    public static IntPtr OpenPersistentJob(string nativeName)
    {
        IntPtr job=OpenJobObjectW(JOB_OBJECT_QUERY|JOB_OBJECT_TERMINATE|JOB_OBJECT_ASSIGN_PROCESS|JOB_OBJECT_SET_ATTRIBUTES,false,nativeName);
        if(job==IntPtr.Zero){int e=Marshal.GetLastWin32Error();if(e==2||e==6)throw new ShellEyeException("destroyed","Native Job Object no longer exists.",e);throw Win32("native_error","OpenJobObject failed.");}
        return job;
    }

    public static IReadOnlyList<uint> QueryJobProcessIds(IntPtr job)
    {
        int size=64*1024;IntPtr p=Marshal.AllocHGlobal(size);
        try
        {
            if(!QueryInformationJobObject(job,JobObjectBasicProcessIdList,p,(uint)size,out _)) throw Win32("native_error","QueryInformationJobObject(process list) failed.");
            uint inList=unchecked((uint)Marshal.ReadInt32(p,4));var ids=new List<uint>((int)inList);int off=8;
            for(int i=0;i<inList && off+IntPtr.Size<=size;i++,off+=IntPtr.Size) ids.Add(unchecked((uint)Marshal.ReadIntPtr(p,off).ToInt64()));
            return ids;
        }
        finally{Marshal.FreeHGlobal(p);}
    }

    private static IntPtr CreateInheritedFile(string path,uint access,uint disposition)
    {
        var parent=Path.GetDirectoryName(path);if(!String.IsNullOrEmpty(parent))Directory.CreateDirectory(parent);
        var sa=new SECURITY_ATTRIBUTES{nLength=Marshal.SizeOf<SECURITY_ATTRIBUTES>(),bInheritHandle=true};
        IntPtr h=CreateFileW(path,access,FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE,ref sa,disposition,FILE_ATTRIBUTE_NORMAL,IntPtr.Zero);
        if(h==new IntPtr(-1)) throw Win32("native_error","CreateFile for inherited stream failed.");
        return h;
    }

    public static LaunchedProcess CreateProcessInJob(IntPtr job,string executable,IReadOnlyList<string> args,string cwd,string stdoutPath,string stderrPath)
    {
        IntPtr stdout=IntPtr.Zero,stderr=IntPtr.Zero,stdin=IntPtr.Zero,list=IntPtr.Zero,jobValue=IntPtr.Zero,handlesValue=IntPtr.Zero;
        try
        {
            stdout=CreateInheritedFile(stdoutPath,FILE_APPEND_DATA,OPEN_ALWAYS);
            stderr=CreateInheritedFile(stderrPath,FILE_APPEND_DATA,OPEN_ALWAYS);
            stdin=CreateInheritedFile("NUL",GENERIC_READ,OPEN_EXISTING);
            IntPtr size=IntPtr.Zero;InitializeProcThreadAttributeList(IntPtr.Zero,2,0,ref size);
            list=Marshal.AllocHGlobal(size);if(!InitializeProcThreadAttributeList(list,2,0,ref size))throw Win32("native_error","InitializeProcThreadAttributeList failed.");
            jobValue=Marshal.AllocHGlobal(IntPtr.Size);Marshal.WriteIntPtr(jobValue,job);
            if(!UpdateProcThreadAttribute(list,0,PROC_THREAD_ATTRIBUTE_JOB_LIST,jobValue,(IntPtr)IntPtr.Size,IntPtr.Zero,IntPtr.Zero))throw Win32("native_error","PROC_THREAD_ATTRIBUTE_JOB_LIST failed.");
            handlesValue=Marshal.AllocHGlobal(IntPtr.Size*3);Marshal.WriteIntPtr(handlesValue,0,stdin);Marshal.WriteIntPtr(handlesValue,IntPtr.Size,stdout);Marshal.WriteIntPtr(handlesValue,IntPtr.Size*2,stderr);
            if(!UpdateProcThreadAttribute(list,0,PROC_THREAD_ATTRIBUTE_HANDLE_LIST,handlesValue,(IntPtr)(IntPtr.Size*3),IntPtr.Zero,IntPtr.Zero))throw Win32("native_error","PROC_THREAD_ATTRIBUTE_HANDLE_LIST failed.");
            var si=new STARTUPINFOEX{lpAttributeList=list};si.StartupInfo.cb=Marshal.SizeOf<STARTUPINFOEX>();si.StartupInfo.dwFlags=STARTF_USESTDHANDLES;si.StartupInfo.hStdInput=stdin;si.StartupInfo.hStdOutput=stdout;si.StartupInfo.hStdError=stderr;
            var all=new List<string>{executable};all.AddRange(args);var command=new StringBuilder(string.Join(" ",all.Select(QuoteWindowsArg)));
            if(!CreateProcessW(executable,command,IntPtr.Zero,IntPtr.Zero,true,EXTENDED_STARTUPINFO_PRESENT|CREATE_UNICODE_ENVIRONMENT|CREATE_NO_WINDOW,IntPtr.Zero,cwd,ref si,out var pi))throw Win32("native_error","CreateProcessW with creation-time Job Object assignment failed.");
            if(!DuplicateHandle(GetCurrentProcess(),job,pi.hProcess,out _,0,false,DUPLICATE_SAME_ACCESS)){int error=Marshal.GetLastWin32Error();try{TerminateProcess(pi.hProcess,219);}catch{}CloseHandle(pi.hThread);CloseHandle(pi.hProcess);throw new ShellEyeException("native_error","Could not anchor persistent Job Object lifetime in the created workload process.",error);}
            CloseHandle(pi.hThread);
            return new LaunchedProcess(pi.hProcess,pi.dwProcessId,pi.dwThreadId,stdoutPath,stderrPath);
        }
        finally
        {
            if(list!=IntPtr.Zero)DeleteProcThreadAttributeList(list);
            if(jobValue!=IntPtr.Zero)Marshal.FreeHGlobal(jobValue);if(handlesValue!=IntPtr.Zero)Marshal.FreeHGlobal(handlesValue);if(list!=IntPtr.Zero)Marshal.FreeHGlobal(list);
            if(stdin!=IntPtr.Zero&&stdin!=new IntPtr(-1))CloseHandle(stdin);if(stdout!=IntPtr.Zero&&stdout!=new IntPtr(-1))CloseHandle(stdout);if(stderr!=IntPtr.Zero&&stderr!=new IntPtr(-1))CloseHandle(stderr);
        }
    }

    public static string QuoteWindowsArg(string arg)
    {
        if(arg.Length>0 && !arg.Any(ch=>char.IsWhiteSpace(ch)||ch=='\"')) return arg;
        var sb=new StringBuilder();sb.Append('\"');int slashes=0;
        foreach(char ch in arg){if(ch=='\\'){slashes++;continue;}if(ch=='\"'){sb.Append('\\',slashes*2+1);sb.Append('\"');slashes=0;continue;}sb.Append('\\',slashes);slashes=0;sb.Append(ch);}sb.Append('\\',slashes*2);sb.Append('\"');return sb.ToString();
    }
}

public sealed class LaunchedProcess : IDisposable
{
    private IntPtr _processHandle; public IntPtr ProcessHandle => _processHandle; public uint Pid{get;} public uint ThreadId{get;} public string StdoutPath{get;} public string StderrPath{get;}
    internal LaunchedProcess(IntPtr h,uint pid,uint tid,string stdout,string stderr){_processHandle=h;Pid=pid;ThreadId=tid;StdoutPath=stdout;StderrPath=stderr;}
    public void Dispose(){var h=Interlocked.Exchange(ref _processHandle,IntPtr.Zero);if(h!=IntPtr.Zero)WindowsNative.CloseHandle(h);}
}






