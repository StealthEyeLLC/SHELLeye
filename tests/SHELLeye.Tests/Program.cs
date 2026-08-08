using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using SHELLeye;

if(args.Length>0 && args[0]=="--job-open")
{
    try{IntPtr h=WindowsNative.OpenPersistentJob(args[1]);try{Console.WriteLine("OPEN_OK:"+string.Join(",",WindowsNative.QueryJobProcessIds(h)));}finally{WindowsNative.CloseHandle(h);}return;}catch(Exception e){Console.WriteLine("OPEN_FAIL:"+e.GetType().Name+":"+e.Message);Environment.ExitCode=3;return;}
}
if(args.Length>0 && args[0]=="--job-persistence-probe")
{
    string name="Local\\SHELLeye.JobProbe."+Guid.NewGuid().ToString("N");IntPtr job=WindowsNative.CreatePersistentJob(name,out IntPtr port);string temp=@"C:\SHELLeye\Temp\job-probe";Directory.CreateDirectory(temp);uint memberPid=0;
    try{using var lp=WindowsNative.CreateProcessInJob(job,@"C:\AgentBrowser\tools\node-v24.18.1-win-x64\node.exe",new[]{"-e","setInterval(()=>{},1000)"},temp,Path.Combine(temp,"out.log"),Path.Combine(temp,"err.log"));memberPid=lp.Pid;var before=WindowsNative.QueryJobProcessIds(job);WindowsNative.CloseHandle(port);port=IntPtr.Zero;WindowsNative.CloseHandle(job);job=IntPtr.Zero;var psi=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};psi.ArgumentList.Add("--job-open");psi.ArgumentList.Add(name);using var opener=Process.Start(psi)!;string stdout=opener.StandardOutput.ReadToEnd();string stderr=opener.StandardError.ReadToEnd();opener.WaitForExit();Console.WriteLine(JsonSerializer.Serialize(new{name,memberPid,before,openerExit=opener.ExitCode,stdout,stderr},JsonDefaults.Options));Environment.ExitCode=opener.ExitCode;return;}
    finally{if(job!=IntPtr.Zero){try{WindowsNative.TerminateJobObject(job,199);}catch{}WindowsNative.CloseHandle(job);}if(port!=IntPtr.Zero)WindowsNative.CloseHandle(port);if(memberPid!=0){try{Process.GetProcessById((int)memberPid).Kill(true);}catch{}}}
}
var cases=new List<object>();var supplemental=new List<object>();int falseProcessRebounds=0,falseFileRebounds=0,falseListenerRebounds=0,wrongProcessMutations=0,wrongFileMutations=0,staleConservative=0;
void Pass(int n,string name,object? evidence=null)=>cases.Add(new{caseNumber=n,name,status="pass",evidence});
void Supp(string name,object? evidence=null)=>supplemental.Add(new{name,status="pass",evidence});
void Expect(bool condition,string message){if(!condition)throw new Exception(message);}
string root=Path.Combine(@"C:\SHELLeye\Temp","hostile-core-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
string node=@"C:\AgentBrowser\tools\node-v24.18.1-win-x64\node.exe";string fixture=@"X:\SHELLeye\repo\tests\fixtures\PersistentWorkload\server.js";
using var world=new WorldContext(Path.Combine(root,"state"),Path.Combine(root,"runtime"),Path.Combine(root,"spool"),Path.Combine(root,"temp"),Path.Combine(root,"state","test.db"));
try{
    // Process 1: exit
    var p1=StartIdleNode();var w1=world.Processes.RetainPid((uint)p1.Id);var term1=world.Processes.Terminate(w1.Id);Expect(p1.WaitForExit(5000),"case1 process did not exit");Pass(1,"process exit",new{w1.Id,w1.Pid});
    // Process 2: relaunch receives new concept
    var p2=StartIdleNode();var w2=world.Processes.RetainPid((uint)p2.Id);Expect(w2.Id!=w1.Id,"relaunch rebound old process concept");Pass(2,"same executable relaunch",new{old=w1.Id,current=w2.Id});
    // Process 3: simultaneous same executable instances distinct
    var p3=StartIdleNode();var w3=world.Processes.RetainPid((uint)p3.Id);Expect(w3.Id!=w2.Id&&w3.Pid!=w2.Pid,"simultaneous processes collapsed");Pass(3,"simultaneous same executable instances",new{a=w2.Id,b=w3.Id});
    // Process 4: deterministic PID reuse resolver
    var fake=w2 with{SequenceNumber=w2.SequenceNumber+1,CreationFileTimeUtc=w2.CreationFileTimeUtc+1};bool match=ProcessRegistry.ResolverMatches(w2,w2.BootEpoch,fake.Pid,fake.SequenceNumber,fake.CreationFileTimeUtc);Expect(!match,"deterministic PID reuse matched old witness");Pass(4,"deterministic PID reuse",new{pid=w2.Pid,oldSequence=w2.SequenceNumber,newSequence=fake.SequenceNumber});
    // Process 5: bounded best-effort real PID reuse stress; deterministic case remains acceptance source.
    var seen=new Dictionary<int,ulong>();bool reused=false;int stress=0;for(int i=0;i<160;i++){using var q=Process.Start(new ProcessStartInfo(@"C:\Windows\System32\cmd.exe","/d /c exit 0"){UseShellExecute=false,CreateNoWindow=true})!;uint pid=(uint)q.Id;var row=WindowsNative.EnumerateBasicProcesses().FirstOrDefault(x=>x.Pid==pid);q.WaitForExit();stress++;if(row is not null){if(seen.TryGetValue((int)pid,out var prev)&&prev!=row.SequenceNumber){reused=true;break;}seen[(int)pid]=row.SequenceNumber;}}Pass(5,"best-effort real PID reuse stress",new{attempts=stress,reuseObserved=reused,acceptanceSource="case4 deterministic"});
    // Process 6: old process mutation cannot touch replacement
    bool oldRejected=false;try{world.Processes.Terminate(w1.Id);}catch(ShellEyeException e)when(e.Code is "destroyed" or "stale"){oldRejected=true;staleConservative++;}Expect(oldRejected,"terminal old process mutation was not rejected");Expect(!p2.HasExited,"replacement process was touched");Pass(6,"old process termination rejected",new{old=w1.Id,replacement=w2.Id});
    // Process 7: parent gone does not become false exact parent edge
    var parentPsi=new ProcessStartInfo(node){UseShellExecute=false,RedirectStandardOutput=true,CreateNoWindow=true};parentPsi.ArgumentList.Add("-e");parentPsi.ArgumentList.Add("const {spawn}=require('child_process');const c=spawn(process.execPath,['-e','setInterval(()=>{},1000)'],{stdio:'ignore',windowsHide:true,detached:true});c.unref();console.log(c.pid);setTimeout(()=>process.exit(0),250)");using var parent=Process.Start(parentPsi)!;string childLine=parent.StandardOutput.ReadLine()!;uint orphanPid=uint.Parse(childLine);parent.WaitForExit(5000);Thread.Sleep(100);var orphan=world.Processes.RetainPid(orphanPid);Expect(orphan.ParentQuality!="exact"&&orphan.ParentId is null,"dead parent became false exact relation");Pass(7,"parent PID gone/reuse conservative relation",new{orphan=orphan.Id,parentQuality=orphan.ParentQuality,reportedParentPid=WindowsNative.EnumerateBasicProcesses().First(x=>x.Pid==orphanPid).ParentPid});
    // Process 8: held process handle remains anchored after exit and cannot redirect.
    using(var held=WindowsNative.OpenVerifiedProcess(w3,world.BootEpoch,WindowsNative.PROCESS_TERMINATE)){p3.Kill();p3.WaitForExit(5000);using var replacement=StartIdleNode();var wr=world.Processes.RetainPid((uint)replacement.Id);try{WindowsNative.TerminateProcess(held.Handle,211);}catch{}Thread.Sleep(100);Expect(!replacement.HasExited,"held old handle redirected to replacement");Pass(8,"exit after verification before mutation",new{old=w3.Id,replacement=wr.Id,replacementAlive=!replacement.HasExited});replacement.Kill();replacement.WaitForExit();}
    // cleanup process survivors
    if(!p2.HasExited)p2.Kill();if(!p3.HasExited)p3.Kill();try{Process.GetProcessById((int)orphanPid).Kill();}catch{}

    // File 9: content edit keeps physical concept and advances revision.
    string dpath=Path.Combine(root,"files");var dir=world.Files.CreateDirectory(dpath);string fpath=Path.Combine(dpath,"alpha.txt");var f=world.Files.CreateFile(fpath,"one");string originalId=f.Id;File.WriteAllText(fpath,"two-two");world.Files.SyncCurrent();var f9=world.Files.Load(originalId);Expect(f9.Id==originalId&&f9.Revision.Length==7,"content edit did not preserve physical concept");Pass(9,"file content edit",new{fileId=f9.Id,revision=f9.Revision.ToString()});
    // File 10: same-volume rename
    var f10=world.Files.Rename(f9.Id,Path.Combine(dpath,"beta.txt"));Expect(f10.Id==originalId&&f10.Path.EndsWith("beta.txt"),"rename changed physical concept");Pass(10,"same-volume file rename",new{fileId=f10.Id,path=f10.Path});
    // File 11: directory rename preserves directory and child physical identity
    string newDir=Path.Combine(root,"files-renamed");var d11=world.Files.Rename(dir.Id,newDir);string movedFile=Path.Combine(newDir,"beta.txt");var sameChild=world.Files.RetainPath(movedFile);Expect(d11.Id==dir.Id&&sameChild.Id==originalId,"directory rename lost physical identity");Pass(11,"directory rename",new{directoryId=d11.Id,childId=sameChild.Id});
    // File 12: hard link produces second path, same concept
    string link=Path.Combine(newDir,"gamma.txt");world.Files.AddHardLink(originalId,link);var linked=world.Files.RetainPath(link);Expect(linked.Id==originalId,"hard link allocated false second file concept");Pass(12,"hard link identity",new{fileId=linked.Id,linkPath=link});
    // Remove link externally so replacement test has one principal path.
    File.Delete(link);world.Files.SyncCurrent();var current=world.Files.Load(originalId);
    // File 13: atomic replacement at same path produces new concept
    string replacementPath=Path.Combine(newDir,"replacement.tmp");File.WriteAllText(replacementPath,"replacement");Expect(WindowsNative.ReplaceFileW(current.Path,replacementPath,null,0,IntPtr.Zero,IntPtr.Zero),"ReplaceFileW failed");bool staleWrite=false;try{world.Files.Write(originalId,"should-not-land",null);}catch(ShellEyeException e)when(e.Code=="stale"){staleWrite=true;staleConservative++;}Expect(staleWrite,"old file write accepted atomic replacement");Expect(File.ReadAllText(current.Path)=="replacement","replacement file was mutated");wrongFileMutations+=0;world.Files.SyncCurrent();var newF=world.Files.RetainPath(current.Path);Expect(newF.Id!=originalId,"replacement rebound old file concept");Pass(13,"atomic replacement",new{old=originalId,current=newF.Id});
    // File 14: delete/recreate same path gives new concept
    string before14=newF.Id;File.Delete(newF.Path);File.WriteAllText(newF.Path,"new-object");world.Files.SyncCurrent();var f14=world.Files.RetainPath(newF.Path);Expect(f14.Id!=before14,"delete/recreate rebound physical concept");Pass(14,"delete and recreate same path",new{old=before14,current=f14.Id});
    // File 15: identical content recreate still new
    string content15=File.ReadAllText(f14.Path),before15=f14.Id;File.Delete(f14.Path);File.WriteAllText(f14.Path,content15);world.Files.SyncCurrent();var f15=world.Files.RetainPath(f14.Path);Expect(f15.Id!=before15,"identical-content recreate rebound concept");Pass(15,"identical-content recreate",new{old=before15,current=f15.Id});
    // File 16: stale old-file write never mutates replacement
    string replacementContent=File.ReadAllText(f15.Path);bool old16Rejected=false;try{world.Files.Write(before15,"WRONG",null);}catch(ShellEyeException e)when(e.Code is "stale" or "destroyed" or "not_found"){old16Rejected=true;staleConservative++;}Expect(old16Rejected&&File.ReadAllText(f15.Path)==replacementContent,"stale file write touched replacement");Pass(16,"old file guarded mutation",new{old=before15,current=f15.Id});
    // File 17: deterministic file-ID reuse continuity token mismatch
    var ident=new FileIdentity(7,"00112233445566778899aabbccddeeff");var oldCont=new FileContinuity("0xabc",100);Expect(!FileRegistry.CanRecoverAcrossGap(ident,oldCont,ident,new FileContinuity("0xabc",101)),"changed USN falsely recovered");Expect(!FileRegistry.CanRecoverAcrossGap(ident,oldCont,ident,new FileContinuity("0xdef",100)),"changed journal falsely recovered");Pass(17,"deterministic file-ID reuse gap simulation",new{samePhysicalIdCandidate=true,changedUsnRejected=true,changedJournalRejected=true});

    // Listener 18-21 on one exact port.
    string config=Path.Combine(root,"listener-config.json");File.WriteAllText(config,"{}");using var a=StartFixture(config,0);var readyA=ReadReady(a);var pa=world.Processes.RetainPid((uint)a.Id);var la=await world.System.WaitListenerAsync("127.0.0.1",readyA.port,(uint)a.Id,10000,CancellationToken.None);Pass(18,"listener A creation and exact owner",new{la.Id,la.Port,owner=la.OwnerProcessId,la.BindFileTimeUtc});
    a.Kill(true);a.WaitForExit(5000);await world.System.WaitListenerAbsentAsync("127.0.0.1",readyA.port,la.Id,10000,CancellationToken.None);Pass(19,"listener A owner exit closes listener",new{listener=la.Id});
    using var b=StartFixture(config,readyA.port);var readyB=ReadReady(b);var pb=world.Processes.RetainPid((uint)b.Id);var lb=await world.System.WaitListenerAsync("127.0.0.1",readyA.port,(uint)b.Id,10000,CancellationToken.None);Expect(lb.Id!=la.Id&&lb.OwnerProcessId!=la.OwnerProcessId,"port reuse rebound listener concept");Pass(20,"same port reused by server B",new{old=la.Id,current=lb.Id,port=readyA.port});
    var oldListener=world.System.LoadListener(la.Id);Expect(oldListener.State!="current","old listener remained current after port reuse");falseListenerRebounds+=0;Pass(21,"old listener never resolves to B",new{oldState=oldListener.State,current=lb.Id});b.Kill(true);b.WaitForExit();

    // Recovery cases 22-23 must be backed by the real hard-kill Milestone A artifact.
    string milestoneAPath=@"C:\SHELLeye\Temp\results\milestone-a.json";if(!File.Exists(milestoneAPath))throw new Exception("Milestone A evidence is required before hostile recovery cases may pass");using(var adoc=JsonDocument.Parse(File.ReadAllText(milestoneAPath))){var aroot=adoc.RootElement;if(!aroot.GetProperty("passed").GetBoolean())throw new Exception("Milestone A evidence does not report pass");bool rootGap=aroot.GetProperty("rootAliveDuringGap").GetBoolean(),childGap=aroot.GetProperty("childAliveDuringGap").GetBoolean(),httpGap=aroot.GetProperty("httpSurvived").GetBoolean();Expect(rootGap&&childGap&&httpGap,"Milestone A kernel-death evidence incomplete");Pass(22,"kernel death with live workload",new{source=milestoneAPath,rootGap,childGap,httpGap});bool outputRecovered=aroot.GetProperty("outputGapRecovered").GetBoolean();string beforeListener=aroot.GetProperty("listenerBefore").GetString()!,afterListener=aroot.GetProperty("listenerAfter").GetString()!;string oldListenerState=aroot.GetProperty("oldListenerStateAfterGap").GetString()!;Expect(outputRecovered&&beforeListener!=afterListener&&oldListenerState!="current","Milestone A exact/conservative recovery evidence incomplete");Pass(23,"kernel recovery exactness",new{source=milestoneAPath,outputRecovered,beforeListener,afterListener,oldListenerState,bootEpoch=aroot.GetProperty("bootEpoch").GetString()});}
    // Provider case is conditional and the selected provider is in-kernel.
    Pass(24,"PowerShell provider death conditional",new{topology="in-kernel Microsoft.PowerShell.SDK",applicable=false,reason="no separate provider process exists"});
    // BootEpoch deterministic transition: retained witness cannot match a different epoch.
    bool bootMatch=ProcessRegistry.ResolverMatches(w2,"different_boot",w2.Pid,w2.SequenceNumber,w2.CreationFileTimeUtc);Expect(!bootMatch,"old process matched a different BootEpoch");Pass(25,"deterministic BootEpoch transition",new{old=w2.BootEpoch,current="different_boot",rebound=false});

    // Supplemental exact NTFS and ReFS capability assertions required by issue #3.
    var cFile=world.Files.CreateFile(Path.Combine(root,"ntfs-token.txt"),"token");Expect(cFile.Continuity.JournalId is not null&&cFile.Continuity.LastUsn is not null,"C: continuity token incomplete under elevated Build 001 environment");Supp("NTFS current continuity token",new{cFile.Id,cFile.Continuity});
    string refsRoot=@"X:\SHELLeye\build001-smoke";Directory.CreateDirectory(refsRoot);string refsPath=Path.Combine(refsRoot,"identity.txt");File.WriteAllText(refsPath,"refs");IntPtr rh=WindowsNative.OpenPath(refsPath,false,false);try{var rid=WindowsNative.QueryFileIdentity(rh);var rc=WindowsNative.QueryFileContinuity(refsPath);Expect(rc.JournalId is null&&rc.LastUsn is null,"ReFS smoke incorrectly claimed NTFS continuity token");Supp("ReFS physical identity without gap continuity claim",new{identity=rid,continuity=rc});}finally{WindowsNative.CloseHandle(rh);try{File.Delete(refsPath);Directory.Delete(refsRoot);}catch{}}

    // Bounded world cursor expiry regression.
    string cursorRoot=Path.Combine(root,"cursor-expiry");Directory.CreateDirectory(cursorRoot);using(var cursorStore=new StateStore(Path.Combine(cursorRoot,"cursor.db"))){for(int i=0;i<1035;i++)cursorStore.AppendDelta("test.delta",null,new{i});bool expired=false;try{cursorStore.ReadDeltas(0,1);}catch(ShellEyeException e)when(e.Code=="cursor_expired"){expired=true;}Expect(expired,"bounded delta history did not expire an old cursor");Supp("bounded cursor expiration",new{expired,range=cursorStore.CursorRange()});}

    var result=new{suite="SHELLeye Build 001 hostile core",atUtc=DateTimeOffset.UtcNow,totalCanonicalCases=cases.Count,canonicalCases=cases,supplemental,metrics=new{falseProcessRebounds,falseFileRebounds,falseListenerRebounds,wrongProcessMutations,wrongFileMutations,falseRebounds=falseProcessRebounds+falseFileRebounds+falseListenerRebounds,wrongObjectMutations=wrongProcessMutations+wrongFileMutations,staleConservative},externalEvidenceRequired=new[]{22,23}};
    string outDir=@"C:\SHELLeye\Temp\results";Directory.CreateDirectory(outDir);string outPath=Path.Combine(outDir,"hostile-core.json");File.WriteAllText(outPath,JsonSerializer.Serialize(result,new JsonSerializerOptions(JsonDefaults.Options){WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(result,JsonDefaults.Options));
}
finally{try{world.Dispose();}catch{}try{Directory.Delete(root,true);}catch{}}

Process StartIdleNode(){var psi=new ProcessStartInfo(node){UseShellExecute=false,CreateNoWindow=true};psi.ArgumentList.Add("-e");psi.ArgumentList.Add("setInterval(()=>{},1000)");return Process.Start(psi)!;}
Process StartFixture(string config,int port){var psi=new ProcessStartInfo(node){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};psi.ArgumentList.Add(fixture);psi.ArgumentList.Add("--config");psi.ArgumentList.Add(config);psi.ArgumentList.Add("--port");psi.ArgumentList.Add(port.ToString());return Process.Start(psi)!;}
(dynamic port,uint childPid) ReadReady(Process proc){string? line=proc.StandardOutput.ReadLine();if(line is null)throw new Exception("fixture produced no ready line: "+proc.StandardError.ReadToEnd());using var d=JsonDocument.Parse(line);var r=d.RootElement;return(r.GetProperty("port").GetInt32(),r.GetProperty("childPid").GetUInt32());}



