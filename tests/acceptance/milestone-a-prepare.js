const fs=require('fs');
const path=require('path');
const {ShellEyeClient}=require('../../program-host/sdk/client');
const results='C:/SHELLeye/Temp/results';fs.mkdirSync(results,{recursive:true});
(async()=>{
 const c=new ShellEyeClient(); const q=(m,p={},t=30000)=>c.request(m,p,t); const started=new Date().toISOString();
 try{
   const hello=await q('rpc.hello'); const machine=await q('machine.inspect'); const session=await q('session.inspect'); const volume=await q('volume.inspect',{drive:'C:'});
   const workspace=`C:/SHELLeye/Temp/milestone-a-${Date.now()}`; const dir=await q('directory.create',{path:workspace});
   const file=await q('file.create',{path:`${workspace}/config.json`,content:JSON.stringify({phase:'before-kill',value:1})});
   if(!file.continuity?.journalId||file.continuity?.lastUsn==null) throw new Error('Milestone A requires exact NTFS journal+USN continuity token');
   const wc=await q('world.cursor'); const job=await q('job.create');
   const node='C:/AgentBrowser/tools/node-v24.18.1-win-x64/node.exe';
   const launch=await q('job.start',{jobId:job.id,executable:node,args:['X:/SHELLeye/repo/tests/fixtures/PersistentWorkload/server.js','--config',file.path,'--port','0'],cwd:'X:/SHELLeye/repo'});
   const readyWait=await q('job.wait_output',{jobId:job.id,contains:'"type":"ready"',afterCursor:null,timeoutMs:15000},20000);
   const ready=JSON.parse(readyWait.record.text.trim());
   await q('job.wait_member_count',{jobId:job.id,atLeast:2,timeoutMs:15000},20000);
   const members=await q('job.members',{jobId:job.id});
   const child=members.members.find(x=>x.pid===ready.childPid); if(!child) throw new Error(`fixture child ${ready.childPid} not retained in job`);
   const root=members.members.find(x=>x.processId===launch.process.id); if(!root) throw new Error('fixture root missing from job');
   const listener=await q('network.wait_listener',{address:'127.0.0.1',port:ready.port,ownerProcessId:launch.process.id,timeoutMs:15000},20000);
   const http=await fetch(`http://127.0.0.1:${ready.port}/`).then(async r=>({status:r.status,body:await r.json()}));
   if(http.status!==200||http.body.pid!==launch.process.pid) throw new Error('fixture HTTP pre-kill check failed');
   const runtime=JSON.parse(fs.readFileSync('C:/SHELLeye/runtime/kernel/runtime.json','utf8'));
   const prep={stage:'prepared',started,preparedUtc:new Date().toISOString(),hello,machine,session,volume,workspace,directory:dir,file,worldCursor:wc.cursor,job,rootProcess:launch.process,childProcess:child,listener,port:ready.port,ready,outputCursor:readyWait.cursor,http,runtime};
   fs.writeFileSync(`${results}/milestone-a-prepared.json`,JSON.stringify(prep,null,2));console.log(JSON.stringify(prep));
 }finally{await c.close();}
})().catch(e=>{console.error(e.stack||String(e));process.exit(1)});
