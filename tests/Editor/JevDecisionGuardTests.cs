// Purpose: Test stale-response rejection and actual Unity Behavior integration in the licensed Unity editor.
using System;
using JevBehavior;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace JevBehaviorTests
{
    public sealed class JevDecisionGuardTests
    {
        private JObject Response() => JObject.Parse("{\"requestId\":\"r1\",\"revision\":\"v1\",\"record\":{\"schemaVersion\":1,\"model\":\"jev-1.13.0\",\"pack\":{\"name\":\"support/triage\"},\"outcome\":\"billing\"}}");
        [Test] public void AcceptsRegisteredOutcome() => Assert.AreEqual("billing", JevDecisionGuard.Accept(Response(), "r1", "v1", "v1", "support/triage", new[]{"billing","review"}));
        [Test] public void RejectsChangedWorld() => Assert.Throws<InvalidOperationException>(() => JevDecisionGuard.Accept(Response(), "r1", "v1", "v2", "support/triage", new[]{"billing"}));
        [Test] public void RejectsAnotherRequest() => Assert.Throws<InvalidOperationException>(() => JevDecisionGuard.Accept(Response(), "r2", "v1", "v1", "support/triage", new[]{"billing"}));
        [Test] public void RejectsUnknownOutcome() => Assert.Throws<InvalidOperationException>(() => JevDecisionGuard.Accept(Response(), "r1", "v1", "v1", "support/triage", new[]{"review"}));
        [Test] public void RejectsAnotherPack() => Assert.Throws<InvalidOperationException>(() => JevDecisionGuard.Accept(Response(), "r1", "v1", "v1", "other/pack", new[]{"billing"}));
        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void RejectsMatchingButEmptyIdentities(string empty)
        {
            var response=Response();response["requestId"]=empty;response["revision"]=empty;response["record"]["pack"]["name"]=empty;
            Assert.Throws<InvalidOperationException>(()=>JevDecisionGuard.Accept(response,empty,empty,empty,empty,new[]{"billing"}));
        }
        [Test] public void RejectsNumericIdentityCoercion()
        {
            var response=Response();response["requestId"]=123;
            Assert.Throws<InvalidOperationException>(()=>JevDecisionGuard.Accept(response,"123","v1","v1","support/triage",new[]{"billing"}));
        }
        [TestCase(1.1)] [TestCase(1.9)] [TestCase(0.0)]
        public void RejectsFractionalOrWrongSchema(double schema)
        {
            var response=Response();response["record"]["schemaVersion"]=schema;
            Assert.Throws<InvalidOperationException>(()=>JevDecisionGuard.Accept(response,"r1","v1","v1","support/triage",new[]{"billing"}));
        }
        [Test] public void AcceptsNumericallyExactSchemaOne()
        {
            var response=Response();response["record"]["schemaVersion"]=1.0;
            Assert.AreEqual("billing",JevDecisionGuard.Accept(response,"r1","v1","v1","support/triage",new[]{"billing"}));
        }
        [Test] public void RejectsNullAndNontextModelWithoutCoercion()
        {
            Assert.Throws<InvalidOperationException>(()=>JevDecisionGuard.Accept(null,"r1","v1","v1","support/triage",new[]{"billing"}));
            var response=Response();response["record"]["model"]=123;
            Assert.Throws<InvalidOperationException>(()=>JevDecisionGuard.Accept(response,"r1","v1","v1","support/triage",new[]{"billing"}));
        }
        [Test] public void ExposesNativeActionAndPolicy()
        {
            var policy=ScriptableObject.CreateInstance<JevBehaviorPolicy>();
            try { var node=new JevDecideAction(); node.Policy.Value=policy; Assert.IsInstanceOf<Unity.Behavior.Action>(node); Assert.Contains("review",policy.AllowedOutcomes); }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
    }
}
