using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SHELLeye;

public sealed class ProcessRegistry
{
    private readonly WorldContext _w;
    private readonly ConcurrentDictionary<string,Process> _short = new();
    public ProcessRegistry(WorldContext world)=>_w=world;
    public string AllocateId()=>_w.Store.NextId("proc");

    public ProcessWitness RetainPid(uint pid,string? exactParentId=null,string? forcedId=null,IntPtr createdHandle=default)
    {
        IntPtr h=createdHandle;bool close=false;
        if(h==IntPtr.Zero){h=WindowsNative.OpenProcess(WindowsNative.PROCESS_QUERY_LIMITED_INFORMATION|WindowsNative.SYNCHRONIZE,false,pid);if(h==IntPtr.Zero)throw WindowsNative.Win32("inaccessible","OpenProcess for retain failed.");close=true;}
        ProcessSnapshot? row=null;long inventoryDeadline=Environment.TickCount64+750;
        do{row=WindowsNative.EnumerateBasicProcesses().FirstOrDefault(x=>x.Pid==pid);if(row is not null)break;if(WindowsNative.WaitForSingleObject(h,0)==0)throw new ShellEyeException("not_found","Process exited before its exact sequence witness became observable.");Thread.Sleep(10);}while(Environment.TickCount64<inventoryDeadline);
        if(row is null)throw new ShellEyeException("unknown","Process exists but its sequence witness did not become observable within the bounded retain interval.");
        try
        {
            long creation=WindowsNative.QueryCreationFileTime(h);var telemetry=WindowsNative.TryQueryTelemetry(h);if(telemetry is not null&&telemetry.SequenceNumber!=0&&telemetry.SequenceNumber!=row.SequenceNumber)throw new ShellEyeException("stale","Enumeration and handle sequence witnesses disagree.");
            var existing=_w.Store.Query("SELECT * FROM processes WHERE boot_epoch=$b AND pid=$p AND sequence=$s AND creation_ft=$c AND state='current' LIMIT 1",("$b",_w.BootEpoch),("$p",pid),("$s",unchecked((long)row.SequenceNumber)),("$c",creation));
            if(existing.Count>0)return FromRow(existing[0]);
            string id=forcedId??AllocateId();string? parentId=null;string quality="reported";
            if(exactParentId!=null){var parent=Load(exactParentId);if(parent.Pid==row.ParentPid){parentId=parent.Id;quality="exact";}}
            else if(row.ParentPid!=0)
            {
                foreach(var candidate in _w.Store.Query("SELECT * FROM processes WHERE boot_epoch=$b AND pid=$p AND state='current' ORDER BY creation_ft DESC",("$b",_w.BootEpoch),("$p",row.ParentPid)))
                {
                    var parent=FromRow(candidate);if(parent.CreationFileTimeUtc>creation)continue;try{using var ph=WindowsNative.OpenVerifiedProcess(parent,_w.BootEpoch);parentId=parent.Id;quality="resolved-current";break;}catch{}
                }
            }
            uint session=WindowsNative.QuerySessionId(pid);string? exe=WindowsNative.QueryExecutablePath(h);string name=String.IsNullOrWhiteSpace(row.Name)?Path.GetFileName(exe)??"":row.Name;
            _w.Store.UpsertConcept(id,"proc","current");
            _w.Store.Exec("INSERT OR REPLACE INTO processes(id,boot_epoch,pid,sequence,creation_ft,name,session_id,exe_path,state,parent_pid,parent_id,parent_quality) VALUES($i,$b,$p,$s,$c,$n,$se,$e,'current',$pp,$pi,$q)",("$i",id),("$b",_w.BootEpoch),("$p",pid),("$s",unchecked((long)row.SequenceNumber)),("$c",creation),("$n",name),("$se",session),("$e",exe),("$pp",row.ParentPid),("$pi",parentId),("$q",quality));
            var witness=new ProcessWitness(id,_w.BootEpoch,pid,row.SequenceNumber,creation,name,session,exe,"current",quality,parentId);_w.AppendDelta("process.started",id,new{id,pid,sequence=row.SequenceNumber,name,sessionId=session,parentId,parentQuality=quality});return witness;
        }finally{if(close)WindowsNative.CloseHandle(h);}
    }

    public ProcessWitness Load(string id)
    {
        var rows=_w.Store.Query("SELECT * FROM processes WHERE id=$i",("$i",id));if(rows.Count==0)throw new ShellEyeException("not_found","Process concept not found.");return FromRow(rows[0]);
    }
    private static ProcessWitness FromRow(Dictionary<string,object?> r)=>new((string)r["id"]!,(string)r["boot_epoch"]!,unchecked((uint)Convert.ToInt64(r["pid"])),unchecked((ulong)Convert.ToInt64(r["sequence"])),Convert.ToInt64(r["creation_ft"]),(string)r["name"]!,unchecked((uint)Convert.ToInt64(r["session_id"])),r["exe_path"] as string,(string)r["state"]!,(string)r["parent_quality"]!,r["parent_id"] as string);

