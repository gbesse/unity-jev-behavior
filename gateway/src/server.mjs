// Purpose: Keep Jev credentials on a bounded, authenticated server and return finite DecisionPack outcomes to Unity.
import http from 'node:http';
import {timingSafeEqual} from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readFile} from 'node:fs/promises';
import {evaluate,createJevProvider,validatePack} from '@gbesse/decisionpacks';
export function createGateway({packs,provider,token,timeoutMs=30000,maxConcurrent=4,onError=error=>console.error(error)}) {
 if(typeof token!=='string'||token.length<16) throw new Error('Set a gateway token of at least 16 characters');
 if(!Number.isInteger(timeoutMs)||timeoutMs<1||timeoutMs>60000) throw new Error('Invalid timeout');
 if(!Number.isInteger(maxConcurrent)||maxConcurrent<1||maxConcurrent>64) throw new Error('Invalid concurrency');
 const registry=structuredClone(packs);Object.values(registry).forEach(validatePack);let active=0;
 const expected=Buffer.from('Bearer '+token);
 const server=http.createServer(async(req,res)=>{
  const respond=(status,body)=>{if(!res.destroyed&&!res.writableEnded){res.writeHead(status,{'content-type':'application/json','cache-control':'no-store'});res.end(JSON.stringify(body));}};
  if(req.method!=='POST'||req.url!=='/v1/decision'){respond(404,{error:'Not found'});return;}
  const supplied=Buffer.from(req.headers.authorization||'');
  if(supplied.length!==expected.length||!timingSafeEqual(supplied,expected)){respond(401,{error:'Unauthorized'});return;}
  if(active>=maxConcurrent){respond(429,{error:'Gateway busy'});return;}
  active++;const controller=new AbortController();
  const timer=setTimeout(()=>{controller.abort(new Error('Gateway deadline'));respond(504,{error:'Decision deadline exceeded'});req.destroy();},timeoutMs);
  const abort=()=>{if(!res.writableEnded)controller.abort(new Error('Client disconnected'));};res.on('close',abort);
  try{
   let chunks=[],size=0;for await(const chunk of req){chunks.push(chunk);size+=chunk.length;if(size>100000){respond(413,{error:'Request too large'});req.destroy();return;}}
   let request;try{request=JSON.parse(Buffer.concat(chunks).toString('utf8'));}catch{respond(400,{error:'Invalid JSON'});return;}
   const {packId,state,revision,requestId}=request??{};
   if(typeof packId!=='string'||!Object.hasOwn(registry,packId)||typeof revision!=='string'||revision.length>128||typeof requestId!=='string'||!requestId||requestId.length>128||!state||typeof state!=='object'||Array.isArray(state)){respond(400,{error:'Invalid decision request'});return;}
   const record=await evaluate(registry[packId],state,{provider:provider??createJevProvider(),timeoutMs,signal:controller.signal});
   if(!controller.signal.aborted)respond(200,{requestId,revision,record});
  }catch(error){onError(error);respond(controller.signal.aborted?504:502,{error:'Decision failed'});}
  finally{clearTimeout(timer);active--;res.off('close',abort);}
 });
 server.requestTimeout=timeoutMs;server.headersTimeout=Math.min(timeoutMs,10000);server.timeout=timeoutMs;server.keepAliveTimeout=1000;
 return server;
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){
 const pack=JSON.parse(await readFile(new URL('../../packs/support-triage.json',import.meta.url)));
 const fixture=process.argv.includes('--fixture')?JSON.parse(await readFile(new URL('../../examples/synthetic-billing-response.json',import.meta.url))):null;
 const server=createGateway({packs:{[pack.name]:pack},token:process.env.JEV_GATEWAY_TOKEN,provider:fixture?async()=>structuredClone(fixture):undefined});
 server.on('error',error=>{console.error(error);process.exitCode=1;});
 server.listen(Number(process.env.PORT||8787),'127.0.0.1',()=>console.log(`Unity Jev gateway: http://127.0.0.1:${server.address().port} (${fixture?'SYNTHETIC FIXTURE':'live Jev'})`));
 for(const signal of ['SIGTERM','SIGINT'])process.on(signal,()=>{server.close();server.closeAllConnections();});
}
