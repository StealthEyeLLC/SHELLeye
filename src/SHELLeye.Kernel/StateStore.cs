using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace SHELLeye;

public sealed class StateStore : IDisposable
{
    private readonly SqliteConnection _db;
    private readonly object _gate = new();
    public string Path { get; }
    public StateStore(string path)
    {
        Path = path;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        _db = new SqliteConnection($"Data Source={path};Mode=ReadWriteCreate;Cache=Shared;Pooling=True");
        _db.Open();
        using var c = _db.CreateCommand();
        c.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        c.ExecuteNonQuery();
        Initialize();
    }

    private void Initialize()
    {
        lock (_gate)
        {
            using var c = _db.CreateCommand();
            c.CommandText = @"
CREATE TABLE IF NOT EXISTS meta(key TEXT PRIMARY KEY, value TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS concepts(id TEXT PRIMARY KEY, kind TEXT NOT NULL, state TEXT NOT NULL, created_utc TEXT NOT NULL, updated_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS processes(id TEXT PRIMARY KEY, boot_epoch TEXT NOT NULL, pid INTEGER NOT NULL, sequence INTEGER NOT NULL, creation_ft INTEGER NOT NULL, name TEXT NOT NULL, session_id INTEGER NOT NULL, exe_path TEXT, state TEXT NOT NULL, parent_pid INTEGER, parent_id TEXT, parent_quality TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS ix_process_pid ON processes(boot_epoch,pid);
CREATE TABLE IF NOT EXISTS jobs(id TEXT PRIMARY KEY, native_name TEXT NOT NULL UNIQUE, boot_epoch TEXT NOT NULL, state TEXT NOT NULL, created_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS job_members(job_id TEXT NOT NULL, process_id TEXT NOT NULL, state TEXT NOT NULL, PRIMARY KEY(job_id,process_id));
CREATE TABLE IF NOT EXISTS files(id TEXT PRIMARY KEY, kind TEXT NOT NULL, path TEXT NOT NULL, volume_id TEXT NOT NULL, volume_serial TEXT NOT NULL, file_id TEXT NOT NULL, revision TEXT NOT NULL, journal_id TEXT, last_usn INTEGER, state TEXT NOT NULL, updated_utc TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS ix_file_path ON files(path);
CREATE TABLE IF NOT EXISTS file_paths(file_id TEXT NOT NULL, path TEXT NOT NULL, PRIMARY KEY(file_id,path));
CREATE TABLE IF NOT EXISTS volumes(id TEXT PRIMARY KEY, drive TEXT NOT NULL UNIQUE, volume_guid TEXT NOT NULL, fs TEXT NOT NULL, serial TEXT NOT NULL, total_bytes INTEGER NOT NULL, free_bytes INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS sessions(id TEXT PRIMARY KEY, session_id INTEGER NOT NULL, user_name TEXT, domain_name TEXT, state TEXT NOT NULL, interactive INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS services(id TEXT PRIMARY KEY, name TEXT NOT NULL UNIQUE, state TEXT NOT NULL, pid INTEGER NOT NULL, process_id TEXT);
CREATE TABLE IF NOT EXISTS listeners(id TEXT PRIMARY KEY, af TEXT NOT NULL, address TEXT NOT NULL, port INTEGER NOT NULL, owner_process_id TEXT NOT NULL, owner_pid INTEGER NOT NULL, bind_ft INTEGER, state TEXT NOT NULL, generation INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS spools(job_id TEXT NOT NULL, process_id TEXT NOT NULL, stream TEXT NOT NULL, path TEXT NOT NULL, completed INTEGER NOT NULL DEFAULT 0, PRIMARY KEY(job_id,process_id,stream));
CREATE TABLE IF NOT EXISTS deltas(seq INTEGER PRIMARY KEY AUTOINCREMENT, type TEXT NOT NULL, concept_id TEXT, payload_json TEXT NOT NULL, at_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS runtime(key TEXT PRIMARY KEY, value TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS provider_worlds(world_id TEXT PRIMARY KEY,provider_kind TEXT NOT NULL,provider_key TEXT NOT NULL UNIQUE,name TEXT NOT NULL,host_machine_id TEXT,state TEXT NOT NULL,world_epoch TEXT,provider_epoch TEXT,capabilities_json TEXT,metadata_json TEXT,last_error TEXT,updated_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS linux_processes(id TEXT PRIMARY KEY,world_id TEXT NOT NULL,world_epoch TEXT NOT NULL,pid INTEGER NOT NULL,start_ticks TEXT NOT NULL,parent_pid INTEGER NOT NULL,name TEXT NOT NULL,executable_path TEXT,state TEXT NOT NULL,parent_quality TEXT NOT NULL,parent_id TEXT,updated_utc TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS ix_linux_process_native ON linux_processes(world_id,world_epoch,pid,start_ticks);
CREATE TABLE IF NOT EXISTS linux_files(id TEXT PRIMARY KEY,world_id TEXT NOT NULL,world_epoch TEXT NOT NULL,kind TEXT NOT NULL,path TEXT NOT NULL,dev_major INTEGER NOT NULL,dev_minor INTEGER NOT NULL,inode TEXT NOT NULL,mount_id TEXT NOT NULL,unique_mount_id INTEGER NOT NULL,revision TEXT NOT NULL,handle_type INTEGER,handle_b64 TEXT,handle_mount_id INTEGER,helper_token TEXT,provider_epoch TEXT,state TEXT NOT NULL,updated_utc TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS ix_linux_file_native ON linux_files(world_id,world_epoch,dev_major,dev_minor,inode,mount_id);
";
            c.ExecuteNonQuery();
        }
    }

    public string? GetMeta(string key)
    {
        lock (_gate) { using var c=_db.CreateCommand(); c.CommandText="SELECT value FROM meta WHERE key=$k"; c.Parameters.AddWithValue("$k",key); return c.ExecuteScalar() as string; }
    }
    public void SetMeta(string key, string value)
    {
        lock (_gate) { using var c=_db.CreateCommand(); c.CommandText="INSERT INTO meta(key,value) VALUES($k,$v) ON CONFLICT(key) DO UPDATE SET value=excluded.value"; c.Parameters.AddWithValue("$k",key); c.Parameters.AddWithValue("$v",value); c.ExecuteNonQuery(); }
    }
    public string NextId(string prefix)
    {
        lock (_gate)
        {
            using var tx=_db.BeginTransaction();
            using var r=_db.CreateCommand(); r.Transaction=tx; r.CommandText="SELECT value FROM meta WHERE key=$k"; r.Parameters.AddWithValue("$k","counter:"+prefix);
            long n=1; var v=r.ExecuteScalar() as string; if(v!=null && long.TryParse(v,out var x)) n=x+1;
            using var w=_db.CreateCommand(); w.Transaction=tx; w.CommandText="INSERT INTO meta(key,value) VALUES($k,$v) ON CONFLICT(key) DO UPDATE SET value=excluded.value"; w.Parameters.AddWithValue("$k","counter:"+prefix); w.Parameters.AddWithValue("$v",n.ToString()); w.ExecuteNonQuery();
            tx.Commit(); return prefix+"_"+n;
        }
    }
    public void UpsertConcept(string id,string kind,string state)
    {
        lock(_gate){using var c=_db.CreateCommand(); c.CommandText="INSERT INTO concepts(id,kind,state,created_utc,updated_utc) VALUES($i,$k,$s,$t,$t) ON CONFLICT(id) DO UPDATE SET state=excluded.state,updated_utc=excluded.updated_utc"; c.Parameters.AddWithValue("$i",id);c.Parameters.AddWithValue("$k",kind);c.Parameters.AddWithValue("$s",state);c.Parameters.AddWithValue("$t",DateTimeOffset.UtcNow.ToString("O"));c.ExecuteNonQuery();}
    }
    public void Exec(string sql, params (string,object?)[] values)
    {
        lock(_gate){using var c=_db.CreateCommand(); c.CommandText=sql; foreach(var (k,v) in values)c.Parameters.AddWithValue(k,v??DBNull.Value); c.ExecuteNonQuery();}
    }
    public List<Dictionary<string,object?>> Query(string sql, params (string,object?)[] values)
    {
        lock(_gate){using var c=_db.CreateCommand();c.CommandText=sql;foreach(var(k,v)in values)c.Parameters.AddWithValue(k,v??DBNull.Value);using var r=c.ExecuteReader();var list=new List<Dictionary<string,object?>>();while(r.Read()){var d=new Dictionary<string,object?>(StringComparer.OrdinalIgnoreCase);for(int i=0;i<r.FieldCount;i++)d[r.GetName(i)]=r.IsDBNull(i)?null:r.GetValue(i);list.Add(d);}return list;}
    }
    public long AppendDelta(string type,string? conceptId,object payload,int maxRows=1024)
    {
        lock(_gate)
        {
            using var tx=_db.BeginTransaction();
            using var c=_db.CreateCommand(); c.Transaction=tx; c.CommandText="INSERT INTO deltas(type,concept_id,payload_json,at_utc) VALUES($t,$i,$p,$a); SELECT last_insert_rowid();";
            c.Parameters.AddWithValue("$t",type);c.Parameters.AddWithValue("$i",(object?)conceptId??DBNull.Value);c.Parameters.AddWithValue("$p",JsonSerializer.Serialize(payload,JsonDefaults.Options));c.Parameters.AddWithValue("$a",DateTimeOffset.UtcNow.ToString("O"));
            long seq=(long)c.ExecuteScalar()!;
            using var prune=_db.CreateCommand();prune.Transaction=tx;prune.CommandText="DELETE FROM deltas WHERE seq <= $cut";prune.Parameters.AddWithValue("$cut",Math.Max(0,seq-maxRows));prune.ExecuteNonQuery();tx.Commit();return seq;
        }
    }
    public long CurrentCursor(){lock(_gate){using var c=_db.CreateCommand();c.CommandText="SELECT COALESCE(MAX(seq),0) FROM deltas";return (long)c.ExecuteScalar()!;}}
    public (long min,long max) CursorRange(){lock(_gate){using var c=_db.CreateCommand();c.CommandText="SELECT COALESCE(MIN(seq),0),COALESCE(MAX(seq),0) FROM deltas";using var r=c.ExecuteReader();r.Read();return(r.GetInt64(0),r.GetInt64(1));}}
    public List<DeltaRecord> ReadDeltas(long after,int limit=200)
    {
        var range=CursorRange(); if(range.min>0 && after<range.min-1) throw new ShellEyeException("cursor_expired","Delta cursor is outside bounded retention.");
        var rows=Query("SELECT seq,type,concept_id,payload_json,at_utc FROM deltas WHERE seq>$a ORDER BY seq LIMIT $l",("$a",after),("$l",limit));
        return rows.Select(r=>new DeltaRecord(Convert.ToInt64(r["seq"]), (string)r["type"]!, r["concept_id"] as string, JsonDocument.Parse((string)r["payload_json"]!).RootElement.Clone(), DateTimeOffset.Parse((string)r["at_utc"]!))).ToList();
    }
    public string IntegrityCheck(){lock(_gate){using var c=_db.CreateCommand();c.CommandText="PRAGMA quick_check";return (string)c.ExecuteScalar()!;}}
    public void Dispose()=>_db.Dispose();
}
