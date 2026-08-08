using SHELLeye;

string stateRoot=Environment.GetEnvironmentVariable("SHELLEYE_STATE_ROOT")??@"C:\SHELLeye\state";
string runtimeRoot=Environment.GetEnvironmentVariable("SHELLEYE_RUNTIME_ROOT")??@"C:\SHELLeye\runtime\kernel";
string spoolRoot=Environment.GetEnvironmentVariable("SHELLEYE_SPOOL_ROOT")??@"C:\SHELLeye\spool";
string tempRoot=Environment.GetEnvironmentVariable("SHELLEYE_TEMP_ROOT")??@"C:\SHELLeye\Temp";
string db=Environment.GetEnvironmentVariable("SHELLEYE_DB")??Path.Combine(stateRoot,"shelleye-dev.db");
string pipe=Environment.GetEnvironmentVariable("SHELLEYE_PIPE")??"shelleye-dev";
using var singleton=new Mutex(true,@"Local\SHELLeye.Kernel.Build001",out bool created);if(!created){Console.Error.WriteLine("SHELLeye.Kernel already running.");return 2;}
using var world=new WorldContext(stateRoot,runtimeRoot,spoolRoot,tempRoot,db);await using var server=new KernelServer(pipe,world);using var cts=new CancellationTokenSource();Console.CancelKeyPress+=(s,e)=>{e.Cancel=true;cts.Cancel();};Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{type="shelleye.kernel.ready",pid=Environment.ProcessId,pipe,db,world.BootEpoch,world.NativeBootId,world.KernelEpoch},JsonDefaults.Options));try{await server.RunAsync(cts.Token);}catch(OperationCanceledException){}return 0;
