const fs=require('fs');
const {ShellEyeClient}=require('../../program-host/sdk/client');
const results='C:/SHELLeye/Temp/results';
const readJson=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const prep=readJson(`${results}/milestone-a-prepared.json`);
const gap=readJson(`${results}/milestone-a-gap.json`);
async function connect(){let last;for(let i=0;i<80;i++){const c=new ShellEyeClient();try{await c.connect();return c}catch(e){last=e;await new Promise(r=>setTimeout(r,100));}}throw last;}
(async()=>{const c=await connect();const q=(m,p={},t=30000)=>c.request(m,p,t);let cleanup={};try{
 const hello=await q('rpc.hello');const runtime=JSON.parse(fs.readFileSync('C:/SHELLeye/runtime/kernel/runtime.json','utf8'));
 if(hello.bootEpoch!==prep.hello.bootEpoch)throw new Error(`BootEpoch changed ${prep.hello.bootEpoch} -> ${hello.bootEpoch}`);
 if(hello.kernelEpoch===prep.hello.kernelEpoch)throw new Error('kernel epoch did not advance after hard kill');
 const jobInspect=await q('job.inspect',{jobId:prep.job.id});const members=await q('job.members',{jobId:prep.job.id});
 const root=members.members.find(x=>x.processId===prep.rootProcess.id);const child=members.members.find(x=>x.processId===prep.childProcess.processId);
 if(!root||root.pid!==prep.rootProcess.pid)throw new Error('same retained root proc_* not recovered');if(!child||child.pid!==prep.childProcess.pid)throw new Error('same retained child proc_* not recovered');
 const file=await q('file.inspect',{fileId:prep.file.id});if(file.state!=='current'||file.identity.fileId128!==prep.file.identity.fileId128||file.continuity.journalId!==prep.file.continuity.journalId||file.continuity.lastUsn!==prep.file.continuity.lastUsn)throw new Error('retained NTFS file continuity was not recovered exactly');
 const output=await q('job.output',{jobId:prep.job.id,afterCursor:prep.outputCursor,maxBytes:65536});const text=output.records.map(x=>x.text).join('');if(!text.includes('kernel-gap'))throw new Error('output produced during kernel gap was not recovered from prior cursor');
 const listener=await q('network.wait_listener',{address:'127.0.0.1',port:prep.port,ownerProcessId:prep.rootProcess.id,timeoutMs:10000},15000);if(listener.id===prep.listener.id)throw new Error('listener continuity was falsely rebound across observation gap');
 const oldListener=await q('listener.inspect',{listenerId:prep.listener.id});if(oldListener.state==='current')throw new Error('old listener remained current across observation gap');
 const http=await fetch(`http://127.0.0.1:${prep.port}/`).then(async r=>({status:r.status,body:await r.json()}));if(http.status!==200||http.body.pid!==prep.rootProcess.pid)throw new Error('HTTP did not survive kernel death/recovery');
 const session=await q('session.inspect');const volume=await q('volume.inspect',{drive:'C:'});const service=await q('service.inspect',{name:'EventLog'});const sync=await q('world.sync');const delta=await q('world.delta',{afterCursor:prep.worldCursor,limit:200});
 const result={passed:true,completedUtc:new Date().toISOString(),kernelPidBefore:prep.runtime.pid,kernelPidAfter:runtime.pid,kernelEpochBefore:prep.hello.kernelEpoch,kernelEpochAfter:hello.kernelEpoch,bootEpoch:hello.bootEpoch,nativeBootId:runtime.nativeBootId,jobId:prep.job.id,nativeJobName:prep.job.nativeName,rootProcessId:prep.rootProcess.id,rootPid:prep.rootProcess.pid,childProcessId:prep.childProcess.processId,childPid:prep.childProcess.pid,httpSurvived:gap.http.status===200&&http.status===200,rootAliveDuringGap:gap.rootAlive,childAliveDuringGap:gap.childAlive,outputGapRecovered:true,outputAfterCursorBytes:text.length,fileId:prep.file.id,fileIdentity:prep.file.identity,fileContinuity:file.continuity,listenerBefore:prep.listener.id,listenerAfter:listener.id,oldListenerStateAfterGap:oldListener.state,session,volume:{id:volume.id,fileSystem:volume.fileSystem},service:{id:service.id,name:service.name,state:service.state,processId:service.processId},recoveryDeltaTypes:[...new Set(delta.deltas.map(x=>x.type))],worldCursorBefore:prep.worldCursor,worldCursorAfter:delta.cursor,sync};
 fs.writeFileSync(`${results}/milestone-a.json`,JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 // typed cleanup only after evidence is durable
 try{await q('job.terminate',{jobId:prep.job.id});await q('job.wait_empty',{jobId:prep.job.id,timeoutMs:10000},15000);await q('network.wait_absent',{address:'127.0.0.1',port:prep.port,listenerId:listener.id,timeoutMs:10000},15000);cleanup.job=true}catch(e){cleanup.jobError={code:e.code,message:e.message}}
 try{await q('file.delete',{fileId:prep.file.id});cleanup.file=true}catch(e){cleanup.fileError={code:e.code,message:e.message}}
 try{await q('file.delete',{fileId:prep.directory.id});cleanup.directory=true}catch(e){cleanup.directoryError={code:e.code,message:e.message}}
 result.cleanup=cleanup;fs.writeFileSync(`${results}/milestone-a.json`,JSON.stringify(result,null,2));
 }finally{await c.close();}})().catch(e=>{console.error(e.stack||String(e));process.exit(1)});

