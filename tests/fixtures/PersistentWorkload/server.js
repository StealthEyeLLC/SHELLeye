const http = require('http');
const fs = require('fs');
const { spawn } = require('child_process');
const path = require('path');
function arg(name, fallback=null){ const i=process.argv.indexOf('--'+name); return i>=0 ? process.argv[i+1] : fallback; }
const configPath = arg('config');
const port = Number(arg('port','0'));
if(!configPath){ console.error(JSON.stringify({type:'fatal',message:'--config required'})); process.exit(2); }
const child = spawn(process.execPath,[path.join(__dirname,'child.js'),'--parent',String(process.pid)],{stdio:['ignore','ignore','ignore'],windowsHide:true});
let requestCount=0;
const server=http.createServer((req,res)=>{
  requestCount++;
  const u=new URL(req.url,'http://127.0.0.1');
  if(u.pathname==='/emit'){
    const tag=u.searchParams.get('tag')||'untagged';
    process.stdout.write(JSON.stringify({type:'emit',tag,pid:process.pid,count:requestCount})+'\n');
    process.stderr.write(JSON.stringify({type:'emit.stderr',tag,pid:process.pid})+'\n');
    res.writeHead(200,{'content-type':'application/json'});res.end(JSON.stringify({ok:true,tag}));return;
  }
  if(u.pathname==='/shutdown'){
    res.writeHead(200,{'content-type':'application/json'});res.end(JSON.stringify({ok:true}));setTimeout(()=>shutdown(0),20);return;
  }
  let config=''; try{config=fs.readFileSync(configPath,'utf8');}catch(e){config='!read-error:'+e.code;}
  res.writeHead(200,{'content-type':'application/json'});res.end(JSON.stringify({ok:true,pid:process.pid,childPid:child.pid,config,requestCount}));
});
server.listen(port,'127.0.0.1',()=>{
  const a=server.address();
  process.stdout.write(JSON.stringify({type:'ready',pid:process.pid,childPid:child.pid,port:a.port,configPath})+'\n');
  process.stderr.write(JSON.stringify({type:'fixture.stderr.ready',pid:process.pid})+'\n');
});
function shutdown(code){ try{server.close();}catch{} try{child.kill();}catch{} setTimeout(()=>process.exit(code),50); }
process.on('SIGTERM',()=>shutdown(0));process.on('SIGINT',()=>shutdown(0));
setInterval(()=>{},1000);
