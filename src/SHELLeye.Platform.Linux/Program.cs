using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace SHELLeye.Platform.Linux;

internal static class Program
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web){PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
    private static readonly string ProviderEpoch="linux_provider_"+Guid.NewGuid().ToString("N");
    private static readonly ConcurrentDictionary<string,HeldFile> Files=new();
    private static readonly ConcurrentDictionary<int,Process> Spawned=new();
    private static string _providerKey="unbound",_distro="unknown";

    private sealed class HeldFile:IDisposable
    {
        public string Token{get;}public int Fd{get;}public bool CanWrite{get;}public string Path{get;set;}public LinuxNative.ExportedHandle? ExportedHandle{get;}
        public HeldFile(string token,int fd,bool canWrite,string path,LinuxNative.ExportedHandle? h){Token=token;Fd=fd;CanWrite=canWrite;Path=path;ExportedHandle=h;}public void Dispose()=>LinuxNative.close(Fd);
    }

    public static async Task<int> Main(string[] args)
    {
        if(!OperatingSystem.IsLinux()){Console.Error.WriteLine("SHELLeye.Platform.Linux requires Linux.");return 64;}
        if(args.Length>0&&args[0]=="--launch-proxy")return RunLaunchProxy(args);
        if(args.Length>0&&args[0]=="--exec-fixture")return RunExecFixture(args);
        for(int i=0;i<args.Length;i++){if(args[i]=="--provider-key"&&i+1<args.Length)_providerKey=args[++i];else if(args[i]=="--distro"&&i+1<args.Length)_distro=args[++i];}
        if(!args.Contains("--server",StringComparer.Ordinal)){Console.Error.WriteLine("Expected --server.");return 64;}
        Console.WriteLine(JsonSerializer.Serialize(new{type="shelleye.linux.ready",providerEpoch=ProviderEpoch,pid=Environment.ProcessId,providerKey=_providerKey,distro=_distro},Json));Console.Out.Flush();
        string? line;while((line=await Console.In.ReadLineAsync()) is not null)
        {
            if(String.IsNullOrWhiteSpace(line))continue;object response;JsonElement id=default;
            try{using var doc=JsonDocument.Parse(line);var root=doc.RootElement;id=root.GetProperty("id").Clone();string method=root.GetProperty("method").GetString()??throw new ProviderException("invalid_argument","Missing method.");JsonElement prm=root.TryGetProperty("params",out var px)?px.Clone():JsonDocument.Parse("{}").RootElement.Clone();object? result=await DispatchAsync(method,prm);response=new{id,result};}
            catch(ProviderException e){response=new{id,error=new{code=e.Code,message=e.Message,nativeCode=e.NativeCode,details=e.Details}};}
            catch(Exception e){response=new{id,error=new{code="native_error",message=e.Message,type=e.GetType().FullName}};}
            Console.WriteLine(JsonSerializer.Serialize(response,Json));Console.Out.Flush();
        }
        foreach(var f in Files.Values)f.Dispose();foreach(var p in Spawned.Values)p.Dispose();return 0;
    }

    private static Task<object> DispatchAsync(string method,JsonElement p)
    {
        object result=method switch
        {
            "probe"=>Probe(),"context.inspect"=>ContextInspect(),"process.inspect"=>ProcessInspect(p),"process.start"=>ProcessStart(p),"process.wait"=>ProcessWait(p),"process.terminate"=>ProcessTerminate(p),
            "file.retain"=>FileRetain(p),"file.create"=>FileCreate(p),"file.inspect"=>FileInspect(p),"file.read"=>FileRead(p),"file.write"=>FileWrite(p),"file.recover"=>FileRecover(p),
            _=>throw new ProviderException("not_found","Unknown helper operation: "+method)
        };return Task.FromResult(result);
    }

    private sealed record WorldFacts(string BootId,string PidNamespace,ulong InitStartTicks,string WorldEpoch);
    private static WorldFacts CurrentWorld(){string bootId=File.ReadAllText("/proc/sys/kernel/random/boot_id").Trim();string pidNs=LinuxNative.ReadLink("/proc/self/ns/pid");ulong initStart=LinuxNative.ReadProc(1).StartTicks;return new(bootId,pidNs,initStart,LinuxNative.WorldEpoch(_providerKey,bootId,pidNs,initStart));}
    private static object ContextInspect(){var w=CurrentWorld();return new{providerEpoch=ProviderEpoch,providerKey=_providerKey,distro=_distro,w.WorldEpoch,uid=LinuxNative.getuid(),euid=LinuxNative.geteuid(),gid=LinuxNative.getgid(),egid=LinuxNative.getegid(),groups=LinuxNative.Groups(),cwd=Environment.CurrentDirectory,namespaces=Namespaces()};}
    private static object Probe()
    {
        var w=CurrentWorld();bool pidfd=false,statx=false,inotify=false;try{int fd=LinuxNative.pidfd_open(Environment.ProcessId,0);if(fd>=0){pidfd=true;LinuxNative.close(fd);}}catch{}try{_=LinuxNative.StatxPath("/");statx=true;}catch{}try{int fd=LinuxNative.inotify_init1(LinuxNative.O_CLOEXEC);if(fd>=0){inotify=true;LinuxNative.close(fd);}}catch{}
        var exportHandle=LinuxNative.TryNameToHandle("/tmp");var mounts=LinuxNative.Mounts();var rootMount=mounts.FirstOrDefault(m=>m.MountPoint=="/");string pid1="";try{pid1=File.ReadAllText("/proc/1/comm").Trim();}catch{}bool systemd=Directory.Exists("/run/systemd/system")&&pid1.Contains("systemd",StringComparison.OrdinalIgnoreCase);string? systemdVersion=TrySystemdVersion();bool cgroupV2=File.Exists("/sys/fs/cgroup/cgroup.controllers");string? machineId=null,kernel=null;try{machineId=File.ReadAllText("/etc/machine-id").Trim();}catch{}try{kernel=File.ReadAllText("/proc/sys/kernel/osrelease").Trim();}catch{}
        return new{providerEpoch=ProviderEpoch,providerKey=_providerKey,distro=_distro,worldEpoch=w.WorldEpoch,kernelBootId=w.BootId,pidNamespace=w.PidNamespace,initStartTicks=w.InitStartTicks,kernelRelease=kernel,osPrettyName=ReadOsRelease("PRETTY_NAME"),machineId,context=ContextInspect(),capabilities=new{pidfd,statx,inotify,cgroupV2,systemd,systemdVersion,exportableFileHandle=exportHandle is not null,mountIdUnique=statx&&LinuxNative.StatxPath("/").UniqueMountId},rootMount,mountCount=mounts.Count,mounts=mounts.Take(128).ToArray()};
    }
    private static object Namespaces(){string Read(string n){try{return LinuxNative.ReadLink($"/proc/self/ns/{n}");}catch{return "unavailable";}}return new{pid=Read("pid"),mount=Read("mnt"),user=Read("user"),cgroup=Read("cgroup")};}
    private static string? TrySystemdVersion(){try{if(!File.Exists("/usr/bin/systemd"))return null;var psi=new ProcessStartInfo("/usr/bin/systemd"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};psi.ArgumentList.Add("--version");using var p=Process.Start(psi)!;string? line=p.StandardOutput.ReadLine();p.WaitForExit(3000);return line;}catch{return null;}}
    private static string? ReadOsRelease(string key){try{foreach(string line in File.ReadLines("/etc/os-release")){if(!line.StartsWith(key+"=",StringComparison.Ordinal))continue;string v=line[(key.Length+1)..];if(v.Length>=2&&v[0]=='"'&&v[^1]=='"')v=v[1..^1];return v.Replace("\\\"","\"");}}catch{}return null;}
    private static void CheckWorld(JsonElement p){string? expected=OptString(p,"expectedWorldEpoch");if(expected is null)return;string current=CurrentWorld().WorldEpoch;if(!StringComparer.Ordinal.Equals(expected,current))throw new ProviderException("stale","Provider-world epoch changed.",details:new{expectedWorldEpoch=expected,currentWorldEpoch=current});}

    private static object ProcessInspect(JsonElement p){CheckWorld(p);int pid=RequiredInt(p,"pid");ulong? expected=OptUlong(p,"expectedStartTicks");using var h=LinuxNative.OpenVerifiedPidfd(pid,expected);return ProcessResult(h.Process);}
    private static object ProcessStart(JsonElement p)
    {
        CheckWorld(p);
        if(!File.Exists("/usr/bin/systemd-run")||!File.Exists("/usr/bin/systemctl"))throw new ProviderException("unsupported_by_provider","systemd transient launch support is unavailable.");
        string requested=RequiredString(p,"executable"),executable=ResolveExecutable(requested);if(!File.Exists(executable))throw new ProviderException("not_found","Executable not found.",2,details:new{executable=requested});
        string launcher=Environment.ProcessPath??throw new ProviderException("native_error","Linux helper process path is unavailable."),cwd=OptString(p,"cwd")??Environment.CurrentDirectory,nonce=Guid.NewGuid().ToString("N"),unit="shelleye-"+nonce+".service",socketPath="/tmp/shelleye-launch-"+nonce+".sock";
        try{if(File.Exists(socketPath))File.Delete(socketPath);}catch{}
        using var listener=new Socket(AddressFamily.Unix,SocketType.Stream,ProtocolType.Unspecified);listener.Bind(new UnixDomainSocketEndPoint(socketPath));listener.Listen(1);
        try
        {
            var launch=new List<string>{"--unit="+unit,"--collect","--quiet","--property=Type=simple","--property=KillMode=process","--working-directory="+cwd};
            if(p.TryGetProperty("environment",out var env)&&env.ValueKind==JsonValueKind.Object)foreach(var kv in env.EnumerateObject())launch.Add("--setenv="+kv.Name+"="+(kv.Value.GetString()??""));
            launch.Add(launcher);launch.Add("--launch-proxy");launch.Add(socketPath);launch.Add(nonce);launch.Add(executable);if(p.TryGetProperty("args",out var ax)&&ax.ValueKind==JsonValueKind.Array)foreach(var a in ax.EnumerateArray())launch.Add(a.GetString()??"");
            ToolResult started=RunTool("/usr/bin/systemd-run",launch,5000);if(started.ExitCode!=0)throw new ProviderException("native_error","systemd transient process launch failed.",details:new{unit,exitCode=started.ExitCode,stdout=started.Stdout,stderr=started.Stderr});
            if(!listener.Poll(5_000_000,SelectMode.SelectRead)){TryStopUnit(unit);throw new ProviderException("timeout","Linux launch proxy did not establish its witness handshake.",details:new{unit});}
            using Socket peer=listener.Accept();peer.ReceiveTimeout=3000;peer.SendTimeout=3000;using var stream=new NetworkStream(peer,ownsSocket:false);using var reader=new StreamReader(stream,Encoding.UTF8,false,1024,true);using var writer=new StreamWriter(stream,new UTF8Encoding(false),1024,true){AutoFlush=true};
            string? ready=reader.ReadLine();string[] fields=(ready??"").Split('|');if(fields.Length!=3||!StringComparer.Ordinal.Equals(fields[0],nonce)||!int.TryParse(fields[1],out int pid)||!ulong.TryParse(fields[2],out ulong startTicks)||pid<=0){TryStopUnit(unit);throw new ProviderException("native_error","Linux launch proxy returned an invalid witness handshake.",details:new{unit,ready});}
            int mainPid=0;for(int i=0;i<40;i++){mainPid=SystemdMainPid(unit);if(mainPid==pid)break;Thread.Sleep(10);}if(mainPid!=pid){TryStopUnit(unit);throw new ProviderException("ambiguous","systemd MainPID did not match the launch-proxy lifetime witness.",details:new{unit,mainPid,proxyPid=pid});}
            LinuxNative.ProcInfo info;try{using var h=LinuxNative.OpenVerifiedPidfd(pid,startTicks);info=h.Process;}catch{TryStopUnit(unit);throw;}
            writer.WriteLine("GO");writer.Flush();return ProcessResult(info);
        }
        catch(ProviderException){throw;}
        catch(Exception e){TryStopUnit(unit);throw new ProviderException("native_error","Linux exact process launch handshake failed.",details:new{unit,socketPath},inner:e);}
        finally{try{if(File.Exists(socketPath))File.Delete(socketPath);}catch{}}
    }
    private static object ProcessWait(JsonElement p){CheckWorld(p);int pid=RequiredInt(p,"pid");ulong expected=RequiredUlong(p,"expectedStartTicks");int timeout=OptInt(p,"timeoutMs")??30000;using var h=LinuxNative.OpenVerifiedPidfd(pid,expected);if(!LinuxNative.WaitPidfd(h.Fd,timeout))throw new ProviderException("timeout","Process wait timed out.");return new{providerEpoch=ProviderEpoch,worldEpoch=CurrentWorld().WorldEpoch,pid,state="exited",startTicks=expected};}
    private static object ProcessTerminate(JsonElement p)
    {
        CheckWorld(p);int pid=RequiredInt(p,"pid");ulong expected=RequiredUlong(p,"expectedStartTicks");using var h=LinuxNative.OpenVerifiedPidfd(pid,expected);int rc;try{rc=LinuxNative.pidfd_send_signal(h.Fd,15,IntPtr.Zero,0);}catch(EntryPointNotFoundException e){throw new ProviderException("unsupported_by_provider","pidfd_send_signal is unavailable.",inner:e);}if(rc!=0){int e=LinuxNative.Errno;if(e==3)throw new ProviderException("destroyed","Process exited before signal delivery.",e);if(e is 1 or 13)throw new ProviderException("permission_denied","pidfd_send_signal permission denied.",e);throw new ProviderException("native_error","pidfd_send_signal failed.",e);}if(!LinuxNative.WaitPidfd(h.Fd,5000)){if(LinuxNative.pidfd_send_signal(h.Fd,9,IntPtr.Zero,0)!=0&&LinuxNative.Errno!=3)throw new ProviderException("native_error","pidfd SIGKILL failed.",LinuxNative.Errno);_=LinuxNative.WaitPidfd(h.Fd,5000);}return new{providerEpoch=ProviderEpoch,worldEpoch=CurrentWorld().WorldEpoch,pid,state="exited",exactPidfdActuation=true,startTicks=expected};
    }
    private static object ProcessResult(LinuxNative.ProcInfo p)=>new{pid=p.Pid,ppid=p.Ppid,startTicks=p.StartTicks,name=p.Name,executablePath=p.Exe,providerEpoch=ProviderEpoch,worldEpoch=CurrentWorld().WorldEpoch};

    private static object FileCreate(JsonElement p){CheckWorld(p);string path=RequiredString(p,"path"),content=OptString(p,"content")??"";string? parent=Path.GetDirectoryName(path);if(!String.IsNullOrEmpty(parent))Directory.CreateDirectory(parent);File.WriteAllText(path,content,new UTF8Encoding(false));return RetainFile(path);}
    private static object FileRetain(JsonElement p){CheckWorld(p);return RetainFile(RequiredString(p,"path"));}
    private static object RetainFile(string path)
    {
        path=Path.GetFullPath(path);var initial=LinuxNative.StatxPath(path);bool isDir=(initial.Mode&0xF000)==0x4000;int fd=-1;bool canWrite=false;if(!isDir){fd=LinuxNative.open(path,LinuxNative.O_RDWR|LinuxNative.O_CLOEXEC);if(fd>=0)canWrite=true;}if(fd<0)fd=LinuxNative.open(path,(isDir?LinuxNative.O_PATH:LinuxNative.O_RDONLY)|LinuxNative.O_CLOEXEC);if(fd<0){int e=LinuxNative.Errno;if(e is 1 or 13)throw new ProviderException("permission_denied","Open retained file permission denied.",e);throw new ProviderException("native_error","Open retained file failed.",e);}string token="lfd_"+Guid.NewGuid().ToString("N");try{var st=LinuxNative.StatxFd(fd);var handle=LinuxNative.TryNameToHandle(path);var held=new HeldFile(token,fd,canWrite,path,handle);Files[token]=held;return FileResult(held,st);}catch{LinuxNative.close(fd);throw;}
    }
    private static object FileInspect(JsonElement p){CheckWorld(p);var held=Held(RequiredString(p,"token"));return FileResult(held,LinuxNative.StatxFd(held.Fd));}
    private static object FileRead(JsonElement p)
    {
        CheckWorld(p);var held=Held(RequiredString(p,"token"));var st=LinuxNative.StatxFd(held.Fd);if((st.Mode&0xF000)==0x4000)throw new ProviderException("unsupported_by_provider","Directory content read is not a file read.");int max=Math.Clamp(OptInt(p,"maxBytes")??1024*1024,1,4*1024*1024);using var safe=new SafeFileHandle((IntPtr)held.Fd,ownsHandle:false);using var fs=new FileStream(safe,FileAccess.Read,4096,isAsync:false);fs.Position=0;byte[] b=new byte[Math.Min(max,checked((int)Math.Min(st.Size,int.MaxValue)))];int n=0;while(n<b.Length){int r=fs.Read(b,n,b.Length-n);if(r==0)break;n+=r;}return new{token=held.Token,content=Encoding.UTF8.GetString(b,0,n),truncated=st.Size>(ulong)n,witness=FileResult(held,st)};
    }
    private static object FileWrite(JsonElement p)
    {
        CheckWorld(p);var held=Held(RequiredString(p,"token"));if(!held.CanWrite)throw new ProviderException("permission_denied","Retained file descriptor is not writable.");var before=LinuxNative.StatxFd(held.Fd);CheckFileExpected(p,before);byte[] bytes=Encoding.UTF8.GetBytes(RequiredString(p,"content"));using var safe=new SafeFileHandle((IntPtr)held.Fd,ownsHandle:false);using var fs=new FileStream(safe,FileAccess.ReadWrite,4096,isAsync:false);fs.Position=0;fs.SetLength(0);fs.Write(bytes,0,bytes.Length);fs.Flush();return FileResult(held,LinuxNative.StatxFd(held.Fd));
    }
    private static object FileRecover(JsonElement p)
    {
        string? handleB64=OptString(p,"handleBytesBase64");int? handleType=OptInt(p,"handleType");if(String.IsNullOrEmpty(handleB64)||!handleType.HasValue)throw new ProviderException("ambiguous","No strong exported file handle is available for post-gap recovery.");uint devMajor=checked((uint)RequiredInt(p,"devMajor")),devMinor=checked((uint)RequiredInt(p,"devMinor"));var handle=new LinuxNative.ExportedHandle(handleType.Value,handleB64,OptInt(p,"handleMountId")??0);int fd=LinuxNative.TryOpenByHandle(handle,devMajor,devMinor,true,out int errno);bool canWrite=fd>=0;if(fd<0)fd=LinuxNative.TryOpenByHandle(handle,devMajor,devMinor,false,out errno);if(fd<0){if(errno==116)throw new ProviderException("destroyed","Exported file handle is stale.",errno);if(errno is 1 or 13)throw new ProviderException("permission_denied","open_by_handle_at permission denied.",errno);throw new ProviderException("ambiguous","Exported file handle could not be reopened exactly.",errno);}string token="lfd_"+Guid.NewGuid().ToString("N"),path=OptString(p,"path")??"";var held=new HeldFile(token,fd,canWrite,path,handle);Files[token]=held;return FileResult(held,LinuxNative.StatxFd(fd));
    }
    private static HeldFile Held(string token){if(!Files.TryGetValue(token,out var held))throw new ProviderException("not_found","Retained helper file token is not current in this provider epoch.");return held;}
    private static void CheckFileExpected(JsonElement p,LinuxNative.StatWitness st){if(p.TryGetProperty("expectedDevMajor",out var dm)&&dm.TryGetUInt32(out uint dmaj)&&dmaj!=st.DevMajor)throw new ProviderException("stale","File device major changed.");if(p.TryGetProperty("expectedDevMinor",out var dn)&&dn.TryGetUInt32(out uint dmin)&&dmin!=st.DevMinor)throw new ProviderException("stale","File device minor changed.");if(p.TryGetProperty("expectedInode",out var ino)&&ino.TryGetUInt64(out ulong i)&&i!=st.Inode)throw new ProviderException("stale","File inode changed.");if(p.TryGetProperty("expectedMountId",out var mi)&&mi.TryGetUInt64(out ulong m)&&m!=st.MountId)throw new ProviderException("stale","File mount identity changed.");}
    private static object FileResult(HeldFile held,LinuxNative.StatWitness st){try{held.Path=LinuxNative.ReadLink($"/proc/self/fd/{held.Fd}");}catch{}return new{token=held.Token,path=held.Path,kind=(st.Mode&0xF000)==0x4000?"dir":"file",canWrite=held.CanWrite,providerEpoch=ProviderEpoch,worldEpoch=CurrentWorld().WorldEpoch,identity=new{devMajor=st.DevMajor,devMinor=st.DevMinor,inode=st.Inode,mountId=st.MountId,uniqueMountId=st.UniqueMountId},revision=new{size=st.Size,mtimeNs=st.MTimeNs,ctimeNs=st.CTimeNs,btimeNs=st.BTimeNs},owner=new{uid=st.Uid,gid=st.Gid,mode=st.Mode},exportedHandle=held.ExportedHandle is null?null:new{type=held.ExportedHandle.Type,bytesBase64=held.ExportedHandle.BytesBase64,mountId=held.ExportedHandle.MountId}};}

    private sealed record ToolResult(int ExitCode,string Stdout,string Stderr);
    private static ToolResult RunTool(string executable,IEnumerable<string> args,int timeoutMs)
    {
        var psi=new ProcessStartInfo(executable){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(string arg in args)psi.ArgumentList.Add(arg);Process process;try{process=Process.Start(psi)??throw new ProviderException("native_error","Linux provider tool process start returned null.",details:new{executable});}catch(System.ComponentModel.Win32Exception e)when(e.NativeErrorCode==2){throw new ProviderException("not_found","Linux provider tool executable not found.",e.NativeErrorCode,details:new{executable},inner:e);}using(process){Task<string> so=process.StandardOutput.ReadToEndAsync(),se=process.StandardError.ReadToEndAsync();if(!process.WaitForExit(timeoutMs)){try{process.Kill(true);}catch{}throw new ProviderException("timeout","Linux provider tool timed out.",details:new{executable});}Task.WaitAll(so,se);return new(process.ExitCode,so.Result.Trim(),se.Result.Trim());}
    }
    private static int SystemdMainPid(string unit){ToolResult r=RunTool("/usr/bin/systemctl",new[]{"show",unit,"--property=MainPID","--value","--no-pager"},3000);return r.ExitCode==0&&int.TryParse(r.Stdout,out int pid)?pid:0;}
    private static void TryStopUnit(string unit){try{_=RunTool("/usr/bin/systemctl",new[]{"stop",unit},3000);}catch{}try{_=RunTool("/usr/bin/systemctl",new[]{"reset-failed",unit},3000);}catch{}}
    private static string ResolveExecutable(string executable)
    {
        if(executable.Contains('/'))return Path.GetFullPath(executable);string? path=Environment.GetEnvironmentVariable("PATH");if(path is not null)foreach(string dir in path.Split(':',StringSplitOptions.RemoveEmptyEntries)){string candidate=Path.Combine(dir,executable);if(File.Exists(candidate))return candidate;}return executable;
    }
    private static int RunLaunchProxy(string[] args)
    {
        if(args.Length<4)return 64;string socketPath=args[1],nonce=args[2],exe=ResolveExecutable(args[3]);string[] nativeArgs=new[]{exe}.Concat(args.Skip(4)).ToArray();
        try
        {
            using var socket=new Socket(AddressFamily.Unix,SocketType.Stream,ProtocolType.Unspecified);socket.Connect(new UnixDomainSocketEndPoint(socketPath));socket.ReceiveTimeout=5000;socket.SendTimeout=5000;using var stream=new NetworkStream(socket,ownsSocket:false);using var reader=new StreamReader(stream,Encoding.UTF8,false,1024,true);using var writer=new StreamWriter(stream,new UTF8Encoding(false),1024,true){AutoFlush=true};LinuxNative.ProcInfo self=LinuxNative.ReadProc(Environment.ProcessId);writer.WriteLine($"{nonce}|{self.Pid}|{self.StartTicks}");string? go=reader.ReadLine();if(!StringComparer.Ordinal.Equals(go,"GO")){Console.Error.WriteLine("launch proxy authorization handshake failed");return 125;}
        }
        catch(Exception e){Console.Error.WriteLine("launch proxy handshake failed: "+e.Message);return 126;}
        return ExecNative(exe,nativeArgs);
    }
    private static int ExecNative(string exe,string[] nativeArgs)
    {
        IntPtr argv=Marshal.AllocHGlobal(IntPtr.Size*(nativeArgs.Length+1));var allocated=new List<IntPtr>();try{for(int i=0;i<nativeArgs.Length;i++){IntPtr sp=Marshal.StringToHGlobalAnsi(nativeArgs[i]);allocated.Add(sp);Marshal.WriteIntPtr(argv,i*IntPtr.Size,sp);}Marshal.WriteIntPtr(argv,nativeArgs.Length*IntPtr.Size,IntPtr.Zero);int rc=LinuxNative.execv(exe,argv);Console.Error.WriteLine($"execv failed exe={exe} rc={rc} errno={LinuxNative.Errno}");return 127;}finally{foreach(var x in allocated)Marshal.FreeHGlobal(x);Marshal.FreeHGlobal(argv);}
    }
    private static int RunExecFixture(string[] args)
    {
        int delay=args.Length>1&&int.TryParse(args[1],out var d)?d:750,seconds=args.Length>2&&int.TryParse(args[2],out var s)?s:30;Thread.Sleep(Math.Max(0,delay));string exe="/usr/bin/sleep";return ExecNative(exe,new[]{exe,seconds.ToString()});
    }
    private static string RequiredString(JsonElement p,string n)=>p.TryGetProperty(n,out var x)&&x.ValueKind==JsonValueKind.String?x.GetString()!:throw new ProviderException("invalid_argument","Missing string parameter: "+n);
    private static string? OptString(JsonElement p,string n)=>p.ValueKind==JsonValueKind.Object&&p.TryGetProperty(n,out var x)&&x.ValueKind==JsonValueKind.String?x.GetString():null;
    private static int RequiredInt(JsonElement p,string n)=>p.TryGetProperty(n,out var x)&&x.TryGetInt32(out int v)?v:throw new ProviderException("invalid_argument","Missing integer parameter: "+n);
    private static int? OptInt(JsonElement p,string n)=>p.ValueKind==JsonValueKind.Object&&p.TryGetProperty(n,out var x)&&x.TryGetInt32(out int v)?v:null;
    private static ulong RequiredUlong(JsonElement p,string n)=>p.TryGetProperty(n,out var x)&&x.TryGetUInt64(out ulong v)?v:throw new ProviderException("invalid_argument","Missing unsigned integer parameter: "+n);
    private static ulong? OptUlong(JsonElement p,string n)=>p.ValueKind==JsonValueKind.Object&&p.TryGetProperty(n,out var x)&&x.TryGetUInt64(out ulong v)?v:null;
}
