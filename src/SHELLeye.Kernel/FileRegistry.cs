using System.Collections.Concurrent;

namespace SHELLeye;

public sealed class FileRegistry : IDisposable
{
    private readonly WorldContext _w;private readonly ConcurrentDictionary<string,FileSystemWatcher> _watchers=new(StringComparer.OrdinalIgnoreCase);
    public FileRegistry(WorldContext world)=>_w=world;

    public FileConcept CreateDirectory(string path){Directory.CreateDirectory(path);var f=RetainPath(path,"dir");_w.AppendDelta("file.created",f.Id,new{id=f.Id,path=f.Path,kind="dir"});return f;}
    public FileConcept CreateFile(string path,string content){Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,content,new System.Text.UTF8Encoding(false));var f=RetainPath(path,"file");_w.AppendDelta("file.created",f.Id,new{id=f.Id,path=f.Path,kind="file",revision=f.Revision.ToString()});return f;}

    public FileConcept RetainPath(string path,string? forcedKind=null)
    {
        path=Path.GetFullPath(path);IntPtr h=WindowsNative.OpenPath(path,false,false);try
        {
            var id=WindowsNative.QueryFileIdentity(h);var(r,dir,_)=WindowsNative.QueryFileRevision(h);string kind=forcedKind??(dir?"dir":"file");string drive=Path.GetPathRoot(path)!.TrimEnd('\\');var vol=_w.System.InspectVolume(drive);var continuity=WindowsNative.QueryFileContinuity(path);
            var rows=_w.Store.Query("SELECT * FROM files WHERE volume_serial=$v AND file_id=$f AND state='current' ORDER BY updated_utc DESC",("$v",id.VolumeSerial.ToString()),("$f",id.FileId128));
            if(rows.Count>0)
            {
                var existing=FromRow(rows[0]);bool oldBindingExists=File.Exists(existing.Path)||Directory.Exists(existing.Path);if(!oldBindingExists){_w.Store.Exec("DELETE FROM file_paths WHERE file_id=$i AND path=$o",("$i",existing.Id),("$o",existing.Path));_w.Store.Exec("UPDATE files SET path=$p,revision=$r,journal_id=$j,last_usn=$u,updated_utc=$t WHERE id=$i",("$p",path),("$r",r.ToString()),("$j",continuity.JournalId),("$u",continuity.LastUsn),("$t",DateTimeOffset.UtcNow.ToString("O")),("$i",existing.Id));}_w.Store.Exec("INSERT OR IGNORE INTO file_paths(file_id,path) VALUES($i,$p)",("$i",existing.Id),("$p",path));return existing with{Path=path,Revision=r,Continuity=continuity};
            }
            string logical=_w.Store.NextId(kind);_w.Store.UpsertConcept(logical,kind,"current");
            _w.Store.Exec("INSERT INTO files(id,kind,path,volume_id,volume_serial,file_id,revision,journal_id,last_usn,state,updated_utc) VALUES($i,$k,$p,$vi,$vs,$fi,$r,$j,$u,'current',$t)",("$i",logical),("$k",kind),("$p",path),("$vi",vol.Id),("$vs",id.VolumeSerial.ToString()),("$fi",id.FileId128),("$r",r.ToString()),("$j",continuity.JournalId),("$u",continuity.LastUsn),("$t",DateTimeOffset.UtcNow.ToString("O")));
            _w.Store.Exec("INSERT OR IGNORE INTO file_paths(file_id,path) VALUES($i,$p)",("$i",logical),("$p",path));EnsureWatcher(Path.GetDirectoryName(path)??path);return new FileConcept(logical,kind,path,id,r,vol.Id,continuity,"current");
        }finally{WindowsNative.CloseHandle(h);}
    }

    public FileConcept Load(string logical)
    {
        var rows=_w.Store.Query("SELECT * FROM files WHERE id=$i",("$i",logical));if(rows.Count==0)throw new ShellEyeException("not_found","File concept not found.");return FromRow(rows[0]);
    }
    private static FileConcept FromRow(Dictionary<string,object?> r)
    {
        var bits=((string)r["revision"]!).Split(':');var rev=new FileRevision(long.Parse(bits[0]),long.Parse(bits[1]));var identity=new FileIdentity(ulong.Parse((string)r["volume_serial"]!),(string)r["file_id"]!);var cont=new FileContinuity(r["journal_id"] as string,r["last_usn"] is null?null:Convert.ToInt64(r["last_usn"]));
        return new FileConcept((string)r["id"]!,(string)r["kind"]!,(string)r["path"]!,identity,rev,(string)r["volume_id"]!,cont,(string)r["state"]!);
    }
    public object Inspect(string logical)
    {
        var f=Load(logical);
        if(f.State=="current")
        {
            try
            {
                using var h=WindowsNative.OpenVerifiedFile(f,false,null);
                var rev=WindowsNative.QueryFileRevision(h.Handle).revision;
                var cont=WindowsNative.QueryFileContinuity(f.Path);
                if(rev!=f.Revision || cont!=f.Continuity){UpdateRevision(f.Id,f.Path,rev,cont);f=f with{Revision=rev,Continuity=cont};}
            }
            catch(ShellEyeException e) when(e.Code is "stale" or "not_found")
            {
                string state=e.Code=="stale"?"replaced":"destroyed";_w.Store.Exec("UPDATE files SET state=$s WHERE id=$i",("$s",state),("$i",f.Id));_w.Store.UpsertConcept(f.Id,f.Kind,state);f=f with{State=state};
            }
        }
        var paths=_w.Store.Query("SELECT path FROM file_paths WHERE file_id=$i",("$i",logical)).Select(x=>(string)x["path"]!).ToArray();return new{fileId=f.Id,kind=f.Kind,state=f.State,path=f.Path,paths,identity=f.Identity,revision=f.Revision,identityToken=f.Identity.ToString(),revisionToken=f.Revision.ToString(),continuity=f.Continuity,volumeId=f.VolumeId};
    }
    public string Read(string logical)=>WindowsNative.ReadVerifiedFile(Load(logical));

    public FileConcept Write(string logical,string content,string? expectedRevision)
    {
        var f=Load(logical);if(f.State!="current")throw new ShellEyeException("destroyed","Retained file is terminal.");var rev=WindowsNative.WriteVerifiedFile(f,content,expectedRevision??f.Revision.ToString());var cont=WindowsNative.QueryFileContinuity(f.Path);UpdateRevision(f.Id,f.Path,rev,cont);_w.AppendDelta("file.changed",f.Id,new{id=f.Id,path=f.Path,revision=rev.ToString()});return Load(f.Id);
    }
    public FileConcept Rename(string logical,string newPath)
    {
        var f=Load(logical);string old=f.Path;newPath=Path.GetFullPath(newPath);WindowsNative.RenameVerifiedFile(f,newPath,false,null);IntPtr h=WindowsNative.OpenPath(newPath,false,false);try{var id=WindowsNative.QueryFileIdentity(h);if(id!=f.Identity)throw new ShellEyeException("stale","Rename did not preserve expected physical identity.");var rev=WindowsNative.QueryFileRevision(h).revision;var cont=WindowsNative.QueryFileContinuity(newPath);_w.Store.Exec("UPDATE files SET path=$p,revision=$r,journal_id=$j,last_usn=$u,updated_utc=$t WHERE id=$i",("$p",newPath),("$r",rev.ToString()),("$j",cont.JournalId),("$u",cont.LastUsn),("$t",DateTimeOffset.UtcNow.ToString("O")),("$i",f.Id));_w.Store.Exec("DELETE FROM file_paths WHERE file_id=$i AND path=$o",("$i",f.Id),("$o",old));_w.Store.Exec("INSERT OR IGNORE INTO file_paths(file_id,path) VALUES($i,$p)",("$i",f.Id),("$p",newPath));EnsureWatcher(Path.GetDirectoryName(newPath)??newPath);_w.AppendDelta("file.renamed",f.Id,new{id=f.Id,oldPath=old,newPath,identity=f.Identity});return Load(f.Id);}finally{WindowsNative.CloseHandle(h);}
    }
    public void Delete(string logical)
    {
        var f=Load(logical);WindowsNative.DeleteVerifiedFile(f,null);_w.Store.Exec("UPDATE files SET state='destroyed',updated_utc=$t WHERE id=$i",("$t",DateTimeOffset.UtcNow.ToString("O")),("$i",logical));_w.Store.UpsertConcept(logical,f.Kind,"destroyed");_w.AppendDelta("file.deleted",logical,new{id=logical,path=f.Path});
    }
    public string AddHardLink(string logical,string newPath)
    {
        var f=Load(logical);if(f.Kind!="file")throw new ShellEyeException("unsupported","Hard links are only supported for files in Build 001.");newPath=Path.GetFullPath(newPath);if(!WindowsNative.CreateHardLinkW(newPath,f.Path,IntPtr.Zero))throw WindowsNative.Win32("native_error","CreateHardLink failed.");var candidate=RetainPath(newPath);if(candidate.Id!=logical)throw new ShellEyeException("native_error","Hard link resolved to a different physical concept.");_w.Store.Exec("INSERT OR IGNORE INTO file_paths(file_id,path) VALUES($i,$p)",("$i",logical),("$p",newPath));return newPath;
    }
    public object ListDirectory(string logical)
    {
        var d=Load(logical);if(d.Kind!="dir")throw new ShellEyeException("unsupported","Concept is not a directory.");using var vh=WindowsNative.OpenVerifiedFile(d,false,null);return new{directoryId=d.Id,path=d.Path,entries=Directory.EnumerateFileSystemEntries(d.Path).Select(Path.GetFileName).Order().ToArray()};
    }
    public async Task<object> WaitForChangeAsync(string logical,string baselineRevision,int timeoutMs,CancellationToken ct)
    {
        var until=DateTime.UtcNow.AddMilliseconds(timeoutMs);while(DateTime.UtcNow<until){ct.ThrowIfCancellationRequested();var f=Load(logical);try{using var h=WindowsNative.OpenVerifiedFile(f,false,null);var rev=WindowsNative.QueryFileRevision(h.Handle).revision;if(rev.ToString()!=baselineRevision){UpdateRevision(logical,f.Path,rev,WindowsNative.QueryFileContinuity(f.Path));return new{fileId=logical,changed=true,revision=rev.ToString()};}}catch(ShellEyeException e)when(e.Code is "stale" or "not_found"){return new{fileId=logical,changed=true,state=e.Code};}await Task.Delay(50,ct);}throw new ShellEyeException("timeout","File condition wait timed out.");
    }
    private void UpdateRevision(string id,string path,FileRevision rev,FileContinuity cont)=>_w.Store.Exec("UPDATE files SET revision=$r,journal_id=$j,last_usn=$u,updated_utc=$t WHERE id=$i",("$r",rev.ToString()),("$j",cont.JournalId),("$u",cont.LastUsn),("$t",DateTimeOffset.UtcNow.ToString("O")),("$i",id));

    public void RecoverRetained()
    {
        foreach(var row in _w.Store.Query("SELECT * FROM files WHERE state='current'"))
        {
            var f=FromRow(row);bool exact=false;string state="stale";
            foreach(var pr in _w.Store.Query("SELECT path FROM file_paths WHERE file_id=$i",("$i",f.Id)))
            {
                string path=(string)pr["path"]!;if(!File.Exists(path)&&!Directory.Exists(path))continue;try{IntPtr h=WindowsNative.OpenPath(path,false,false);try{var identity=WindowsNative.QueryFileIdentity(h);if(identity!=f.Identity)continue;var c=WindowsNative.QueryFileContinuity(path);if(CanRecoverAcrossGap(f.Identity,f.Continuity,identity,c)){exact=true;state="current";_w.Store.Exec("UPDATE files SET path=$p,state='current' WHERE id=$i",("$p",path),("$i",f.Id));EnsureWatcher(Path.GetDirectoryName(path)??path);break;}state="ambiguous";}finally{WindowsNative.CloseHandle(h);}}catch{}}
            if(!exact){_w.Store.Exec("UPDATE files SET state=$s WHERE id=$i",("$s",state),("$i",f.Id));_w.Store.UpsertConcept(f.Id,f.Kind,state);}
        }
    }
    public object SyncCurrent()
    {
        int checkedCount=0,changed=0,replaced=0;
        foreach(var row in _w.Store.Query("SELECT * FROM files WHERE state='current'"))
        {
            var f=FromRow(row);checkedCount++;try{using var h=WindowsNative.OpenVerifiedFile(f,false,null);var rev=WindowsNative.QueryFileRevision(h.Handle).revision;if(rev.ToString()!=f.Revision.ToString()){UpdateRevision(f.Id,f.Path,rev,WindowsNative.QueryFileContinuity(f.Path));changed++;_w.AppendDelta("file.changed",f.Id,new{id=f.Id,path=f.Path,revision=rev.ToString(),reconciled=true});}}
            catch(ShellEyeException e)when(e.Code is "stale" or "not_found"){string state=e.Code=="stale"?"replaced":"destroyed";_w.Store.Exec("UPDATE files SET state=$s WHERE id=$i",("$s",state),("$i",f.Id));_w.Store.UpsertConcept(f.Id,f.Kind,state);replaced++;_w.AppendDelta(state=="replaced"?"file.replaced":"file.deleted",f.Id,new{id=f.Id,path=f.Path,reconciled=true});}
        }
        return new{checkedCount,changed,replaced};
    }
    public static bool CanRecoverAcrossGap(FileIdentity retainedId,FileContinuity retained,FileIdentity currentId,FileContinuity current)
    {
        if(retainedId!=currentId)return false;if(retained.JournalId is null||retained.LastUsn is null)return false;return StringComparer.OrdinalIgnoreCase.Equals(retained.JournalId,current.JournalId)&&retained.LastUsn==current.LastUsn;
    }
    private void EnsureWatcher(string directory)
    {
        if(!Directory.Exists(directory)||_watchers.ContainsKey(directory))return;var w=new FileSystemWatcher(directory){NotifyFilter=NotifyFilters.FileName|NotifyFilters.DirectoryName|NotifyFilters.LastWrite|NotifyFilters.Size,IncludeSubdirectories=false,EnableRaisingEvents=true};FileSystemEventHandler changed=(s,e)=>_w.AppendDelta("file.changed",null,new{path=e.FullPath,dirty=true,watcher=true});RenamedEventHandler renamed=(s,e)=>_w.AppendDelta("file.renamed",null,new{oldPath=e.OldFullPath,newPath=e.FullPath,dirty=true,watcher=true});ErrorEventHandler error=(s,e)=>_w.AppendDelta("world.reconciled",null,new{scope=directory,observationGap=true,reason="watcher_error",error=e.GetException()?.Message});w.Changed+=changed;w.Created+=changed;w.Deleted+=changed;w.Renamed+=renamed;w.Error+=error;_watchers[directory]=w;
    }
    public void Dispose(){foreach(var w in _watchers.Values)w.Dispose();_watchers.Clear();}
}






