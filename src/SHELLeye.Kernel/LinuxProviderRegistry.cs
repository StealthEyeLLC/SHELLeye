using System.Globalization;
using System.Text.Json;

namespace SHELLeye;

public sealed class LinuxProviderRegistry : IDisposable
{
    private readonly WorldContext _w;
    private readonly LinuxWslProvider _provider;
    private readonly string _worldId;

    public LinuxProviderRegistry(WorldContext world)
    {
        _w=world;
        _provider=new LinuxWslProvider();
        InitializeSchema();
        _worldId=EnsureWorld();
    }

    private void InitializeSchema()
    {
        _w.Store.Exec(@"
CREATE TABLE IF NOT EXISTS provider_worlds(world_id TEXT PRIMARY KEY,provider_kind TEXT NOT NULL,provider_key TEXT NOT NULL UNIQUE,name TEXT NOT NULL,host_machine_id TEXT,state TEXT NOT NULL,world_epoch TEXT,provider_epoch TEXT,capabilities_json TEXT,metadata_json TEXT,last_error TEXT,updated_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS linux_processes(id TEXT PRIMARY KEY,world_id TEXT NOT NULL,world_epoch TEXT NOT NULL,pid INTEGER NOT NULL,start_ticks TEXT NOT NULL,parent_pid INTEGER NOT NULL,name TEXT NOT NULL,executable_path TEXT,state TEXT NOT NULL,parent_quality TEXT NOT NULL,parent_id TEXT,updated_utc TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS ix_linux_process_native ON linux_processes(world_id,world_epoch,pid,start_ticks);
CREATE TABLE IF NOT EXISTS linux_files(id TEXT PRIMARY KEY,world_id TEXT NOT NULL,world_epoch TEXT NOT NULL,kind TEXT NOT NULL,path TEXT NOT NULL,dev_major INTEGER NOT NULL,dev_minor INTEGER NOT NULL,inode TEXT NOT NULL,mount_id TEXT NOT NULL,unique_mount_id INTEGER NOT NULL,revision TEXT NOT NULL,handle_type INTEGER,handle_b64 TEXT,handle_mount_id INTEGER,helper_token TEXT,provider_epoch TEXT,state TEXT NOT NULL,updated_utc TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS ix_linux_file_native ON linux_files(world_id,world_epoch,dev_major,dev_minor,inode,mount_id);
");
    }

    private string EnsureWorld()
    {
        var rows=_w.Store.Query("SELECT world_id FROM provider_worlds WHERE provider_key=$k",("$k",_provider.ProviderKey));
        if(rows.Count>0)return (string)rows[0]["world_id"]!;
        string id=_w.Store.NextId("world");
        _w.Store.UpsertConcept(id,"machine_world","configured");
        string metadata=JsonSerializer.Serialize(new{distribution=_provider.Distro,relationship="hosted",hostMachineId=_w.MachineId,transport="wsl2"},JsonDefaults.Options);
        _w.Store.Exec("INSERT INTO provider_worlds(world_id,provider_kind,provider_key,name,host_machine_id,state,world_epoch,provider_epoch,capabilities_json,metadata_json,last_error,updated_utc) VALUES($i,'linux-wsl2',$k,$n,$h,'configured',NULL,NULL,'{}',$m,NULL,$u)",("$i",id),("$k",_provider.ProviderKey),("$n",_provider.Distro),("$h",_w.MachineId),("$m",metadata),("$u",Now()));
        _w.AppendDelta("provider.world_configured",id,new{provider="linux-wsl2",providerKey=_provider.ProviderKey,distribution=_provider.Distro,hostMachineId=_w.MachineId});
        return id;
    }

    public object[] ProviderWorlds()
    {
        var windows=new{worldId=_w.MachineId,providerKind="windows",providerKey=_w.MachineUuid,name=Environment.MachineName,hostMachineId=(string?)null,state="current",worldEpoch=_w.BootEpoch,providerEpoch=_w.PowerShellProviderEpoch,capabilities=new{deepWindowsBuild001=true},metadata=new{relationship="host"},lastError=(string?)null};
        return new object[]{windows,Describe()};
    }

    public object Describe()
    {
        var r=WorldRow();
        return new{worldId=r["world_id"],providerKind=r["provider_kind"],providerKey=r["provider_key"],name=r["name"],hostMachineId=r["host_machine_id"],state=r["state"],worldEpoch=r["world_epoch"],providerEpoch=r["provider_epoch"],capabilities=StoredJson(r["capabilities_json"] as string),metadata=StoredJson(r["metadata_json"] as string),lastError=r["last_error"]};
    }

    public async Task<object> ProbeAsync(CancellationToken ct)
    {
        try
        {
            JsonElement probe=await _provider.ProbeAsync(ct);
            BindProbe(probe);
            return new{world=Describe(),probe};
        }
        catch(ShellEyeException e)
        {
            _w.Store.Exec("UPDATE provider_worlds SET state='provider_unavailable',last_error=$e,provider_epoch=NULL,updated_utc=$u WHERE world_id=$i",("$e",e.Message),("$u",Now()),("$i",_worldId));
            _w.AppendDelta("provider.unavailable",_worldId,new{provider="linux-wsl2",error=e.Code,message=e.Message});
            throw;
        }
    }

    public async Task<object> ContextAsync(CancellationToken ct)
    {
        string epoch=await EnsureProviderAsync(ct);
        JsonElement context=await _provider.RequestAsync("context.inspect",new{expectedWorldEpoch=epoch},ct);
        return new{worldId=_worldId,provider="linux-wsl2",context};
    }

    public bool IsWorld(string? worldId)=>string.Equals(worldId,_worldId,StringComparison.Ordinal);
    public bool OwnsProcess(string id)=>_w.Store.Query("SELECT id FROM linux_processes WHERE id=$i",("$i",id)).Count!=0;
    public bool OwnsFile(string id)=>_w.Store.Query("SELECT id FROM linux_files WHERE id=$i",("$i",id)).Count!=0;

    public async Task<object> RetainProcessAsync(uint pid,CancellationToken ct)
    {
        string epoch=await EnsureProviderAsync(ct);
        JsonElement p=await _provider.RequestAsync("process.inspect",new{pid,expectedWorldEpoch=epoch},ct);
        BindResultWorld(p);return PersistProcess(p,true);
    }

    public async Task<object> StartProcessAsync(string executable,string[] args,string? cwd,Dictionary<string,string>? environment,CancellationToken ct)
    {
        string epoch=await EnsureProviderAsync(ct);
        JsonElement p=await _provider.RequestAsync("process.start",new{executable,args,cwd,environment,expectedWorldEpoch=epoch},ct);
        BindResultWorld(p);return PersistProcess(p,true);
    }

    public async Task<object> InspectProcessAsync(string id,CancellationToken ct)
    {
        var row=LoadProcess(id);if(!IsCurrent(row))return ProcessObject(row);
        string epoch=await EnsureProviderAsync(ct);if(!string.Equals(row["world_epoch"] as string,epoch,StringComparison.Ordinal)){MarkProcess(id,"destroyed","process.identity_lost");return ProcessObject(LoadProcess(id));}
        try
        {
            int pid=Convert.ToInt32(row["pid"],CultureInfo.InvariantCulture);ulong start=ulong.Parse((string)row["start_ticks"]!,CultureInfo.InvariantCulture);
            JsonElement p=await _provider.RequestAsync("process.inspect",new{pid,expectedStartTicks=start,expectedWorldEpoch=epoch},ct);BindResultWorld(p);
            if(!ProviderIdentityRules.LinuxProcessMatches(ToProcessWitness(row),_worldId,RequiredString(p,"worldEpoch"),RequiredInt(p,"pid"),RequiredUlong(p,"startTicks"))){MarkProcess(id,"destroyed","process.identity_lost");return ProcessObject(LoadProcess(id));}
            UpdateProcess(row,p);return ProcessObject(LoadProcess(id),p);
        }
        catch(ShellEyeException e) when(e.Code is "destroyed" or "stale" or "not_found") {MarkProcess(id,"destroyed","process.exited");return ProcessObject(LoadProcess(id));}
    }

    public async Task<object> WaitProcessAsync(string id,int timeoutMs,CancellationToken ct)
    {
        var row=LoadProcess(id);if(!IsCurrent(row))return new{processId=id,worldId=_worldId,provider="linux-wsl2",state=row["state"],exited=true};string epoch=await EnsureProviderAsync(ct);int pid=Convert.ToInt32(row["pid"],CultureInfo.InvariantCulture);ulong start=ulong.Parse((string)row["start_ticks"]!,CultureInfo.InvariantCulture);
        try{JsonElement r=await _provider.RequestAsync("process.wait",new{pid,expectedStartTicks=start,expectedWorldEpoch=epoch,timeoutMs},ct);MarkProcess(id,"exited","process.exited");return new{processId=id,worldId=_worldId,provider="linux-wsl2",state="exited",exited=true,result=r};}
        catch(ShellEyeException e) when(e.Code is "destroyed" or "not_found") {MarkProcess(id,"exited","process.exited");return new{processId=id,worldId=_worldId,provider="linux-wsl2",state="exited",exited=true};}
    }

    public async Task<object> TerminateProcessAsync(string id,CancellationToken ct)
    {
        var row=LoadProcess(id);RequireCurrent(row,"process");string epoch=await EnsureProviderAsync(ct);int pid=Convert.ToInt32(row["pid"],CultureInfo.InvariantCulture);ulong start=ulong.Parse((string)row["start_ticks"]!,CultureInfo.InvariantCulture);
        JsonElement r=await _provider.RequestAsync("process.terminate",new{pid,expectedStartTicks=start,expectedWorldEpoch=epoch},ct);MarkProcess(id,"exited","process.exited");_w.AppendDelta("process.terminated",id,new{provider="linux-wsl2",worldId=_worldId,pid,exactPidfdActuation=true});return new{processId=id,worldId=_worldId,provider="linux-wsl2",result=r};
    }

    public async Task<object> CreateFileAsync(string path,string content,CancellationToken ct)
    {
        string epoch=await EnsureProviderAsync(ct);JsonElement f=await _provider.RequestAsync("file.create",new{path,content,expectedWorldEpoch=epoch},ct);BindResultWorld(f);return PersistFile(f,true);
    }

    public async Task<object> RetainFileAsync(string path,CancellationToken ct)
    {
        string epoch=await EnsureProviderAsync(ct);JsonElement f=await _provider.RequestAsync("file.retain",new{path,expectedWorldEpoch=epoch},ct);BindResultWorld(f);return PersistFile(f,true);
    }

    public async Task<object> InspectFileAsync(string id,CancellationToken ct)
    {
        var row=LoadFile(id);if(!IsCurrent(row))return FileObject(row);try{row=await PrepareFileAsync(row,ct);JsonElement f=await _provider.RequestAsync("file.inspect",new{token=(string)row["helper_token"]!,expectedWorldEpoch=(string)row["world_epoch"]!},ct);if(!FileMatches(row,f)){MarkFile(id,"stale","file.identity_lost");return FileObject(LoadFile(id));}UpdateFile(row,f);return FileObject(LoadFile(id),f);}catch(ShellEyeException e)when(e.Code is "destroyed" or "stale" or "ambiguous" or "not_found"){MarkFile(id,e.Code=="destroyed"?"destroyed":"stale","file.identity_lost");return FileObject(LoadFile(id));}
    }

    public async Task<object> ReadFileAsync(string id,CancellationToken ct)
    {
        var row=LoadFile(id);RequireCurrent(row,"file");row=await PrepareFileAsync(row,ct);JsonElement r=await _provider.RequestAsync("file.read",new{token=(string)row["helper_token"]!,expectedWorldEpoch=(string)row["world_epoch"]!,maxBytes=4*1024*1024},ct);JsonElement witness=r.GetProperty("witness");if(!FileMatches(row,witness)){MarkFile(id,"stale","file.identity_lost");throw new ShellEyeException("stale","Linux retained file identity changed before read.");}UpdateFile(row,witness);return new{fileId=id,worldId=_worldId,provider="linux-wsl2",content=RequiredString(r,"content"),truncated=r.TryGetProperty("truncated",out var t)&&t.GetBoolean(),revision=Revision(witness.GetProperty("revision"))};
    }

    public async Task<object> WriteFileAsync(string id,string content,string? expectedRevision,CancellationToken ct)
    {
        var row=LoadFile(id);RequireCurrent(row,"file");row=await PrepareFileAsync(row,ct);if(expectedRevision!=null&&!string.Equals(expectedRevision,row["revision"] as string,StringComparison.Ordinal))throw new ShellEyeException("stale","Linux file revision precondition does not match.");
        JsonElement r=await _provider.RequestAsync("file.write",new{token=(string)row["helper_token"]!,content,expectedWorldEpoch=(string)row["world_epoch"]!,expectedDevMajor=Convert.ToUInt32(row["dev_major"],CultureInfo.InvariantCulture),expectedDevMinor=Convert.ToUInt32(row["dev_minor"],CultureInfo.InvariantCulture),expectedInode=ulong.Parse((string)row["inode"]!,CultureInfo.InvariantCulture),expectedMountId=ulong.Parse((string)row["mount_id"]!,CultureInfo.InvariantCulture)},ct);if(!FileMatches(row,r)){MarkFile(id,"stale","file.identity_lost");throw new ShellEyeException("stale","Linux retained file identity changed during write.");}UpdateFile(row,r);_w.AppendDelta("file.changed",id,new{provider="linux-wsl2",worldId=_worldId,revision=Revision(r.GetProperty("revision"))});return FileObject(LoadFile(id),r);
    }

    public ShellEyeException UnsupportedFileMutation(string operation)=>new("unsupported_by_provider",$"Linux retained-file {operation} is not exposed in Build 002 because the exact retained FD/handle contract does not make pathname mutation race-free.");

    public async Task<object> ReconcileRetainedAsync(CancellationToken ct)
    {
        var prows=_w.Store.Query("SELECT id FROM linux_processes WHERE state='current'");var frows=_w.Store.Query("SELECT id FROM linux_files WHERE state='current'");if(prows.Count==0&&frows.Count==0)return new{provider="linux-wsl2",attempted=false,state=WorldRow()["state"],processes=0,files=0,stale=0};
        try{await EnsureProviderAsync(ct);}catch(ShellEyeException e)when(e.Code=="provider_unavailable"){return new{provider="linux-wsl2",attempted=true,available=false,processes=prows.Count,files=frows.Count,stale=0,error=e.Message};}
        int pc=0,fc=0,stale=0;foreach(var p in prows){await InspectProcessAsync((string)p["id"]!,ct);pc++;}foreach(var f in frows){await InspectFileAsync((string)f["id"]!,ct);fc++;if(!IsCurrent(LoadFile((string)f["id"]!)))stale++;}return new{provider="linux-wsl2",attempted=true,available=true,processes=pc,files=fc,stale};
    }

    private async Task<Dictionary<string,object?>> PrepareFileAsync(Dictionary<string,object?> row,CancellationToken ct)
    {
        string currentEpoch=await EnsureProviderAsync(ct);string retainedEpoch=(string)row["world_epoch"]!;
        if(string.Equals(currentEpoch,retainedEpoch,StringComparison.Ordinal)&&!string.IsNullOrEmpty(row["helper_token"] as string)&&string.Equals(row["provider_epoch"] as string,_provider.ProviderEpoch,StringComparison.Ordinal))return row;
        if(row["handle_b64"] is null||row["handle_type"] is null){MarkFile((string)row["id"]!,"stale",string.Equals(currentEpoch,retainedEpoch,StringComparison.Ordinal)?"file.gap_unproven":"file.world_epoch_changed_unproven");throw new ShellEyeException("ambiguous","No strong exported file handle is available for exact Linux file recovery across the provider gap.");}
        JsonElement recovered=await _provider.RequestAsync("file.recover",new{path=row["path"],devMajor=Convert.ToUInt32(row["dev_major"],CultureInfo.InvariantCulture),devMinor=Convert.ToUInt32(row["dev_minor"],CultureInfo.InvariantCulture),handleType=Convert.ToInt32(row["handle_type"],CultureInfo.InvariantCulture),handleBytesBase64=(string)row["handle_b64"]!,handleMountId=row["handle_mount_id"]},ct);
        var retainedHandle=ToFileConcept(row).ExportedHandle;var recoveredHandle=ExportHandle(recovered);
        bool exactHandle=ProviderIdentityRules.ExportedHandlesEqual(retainedHandle,recoveredHandle);
        bool sameEpochIdentity=string.Equals(currentEpoch,retainedEpoch,StringComparison.Ordinal)&&FileMatches(row,recovered);
        if(!exactHandle&&!sameEpochIdentity){MarkFile((string)row["id"]!,"stale","file.identity_lost");throw new ShellEyeException("stale","Recovered Linux file witness did not prove retained physical identity.");}
        UpdateFile(row,recovered);return LoadFile((string)row["id"]!);
    }

    private async Task<string> EnsureProviderAsync(CancellationToken ct)
    {
        if(_provider.ProviderEpoch is null){JsonElement p=await _provider.ProbeAsync(ct);BindProbe(p);}var row=WorldRow();string? epoch=row["world_epoch"] as string;if(epoch is null){JsonElement p=await _provider.ProbeAsync(ct);BindProbe(p);epoch=RequiredString(p,"worldEpoch");}return epoch;
    }

    private void BindProbe(JsonElement probe)
    {
        string epoch=RequiredString(probe,"worldEpoch"),providerEpoch=RequiredString(probe,"providerEpoch");string? old=WorldRow()["world_epoch"] as string;if(old!=null&&!string.Equals(old,epoch,StringComparison.Ordinal)){_w.Store.Exec("UPDATE linux_processes SET state='destroyed',updated_utc=$u WHERE world_id=$w AND state='current'",("$u",Now()),("$w",_worldId));_w.Store.Exec("UPDATE linux_files SET helper_token=NULL,provider_epoch=NULL,updated_utc=$u WHERE world_id=$w AND state='current'",("$u",Now()),("$w",_worldId));_w.AppendDelta("provider.world_epoch_changed",_worldId,new{provider="linux-wsl2",oldEpoch=old,newEpoch=epoch});}
        string caps=probe.TryGetProperty("capabilities",out var c)?c.GetRawText():"{}";string metadata=JsonSerializer.Serialize(new{distribution=_provider.Distro,relationship="hosted",hostMachineId=_w.MachineId,kernelRelease=OptionalString(probe,"kernelRelease"),machineId=OptionalString(probe,"machineId"),kernelBootId=OptionalString(probe,"kernelBootId"),pidNamespace=OptionalString(probe,"pidNamespace"),initStartTicks=probe.TryGetProperty("initStartTicks",out var it)?it.GetRawText():null},JsonDefaults.Options);
        _w.Store.Exec("UPDATE provider_worlds SET state='current',world_epoch=$e,provider_epoch=$p,capabilities_json=$c,metadata_json=$m,last_error=NULL,updated_utc=$u WHERE world_id=$i",("$e",epoch),("$p",providerEpoch),("$c",caps),("$m",metadata),("$u",Now()),("$i",_worldId));
    }
    private void BindResultWorld(JsonElement result){string epoch=RequiredString(result,"worldEpoch");var row=WorldRow();string? current=row["world_epoch"] as string;if(current is null||!string.Equals(current,epoch,StringComparison.Ordinal)){JsonElement synthetic=JsonDefaults.Element(new{worldEpoch=epoch,providerEpoch=OptionalString(result,"providerEpoch")??_provider.ProviderEpoch??"unknown",capabilities=StoredJson(row["capabilities_json"] as string)});BindProbe(synthetic);}}

    private object PersistProcess(JsonElement p,bool emit)
    {
        string epoch=RequiredString(p,"worldEpoch"),start=RequiredUlong(p,"startTicks").ToString(CultureInfo.InvariantCulture);int pid=RequiredInt(p,"pid"),ppid=RequiredInt(p,"ppid");var existing=_w.Store.Query("SELECT id FROM linux_processes WHERE world_id=$w AND world_epoch=$e AND pid=$p AND start_ticks=$s AND state='current'",("$w",_worldId),("$e",epoch),("$p",pid),("$s",start));string id=existing.Count>0?(string)existing[0]["id"]!:_w.Store.NextId("proc");string? parentId=_w.Store.Query("SELECT id FROM linux_processes WHERE world_id=$w AND world_epoch=$e AND pid=$p AND state='current' ORDER BY updated_utc DESC LIMIT 1",("$w",_worldId),("$e",epoch),("$p",ppid)).FirstOrDefault()?["id"] as string;string pq=parentId is null?"reported":"exact_observed";
        if(existing.Count==0){_w.Store.UpsertConcept(id,"process","current");_w.Store.Exec("INSERT INTO linux_processes(id,world_id,world_epoch,pid,start_ticks,parent_pid,name,executable_path,state,parent_quality,parent_id,updated_utc) VALUES($i,$w,$e,$p,$s,$pp,$n,$x,'current',$q,$pi,$u)",("$i",id),("$w",_worldId),("$e",epoch),("$p",pid),("$s",start),("$pp",ppid),("$n",RequiredString(p,"name")),("$x",OptionalString(p,"executablePath")),("$q",pq),("$pi",parentId),("$u",Now()));if(emit)_w.AppendDelta("process.started",id,new{provider="linux-wsl2",worldId=_worldId,pid,startTicks=start,name=RequiredString(p,"name")});}else UpdateProcess(LoadProcess(id),p);return ProcessObject(LoadProcess(id),p);
    }
    private void UpdateProcess(Dictionary<string,object?> row,JsonElement p)=>_w.Store.Exec("UPDATE linux_processes SET parent_pid=$pp,name=$n,executable_path=$x,updated_utc=$u WHERE id=$i",("$pp",RequiredInt(p,"ppid")),("$n",RequiredString(p,"name")),("$x",OptionalString(p,"executablePath")),("$u",Now()),("$i",row["id"]));
    private LinuxProcessWitness ToProcessWitness(Dictionary<string,object?> r)=>new((string)r["id"]!,(string)r["world_id"]!,(string)r["world_epoch"]!,Convert.ToInt32(r["pid"],CultureInfo.InvariantCulture),Convert.ToInt32(r["parent_pid"],CultureInfo.InvariantCulture),ulong.Parse((string)r["start_ticks"]!,CultureInfo.InvariantCulture),(string)r["name"]!,r["executable_path"] as string,(string)r["state"]!,(string)r["parent_quality"]!,r["parent_id"] as string);
    private object ProcessObject(Dictionary<string,object?> r,JsonElement? live=null)=>new{processId=r["id"],provider="linux-wsl2",worldId=r["world_id"],worldEpoch=r["world_epoch"],pid=r["pid"],startTicks=r["start_ticks"],parentPid=r["parent_pid"],parentId=r["parent_id"],parentQuality=r["parent_quality"],name=r["name"],executablePath=r["executable_path"],state=r["state"],live};

    private object PersistFile(JsonElement f,bool emit)
    {
        string epoch=RequiredString(f,"worldEpoch"),path=RequiredString(f,"path"),kind=RequiredString(f,"kind");var ident=FileIdentity(f);var rev=FileRevision(f);var existing=_w.Store.Query("SELECT id FROM linux_files WHERE world_id=$w AND world_epoch=$e AND dev_major=$a AND dev_minor=$b AND inode=$ino AND mount_id=$m AND state='current'",("$w",_worldId),("$e",epoch),("$a",(long)ident.DevMajor),("$b",(long)ident.DevMinor),("$ino",ident.Inode.ToString(CultureInfo.InvariantCulture)),("$m",ident.MountId.ToString(CultureInfo.InvariantCulture)));string id=existing.Count>0?(string)existing[0]["id"]!:_w.Store.NextId(kind=="dir"?"dir":"file");var handle=ExportHandle(f);string token=RequiredString(f,"token"),providerEpoch=RequiredString(f,"providerEpoch");
        if(existing.Count==0){_w.Store.UpsertConcept(id,kind=="dir"?"directory":"file","current");_w.Store.Exec("INSERT INTO linux_files(id,world_id,world_epoch,kind,path,dev_major,dev_minor,inode,mount_id,unique_mount_id,revision,handle_type,handle_b64,handle_mount_id,helper_token,provider_epoch,state,updated_utc) VALUES($i,$w,$e,$k,$p,$a,$b,$ino,$m,$um,$r,$ht,$hb,$hm,$t,$pe,'current',$u)",("$i",id),("$w",_worldId),("$e",epoch),("$k",kind),("$p",path),("$a",(long)ident.DevMajor),("$b",(long)ident.DevMinor),("$ino",ident.Inode.ToString(CultureInfo.InvariantCulture)),("$m",ident.MountId.ToString(CultureInfo.InvariantCulture)),("$um",ident.UniqueMountId?1:0),("$r",rev.ToString()),("$ht",handle?.Type),("$hb",handle?.BytesBase64),("$hm",handle?.MountId),("$t",token),("$pe",providerEpoch),("$u",Now()));if(emit)_w.AppendDelta("file.retained",id,new{provider="linux-wsl2",worldId=_worldId,path,identity=ident.ToString(),strongGapHandle=handle is not null});}else UpdateFile(LoadFile(id),f);return FileObject(LoadFile(id),f);
    }
private void UpdateFile(Dictionary<string,object?> row,JsonElement f){var h=ExportHandle(f);var ident=FileIdentity(f);_w.Store.Exec("UPDATE linux_files SET world_epoch=$e,path=$p,dev_major=$a,dev_minor=$b,inode=$ino,mount_id=$m,unique_mount_id=$um,revision=$r,helper_token=$t,provider_epoch=$pe,handle_type=COALESCE($ht,handle_type),handle_b64=COALESCE($hb,handle_b64),handle_mount_id=COALESCE($hm,handle_mount_id),updated_utc=$u WHERE id=$i",("$e",RequiredString(f,"worldEpoch")),("$p",RequiredString(f,"path")),("$a",(long)ident.DevMajor),("$b",(long)ident.DevMinor),("$ino",ident.Inode.ToString(CultureInfo.InvariantCulture)),("$m",ident.MountId.ToString(CultureInfo.InvariantCulture)),("$um",ident.UniqueMountId?1:0),("$r",FileRevision(f).ToString()),("$t",RequiredString(f,"token")),("$pe",RequiredString(f,"providerEpoch")),("$ht",h?.Type),("$hb",h?.BytesBase64),("$hm",h?.MountId),("$u",Now()),("$i",row["id"]));}
    private bool FileMatches(Dictionary<string,object?> row,JsonElement f){var retained=ToFileConcept(row);return ProviderIdentityRules.LinuxFileCurrentMatches(retained,_worldId,RequiredString(f,"worldEpoch"),FileIdentity(f));}
    private LinuxFileConcept ToFileConcept(Dictionary<string,object?> r)=>new((string)r["id"]!,(string)r["world_id"]!,(string)r["world_epoch"]!,(string)r["kind"]!,(string)r["path"]!,new LinuxFileIdentity(Convert.ToUInt32(r["dev_major"],CultureInfo.InvariantCulture),Convert.ToUInt32(r["dev_minor"],CultureInfo.InvariantCulture),ulong.Parse((string)r["inode"]!,CultureInfo.InvariantCulture),ulong.Parse((string)r["mount_id"]!,CultureInfo.InvariantCulture),Convert.ToInt32(r["unique_mount_id"],CultureInfo.InvariantCulture)!=0),ParseRevision((string)r["revision"]!),r["handle_b64"] is string hb&&r["handle_type"] is not null?new LinuxExportedFileHandle(Convert.ToInt32(r["handle_type"],CultureInfo.InvariantCulture),hb,Convert.ToInt32(r["handle_mount_id"]??0,CultureInfo.InvariantCulture)):null,(string)r["state"]!);
    private object FileObject(Dictionary<string,object?> r,JsonElement? live=null)=>new{fileId=r["id"],provider="linux-wsl2",worldId=r["world_id"],worldEpoch=r["world_epoch"],kind=r["kind"],path=r["path"],state=r["state"],identity=new{devMajor=r["dev_major"],devMinor=r["dev_minor"],inode=r["inode"],mountId=r["mount_id"],uniqueMountId=Convert.ToInt32(r["unique_mount_id"],CultureInfo.InvariantCulture)!=0},revision=r["revision"],strongGapHandle=r["handle_b64"] is not null,live};
    private static LinuxFileIdentity FileIdentity(JsonElement f){var i=f.GetProperty("identity");return new(i.GetProperty("devMajor").GetUInt32(),i.GetProperty("devMinor").GetUInt32(),i.GetProperty("inode").GetUInt64(),i.GetProperty("mountId").GetUInt64(),i.TryGetProperty("uniqueMountId",out var u)&&u.GetBoolean());}
    private static LinuxFileRevision FileRevision(JsonElement f){var r=f.GetProperty("revision");return new(r.GetProperty("size").GetUInt64(),r.GetProperty("mtimeNs").GetInt64(),r.GetProperty("ctimeNs").GetInt64(),r.TryGetProperty("btimeNs",out var b)&&b.ValueKind==JsonValueKind.Number?b.GetInt64():null);}
    private static string Revision(JsonElement r)=>new LinuxFileRevision(r.GetProperty("size").GetUInt64(),r.GetProperty("mtimeNs").GetInt64(),r.GetProperty("ctimeNs").GetInt64(),r.TryGetProperty("btimeNs",out var b)&&b.ValueKind==JsonValueKind.Number?b.GetInt64():null).ToString();
    private static LinuxFileRevision ParseRevision(string s){var p=s.Split(':');return new(ulong.Parse(p[0],CultureInfo.InvariantCulture),long.Parse(p[1],CultureInfo.InvariantCulture),long.Parse(p[2],CultureInfo.InvariantCulture),p[3]=="-"?null:long.Parse(p[3],CultureInfo.InvariantCulture));}
    private static LinuxExportedFileHandle? ExportHandle(JsonElement f){if(!f.TryGetProperty("exportedHandle",out var h)||h.ValueKind!=JsonValueKind.Object)return null;return new(h.GetProperty("type").GetInt32(),RequiredString(h,"bytesBase64"),h.GetProperty("mountId").GetInt32());}

    private Dictionary<string,object?> WorldRow(){var r=_w.Store.Query("SELECT * FROM provider_worlds WHERE world_id=$i",("$i",_worldId));return r.Count==1?r[0]:throw new ShellEyeException("not_found","Linux provider world disappeared.");}
    private Dictionary<string,object?> LoadProcess(string id){var r=_w.Store.Query("SELECT * FROM linux_processes WHERE id=$i",("$i",id));return r.Count==1?r[0]:throw new ShellEyeException("not_found","Unknown Linux process: "+id);}
    private Dictionary<string,object?> LoadFile(string id){var r=_w.Store.Query("SELECT * FROM linux_files WHERE id=$i",("$i",id));return r.Count==1?r[0]:throw new ShellEyeException("not_found","Unknown Linux file: "+id);}
    private static bool IsCurrent(Dictionary<string,object?> r)=>string.Equals(r["state"] as string,"current",StringComparison.Ordinal);
    private static void RequireCurrent(Dictionary<string,object?> r,string kind){if(!IsCurrent(r))throw new ShellEyeException(r["state"] as string is "destroyed" or "exited"?"destroyed":"stale",$"Retained Linux {kind} is not current.");}
    private void MarkProcess(string id,string state,string delta){_w.Store.Exec("UPDATE linux_processes SET state=$s,updated_utc=$u WHERE id=$i",("$s",state),("$u",Now()),("$i",id));_w.Store.UpsertConcept(id,"process",state);_w.AppendDelta(delta,id,new{provider="linux-wsl2",worldId=_worldId,state});}
    private void MarkFile(string id,string state,string delta){_w.Store.Exec("UPDATE linux_files SET state=$s,updated_utc=$u WHERE id=$i",("$s",state),("$u",Now()),("$i",id));_w.Store.UpsertConcept(id,"file",state);_w.AppendDelta(delta,id,new{provider="linux-wsl2",worldId=_worldId,state});}

    private static JsonElement StoredJson(string? s){using var d=JsonDocument.Parse(string.IsNullOrWhiteSpace(s)?"{}":s);return d.RootElement.Clone();}
    private static string RequiredString(JsonElement e,string name)=>e.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()!:throw new ShellEyeException("native_error","Linux provider result missing string: "+name);
    private static string? OptionalString(JsonElement e,string name)=>e.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():null;
    private static int RequiredInt(JsonElement e,string name)=>e.TryGetProperty(name,out var v)&&v.TryGetInt32(out int x)?x:throw new ShellEyeException("native_error","Linux provider result missing integer: "+name);
    private static ulong RequiredUlong(JsonElement e,string name)=>e.TryGetProperty(name,out var v)&&v.TryGetUInt64(out ulong x)?x:throw new ShellEyeException("native_error","Linux provider result missing unsigned integer: "+name);
    private static string Now()=>DateTimeOffset.UtcNow.ToString("O");
    public void Dispose()=>_provider.Dispose();
}
