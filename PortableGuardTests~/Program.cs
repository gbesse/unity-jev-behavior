// Compile the actual production guard without requiring a licensed Unity Editor.
using System;
using JevBehavior;
using Newtonsoft.Json.Linq;
class Program
{
 static int checks;
 static JObject Response() => JObject.Parse("{\"requestId\":\"r1\",\"revision\":\"v1\",\"record\":{\"schemaVersion\":1,\"model\":\"jev-1.13.0\",\"pack\":{\"name\":\"support/triage\"},\"outcome\":\"billing\"}}");
 static string Accept(JObject response) => JevDecisionGuard.Accept(response,"r1","v1","v1","support/triage",new[]{"billing","review"});
 static void Reject(Action action){checks++;try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected guard rejection");}
 static void Main()
 {
  if(Accept(Response())!="billing")throw new Exception("Valid response rejected");checks++;
  foreach(var empty in new string[]{null,""," "}){var r=Response();r["requestId"]=empty;r["revision"]=empty;r["record"]["pack"]["name"]=empty;Reject(()=>JevDecisionGuard.Accept(r,empty,empty,empty,empty,new[]{"billing"}));}
  foreach(var key in new[]{"requestId","revision"}){var r=Response();r[key]=123;Reject(()=>JevDecisionGuard.Accept(r,key=="requestId"?"123":"r1",key=="revision"?"123":"v1",key=="revision"?"123":"v1","support/triage",new[]{"billing"}));}
  foreach(var schema in new JToken[]{new JValue(1.1),new JValue(1.9),new JValue(0),new JValue(-1),new JValue(true),new JValue("1"),JValue.CreateNull(),new JValue(double.NaN),new JValue(double.PositiveInfinity)}){var r=Response();r["record"]["schemaVersion"]=schema;Reject(()=>Accept(r));}
  {var r=Response();r["record"]["schemaVersion"]=1.0;if(Accept(r)!="billing")throw new Exception("Exact one rejected");checks++;}
  foreach(var key in new[]{"model","outcome"}){var r=Response();r["record"][key]=123;Reject(()=>Accept(r));}
  {var r=Response();r["record"]["pack"]=123;Reject(()=>Accept(r));}
  {var r=Response();r["record"]["outcome"]="unknown";Reject(()=>Accept(r));}
  Reject(()=>Accept(null));
  Reject(()=>JevDecisionGuard.Accept(Response(),"r1","v1","v2","support/triage",new[]{"billing"}));
  foreach(var allowed in new string[][]{new string[0],new[]{"billing","billing"},new[]{"billing"," "},new[]{"billing",null}})
   Reject(()=>JevDecisionGuard.Accept(Response(),"r1","v1","v1","support/triage",allowed));
  foreach(var raw in new[]{"{\"requestId\":\"a\",\"requestId\":\"b\"}","{\"record\":{\"outcome\":\"a\",\"outcome\":\"b\"}}","{\"state\":{\"x\":1,\"x\":2}}","[]","null","","{"})
   Reject(()=>JevDecisionGuard.Parse(raw));
  if((int)JevDecisionGuard.Parse("{\"x\":1}")["x"]!=1)throw new Exception("Valid JSON rejected");checks++;
  Console.WriteLine("{\"passed\":true,\"productionGuardCompiled\":true,\"checks\":"+checks+",\"unityEditorValidated\":false,\"liveCalls\":0}");
 }
}
