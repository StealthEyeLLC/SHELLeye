using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SHELLeye;

public sealed class JobRegistry : IDisposable
{
    private sealed class Runtime : IDisposable { public IntPtr Job;public IntPtr Port;public CancellationTokenSource Cts=new();public Task? Monitor;public void Dispose(){Cts.Cancel();try{Monitor?.Wait(1000);}catch{}if(Port!=IntPtr.Zero)WindowsNative.CloseHandle(Port);if(Job!=IntPtr.Zero)WindowsNative.CloseHandle(Job);Cts.Dispose();} }
    private readonly WorldContext _w;private readonly ConcurrentDictionary<string,Runtime> _runtime=new();
    public JobRegistry(WorldContext world)=>_w=world;

    public JobConcept Create()
    {
        string id=_w.Store.NextId("job"),native="Local\\SHELLeye."+_w.MachineUuid+"."+Guid.NewGuid().ToString("N");IntPtr job=WindowsNative.CreatePersistentJob(native,out IntPtr port);
        var rt=new Runtime{Job=job,Port=port};_runtime[id]=rt;StartMonitor(id,rt);
        _w.Store.UpsertConcept(id,"job","current");_w.Store.Exec("INSERT INTO jobs(id,native_name,boot_epoch,state,created_utc) VALUES($i,$n,$b,'current',$t)",("$i",id),("$n",native),("$b",_w.BootEpoch),("$t",DateTimeOffset.UtcNow.ToString("O")));_w.AppendDelta("job.created",id,new{id,nativeName=native});return new JobConcept(id,native,_w.BootEpoch,"current");
    }
    public JobConcept Load(string id){var r=_w.Store.Query("SELECT * FROM jobs WHERE id=$i",("$i",id));if(r.Count==0)throw new ShellEyeException("not_found","Job concept not found.");var x=r[0];return new JobConcept((string)x["id"]!,(string)x["native_name"]!,(string)x["boot_epoch"]!,(string)x["state"]!);}
    private Runtime GetRuntime(string id)
    {
        if(_runtime.TryGetValue(id,out var rt))return rt;var j=Load(id);if(j.BootEpoch!=_w.BootEpoch)throw new ShellEyeException("destroyed","Job belongs to a prior BootEpoch.");IntPtr h=WindowsNative.OpenPersistentJob(j.NativeName);rt=new Runtime{Job=h,Port=IntPtr.Zero};_runtime[id]=rt;return rt;
    }
    private void StartMonitor(string id,Runtime rt)
    {
        if(rt.Port==IntPtr.Zero)return;rt.Monitor=Task.Run(async()=>{while(!rt.Cts.IsCancellationRequested){bool ok=WindowsNative.GetQueuedCompletionStatus(rt.Port,out uint msg,out _,out IntPtr ov,500);if(!ok)continue;uint pid=unchecked((uint)ov.ToInt64());string? type=msg switch{4=>"job.empty",6=>"job.member_added",7=>"job.member_exited",8=>"job.member_exited",_=>null};if(type!=null){try{_w.AppendDelta(type,id,new{jobId=id,pid,message=msg,nativeSignal=true});}catch{}}await Task.Yield();}});
    }

    public object Start(string id,string executable,IReadOnlyList<string> args,string cwd)
    {
        var job=Load(id);if(job.State!="current")throw new ShellEyeException("destroyed","Job is terminal.");var rt=GetRuntime(id);string procId=_w.Processes.AllocateId();string dir=Path.Combine(_w.SpoolRoot,id);Directory.CreateDirectory(dir);string stdout=Path.Combine(dir,procId+".stdout.log"),stderr=Path.Combine(dir,procId+".stderr.log");try{File.Delete(stdout);}catch{}try{File.Delete(stderr);}catch{}
        using var launch=WindowsNative.CreateProcessInJob(rt.Job,executable,args,cwd,stdout,stderr);var witness=_w.Processes.RetainPid(launch.Pid,null,procId,launch.ProcessHandle);
        _w.Store.Exec("INSERT OR REPLACE INTO job_members(job_id,process_id,state) VALUES($j,$p,'current')",("$j",id),("$p",procId));_w.Store.Exec("INSERT OR REPLACE INTO spools(job_id,process_id,stream,path,completed) VALUES($j,$p,'stdout',$o,0),($j,$p,'stderr',$e,0)",("$j",id),("$p",procId),("$o",stdout),("$e",stderr));_w.AppendDelta("job.member_added",id,new{jobId=id,processId=procId,pid=launch.Pid});return new{jobId=id,process=witness,stdoutPath=stdout,stderrPath=stderr};
    }

