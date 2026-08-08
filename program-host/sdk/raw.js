function raw(s,c){return{exec:(command,timeoutMs=30000)=>s.request('raw.exec',{command,timeoutMs},timeoutMs)}}
module.exports={raw};
