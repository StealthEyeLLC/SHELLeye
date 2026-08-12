using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SHELLeye;

public sealed class LinuxWslProvider : IDisposable
{
    private readonly SemaphoreSlim _gate=new(1,1);
    private readonly string _distro;
    private readonly string _providerKey;
    private readonly string _wslExe;
    private readonly string _helperWindowsPath;
    private readonly string _helperLinuxPath;
    private readonly StringBuilder _stderr=new();
    private Process? _helper;
    private Process? _anchor;
    private readonly string _anchorStatePath;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private long _nextId;
    private string? _providerEpoch;

    public string Distro=>_distro;
    public string ProviderKey=>_providerKey;
    public string? ProviderEpoch=>_providerEpoch;

    public LinuxWslProvider()
    {
        _distro=Environment.GetEnvironmentVariable("SHELLEYE_WSL_DISTRO")??"Ubuntu-24.04";
        string registration=Environment.GetEnvironmentVariable("SHELLEYE_WSL_REGISTRATION")??"{aa957c59-794f-4ad3-ae28-9188cae51ee3}";
        _providerKey="wsl:"+registration;
        _wslExe=File.Exists(@"C:\Program Files\WSL\wsl.exe")?@"C:\Program Files\WSL\wsl.exe":@"C:\Windows\System32\wsl.exe";
        _helperWindowsPath=Environment.GetEnvironmentVariable("SHELLEYE_LINUX_HELPER_WINDOWS")??@"C:\SHELLeye\runtime\linux\app\SHELLeye.Platform.Linux";
        _helperLinuxPath=Environment.GetEnvironmentVariable("SHELLEYE_LINUX_HELPER_LINUX")??"/var/tmp/shelleye-build002/SHELLeye.Platform.Linux";
        string stateRoot=Environment.GetEnvironmentVariable("SHELLEYE_STATE_ROOT")??Path.GetDirectoryName(Path.GetDirectoryName(_helperWindowsPath))??Path.GetTempPath();_anchorStatePath=Environment.GetEnvironmentVariable("SHELLEYE_LINUX_ANCHOR_STATE")??Path.Combine(stateRoot,"linux-provider-anchor.json");
    }

    public Task<JsonElement> ProbeAsync(CancellationToken ct=default)=>RequestAsync("probe",new{},ct);