    public object Members(string id)
    {
        var rt=GetRuntime(id);var pids=WindowsNative.QueryJobProcessIds(rt.Job);var current=new List<ProcessWitness>();
        foreach(uint pid in pids)
        {
            var rows=_w.Store.Query("SELECT p.* FROM processes p JOIN job_members jm ON jm.process_id=p.id WHERE jm.job_id=$j AND p.boot_epoch=$b AND p.pid=$p AND p.state='current' LIMIT 1",("$j",id),("$b",_w.BootEpoch),("$p",pid));ProcessWitness w;
            if(rows.Count>0)w=_w.Processes.Load((string)rows[0]["id"]!);else{w=_w.Processes.RetainPid(pid);_w.Store.Exec("INSERT OR REPLACE INTO job_members(job_id,process_id,state) VALUES($j,$p,'current')",("$j",id),("$p",w.Id));_w.AppendDelta("job.member_added",id,new{jobId=id,processId=w.Id,pid});}current.Add(w);
        }
        foreach(var r in _w.Store.Query("SELECT process_id FROM job_members WHERE job_id=$j AND state='current'",("$j",id))){string pid=(string)r["process_id"]!;if(!current.Any(x=>x.Id==pid))_w.Store.Exec("UPDATE job_members SET state='exited' WHERE job_id=$j AND process_id=$p",("$j",id),("$p",pid));}
        return new{jobId=id,members=current.Select(p=>new{processId=p.Id,pid=p.Pid,name=p.Name,parentId=p.ParentId,parentQuality=p.ParentQuality}).ToArray()};
    }

    public async Task<object> WaitEmptyAsync(string id,int timeoutMs,CancellationToken ct)
    {
        var rt=GetRuntime(id);var end=DateTime.UtcNow.AddMilliseconds(timeoutMs);while(DateTime.UtcNow<end){ct.ThrowIfCancellationRequested();if(WindowsNative.QueryJobProcessIds(rt.Job).Count==0){_w.AppendDelta("job.empty",id,new{jobId=id,reconciled=true});return new{jobId=id,empty=true};}await Task.Delay(50,ct);}throw new ShellEyeException("timeout","Job-empty wait timed out.");
    }
    public async Task<object> WaitMemberCountAsync(string id,int atLeast,int timeoutMs,CancellationToken ct)
    {
        var rt=GetRuntime(id);var end=DateTime.UtcNow.AddMilliseconds(timeoutMs);while(DateTime.UtcNow<end){ct.ThrowIfCancellationRequested();var ids=WindowsNative.QueryJobProcessIds(rt.Job);if(ids.Count>=atLeast)return new{jobId=id,count=ids.Count,pids=ids};await Task.Delay(50,ct);}throw new ShellEyeException("timeout","Job member wait timed out.");
    }

    public object Terminate(string id,uint exitCode=143)
    {
        var rt=GetRuntime(id);if(!WindowsNative.TerminateJobObject(rt.Job,exitCode))throw WindowsNative.Win32("native_error","TerminateJobObject failed.");_w.Store.Exec("UPDATE jobs SET state='terminating' WHERE id=$i",("$i",id));return new{jobId=id,terminationRequested=true};
    }
    public void MarkTerminal(string id)
    {
        _w.Store.Exec("UPDATE jobs SET state='terminal' WHERE id=$i",("$i",id));_w.Store.UpsertConcept(id,"job","terminal");_w.Store.Exec("UPDATE spools SET completed=1 WHERE job_id=$i",("$i",id));foreach(var r in _w.Store.Query("SELECT process_id FROM job_members WHERE job_id=$i AND state='current'",("$i",id))){string p=(string)r["process_id"]!;try{_w.Processes.MarkState(p,"exited");}catch{}}_w.Store.Exec("UPDATE job_members SET state='exited' WHERE job_id=$i",("$i",id));if(_runtime.TryRemove(id,out var rt))rt.Dispose();
    }

