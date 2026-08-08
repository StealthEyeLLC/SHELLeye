namespace SHELLeye;

public sealed class SystemRegistry
{
    private readonly WorldContext _w;private long _listenerGeneration;
    public SystemRegistry(WorldContext world)=>_w=world;

    public VolumeConcept InspectVolume(string drive)
    {
        drive=drive.TrimEnd('\\');var rows=_w.Store.Query("SELECT id FROM volumes WHERE drive=$d",("$d",drive));string id=rows.Count>0?(string)rows[0]["id"]!:_w.Store.NextId("vol");var v=WindowsNative.QueryVolume(id,drive);_w.Store.UpsertConcept(id,"vol","current");_w.Store.Exec("INSERT INTO volumes(id,drive,volume_guid,fs,serial,total_bytes,free_bytes) VALUES($i,$d,$g,$f,$s,$t,$r) ON CONFLICT(drive) DO UPDATE SET volume_guid=excluded.volume_guid,fs=excluded.fs,serial=excluded.serial,total_bytes=excluded.total_bytes,free_bytes=excluded.free_bytes",("$i",id),("$d",drive),("$g",v.VolumeGuid),("$f",v.FileSystem),("$s",v.Serial.ToString()),("$t",v.TotalBytes),("$r",v.FreeBytes));return v;
    }
    public SessionConcept InspectInteractiveSession()
    {
        var s=WindowsNative.QueryInteractiveSession();var rows=_w.Store.Query("SELECT id FROM sessions WHERE session_id=$s",("$s",s.SessionId));string id=rows.Count>0?(string)rows[0]["id"]!:_w.Store.NextId("session");_w.Store.UpsertConcept(id,"session","current");_w.Store.Exec("INSERT INTO sessions(id,session_id,user_name,domain_name,state,interactive) VALUES($i,$s,$u,$d,$st,1) ON CONFLICT(id) DO UPDATE SET session_id=excluded.session_id,user_name=excluded.user_name,domain_name=excluded.domain_name,state=excluded.state,interactive=1",("$i",id),("$s",s.SessionId),("$u",s.User),("$d",s.Domain),("$st",s.State));return new SessionConcept(id,s.SessionId,s.User,s.Domain,s.State,true);
    }
    public ServiceConcept InspectService(string name)
    {
        var n=WindowsNative.QueryService(name);var rows=_w.Store.Query("SELECT id FROM services WHERE name=$n",("$n",name));string id=rows.Count>0?(string)rows[0]["id"]!:_w.Store.NextId("svc");string? processId=null;if(n.Pid!=0&&n.State=="running"){try{processId=_w.Processes.RetainPid(n.Pid).Id;}catch{}}
        _w.Store.UpsertConcept(id,"svc","current");_w.Store.Exec("INSERT INTO services(id,name,state,pid,process_id) VALUES($i,$n,$s,$p,$pi) ON CONFLICT(name) DO UPDATE SET state=excluded.state,pid=excluded.pid,process_id=excluded.process_id",("$i",id),("$n",name),("$s",n.State),("$p",n.Pid),("$pi",processId));return new ServiceConcept(id,name,n.State,n.Pid,processId);
    }

