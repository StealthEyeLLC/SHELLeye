function arg(name, fallback=null){ const i=process.argv.indexOf('--'+name); return i>=0 ? process.argv[i+1] : fallback; }
const parent=Number(arg('parent','0'));
const timer=setInterval(()=>{ try{ process.kill(parent,0); } catch { clearInterval(timer); process.exit(0); } },100);