    private sealed record CursorState(int Version,Dictionary<string,long> Offsets);
    private static string EncodeCursor(CursorState c)=>Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(c,JsonDefaults.Options)));
    private static CursorState DecodeCursor(string? cursor){if(String.IsNullOrWhiteSpace(cursor))return new CursorState(1,new Dictionary<string,long>());try{return JsonSerializer.Deserialize<CursorState>(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)),JsonDefaults.Options)??new CursorState(1,new());}catch{throw new ShellEyeException("cursor_expired","Invalid output cursor.");}}
    public object Output(string id,string? afterCursor,int maxBytes=65536)
    {
        maxBytes=Math.Clamp(maxBytes,1,1024*1024);var cursor=DecodeCursor(afterCursor);var next=new Dictionary<string,long>(cursor.Offsets);var records=new List<object>();int remaining=maxBytes;bool more=false;
        foreach(var r in _w.Store.Query("SELECT process_id,stream,path FROM spools WHERE job_id=$j ORDER BY process_id,stream",("$j",id)))
        {
            string processId=(string)r["process_id"]!,stream=(string)r["stream"]!,path=(string)r["path"]!,key=processId+":"+stream;long offset=cursor.Offsets.TryGetValue(key,out var o)?o:0;if(!File.Exists(path)){if(offset>0)throw new ShellEyeException("cursor_expired","Referenced spool segment is no longer retained.");continue;}
            using var fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);if(offset>fs.Length)throw new ShellEyeException("cursor_expired","Output cursor exceeds retained segment.");fs.Position=offset;int take=(int)Math.Min(remaining,fs.Length-offset);if(take>0){byte[] b=new byte[take];int n=fs.Read(b,0,b.Length);string text=Encoding.UTF8.GetString(b,0,n);records.Add(new{processId,stream,text,from=offset,to=offset+n});offset+=n;remaining-=n;}next[key]=offset;if(offset<fs.Length)more=true;if(remaining==0)break;
        }
        return new{jobId=id,records,cursor=EncodeCursor(new CursorState(1,next)),more};
    }
    public async Task<object> WaitOutputAsync(string id,string contains,string? afterCursor,int timeoutMs,CancellationToken ct)
    {
        string? cursor=afterCursor;var end=DateTime.UtcNow.AddMilliseconds(timeoutMs);while(DateTime.UtcNow<end){ct.ThrowIfCancellationRequested();dynamic result=Output(id,cursor,65536);var element=JsonDefaults.Element(result);string newCursor=element.GetProperty("cursor").GetString()!;foreach(var rec in element.GetProperty("records").EnumerateArray())if(rec.GetProperty("text").GetString()?.Contains(contains,StringComparison.Ordinal)!=false)return new{jobId=id,matched=true,record=rec,cursor=newCursor};cursor=newCursor;await Task.Delay(50,ct);}throw new ShellEyeException("timeout","Output wait timed out.");
    }

    public void RecoverRetained()
    {
        foreach(var r in _w.Store.Query("SELECT * FROM jobs WHERE state='current'"))
        {
            string id=(string)r["id"]!,boot=(string)r["boot_epoch"]!,name=(string)r["native_name"]!;if(boot!=_w.BootEpoch){_w.Store.Exec("UPDATE jobs SET state='destroyed' WHERE id=$i",("$i",id));_w.Store.UpsertConcept(id,"job","destroyed");continue;}
            try{IntPtr h=WindowsNative.OpenPersistentJob(name);var rt=new Runtime{Job=h,Port=IntPtr.Zero};_runtime[id]=rt;var pids=WindowsNative.QueryJobProcessIds(h);foreach(uint pid in pids){var rows=_w.Store.Query("SELECT id FROM processes WHERE boot_epoch=$b AND pid=$p AND state='current'",("$b",_w.BootEpoch),("$p",pid));if(rows.Count==0){try{var pw=_w.Processes.RetainPid(pid);_w.Store.Exec("INSERT OR REPLACE INTO job_members(job_id,process_id,state) VALUES($j,$p,'current')",("$j",id),("$p",pw.Id));}catch{}}}}catch(ShellEyeException e)when(e.Code=="destroyed"){_w.Store.Exec("UPDATE jobs SET state='destroyed' WHERE id=$i",("$i",id));_w.Store.UpsertConcept(id,"job","destroyed");}
        }
    }
    public void Dispose(){foreach(var rt in _runtime.Values)rt.Dispose();_runtime.Clear();}
}


