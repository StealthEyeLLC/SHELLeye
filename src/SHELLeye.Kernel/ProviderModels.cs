using System.Text.Json;

namespace SHELLeye;

public sealed record LinuxProcessWitness(string Id,string WorldId,string WorldEpoch,int Pid,int ParentPid,ulong StartTicks,string Name,string? ExecutablePath,string State,string ParentQuality,string? ParentId);
public sealed record LinuxFileIdentity(uint DevMajor,uint DevMinor,ulong Inode,ulong MountId,bool UniqueMountId)
{
    public override string ToString()=>$"{DevMajor}:{DevMinor}:{Inode}:{MountId}:{(UniqueMountId?1:0)}";
}
public sealed record LinuxFileRevision(ulong Size,long MTimeNs,long CTimeNs,long? BTimeNs)
{
    public override string ToString()=>$"{Size}:{MTimeNs}:{CTimeNs}:{BTimeNs?.ToString()??"-"}";
}
public sealed record LinuxExportedFileHandle(int Type,string BytesBase64,int MountId);
public sealed record LinuxFileConcept(string Id,string WorldId,string WorldEpoch,string Kind,string Path,LinuxFileIdentity Identity,LinuxFileRevision Revision,LinuxExportedFileHandle? ExportedHandle,string State);

public static class ProviderIdentityRules
{
    public static bool LinuxProcessMatches(LinuxProcessWitness retained,string worldId,string worldEpoch,int pid,ulong startTicks)
        =>StringComparer.Ordinal.Equals(retained.WorldId,worldId)&&StringComparer.Ordinal.Equals(retained.WorldEpoch,worldEpoch)&&retained.Pid==pid&&retained.StartTicks==startTicks;
    public static bool LinuxFileCurrentMatches(LinuxFileConcept retained,string worldId,string worldEpoch,LinuxFileIdentity current)
        =>StringComparer.Ordinal.Equals(retained.WorldId,worldId)&&StringComparer.Ordinal.Equals(retained.WorldEpoch,worldEpoch)&&retained.Identity.DevMajor==current.DevMajor&&retained.Identity.DevMinor==current.DevMinor&&retained.Identity.Inode==current.Inode&&retained.Identity.MountId==current.MountId;
    public static bool ExportedHandlesEqual(LinuxExportedFileHandle? a,LinuxExportedFileHandle? b)
        =>a is not null&&b is not null&&a.Type==b.Type&&StringComparer.Ordinal.Equals(a.BytesBase64,b.BytesBase64);
}