    public async Task<JsonElement> RequestAsync(string method,object parameters,CancellationToken ct=default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await EnsureHelperAsync(ct);
            long id=Interlocked.Increment(ref _nextId);
            string request=JsonSerializer.Serialize(new{id,method,@params=parameters},JsonDefaults.Options);
            try{await _writer!.WriteLineAsync(request.AsMemory(),ct);await _writer.FlushAsync(ct);}
            catch(Exception e){ResetHelper();throw new ShellEyeException("provider_unavailable","Linux provider transport write failed.",details:new{distro=_distro},inner:e);}
            string? line;
            try{line=await _reader!.ReadLineAsync(ct);}
            catch(Exception e){ResetHelper();throw new ShellEyeException("provider_unavailable","Linux provider transport read failed.",details:new{distro=_distro},inner:e);}
            if(line is null){string err=CurrentStderr();ResetHelper();throw new ShellEyeException("provider_unavailable","Linux provider helper exited.",details:new{distro=_distro,stderr=err});}
            using var doc=JsonDocument.Parse(line);var root=doc.RootElement;
            if(root.TryGetProperty("error",out var error)&&error.ValueKind==JsonValueKind.Object)
            {
                string code=error.TryGetProperty("code",out var c)?c.GetString()??"native_error":"native_error";
                string message=error.TryGetProperty("message",out var m)?m.GetString()??"Linux provider operation failed.":"Linux provider operation failed.";
                int? native=error.TryGetProperty("nativeCode",out var n)&&n.ValueKind==JsonValueKind.Number&&n.TryGetInt32(out int ni)?ni:null;
                object? details=error.TryGetProperty("details",out var d)&&d.ValueKind!=JsonValueKind.Null?d.Clone():null;
                throw new ShellEyeException(code,message,native,details);
            }
            if(!root.TryGetProperty("result",out var result))throw new ShellEyeException("native_error","Linux provider response omitted result.");
            return result.Clone();
        }
        finally{_gate.Release();}
    }

    private async Task EnsureHelperAsync(CancellationToken ct)
    {
        if(_helper is{HasExited:false}&&_writer is not null&&_reader is not null)return;
        ResetHelper();
        EnsureProviderAnchor();
        if(!File.Exists(_helperWindowsPath))throw new ShellEyeException("provider_unavailable","Cross-published Linux helper is missing.",details:new{helperWindowsPath=_helperWindowsPath});
        string? sourceDirectory=Path.GetDirectoryName(_helperWindowsPath);if(String.IsNullOrEmpty(sourceDirectory))throw new ShellEyeException("provider_unavailable","Cross-published Linux helper directory is invalid.",details:new{helperWindowsPath=_helperWindowsPath});
        int slash=_helperLinuxPath.LastIndexOf('/');if(slash<=0)throw new ShellEyeException("provider_unavailable","Linux helper target path is invalid.",details:new{helperLinuxPath=_helperLinuxPath});
        string targetDirectory=_helperLinuxPath[..slash],sourceDirectoryWsl=ToWslPath(sourceDirectory);
        await RunWslCommandAsync("/bin/rm",new[]{"-rf",targetDirectory},ct);
        await RunWslCommandAsync("/bin/mkdir",new[]{"-p",targetDirectory},ct);
        await RunWslCommandAsync("/bin/cp",new[]{"-a",sourceDirectoryWsl+"/.",targetDirectory+"/"},ct);
        await RunWslCommandAsync("/bin/chmod",new[]{"0755",_helperLinuxPath},ct);
        var psi=CreateWslStartInfo(_helperLinuxPath,new[]{"--server","--provider-key",_providerKey,"--distro",_distro});
        psi.RedirectStandardInput=true;psi.RedirectStandardOutput=true;psi.RedirectStandardError=true;
        Process p;
        try{p=Process.Start(psi)??throw new ShellEyeException("provider_unavailable","wsl.exe returned no provider process.");}
        catch(ShellEyeException){throw;}catch(Exception e){throw new ShellEyeException("provider_unavailable","Failed to start WSL provider bridge.",details:new{distro=_distro,wsl=_wslExe},inner:e);}
        _stderr.Clear();p.ErrorDataReceived+=(_,e)=>{if(e.Data is not null)lock(_stderr){if(_stderr.Length<16384)_stderr.AppendLine(NormalizeWslText(e.Data));}};p.BeginErrorReadLine();
        _helper=p;_writer=p.StandardInput;_reader=p.StandardOutput;
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(10));
        string? ready;
        try{ready=await _reader.ReadLineAsync(timeout.Token);}
        catch(OperationCanceledException e){string err=CurrentStderr();ResetHelper();throw new ShellEyeException("provider_unavailable","Timed out starting Linux provider helper.",details:new{distro=_distro,stderr=err},inner:e);}
        if(ready is null){string err=CurrentStderr();int? exit=p.HasExited?p.ExitCode:null;ResetHelper();throw new ShellEyeException("provider_unavailable","WSL provider helper did not start.",details:new{distro=_distro,exitCode=exit,stderr=err});}
        try{using var d=JsonDocument.Parse(ready);var root=d.RootElement;if(!root.TryGetProperty("type",out var t)||t.GetString()!="shelleye.linux.ready")throw new Exception("Unexpected ready record.");_providerEpoch=root.TryGetProperty("providerEpoch",out var pe)?pe.GetString():null;}
        catch(Exception e){string err=CurrentStderr();ResetHelper();throw new ShellEyeException("provider_unavailable","Linux provider helper returned an invalid ready record.",details:new{ready,stderr=err},inner:e);}
    }

    private sealed record AnchorState(int Pid,long StartTimeUtcTicks,string Executable,string Distro,string ProviderKey,string CreatedUtc);
    private void EnsureProviderAnchor()
    {
        if(_anchor is{HasExited:false})return;
        try{_anchor?.Dispose();}catch{} _anchor=null;
        try
        {
            if(File.Exists(_anchorStatePath))
            {
                AnchorState? state=null;try{state=JsonSerializer.Deserialize<AnchorState>(File.ReadAllText(_anchorStatePath),JsonDefaults.Options);}catch{}
                if(state is not null&&StringComparer.Ordinal.Equals(state.Distro,_distro)&&StringComparer.Ordinal.Equals(state.ProviderKey,_providerKey))
                {
                    try
                    {
                        var existing=Process.GetProcessById(state.Pid);
                        string? existingExe=existing.MainModule?.FileName;
                        long ticks=existing.StartTime.ToUniversalTime().Ticks;
                        if(!existing.HasExited&&ticks==state.StartTimeUtcTicks&&existingExe is not null&&StringComparer.OrdinalIgnoreCase.Equals(Path.GetFullPath(existingExe),Path.GetFullPath(state.Executable))){_anchor=existing;return;}
                        existing.Dispose();
                    }
                    catch{}
                }
                try{File.Delete(_anchorStatePath);}catch{}
            }
            string? dir=Path.GetDirectoryName(_anchorStatePath);if(!String.IsNullOrEmpty(dir))Directory.CreateDirectory(dir);
            var psi=CreateWslStartInfo("/usr/bin/sleep",new[]{"infinity"});psi.RedirectStandardInput=false;psi.RedirectStandardOutput=false;psi.RedirectStandardError=false;
            var anchor=Process.Start(psi)??throw new ShellEyeException("provider_unavailable","WSL provider lifetime anchor did not start.",details:new{distro=_distro});
            if(anchor.WaitForExit(300)){int exit=anchor.ExitCode;anchor.Dispose();throw new ShellEyeException("provider_unavailable","WSL provider lifetime anchor exited during startup.",details:new{distro=_distro,exitCode=exit});}
            string exe=anchor.MainModule?.FileName??throw new ShellEyeException("provider_unavailable","WSL provider lifetime anchor executable identity is unavailable.",details:new{distro=_distro,pid=anchor.Id});
            long startTicks=anchor.StartTime.ToUniversalTime().Ticks;
            _anchor=anchor;
            var record=new AnchorState(anchor.Id,startTicks,Path.GetFullPath(exe),_distro,_providerKey,DateTimeOffset.UtcNow.ToString("O"));
            string temp=_anchorStatePath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(record,JsonDefaults.Options));File.Move(temp,_anchorStatePath,true);
        }
        catch(ShellEyeException){ResetAnchor();throw;}
        catch(Exception e){ResetAnchor();throw new ShellEyeException("provider_unavailable","Failed to establish WSL provider lifetime anchor.",details:new{distro=_distro,anchorStatePath=_anchorStatePath},inner:e);}
    }
    private void ResetAnchor()
    {
        if(_anchor is not null){try{if(!_anchor.HasExited)_anchor.Kill(true);}catch{}try{_anchor.Dispose();}catch{}}_anchor=null;
        try{if(File.Exists(_anchorStatePath))File.Delete(_anchorStatePath);}catch{}
        try{if(File.Exists(_anchorStatePath+".tmp"))File.Delete(_anchorStatePath+".tmp");}catch{}
    }
    private async Task RunWslCommandAsync(string executable,IEnumerable<string> args,CancellationToken ct)
    {
        var psi=CreateWslStartInfo(executable,args);psi.RedirectStandardOutput=true;psi.RedirectStandardError=true;
        Process p;try{p=Process.Start(psi)??throw new ShellEyeException("provider_unavailable","wsl.exe returned no process.");}catch(ShellEyeException){throw;}catch(Exception e){throw new ShellEyeException("provider_unavailable","Failed to invoke WSL provider command.",details:new{distro=_distro,executable},inner:e);}
        using(p)
        {
            Task<string> stdoutTask=p.StandardOutput.ReadToEndAsync(ct),stderrTask=p.StandardError.ReadToEndAsync(ct);using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(15));
            try{await p.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException e){try{p.Kill(true);}catch{}throw new ShellEyeException("provider_unavailable","WSL provider bootstrap timed out.",details:new{distro=_distro,executable},inner:e);}
            string stdout=NormalizeWslText(await stdoutTask),stderr=NormalizeWslText(await stderrTask);
            if(p.ExitCode!=0)
            {
                string combined=(stdout+"\n"+stderr).Trim();string message=combined.Contains("WSL_E_LOCAL_SYSTEM_NOT_SUPPORTED",StringComparison.OrdinalIgnoreCase)||combined.Contains("local system is not supported",StringComparison.OrdinalIgnoreCase)?"WSL is unavailable from the LocalSystem execution context.":"WSL provider bootstrap failed.";
                throw new ShellEyeException("provider_unavailable",message,details:new{distro=_distro,executable,exitCode=p.ExitCode,stdout,stderr});
            }
        }
    }

    private ProcessStartInfo CreateWslStartInfo(string executable,IEnumerable<string> args)
    {
        var psi=new ProcessStartInfo(_wslExe){UseShellExecute=false,CreateNoWindow=true};
        psi.ArgumentList.Add("--distribution");psi.ArgumentList.Add(_distro);psi.ArgumentList.Add("--user");psi.ArgumentList.Add("root");psi.ArgumentList.Add("--exec");psi.ArgumentList.Add(executable);foreach(string arg in args)psi.ArgumentList.Add(arg);return psi;
    }
    private static string ToWslPath(string windowsPath){string full=Path.GetFullPath(windowsPath);if(full.Length<3||full[1]!=':')throw new ShellEyeException("invalid_argument","Linux helper path must be on a drive visible to WSL.");return $"/mnt/{Char.ToLowerInvariant(full[0])}/"+full[3..].Replace('\\','/');}
    private string CurrentStderr(){lock(_stderr)return _stderr.ToString();}
    private static string NormalizeWslText(string text)=>text.Replace("\0","").Trim();
    private void ResetHelper(){try{_writer?.Dispose();}catch{}try{_reader?.Dispose();}catch{}if(_helper is not null){try{if(!_helper.HasExited)_helper.Kill(true);}catch{}try{_helper.Dispose();}catch{}}_helper=null;_writer=null;_reader=null;_providerEpoch=null;}
    public void Dispose(){_gate.Wait();try{ResetHelper();ResetAnchor();}finally{_gate.Release();_gate.Dispose();}}
}
