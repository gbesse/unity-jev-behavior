// Purpose: Exercise UnityWebRequest against a loopback HTTP fixture inside the Unity editor.
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JevBehavior;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace JevBehaviorTests
{
    public sealed class JevHttpClientTests
    {
        [UnityTest] public IEnumerator SendsAuthenticatedRequestAndReceivesDecision()
        {
            var portFinder=new TcpListener(IPAddress.Loopback,0); portFinder.Start(); int port=((IPEndPoint)portFinder.LocalEndpoint).Port; portFinder.Stop();
            using var listener=new HttpListener(); listener.Prefixes.Add($"http://127.0.0.1:{port}/"); listener.Start();
            var server=Task.Run(async()=>{
                var context=await listener.GetContextAsync();
                using var reader=new StreamReader(context.Request.InputStream); var body=JObject.Parse(await reader.ReadToEndAsync());
                if(context.Request.Headers["Authorization"]!="Bearer local-test-session") throw new InvalidOperationException("Missing auth");
                var response=new JObject{["requestId"]=body["requestId"],["revision"]=body["revision"],["record"]=new JObject{["schemaVersion"]=1,["model"]="jev-1.13.0",["pack"]=new JObject{["name"]="support/triage"},["outcome"]="billing"}};
                var bytes=Encoding.UTF8.GetBytes(response.ToString()); context.Response.ContentType="application/json"; context.Response.ContentLength64=bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes,0,bytes.Length); context.Response.Close();
            });
            var go=new GameObject("Jev HTTP test");
            try
            {
                var client=go.AddComponent<JevDecisionClient>();client.GatewayUrl=$"http://127.0.0.1:{port}/v1/decision";client.SessionToken="local-test-session";client.TimeoutSeconds=3;
                var task=client.EvaluateAsync("support/triage","{\"text\":\"Charged twice\"}","v1","r1",CancellationToken.None);
                var until=DateTime.UtcNow.AddSeconds(5);while(!task.IsCompleted&&DateTime.UtcNow<until)yield return null;
                Assert.IsTrue(task.IsCompleted,"Unity request did not finish");
                Assert.AreEqual("billing",JevDecisionGuard.Accept(task.GetAwaiter().GetResult(),"r1","v1","v1","support/triage",new[]{"billing"}));
                Assert.IsTrue(server.IsCompleted);server.GetAwaiter().GetResult();
            }
            finally { listener.Stop(); UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
