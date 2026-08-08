using System.Text.Json;
using System.Text.Json.Serialization;

namespace SHELLeye;

public sealed class ShellEyeException : Exception
{
    public string Code { get; }
    public int? NativeCode { get; }
    public object? Details { get; }
    public ShellEyeException(string code, string message, int? nativeCode = null, object? details = null, Exception? inner = null)
        : base(message, inner) { Code = code; NativeCode = nativeCode; Details = details; }
}

public sealed record ProcessSnapshot(uint Pid, uint ParentPid, ulong SequenceNumber, string Name);
public sealed record ProcessWitness(string Id, string BootEpoch, uint Pid, ulong SequenceNumber, long CreationFileTimeUtc,
    string Name, uint SessionId, string? ExecutablePath, string State, string ParentQuality, string? ParentId);
public sealed record ProcessTelemetry(uint ProcessId, ulong SequenceNumber, uint SessionId, uint BootId, ulong ProcessStartKey, long CreateTime);
public sealed record FileIdentity(ulong VolumeSerial, string FileId128)
{
    public override string ToString() => $"{VolumeSerial:x16}:{FileId128}";
}
public sealed record FileRevision(long Length, long LastWriteFileTime)
{
    public override string ToString() => $"{Length}:{LastWriteFileTime}";
}
public sealed record FileContinuity(string? JournalId, long? LastUsn);
public sealed record FileConcept(string Id, string Kind, string Path, FileIdentity Identity, FileRevision Revision,
    string VolumeId, FileContinuity Continuity, string State);
public sealed record JobConcept(string Id, string NativeName, string BootEpoch, string State);
public sealed record ListenerConcept(string Id, string AddressFamily, string Address, int Port, string OwnerProcessId,
    uint OwnerPid, long? BindFileTimeUtc, string State, long ObservationGeneration);
public sealed record DeltaRecord(long Cursor, string Type, string? ConceptId, JsonElement Payload, DateTimeOffset AtUtc);
public sealed record VolumeConcept(string Id, string Drive, string VolumeGuid, string FileSystem, ulong Serial,
    long TotalBytes, long FreeBytes);
public sealed record SessionConcept(string Id, uint SessionId, string? User, string? Domain, string State, bool IsInteractive);
public sealed record ServiceConcept(string Id, string Name, string State, uint Pid, string? ProcessId);
public sealed record RpcRequest(string? Jsonrpc, JsonElement Id, string Method, JsonElement Params, int? TimeoutMs);

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
    public static JsonElement Element(object value) => JsonSerializer.SerializeToElement(value, Options);
}