    public ListenerConcept RetainListener(string address,int port,uint? ownerPid=null)
    {
        var native=WindowsNative.QueryTcpListeners().Where(x=>x.Port==port&&(address=="*"||AddressEquivalent(address,x.Address))&&(!ownerPid.HasValue||x.Pid==ownerPid.Value)).ToArray();if(native.Length==0)throw new ShellEyeException("not_found","Listener is not current.");if(native.Length>1&&ownerPid is null)throw new ShellEyeException("ambiguous","Multiple current listeners match endpoint.");var n=native[0];var owner=_w.Processes.RetainPid(n.Pid);
        var rows=_w.Store.Query("SELECT * FROM listeners WHERE address=$a AND port=$p AND owner_process_id=$o AND state='current' ORDER BY generation DESC",("$a",n.Address),("$p",n.Port),("$o",owner.Id));
        foreach(var r in rows){long? ft=r["bind_ft"] is null?null:Convert.ToInt64(r["bind_ft"]);if(ft==n.BindFileTimeUtc)return FromRow(r);}
        foreach(var stale in _w.Store.Query("SELECT id FROM listeners WHERE address=$a AND port=$p AND state='current'",("$a",n.Address),("$p",n.Port))){string old=(string)stale["id"]!;_w.Store.Exec("UPDATE listeners SET state='closed' WHERE id=$i",("$i",old));_w.Store.UpsertConcept(old,"listener","closed");_w.AppendDelta("listener.closed",old,new{listenerId=old,address=n.Address,port=n.Port,replaced=true});}
        string id=_w.Store.NextId("listener");long gen=Interlocked.Increment(ref _listenerGeneration);_w.Store.UpsertConcept(id,"listener","current");_w.Store.Exec("INSERT INTO listeners(id,af,address,port,owner_process_id,owner_pid,bind_ft,state,generation) VALUES($i,$af,$a,$p,$o,$pid,$ft,'current',$g)",("$i",id),("$af",n.AddressFamily),("$a",n.Address),("$p",n.Port),("$o",owner.Id),("$pid",n.Pid),("$ft",n.BindFileTimeUtc),("$g",gen));var l=new ListenerConcept(id,n.AddressFamily,n.Address,n.Port,owner.Id,n.Pid,n.BindFileTimeUtc,"current",gen);_w.AppendDelta("listener.opened",id,new{listenerId=id,address=n.Address,port=n.Port,ownerProcessId=owner.Id,ownerPid=n.Pid,bindFileTimeUtc=n.BindFileTimeUtc});return l;
    }
    public ListenerConcept LoadListener(string id){var r=_w.Store.Query("SELECT * FROM listeners WHERE id=$i",("$i",id));if(r.Count==0)throw new ShellEyeException("not_found","Listener concept not found.");return FromRow(r[0]);}
    private static ListenerConcept FromRow(Dictionary<string,object?> r)=>new((string)r["id"]!,(string)r["af"]!,(string)r["address"]!,Convert.ToInt32(r["port"]),(string)r["owner_process_id"]!,unchecked((uint)Convert.ToInt64(r["owner_pid"])),r["bind_ft"] is null?null:Convert.ToInt64(r["bind_ft"]),(string)r["state"]!,Convert.ToInt64(r["generation"]));
    private static bool AddressEquivalent(string requested,string actual)=>StringComparer.OrdinalIgnoreCase.Equals(requested,actual)||(requested=="127.0.0.1"&&(actual=="0.0.0.0"||actual=="127.0.0.1"))||(requested=="::1"&&(actual=="::"||actual=="::1"));

    public async Task<ListenerConcept> WaitListenerAsync(string address,int port,uint? ownerPid,int timeoutMs,CancellationToken ct)
    {
        var end=DateTime.UtcNow.AddMilliseconds(timeoutMs);while(DateTime.UtcNow<end){ct.ThrowIfCancellationRequested();try{return RetainListener(address,port,ownerPid);}catch(ShellEyeException e)when(e.Code=="not_found"){}await Task.Delay(50,ct);}throw new ShellEyeException("timeout","Listener-open wait timed out.");
    }
    public async Task<object> WaitListenerAbsentAsync(string address,int port,string? listenerId,int timeoutMs,CancellationToken ct)
    {
        var end=DateTime.UtcNow.AddMilliseconds(timeoutMs);while(DateTime.UtcNow<end){ct.ThrowIfCancellationRequested();bool exists=WindowsNative.QueryTcpListeners().Any(x=>x.Port==port&&AddressEquivalent(address,x.Address));if(!exists){if(listenerId!=null){_w.Store.Exec("UPDATE listeners SET state='closed' WHERE id=$i",("$i",listenerId));_w.Store.UpsertConcept(listenerId,"listener","closed");_w.AppendDelta("listener.closed",listenerId,new{listenerId,address,port});}return new{address,port,absent=true};}await Task.Delay(50,ct);}throw new ShellEyeException("timeout","Listener-close wait timed out.");
    }
    public object RecoverAfterObservationGap()
    {
        int uncertain=0,services=0;
        foreach(var r in _w.Store.Query("SELECT id FROM listeners WHERE state='current'"))
        {
            string id=(string)r["id"]!;_w.Store.Exec("UPDATE listeners SET state='unknown' WHERE id=$i",("$i",id));_w.Store.UpsertConcept(id,"listener","unknown");uncertain++;_w.AppendDelta("listener.closed",id,new{listenerId=id,continuity="unknown_after_observation_gap",currentStateMustBeRediscovered=true});
        }
        try{InspectInteractiveSession();}catch{}
        foreach(var drive in new[]{"C:","X:"})try{InspectVolume(drive);}catch{}
        foreach(var r in _w.Store.Query("SELECT name FROM services")){try{InspectService((string)r["name"]!);services++;}catch{}}
        return new{listenerContinuityUnknown=uncertain,servicesReconciled=services};
    }
    public void ReconcileListeners()
    {
        var current=WindowsNative.QueryTcpListeners();foreach(var r in _w.Store.Query("SELECT * FROM listeners WHERE state='current'")){var l=FromRow(r);bool match=current.Any(n=>n.Port==l.Port&&n.Pid==l.OwnerPid&&n.BindFileTimeUtc==l.BindFileTimeUtc&&AddressEquivalent(l.Address,n.Address));if(!match){_w.Store.Exec("UPDATE listeners SET state='closed' WHERE id=$i",("$i",l.Id));_w.Store.UpsertConcept(l.Id,"listener","closed");_w.AppendDelta("listener.closed",l.Id,new{listenerId=l.Id,reconciled=true});}}
    }
}

