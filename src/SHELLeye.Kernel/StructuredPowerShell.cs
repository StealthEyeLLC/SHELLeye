using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace SHELLeye;

public sealed class StructuredPowerShell : IDisposable
{
    private readonly Runspace _runspace;private readonly SemaphoreSlim _gate=new(1,1);private readonly WorldContext _w;
    public StructuredPowerShell(WorldContext world)
    {
        _w=world;string bundledModules=Path.Combine(AppContext.BaseDirectory,"runtimes","win","lib","net10.0","Modules");if(Directory.Exists(bundledModules)){string? current=Environment.GetEnvironmentVariable("PSModulePath");if(current is null||!current.Split(Path.PathSeparator,StringSplitOptions.RemoveEmptyEntries).Contains(bundledModules,StringComparer.OrdinalIgnoreCase))Environment.SetEnvironmentVariable("PSModulePath",String.IsNullOrEmpty(current)?bundledModules:bundledModules+Path.PathSeparator+current);}var iss=InitialSessionState.CreateDefault2();_runspace=RunspaceFactory.CreateRunspace(iss);_runspace.Open();
    }
    public async Task<object> InvokeAsync(string command,Dictionary<string,object?> parameters,string[]? properties,CancellationToken ct)
    {
        await _gate.WaitAsync(ct);try
        {
            using var ps=System.Management.Automation.PowerShell.Create();ps.Runspace=_runspace;ps.AddCommand(command);foreach(var kv in parameters)ps.AddParameter(kv.Key,kv.Value);
            using var reg=ct.Register(()=>{try{ps.Stop();}catch{}});var output=await Task.Run(()=>ps.Invoke(),ct);var projected=output.Select(o=>Project(o,properties)).ToArray();
            return new{provider="Microsoft.PowerShell.SDK",engineVersion=PSVersionInfo.PSVersion.ToString(),providerEpoch=_w.PowerShellProviderEpoch,objectCount=projected.Length,objects=projected,errors=ps.Streams.Error.Select(e=>new{message=e.ToString(),fullyQualifiedErrorId=e.FullyQualifiedErrorId,category=e.CategoryInfo.Category.ToString()}).ToArray(),warnings=ps.Streams.Warning.Select(x=>x.Message).ToArray(),verbose=ps.Streams.Verbose.Select(x=>x.Message).ToArray(),information=ps.Streams.Information.Select(x=>x.MessageData?.ToString()).ToArray(),structured=true};
        }finally{_gate.Release();}
    }
    private static object Project(PSObject o,string[]? selected)
    {
        var values=new Dictionary<string,object?>();IEnumerable<PSPropertyInfo> props=selected is {Length:>0}?selected.Select(n=>o.Properties[n]).Where(p=>p!=null)!:o.Properties.Take(48);
        foreach(var p in props){object? v;try{v=Normalize(p.Value,0);}catch(Exception e){v=new{error=e.GetType().Name};}values[p.Name]=v;}
        return new{typeNames=o.TypeNames.Take(6).ToArray(),properties=values};
    }
    private static object? Normalize(object? v,int depth)
    {
        if(v is null)return null;if(depth>2)return v.ToString();if(v is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or DateTime or DateTimeOffset or Guid)return v;if(v is Enum)return v.ToString();if(v is IEnumerable e && v is not IDictionary){var list=new List<object?>();foreach(var x in e){if(list.Count>=32)break;list.Add(Normalize(x,depth+1));}return list;}return v.ToString();
    }
    public void Dispose(){try{_runspace.Close();}catch{} _runspace.Dispose();_gate.Dispose();}
}
