using System.Text.Json;

namespace SHELLeye;

public sealed class WorldContext : IDisposable
{
    public StateStore Store { get; }
    public string StateRoot { get; }
    public string RuntimeRoot { get; }
    public string SpoolRoot { get; }
    public string TempRoot { get; }
    public string MachineId { get; }
    public string MachineUuid { get; }
    public string BootEpoch { get; }
    public uint? NativeBootId { get; }
    public string KernelEpoch { get; }
    public string PowerShellProviderEpoch { get; }
    public ProcessRegistry Processes { get; }
    public FileRegistry Files { get; }
    public JobRegistry Jobs { get; }
    public SystemRegistry System { get; }
    public StructuredPowerShell PowerShell { get; }
    public event Action<DeltaRecord>? DeltaAppended;

    public WorldContext(string stateRoot,string runtimeRoot,string spoolRoot,string tempRoot,string dbPath)
    {
        StateRoot=stateRoot;RuntimeRoot=runtimeRoot;SpoolRoot=spoolRoot;TempRoot=tempRoot;
        Directory.CreateDirectory(stateRoot);Directory.CreateDirectory(runtimeRoot);Directory.CreateDirectory(spoolRoot);Directory.CreateDirectory(tempRoot);
        Store=new StateStore(dbPath);
        MachineUuid=Store.GetMeta("machine_uuid") ?? Guid.NewGuid().ToString("D"); Store.SetMeta("machine_uuid",MachineUuid);
        MachineId=Store.GetMeta("machine_id") ?? Store.NextId("machine"); Store.SetMeta("machine_id",MachineId); Store.UpsertConcept(MachineId,"machine","current");
        (BootEpoch,NativeBootId)=ResolveBootEpoch();
        KernelEpoch=Store.NextId("kernel");
        PowerShellProviderEpoch=Store.NextId("provider");
        Processes=new ProcessRegistry(this);Files=new FileRegistry(this);Jobs=new JobRegistry(this);System=new SystemRegistry(this);PowerShell=new StructuredPowerShell(this);
        Recover();
        WriteRuntimeDescriptor();
    }

    private (string,uint?) ResolveBootEpoch()
    {
        var telemetry=WindowsNative.TryQueryCurrentTelemetry();uint? bootId=telemetry?.BootId;
        string? oldEpoch=Store.GetMeta("boot_epoch");string? oldNative=Store.GetMeta("boot_native_id");
        string epoch;
        if(bootId.HasValue)
        {
            epoch=(oldEpoch!=null && oldNative==bootId.Value.ToString())?oldEpoch:Store.NextId("boot");
            Store.SetMeta("boot_native_id",bootId.Value.ToString());
        }
        else
        {
            long estimated=(DateTimeOffset.UtcNow-TimeSpan.FromMilliseconds(WindowsNative.GetTickCount64())).ToUnixTimeSeconds();
            long oldEstimate=long.TryParse(Store.GetMeta("boot_estimate_unix"),out var x)?x:long.MinValue;
            epoch=(oldEpoch!=null && Math.Abs(estimated-oldEstimate)<=120)?oldEpoch:Store.NextId("boot");
            Store.SetMeta("boot_estimate_unix",estimated.ToString());
        }
        Store.SetMeta("boot_epoch",epoch);return(epoch,bootId);
    }

    private void Recover()
    {
        var priorKernel=Store.GetMeta("last_kernel_epoch");
        if(priorKernel is not null) System.RecoverAfterObservationGap();
        Processes.ReconcileRetained();
        Files.RecoverRetained();
        Jobs.RecoverRetained();
        var seq=AppendDelta("world.reconciled",null,new{reason=priorKernel is null?"startup":"kernel_restart",priorKernel,kernelEpoch=KernelEpoch,bootEpoch=BootEpoch,observationGap=priorKernel is not null});
        Store.SetMeta("last_kernel_epoch",KernelEpoch);
    }

    public long AppendDelta(string type,string? conceptId,object payload)
    {
        long seq=Store.AppendDelta(type,conceptId,payload);var d=Store.ReadDeltas(seq-1,1).Single();DeltaAppended?.Invoke(d);return seq;
    }

    public object MachineInspect()
    {
        var session=System.InspectInteractiveSession();var c=System.InspectVolume("C:");var x=System.InspectVolume("X:");
        return new{machineId=MachineId,machineUuid=MachineUuid,computer=Environment.MachineName,bootEpoch=BootEpoch,nativeBootId=NativeBootId,kernelEpoch=KernelEpoch,powerShellProviderEpoch=PowerShellProviderEpoch,session,volumes=new[]{c,x},worldCursor=Store.CurrentCursor()};
    }

    public void WriteRuntimeDescriptor()
    {
        string pipe=Environment.GetEnvironmentVariable("SHELLEYE_PIPE")??"shelleye-dev";var value=new{pid=Environment.ProcessId,pipe,database=Store.Path,bootEpoch=BootEpoch,nativeBootId=NativeBootId,kernelEpoch=KernelEpoch,powerShellProviderEpoch=PowerShellProviderEpoch,startedUtc=DateTimeOffset.UtcNow};
        File.WriteAllText(Path.Combine(RuntimeRoot,"runtime.json"),JsonSerializer.Serialize(value,new JsonSerializerOptions(JsonDefaults.Options){WriteIndented=true}));
    }

    public void Dispose(){PowerShell.Dispose();Jobs.Dispose();Files.Dispose();Store.Dispose();}
}

