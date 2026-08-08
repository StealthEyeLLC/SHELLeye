using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SHELLeye;

public sealed class KernelServer : IAsyncDisposable
{
    private sealed class Connection : IAsyncDisposable
    {
        public Guid Id{get;}=Guid.NewGuid();public NamedPipeServerStream Pipe{get;}public StreamReader Reader{get;}public StreamWriter Writer{get;}public SemaphoreSlim WriteGate{get;}=new(1,1);public ConcurrentDictionary<string,CancellationTokenSource> Active{get;}=new();
        public Connection(NamedPipeServerStream p){Pipe=p;Reader=new StreamReader(p,new UTF8Encoding(false),false,8192,true);Writer=new StreamWriter(p,new UTF8Encoding(false),8192,true){AutoFlush=true};}
        public async Task SendAsync(object value){string json=JsonSerializer.Serialize(value,JsonDefaults.Options);await WriteGate.WaitAsync();try{await Writer.WriteLineAsync(json);}finally{WriteGate.Release();}}
        public async ValueTask DisposeAsync(){foreach(var c in Active.Values)c.Cancel();foreach(var c in Active.Values)c.Dispose();Active.Clear();Writer.Dispose();Reader.Dispose();await Pipe.DisposeAsync();WriteGate.Dispose();}
    }
    private readonly string _pipeName;private readonly WorldContext _world;private readonly KernelDispatcher _dispatcher;private readonly ConcurrentDictionary<Guid,Connection> _connections=new();private readonly CancellationTokenSource _shutdown=new();
    public KernelServer(string pipeName,WorldContext world){_pipeName=pipeName;_world=world;_dispatcher=new KernelDispatcher(world);_world.DeltaAppended+=OnDelta;}
    public async Task RunAsync(CancellationToken ct)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,_shutdown.Token);while(!linked.IsCancellationRequested)
        {
            var security=new PipeSecurity();var owner=WindowsIdentity.GetCurrent().User??throw new InvalidOperationException("Kernel token has no user SID.");security.AddAccessRule(new PipeAccessRule(owner,PipeAccessRights.FullControl,AccessControlType.Allow));security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid,null),PipeAccessRights.FullControl,AccessControlType.Allow));var pipe=NamedPipeServerStreamAcl.Create(_pipeName,PipeDirection.InOut,NamedPipeServerStream.MaxAllowedServerInstances,PipeTransmissionMode.Byte,PipeOptions.Asynchronous,65536,65536,security,HandleInheritability.None);
            try{await pipe.WaitForConnectionAsync(linked.Token);var conn=new Connection(pipe);_connections[conn.Id]=conn;_=Task.Run(()=>ServeConnectionAsync(conn,linked.Token));}
            catch{await pipe.DisposeAsync();throw;}
        }
    }
    private async Task ServeConnectionAsync(Connection c,CancellationToken serverCt)
    {
        try
        {
            while(c.Pipe.IsConnected&&!serverCt.IsCancellationRequested)
            {
                string? line=await c.Reader.ReadLineAsync(serverCt);if(line is null)break;JsonDocument doc;try{doc=JsonDocument.Parse(line);}catch{await c.SendAsync(new{jsonrpc="2.0",id=(object?)null,error=new{code="invalid_argument",message="Invalid JSON request."}});continue;}
                using(doc){var root=doc.RootElement;string method=root.TryGetProperty("method",out var m)?m.GetString()??"":"";var id=root.TryGetProperty("id",out var i)?i.Clone():default;var param=root.TryGetProperty("params",out var p)?p.Clone():JsonDefaults.Element(new{});
                    if(method=="rpc.cancel"){string key=param.TryGetProperty("requestId",out var target)?target.GetRawText():"";bool cancelled=c.Active.TryGetValue(key,out var active);active?.Cancel();if(id.ValueKind!=JsonValueKind.Undefined)await c.SendAsync(new{jsonrpc="2.0",id,result=new{cancelled,key}});continue;}
                    int? timeout=root.TryGetProperty("timeoutMs",out var to)&&to.TryGetInt32(out int tv)?tv:null;_=HandleRequestAsync(c,id,method,param,timeout,serverCt);
                }
            }
        }
        catch(OperationCanceledException){}catch(IOException){}finally{_connections.TryRemove(c.Id,out _);await c.DisposeAsync();}
    }
    private async Task HandleRequestAsync(Connection c,JsonElement id,string method,JsonElement param,int? timeout,CancellationToken serverCt)
    {
        string key=id.ValueKind==JsonValueKind.Undefined?Guid.NewGuid().ToString("N"):id.GetRawText();using var cts=CancellationTokenSource.CreateLinkedTokenSource(serverCt);if(timeout is >0)cts.CancelAfter(timeout.Value);c.Active[key]=cts;
        try
        {
            object? result=await _dispatcher.DispatchAsync(method,param,cts.Token);if(id.ValueKind!=JsonValueKind.Undefined)await c.SendAsync(new{jsonrpc="2.0",id,result});
        }
        catch(OperationCanceledException){if(id.ValueKind!=JsonValueKind.Undefined)await SafeSend(c,new{jsonrpc="2.0",id,error=new{code="timeout",message="Request was cancelled or timed out."}});}
        catch(ShellEyeException e){if(id.ValueKind!=JsonValueKind.Undefined)await SafeSend(c,new{jsonrpc="2.0",id,error=new{code=e.Code,message=e.Message,nativeCode=e.NativeCode,details=e.Details}});}
        catch(Exception e){if(id.ValueKind!=JsonValueKind.Undefined)await SafeSend(c,new{jsonrpc="2.0",id,error=new{code="native_error",message=e.Message,type=e.GetType().FullName}});}
        finally{c.Active.TryRemove(key,out _);}
    }
    private static async Task SafeSend(Connection c,object x){try{await c.SendAsync(x);}catch{}}
    private void OnDelta(DeltaRecord d){var n=new{jsonrpc="2.0",method="world.delta",@params=d};foreach(var c in _connections.Values)_=SafeSend(c,n);}
    public async ValueTask DisposeAsync(){_world.DeltaAppended-=OnDelta;_shutdown.Cancel();foreach(var c in _connections.Values)await c.DisposeAsync();_connections.Clear();_shutdown.Dispose();}
}