    public object Inspect(string id)
    {
        var p=Load(id);bool current=p.State=="current";string state=p.State;
        if(current){try{using var h=WindowsNative.OpenVerifiedProcess(p,_w.BootEpoch);}catch(ShellEyeException e)when(e.Code is "destroyed" or "stale"){state=e.Code;MarkState(id,state);current=false;}}
        return new{processId=p.Id,pid=p.Pid,sequence=p.SequenceNumber,creationFileTimeUtc=p.CreationFileTimeUtc,name=p.Name,sessionId=p.SessionId,executablePath=p.ExecutablePath,state,parent=new{processId=p.ParentId,quality=p.ParentQuality}};
    }

    public object Resources(string id)=>WindowsNative.QueryProcessResources(Load(id),_w.BootEpoch);

    public object Terminate(string id,uint exitCode=143)
    {
        var p=Load(id);if(p.State!="current")throw new ShellEyeException("destroyed","Retained process is terminal.");
        using var h=WindowsNative.OpenVerifiedProcess(p,_w.BootEpoch,WindowsNative.PROCESS_TERMINATE);
        if(!WindowsNative.TerminateProcess(h.Handle,exitCode))
        {
            if(WindowsNative.WaitForSingleObject(h.Handle,0)==0){MarkState(id,"exited");return new{processId=id,state="exited",alreadyExited=true};}
            throw WindowsNative.Win32("native_error","TerminateProcess failed on verified handle.");
        }
        uint code=WindowsNative.WaitProcess(h,TimeSpan.FromSeconds(10));MarkState(id,"exited",code);return new{processId=id,state="exited",exitCode=code};
    }

    public object Wait(string id,int timeoutMs=30000)
    {
        var p=Load(id);if(p.State!="current")return new{processId=id,state=p.State,exitCode=(uint?)null};
        try{using var h=WindowsNative.OpenVerifiedProcess(p,_w.BootEpoch);uint code=WindowsNative.WaitProcess(h,TimeSpan.FromMilliseconds(timeoutMs));MarkState(id,"exited",code);return new{processId=id,state="exited",exitCode=code};}
        catch(ShellEyeException e)when(e.Code=="destroyed"){MarkState(id,"destroyed");return new{processId=id,state="destroyed",exitCode=(uint?)null};}
    }

    public object StartDirect(string executable,IEnumerable<string> args,string? cwd=null,Dictionary<string,string>? environment=null)
    {
        string cmdId=_w.Store.NextId("cmd");_w.Store.UpsertConcept(cmdId,"cmd","running");
        var psi=new ProcessStartInfo(executable){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true,WorkingDirectory=cwd??Environment.CurrentDirectory};foreach(var a in args)psi.ArgumentList.Add(a);if(environment!=null)foreach(var kv in environment)psi.Environment[kv.Key]=kv.Value;
        var proc=Process.Start(psi)??throw new ShellEyeException("native_error","Process.Start returned null.");string id=AllocateId();ProcessWitness witness;
        try{witness=RetainPid(unchecked((uint)proc.Id),null,id,proc.SafeHandle.DangerousGetHandle());}catch{try{proc.Kill(true);}catch{}proc.Dispose();throw;}
        _short[id]=proc;return new{commandId=cmdId,process=witness};
    }

    public object CollectShortResult(string id,int timeoutMs=30000)
    {
        if(!_short.TryGetValue(id,out var proc))return Wait(id,timeoutMs);if(!proc.WaitForExit(timeoutMs))throw new ShellEyeException("timeout","Short process result timed out.");string stdout=proc.StandardOutput.ReadToEnd(),stderr=proc.StandardError.ReadToEnd();int code=proc.ExitCode;MarkState(id,"exited",unchecked((uint)code));_short.TryRemove(id,out _);proc.Dispose();return new{processId=id,state="exited",exitCode=code,stdout,stderr};
    }

    public void MarkState(string id,string state,uint? exitCode=null)
    {
        _w.Store.Exec("UPDATE processes SET state=$s WHERE id=$i",("$s",state),("$i",id));_w.Store.UpsertConcept(id,"proc",state);_w.AppendDelta("process.exited",id,new{id,state,exitCode});
    }

    public void ReconcileRetained()
    {
        foreach(var row in _w.Store.Query("SELECT * FROM processes WHERE state='current'"))
        {
            var p=FromRow(row);if(p.BootEpoch!=_w.BootEpoch){_w.Store.Exec("UPDATE processes SET state='destroyed' WHERE id=$i",("$i",p.Id));_w.Store.UpsertConcept(p.Id,"proc","destroyed");continue;}
            try{using var h=WindowsNative.OpenVerifiedProcess(p,_w.BootEpoch);}
            catch(ShellEyeException e)when(e.Code is "destroyed" or "stale"){_w.Store.Exec("UPDATE processes SET state=$s WHERE id=$i",("$s",e.Code),("$i",p.Id));_w.Store.UpsertConcept(p.Id,"proc",e.Code);}
        }
    }

    public static bool ResolverMatches(ProcessWitness retained,string bootEpoch,uint pid,ulong sequence,long creation)=>retained.BootEpoch==bootEpoch&&retained.Pid==pid&&retained.SequenceNumber==sequence&&retained.CreationFileTimeUtc==creation;
}

