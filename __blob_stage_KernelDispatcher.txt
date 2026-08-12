using System.Diagnostics;
using System.Text.Json;

namespace SHELLeye;

public sealed class KernelDispatcher
{
    private readonly WorldContext _w;private readonly LinuxProviderRegistry _linux;
    public KernelDispatcher(WorldContext w){_w=w;_linux=new LinuxProviderRegistry(w);}
    public async Task<object?> DispatchAsync(string method,JsonElement p,CancellationToken ct)
    {
        switch(method)
        {
            case "rpc.hello": return new{protocol="shelleye-rpc",version=1,kernelEpoch=_w.KernelEpoch,providerEpoch=_w.PowerShellProviderEpoch,bootEpoch=_w.BootEpoch};
            case "machine.inspect": return _w.MachineInspect();
            case "world.providers": return new{providers=_linux.ProviderWorlds()};
            case "provider.probe": return await _linux.ProbeAsync(ct);
            case "provider.context": return await _linux.ContextAsync(ct);
            case "session.inspect": return _w.System.InspectInteractiveSession();
            case "volume.inspect": return _w.System.InspectVolume(S(p,"drive"));
            case "process.retain": return UseLinuxWorld(p)?await _linux.RetainProcessAsync(U(p,"pid"),ct):_w.Processes.RetainPid(U(p,"pid"));
            case "process.inspect": {string id=S(p,"processId");return _linux.OwnsProcess(id)?await _linux.InspectProcessAsync(id,ct):_w.Processes.Inspect(id);}
            case "process.resources": {string id=S(p,"processId");if(_linux.OwnsProcess(id))throw new ShellEyeException("unsupported_by_provider","Build 002 does not universalize Windows process resource sampling onto Linux.");return _w.Processes.Resources(id);}
            case "process.start": return UseLinuxWorld(p)?await _linux.StartProcessAsync(S(p,"executable"),Strings(p,"args"),OptS(p,"cwd"),StringMap(p,"environment"),ct):_w.Processes.StartDirect(S(p,"executable"),Strings(p,"args"),OptS(p,"cwd"),StringMap(p,"environment"));
            case "process.wait": {string id=S(p,"processId");return _linux.OwnsProcess(id)?await _linux.WaitProcessAsync(id,OptI(p,"timeoutMs")??30000,ct):_w.Processes.Wait(id,OptI(p,"timeoutMs")??30000);}
            case "process.result": {string id=S(p,"processId");if(_linux.OwnsProcess(id))throw new ShellEyeException("unsupported_by_provider","Build 002 Linux direct processes do not claim restart-safe captured stdout/stderr; workload output remains a provider facet.");return _w.Processes.CollectShortResult(id,OptI(p,"timeoutMs")??30000);}
            case "process.terminate": {string id=S(p,"processId");return _linux.OwnsProcess(id)?await _linux.TerminateProcessAsync(id,ct):_w.Processes.Terminate(id);}
            case "job.create": return _w.Jobs.Create();
            case "job.inspect": {var id=S(p,"jobId");return new{job=_w.Jobs.Load(id),members=_w.Jobs.Members(id)};}
            case "job.start": return _w.Jobs.Start(S(p,"jobId"),S(p,"executable"),Strings(p,"args"),S(p,"cwd"));
            case "job.members": return _w.Jobs.Members(S(p,"jobId"));
            case "job.wait_member_count": return await _w.Jobs.WaitMemberCountAsync(S(p,"jobId"),I(p,"atLeast"),OptI(p,"timeoutMs")??30000,ct);
            case "job.terminate": return _w.Jobs.Terminate(S(p,"jobId"));
            case "job.wait_empty": {string id=S(p,"jobId");var r=await _w.Jobs.WaitEmptyAsync(id,OptI(p,"timeoutMs")??30000,ct);if(_w.Jobs.Load(id).State=="terminating")_w.Jobs.MarkTerminal(id);return r;}
            case "job.output": return _w.Jobs.Output(S(p,"jobId"),OptS(p,"afterCursor"),OptI(p,"maxBytes")??65536);
            case "job.wait_output": return await _w.Jobs.WaitOutputAsync(S(p,"jobId"),S(p,"contains"),OptS(p,"afterCursor"),OptI(p,"timeoutMs")??30000,ct);
            case "directory.create": if(UseLinuxWorld(p))throw _linux.UnsupportedFileMutation("directory.create");return _w.Files.CreateDirectory(S(p,"path"));
            case "directory.list": {string id=S(p,"directoryId");if(_linux.OwnsFile(id))throw _linux.UnsupportedFileMutation("directory.list");return _w.Files.ListDirectory(id);}
            case "file.create": return UseLinuxWorld(p)?await _linux.CreateFileAsync(S(p,"path"),OptS(p,"content")??"",ct):_w.Files.CreateFile(S(p,"path"),OptS(p,"content")??"");
            case "file.retain": return UseLinuxWorld(p)?await _linux.RetainFileAsync(S(p,"path"),ct):_w.Files.RetainPath(S(p,"path"));
            case "file.inspect": {string id=S(p,"fileId");return _linux.OwnsFile(id)?await _linux.InspectFileAsync(id,ct):_w.Files.Inspect(id);}
            case "file.read": {string id=S(p,"fileId");return _linux.OwnsFile(id)?await _linux.ReadFileAsync(id,ct):new{fileId=id,content=_w.Files.Read(id)};}
            case "file.write": {string id=S(p,"fileId");return _linux.OwnsFile(id)?await _linux.WriteFileAsync(id,S(p,"content"),OptS(p,"expectedRevision"),ct):_w.Files.Write(id,S(p,"content"),OptS(p,"expectedRevision"));}
            case "file.rename": {string id=S(p,"fileId");if(_linux.OwnsFile(id))throw _linux.UnsupportedFileMutation("rename");return _w.Files.Rename(id,S(p,"newPath"));}
            case "file.delete": {string id=S(p,"fileId");if(_linux.OwnsFile(id))throw _linux.UnsupportedFileMutation("delete");_w.Files.Delete(id);return new{fileId=id,deleted=true};}
            case "file.hardlink": {string id=S(p,"fileId");if(_linux.OwnsFile(id))throw _linux.UnsupportedFileMutation("hardlink");return new{fileId=id,path=_w.Files.AddHardLink(id,S(p,"newPath"))};}
            case "file.wait_change": {string id=S(p,"fileId");if(_linux.OwnsFile(id))throw new ShellEyeException("unsupported_by_provider","Linux inotify is capability-probed in Build 002 but retained-file wait is not yet promoted into the frozen implementation slice.");return await _w.Files.WaitForChangeAsync(id,S(p,"baselineRevision"),OptI(p,"timeoutMs")??30000,ct);}
            case "network.retain_listener": return _w.System.RetainListener(S(p,"address"),I(p,"port"),OwnerPid(p));
            case "network.wait_listener": return await _w.System.WaitListenerAsync(S(p,"address"),I(p,"port"),OwnerPid(p),OptI(p,"timeoutMs")??30000,ct);
            case "network.wait_absent": return await _w.System.WaitListenerAbsentAsync(S(p,"address"),I(p,"port"),OptS(p,"listenerId"),OptI(p,"timeoutMs")??30000,ct);
            case "listener.inspect": return _w.System.LoadListener(S(p,"listenerId"));
            case "service.inspect": return _w.System.InspectService(S(p,"name"));
            case "powershell.invoke": return await _w.PowerShell.InvokeAsync(S(p,"command"),ObjectMap(p,"parameters"),StringsOrNull(p,"properties"),ct);
            case "raw.exec": return await RawExecAsync(S(p,"command"),OptI(p,"timeoutMs")??30000,ct);
            case "world.cursor": return new{cursor=_w.Store.CurrentCursor()};
            case "world.delta": {long after=OptL(p,"afterCursor")??0;var d=_w.Store.ReadDeltas(after,OptI(p,"limit")??200);return new{afterCursor=after,cursor=_w.Store.CurrentCursor(),deltas=d};}
            case "world.sync": return await WorldSyncAsync(ct);
            case "state.health": return new{database=_w.Store.Path,quickCheck=_w.Store.IntegrityCheck(),journalMode="wal",cursor=_w.Store.CurrentCursor()};
            default: throw new ShellEyeException("not_found","Unknown RPC method: "+method);
        }
    }
    private async Task<object> WorldSyncAsync(CancellationToken ct){_w.Processes.ReconcileRetained();var files=_w.Files.SyncCurrent();_w.System.ReconcileListeners();foreach(var j in _w.Store.Query("SELECT id FROM jobs WHERE state IN ('current','terminating')")){try{_w.Jobs.Members((string)j["id"]!);}catch{}}object linux=await _linux.ReconcileRetainedAsync(ct);long c=_w.AppendDelta("world.reconciled",null,new{scope="promoted_interests",providers=new[]{"windows","linux-wsl2"},globalQuiescence=false,completeHistory=false});return new{cursor=c,files,linux,reconciled=true,globalQuiescence=false,completeHistory=false};}
    private bool UseLinuxWorld(JsonElement p){string? id=OptS(p,"worldId");if(id is null||string.Equals(id,_w.MachineId,StringComparison.Ordinal))return false;if(_linux.IsWorld(id))return true;throw new ShellEyeException("not_found","Unknown provider world: "+id);}
    private uint? OwnerPid(JsonElement p){var id=OptS(p,"ownerProcessId");if(id!=null)return _w.Processes.Load(id).Pid;if(p.ValueKind==JsonValueKind.Object&&p.TryGetProperty("ownerPid",out var x)&&x.ValueKind==JsonValueKind.Number)return x.GetUInt32();return null;}
    private static async Task<object> RawExecAsync(string command,int timeoutMs,CancellationToken ct)
    {
        var psi=new ProcessStartInfo("C:\\Windows\\System32\\cmd.exe"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};psi.ArgumentList.Add("/d");psi.ArgumentList.Add("/s");psi.ArgumentList.Add("/c");psi.ArgumentList.Add(command);using var proc=Process.Start(psi)??throw new ShellEyeException("native_error","Raw process start failed.");using var cts=CancellationTokenSource.CreateLinkedTokenSource(ct);cts.CancelAfter(timeoutMs);try{await proc.WaitForExitAsync(cts.Token);}catch(OperationCanceledException){try{proc.Kill(true);}catch{}throw new ShellEyeException("timeout","Raw execution timed out.");}return new{command,exitCode=proc.ExitCode,stdout=await proc.StandardOutput.ReadToEndAsync(ct),stderr=await proc.StandardError.ReadToEndAsync(ct)};
    }
    private static string S(JsonElement p,string n)=>p.TryGetProperty(n,out var x)&&x.ValueKind==JsonValueKind.String?x.GetString()!:throw new ShellEyeException("invalid_argument","Missing string parameter: "+n);
    private static string? OptS(JsonElement p,string n)=>p.ValueKind==JsonValueKind.Object&&p.TryGetProperty(n,out var x)&&x.ValueKind==JsonValueKind.String?x.GetString():null;
    private static int I(JsonElement p,string n)=>p.TryGetProperty(n,out var x)&&x.TryGetInt32(out int v)?v:throw new ShellEyeException("invalid_argument","Missing integer parameter: "+n);
    private static int? OptI(JsonElement p,string n)=>p.ValueKind==JsonValueKind.Object&&p.TryGetProperty(n,out var x)&&x.TryGetInt32(out int v)?v:null;
    private static long? OptL(JsonElement p,string n)=>p.ValueKind==JsonValueKind.Object&&p.TryGetProperty(n,out var x)&&x.TryGetInt64(out long v)?v:null;
    private static uint U(JsonElement p,string n)=>p.TryGetProperty(n,out var x)&&x.TryGetUInt32(out uint v)?v:throw new ShellEyeException("invalid_argument","Missing unsigned parameter: "+n);
    private static string[] Strings(JsonElement p,string n)=>p.ValueKind==JsonValueKind.Object&&p.TryGetProperty(n,out var x)&&x.ValueKind==JsonValueKind.Array?x.EnumerateArray().Select(e=>e.GetString()??"").ToArray():Array.Empty<string>();
    private static string[]? StringsOrNull(JsonElement p,string n)=>p.ValueKind==JsonValueKind.Object&&p.TryGetProperty(n,out var x)&&x.ValueKind==JsonValueKind.Array?x.EnumerateArray().Select(e=>e.GetString()??"").ToArray():null;
    private static Dictionary<string,string>? StringMap(JsonElement p,string n){if(p.ValueKind!=JsonValueKind.Object||!p.TryGetProperty(n,out var x)||x.ValueKind!=JsonValueKind.Object)return null;return x.EnumerateObject().ToDictionary(v=>v.Name,v=>v.Value.GetString()??"");}
    private static Dictionary<string,object?> ObjectMap(JsonElement p,string n){if(p.ValueKind!=JsonValueKind.Object||!p.TryGetProperty(n,out var x)||x.ValueKind!=JsonValueKind.Object)return new();return x.EnumerateObject().ToDictionary(v=>v.Name,v=>JsonValue(v.Value));}
    private static object? JsonValue(JsonElement x)=>x.ValueKind switch{JsonValueKind.String=>x.GetString(),JsonValueKind.Number=>x.TryGetInt64(out var l)?l:x.GetDouble(),JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.Null=>null,_=>x.GetRawText()};
}
