async function main(){
 const url=process.argv[2]; if(!url) throw new Error('url required');
 const r=await fetch(url); const text=await r.text();
 process.stdout.write(JSON.stringify({status:r.status,body:text})+'\n');
 await new Promise(r=>setTimeout(r,120));
}
main().catch(e=>{console.error(e.stack||String(e));process.exit(1)});
