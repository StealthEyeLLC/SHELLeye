const fs=require('fs');
const {ShellEyeClient,sdk}=require('../sdk');
const results='C:/SHELLeye/Temp/build002';fs.mkdirSync(results,{recursive:true});
(async()=>{
 const client=new ShellEyeClient();await client.connect();const s=sdk(client);let total=0,linuxOps=0;const trace=[];
 const op=async(name,fn,linux=false)=>{const t=Date.now();const value=await fn();total++;if(linux)linuxOps++;trace.push({n:total,name,linux,ms:Date.now()-t});return value;};
 const expectError=async(name,code,fn)=>{try{await fn();throw new Error(`${name}: expected ${code}`)}catch(e){if(e.code!==code)throw e;trace.push({n:null,name,expectedError:code});return e;}};
 const waitExec=async(id,oldExe,timeoutMs=5000)=>{const end=Date.now()+timeoutMs;let last;while(Date.now()<end){last=await s.process.inspect(id);if(last.executablePath&&last.executablePath!==oldExe)return last;await new Promise(r=>setTimeout(r,50));}throw new Error(`exec transition not observed: ${JSON.stringify(last)}`)};
 let linuxPath,linuxReplacementId,windowsFile,windowsDir;
 try{
  const hello=await op('rpc.hello',()=>s.rpc.hello());
  const providers=await op('world.providers',()=>s.world.providers());
  const linux=providers.providers.find(x=>x.providerKind==='linux-wsl2');if(!linux)throw new Error('frozen Linux provider world absent');
  const linuxWorld=linux.worldId;if(linux.state==='provider_unavailable')throw Object.assign(new Error(linux.lastError||'Linux provider unavailable'),{code:'provider_unavailable'});
  const machine=await op('machine.inspect',()=>s.machine.inspect());
  const session=await op('session.inspect',()=>s.session.inspect());
  const volume=await op('volume.inspect.C',()=>s.volume.inspect('C:'));
  const probe=await op('provider.probe',()=>s.provider.probe(),true);
  const context=await op('provider.context',()=>s.provider.context(),true);
  const cursor0=await op('world.cursor',()=>s.world.cursor());
  if(!probe.probe?.capabilities?.pidfd||!probe.probe?.capabilities?.statx)throw new Error('pidfd/statx capability bind failed');

  const stamp=Date.now();linuxPath=`/tmp/shelleye-build002-${stamp}/identity.txt`;const renamed=`/tmp/shelleye-build002-${stamp}/renamed.txt`;const hard=`/tmp/shelleye-build002-${stamp}/hard.txt`;
  const lf=await op('linux.file.create',()=>s.file.create(linuxPath,'alpha',linuxWorld),true);
  const li0=await op('linux.file.inspect.initial',()=>s.file.inspect(lf.fileId),true);
  const lr0=await op('linux.file.read.initial',()=>s.file.read(lf.fileId),true);if(lr0.content!=='alpha')throw new Error('Linux file read mismatch');
  const lw=await op('linux.file.write.guarded',()=>s.file.write(lf.fileId,'beta',li0.revision),true);
  const li1=await op('linux.file.inspect.after-write',()=>s.file.inspect(lf.fileId),true);if(li1.fileId!==lf.fileId)throw new Error('content edit changed physical concept');

  const mv=await op('linux.process.start.mv',()=>s.process.start('/bin/mv',[linuxPath,renamed],null,linuxWorld),true);
  await op('linux.process.wait.mv',()=>s.process.wait(mv.processId,5000),true);
  const li2=await op('linux.file.inspect.after-rename',()=>s.file.inspect(lf.fileId),true);if(li2.fileId!==lf.fileId||!String(li2.path).includes('renamed.txt'))throw new Error('rename continuity/path recovery failed');
  const ln=await op('linux.process.start.ln',()=>s.process.start('/bin/ln',[renamed,hard],null,linuxWorld),true);
  await op('linux.process.wait.ln',()=>s.process.wait(ln.processId,5000),true);
  const hardRetain=await op('linux.file.retain.hardlink',()=>s.file.retain(hard,linuxWorld),true);if(hardRetain.fileId!==lf.fileId)throw new Error('hard link did not resolve same physical concept');
  await expectError('linux.file.rename.exact-facet','unsupported_by_provider',()=>s.file.rename(lf.fileId,`${renamed}.sdk`));

  const helper='/var/tmp/shelleye-build002/SHELLeye.Platform.Linux';
  const execp=await op('linux.process.start.exec-fixture',()=>s.process.start(helper,['--exec-fixture','900','30'],null,linuxWorld),true);
  const execBefore=await op('linux.process.inspect.pre-exec',()=>s.process.inspect(execp.processId),true);
  const execAfterPolled=await waitExec(execp.processId,execBefore.executablePath,5000);
  const execAfter=await op('linux.process.inspect.post-exec',()=>s.process.inspect(execp.processId),true);
  if(execAfter.processId!==execBefore.processId||execAfter.pid!==execBefore.pid||String(execAfter.startTicks)!==String(execBefore.startTicks))throw new Error('exec transition changed Linux process concept');
  await op('linux.process.terminate.exec-fixture',()=>s.process.terminate(execp.processId),true);

  const a=await op('linux.process.start.same-A',()=>s.process.start('/usr/bin/sleep',['30'],null,linuxWorld),true);
  const b=await op('linux.process.start.same-B',()=>s.process.start('/usr/bin/sleep',['30'],null,linuxWorld),true);
  const ai=await op('linux.process.inspect.same-A',()=>s.process.inspect(a.processId),true);
  const bi=await op('linux.process.inspect.same-B',()=>s.process.inspect(b.processId),true);if(ai.processId===bi.processId||ai.pid===bi.pid)throw new Error('simultaneous same executable collapsed identity');
  await op('linux.process.terminate.same-A',()=>s.process.terminate(a.processId),true);
  await op('linux.process.terminate.same-B',()=>s.process.terminate(b.processId),true);
  const short=await op('linux.process.start.short',()=>s.process.start('/usr/bin/sleep',['1'],null,linuxWorld),true);
  await op('linux.process.wait.short',()=>s.process.wait(short.processId,5000),true);
  const replacement=await op('linux.process.start.replacement',()=>s.process.start('/usr/bin/sleep',['30'],null,linuxWorld),true);
  await expectError('linux.process.terminate.old','destroyed',()=>s.process.terminate(short.processId));
  await op('linux.process.terminate.replacement',()=>s.process.terminate(replacement.processId),true);

  const rm=await op('linux.process.start.rm-original',()=>s.process.start('/bin/rm',[renamed,hard],null,linuxWorld),true);
  await op('linux.process.wait.rm-original',()=>s.process.wait(rm.processId,5000),true);
  const oldUnlinked=await op('linux.file.inspect.old-unlinked',()=>s.file.inspect(lf.fileId),true);
  const replacementFile=await op('linux.file.create.replacement',()=>s.file.create(renamed,'beta',linuxWorld),true);linuxReplacementId=replacementFile.fileId;if(replacementFile.fileId===lf.fileId)throw new Error('delete/recreate rebound old Linux file concept');
  const replacementBefore=await op('linux.file.read.replacement-before-old-write',()=>s.file.read(replacementFile.fileId),true);
  await op('linux.file.write.old-unlinked-fd',()=>s.file.write(lf.fileId,'old-object-only',oldUnlinked.revision),true);
  const replacementAfter=await op('linux.file.read.replacement-after-old-write',()=>s.file.read(replacementFile.fileId),true);if(replacementBefore.content!==replacementAfter.content)throw new Error('old Linux file write mutated replacement');

  const wr=`C:/SHELLeye/Temp/build002-cross-${stamp}`;windowsDir=await op('windows.directory.create.cross',()=>s.directory.create(wr));windowsFile=await op('windows.file.create.cross',()=>s.file.create(`${wr}/shared.txt`,'cross-provider'));
  const wi=await op('windows.file.inspect.cross',()=>s.file.inspect(windowsFile.id));
  const lcross=await op('linux.file.retain.mnt-c',()=>s.file.retain(`/mnt/c/SHELLeye/Temp/build002-cross-${stamp}/shared.txt`,linuxWorld),true);if(lcross.fileId===windowsFile.id||lcross.worldId===machine.machineId)throw new Error('cross-provider path translation merged identities');
  await op('windows.file.write.cross',()=>s.file.write(windowsFile.id,'cross-provider-2',wi.revisionToken));
  await op('linux.file.inspect.cross-after-windows-write',()=>s.file.inspect(lcross.fileId),true);

  const service=await op('windows.service.inspect.EventLog',()=>s.service.inspect('EventLog'));
  const ps=await op('windows.powershell.structured',()=>s.powershell.invoke('Get-Process',{Id:process.pid},['Id','ProcessName']));
  const health=await op('state.health',()=>s.state.health());
  const sync=await op('world.sync',()=>s.world.sync());
  const delta=await op('world.delta',()=>s.world.delta(cursor0.cursor,200));
  await op('windows.file.delete.cross',()=>s.file.delete(windowsFile.id));windowsFile=null;
  await op('windows.directory.delete.cross',()=>client.request('file.delete',{fileId:windowsDir.id},10000));windowsDir=null;
  const cleanupRm=await op('linux.process.start.cleanup-rm',()=>s.process.start('/bin/rm',['-rf',`/tmp/shelleye-build002-${stamp}`],null,linuxWorld),true);
  await op('linux.process.wait.cleanup-rm',()=>s.process.wait(cleanupRm.processId,5000),true);

  if(total<40)throw new Error(`typed operation count ${total} below frozen threshold`);if(linuxOps<12)throw new Error(`Linux operation count ${linuxOps} below frozen threshold`);
  const result={passed:true,completedUtc:new Date().toISOString(),oneProgramHostInvocation:true,onePersistentConnection:true,typedOperationCount:total,linuxOperationCount:linuxOps,modelCallsBetweenPrimitives:0,providerWorld:{worldId:linuxWorld,state:linux.state},process:{execSameConcept:execAfter.processId===execBefore.processId,simultaneousDistinct:ai.processId!==bi.processId,oldActuationRejected:true},file:{renamePreserved:true,hardlinkSameConcept:true,recreateNewConcept:replacementFile.fileId!==lf.fileId,oldWriteTouchedReplacement:false,exactRenameUnsupported:true},crossProvider:{windowsFileId:wi.fileId,linuxFileId:lcross.fileId,identitiesMerged:false},service:{id:service.id,state:service.state},powershell:{structured:ps.structured,provider:ps.provider},health,sync,deltaCount:delta.deltas.length,trace};
  fs.writeFileSync(`${results}/build002-program-host.json`,JSON.stringify(result,null,2));console.log(JSON.stringify({...result,trace:undefined}));
 }finally{
  try{if(windowsFile)await s.file.delete(windowsFile.id)}catch{};try{if(windowsDir)await client.request('file.delete',{fileId:windowsDir.id},10000)}catch{};await client.close();
 }
})().catch(e=>{console.error(JSON.stringify({passed:false,code:e.code||null,message:e.message,stack:e.stack}));process.exit(1)});
