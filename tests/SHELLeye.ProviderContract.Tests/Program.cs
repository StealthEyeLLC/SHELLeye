using System.Security.Principal;
using System.Text.Json;
using SHELLeye;

var cases=new List<object>();
void Pass(string name,object? evidence=null)=>cases.Add(new{name,status="pass",evidence});
void Expect(bool ok,string message){if(!ok)throw new Exception(message);}

var proc=new LinuxProcessWitness("proc_x","world_x","epoch_a",42,1,100,"sleep","/bin/sleep","current","reported",null);
Expect(ProviderIdentityRules.LinuxProcessMatches(proc,"world_x","epoch_a",42,100),"exact process witness rejected");
Expect(!ProviderIdentityRules.LinuxProcessMatches(proc,"world_x","epoch_a",42,101),"PID/start-time reuse accepted");
Expect(!ProviderIdentityRules.LinuxProcessMatches(proc,"world_x","epoch_b",42,100),"provider-world epoch change accepted");
Expect(!ProviderIdentityRules.LinuxProcessMatches(proc,"world_y","epoch_a",42,100),"cross-world PID equality accepted");
Pass("process identity contract",new{pidReuseRejected=true,epochChangeRejected=true,crossWorldRejected=true});

var file=new LinuxFileConcept("file_x","world_x","epoch_a","file","/tmp/x",new LinuxFileIdentity(8,1,42,900,true),new LinuxFileRevision(4,10,11,9),new LinuxExportedFileHandle(1,"AA==",7),"current");
Expect(ProviderIdentityRules.LinuxFileCurrentMatches(file,"world_x","epoch_a",new LinuxFileIdentity(8,1,42,900,true)),"exact file witness rejected");
Expect(!ProviderIdentityRules.LinuxFileCurrentMatches(file,"world_x","epoch_a",new LinuxFileIdentity(8,1,43,900,true)),"inode replacement accepted");
Expect(!ProviderIdentityRules.LinuxFileCurrentMatches(file,"world_x","epoch_a",new LinuxFileIdentity(8,1,42,901,true)),"mount transition accepted");
Expect(!ProviderIdentityRules.LinuxFileCurrentMatches(file,"world_x","epoch_b",new LinuxFileIdentity(8,1,42,900,true)),"provider-world file rebound accepted");
Pass("file identity contract",new{inodeReplacementRejected=true,mountTransitionRejected=true,epochChangeRejected=true});

Expect(ProviderIdentityRules.ExportedHandlesEqual(file.ExportedHandle,new LinuxExportedFileHandle(1,"AA==",99)),"same exported handle rejected");
Expect(!ProviderIdentityRules.ExportedHandlesEqual(file.ExportedHandle,new LinuxExportedFileHandle(2,"AA==",7)),"different exported handle type accepted");
Expect(!ProviderIdentityRules.ExportedHandlesEqual(file.ExportedHandle,null),"missing strong handle accepted");
Pass("strong file gap witness contract",new{missingHandleRejected=true});

string root=Path.Combine(@"C:\SHELLeye\Temp","build002-contract-"+Guid.NewGuid().ToString("N"));
using(var world=new WorldContext(Path.Combine(root,"state"),Path.Combine(root,"runtime"),Path.Combine(root,"spool"),Path.Combine(root,"temp"),Path.Combine(root,"state","contract.db")))
using(var linux=new LinuxProviderRegistry(world))
{
    string descriptorJson=JsonSerializer.Serialize(linux.Describe(),JsonDefaults.Options);
    Expect(descriptorJson.Contains("linux-wsl2",StringComparison.Ordinal),"provider descriptor missing Linux qualification");
    Expect(descriptorJson.Contains("hostMachineId",StringComparison.Ordinal),"provider descriptor missing host relation");
    var providers=linux.ProviderWorlds();
    string pj=JsonSerializer.Serialize(providers,JsonDefaults.Options);
    Expect(pj.Contains("windows",StringComparison.Ordinal)&&pj.Contains("linux-wsl2",StringComparison.Ordinal),"provider world surface does not preserve both worlds");
    Pass("provider worlds preserve qualification",JsonDocument.Parse(descriptorJson).RootElement.Clone());
    if(WindowsIdentity.GetCurrent().IsSystem)
    {
        bool unavailable=false;
        try{await linux.ProbeAsync(CancellationToken.None);}catch(ShellEyeException e)when(e.Code=="provider_unavailable"){unavailable=true;}
        Expect(unavailable,"LocalSystem WSL probe failed to surface structured provider_unavailable");
        Pass("LocalSystem WSL is structured provider_unavailable");
    }
}
try{Directory.Delete(root,true);}catch{}
Console.WriteLine(JsonSerializer.Serialize(new{suite="SHELLeye Build 002 provider contracts",atUtc=DateTimeOffset.UtcNow,total=cases.Count,cases},JsonDefaults.Options));
