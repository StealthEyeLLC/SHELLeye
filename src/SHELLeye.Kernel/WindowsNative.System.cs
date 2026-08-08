using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace SHELLeye;

public sealed record NativeListener(string AddressFamily,string Address,int Port,uint Pid,long? BindFileTimeUtc);
public sealed record NativeService(string Name,string State,uint Pid);
public sealed record NativeSession(uint SessionId,string? User,string? Domain,string State,bool Interactive);

public static partial class WindowsNative
{
    private const int AF_INET=2,AF_INET6=23,TCP_TABLE_OWNER_MODULE_LISTENER=6;
    private const uint SC_MANAGER_CONNECT=0x0001,SERVICE_QUERY_CONFIG=0x0001,SERVICE_QUERY_STATUS=0x0004,SC_STATUS_PROCESS_INFO=0;
    private const int WTSUserName=5,WTSDomainName=7,WTSConnectState=8;
    [StructLayout(LayoutKind.Sequential)] private struct SERVICE_STATUS_PROCESS { public uint serviceType,currentState,controlsAccepted,win32ExitCode,serviceSpecificExitCode,checkPoint,waitHint,processId,serviceFlags; }
    [DllImport("iphlpapi.dll", SetLastError=true)] private static extern uint GetExtendedTcpTable(IntPtr table,ref uint size,bool order,int af,int tableClass,uint reserved);
    [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern IntPtr OpenSCManagerW(string? machine,string? database,uint access);
    [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern IntPtr OpenServiceW(IntPtr scm,string name,uint access);
    [DllImport("advapi32.dll", SetLastError=true)] private static extern bool QueryServiceStatusEx(IntPtr service,uint infoLevel,IntPtr buffer,uint size,out uint needed);
    [DllImport("advapi32.dll")] private static extern bool CloseServiceHandle(IntPtr h);
    [DllImport("kernel32.dll")] private static extern uint WTSGetActiveConsoleSessionId();
    [DllImport("wtsapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool WTSQuerySessionInformationW(IntPtr server,uint sessionId,int infoClass,out IntPtr buffer,out uint bytes);
    [DllImport("wtsapi32.dll")] private static extern void WTSFreeMemory(IntPtr p);

    public static IReadOnlyList<NativeListener> QueryTcpListeners()
    {
        var list=new List<NativeListener>();QueryTcpTable(AF_INET,list);QueryTcpTable(AF_INET6,list);return list;
    }
    private static void QueryTcpTable(int af,List<NativeListener> output)
    {
        uint size=0;uint r=GetExtendedTcpTable(IntPtr.Zero,ref size,true,af,TCP_TABLE_OWNER_MODULE_LISTENER,0);if(r!=122&&r!=0)throw new ShellEyeException("native_error","GetExtendedTcpTable sizing failed.",(int)r);
        IntPtr p=Marshal.AllocHGlobal((int)size);try
        {
            r=GetExtendedTcpTable(p,ref size,true,af,TCP_TABLE_OWNER_MODULE_LISTENER,0);if(r!=0)throw new ShellEyeException("native_error","GetExtendedTcpTable failed.",(int)r);
            int count=Marshal.ReadInt32(p,0),off=8,rowSize=af==AF_INET?160:192;
            for(int i=0;i<count&&off+rowSize<=size;i++,off+=rowSize)
            {
                if(af==AF_INET)
                {
                    uint addr=unchecked((uint)Marshal.ReadInt32(p,off+4));uint portRaw=unchecked((uint)Marshal.ReadInt32(p,off+8));uint pid=unchecked((uint)Marshal.ReadInt32(p,off+20));long ft=Marshal.ReadInt64(p,off+24);
                    int port=(ushort)IPAddress.NetworkToHostOrder(unchecked((short)(portRaw&0xffff)));string address=new IPAddress(BitConverter.GetBytes(addr)).ToString();
                    output.Add(new NativeListener("IPv4",address,port,pid,ft>0?ft:null));
                }
                else
                {
                    byte[] addr=new byte[16];Marshal.Copy(IntPtr.Add(p,off),addr,0,16);uint scope=unchecked((uint)Marshal.ReadInt32(p,off+16));uint portRaw=unchecked((uint)Marshal.ReadInt32(p,off+20));uint pid=unchecked((uint)Marshal.ReadInt32(p,off+52));long ft=Marshal.ReadInt64(p,off+56);
                    int port=(ushort)IPAddress.NetworkToHostOrder(unchecked((short)(portRaw&0xffff)));string address=new IPAddress(addr,scope).ToString();output.Add(new NativeListener("IPv6",address,port,pid,ft>0?ft:null));
                }
            }
        }finally{Marshal.FreeHGlobal(p);}
    }

    public static NativeService QueryService(string name)
    {
        IntPtr scm=OpenSCManagerW(null,null,SC_MANAGER_CONNECT);if(scm==IntPtr.Zero)throw Win32("inaccessible","OpenSCManager failed.");
        try
        {
            IntPtr svc=OpenServiceW(scm,name,SERVICE_QUERY_STATUS|SERVICE_QUERY_CONFIG);if(svc==IntPtr.Zero){int e=Marshal.GetLastWin32Error();if(e==1060)throw new ShellEyeException("not_found","Windows service not found.",e);throw Win32("inaccessible","OpenService failed.");}
            try
            {
                int n=Marshal.SizeOf<SERVICE_STATUS_PROCESS>();IntPtr p=Marshal.AllocHGlobal(n);try{if(!QueryServiceStatusEx(svc,SC_STATUS_PROCESS_INFO,p,(uint)n,out _))throw Win32("native_error","QueryServiceStatusEx failed.");var s=Marshal.PtrToStructure<SERVICE_STATUS_PROCESS>(p);return new NativeService(name,ServiceStateName(s.currentState),s.processId);}finally{Marshal.FreeHGlobal(p);}
            }finally{CloseServiceHandle(svc);}
        }finally{CloseServiceHandle(scm);}
    }
    private static string ServiceStateName(uint s)=>s switch{1=>"stopped",2=>"start_pending",3=>"stop_pending",4=>"running",5=>"continue_pending",6=>"pause_pending",7=>"paused",_=>"unknown"};

    public static NativeSession QueryInteractiveSession()
    {
        uint sid=WTSGetActiveConsoleSessionId();string? user=QueryWtsString(sid,WTSUserName),domain=QueryWtsString(sid,WTSDomainName);string state="unknown";IntPtr p=IntPtr.Zero;
        if(WTSQuerySessionInformationW(IntPtr.Zero,sid,WTSConnectState,out p,out uint bytes)&&bytes>=4){int x=Marshal.ReadInt32(p);state=x switch{0=>"active",1=>"connected",2=>"connect_query",3=>"shadow",4=>"disconnected",5=>"idle",6=>"listen",7=>"reset",8=>"down",9=>"init",_=>"unknown"};WTSFreeMemory(p);}else if(p!=IntPtr.Zero)WTSFreeMemory(p);
        return new NativeSession(sid,user,domain,state,true);
    }
    private static string? QueryWtsString(uint sid,int cls){if(!WTSQuerySessionInformationW(IntPtr.Zero,sid,cls,out IntPtr p,out uint bytes))return null;try{return bytes>2?Marshal.PtrToStringUni(p):null;}finally{WTSFreeMemory(p);}}
}

