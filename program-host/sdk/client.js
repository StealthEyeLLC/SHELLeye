const net=require('net');
const {EventEmitter}=require('events');
class ShellEyeClient extends EventEmitter{
 constructor(pipe=process.env.SHELLEYE_PIPE||'shelleye-dev'){super();this.pipe=pipe;this.socket=null;this.buffer='';this.nextId=1;this.pending=new Map();}
 async connect(){if(this.socket&&!this.socket.destroyed)return this;await new Promise((resolve,reject)=>{const s=net.createConnection('\\\\.\\pipe\\'+this.pipe,()=>{this.socket=s;resolve();});s.setEncoding('utf8');s.on('data',d=>this._onData(d));s.on('error',e=>{if(!this.socket)reject(e);this.emit('transportError',e)});s.on('close',()=>{for(const [,p] of this.pending)p.reject(Object.assign(new Error('kernel connection closed'),{code:'provider_unavailable'}));this.pending.clear();this.socket=null;});});return this;}
 _onData(d){this.buffer+=d;for(;;){const i=this.buffer.indexOf('\n');if(i<0)break;const line=this.buffer.slice(0,i);this.buffer=this.buffer.slice(i+1);if(!line)continue;let m;try{m=JSON.parse(line)}catch{continue}if(m.id!==undefined&&m.id!==null){const p=this.pending.get(String(m.id));if(!p)continue;this.pending.delete(String(m.id));if(m.error){const e=Object.assign(new Error(m.error.message),m.error);p.reject(e)}else p.resolve(m.result);}else if(m.method)this.emit(m.method,m.params);}}
 async request(method,params={},timeoutMs=30000){await this.connect();const id=this.nextId++;const payload={jsonrpc:'2.0',id,method,params,timeoutMs};return new Promise((resolve,reject)=>{const timer=setTimeout(()=>{this.pending.delete(String(id));reject(Object.assign(new Error('client timeout'),{code:'timeout'}));},timeoutMs+2000);this.pending.set(String(id),{resolve:v=>{clearTimeout(timer);resolve(v)},reject:e=>{clearTimeout(timer);reject(e)}});this.socket.write(JSON.stringify(payload)+'\n');});}
 async close(){if(this.socket){this.socket.end();this.socket.destroy();this.socket=null;}}
}
module.exports={ShellEyeClient};
