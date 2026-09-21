// Purpose: Integrate an asynchronous, cancellable decision into Unity Behavior as a native action node.
using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
namespace JevBehavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Jev Decision", story: "Evaluate [StateJson] using [Policy] on [ClientObject]", category: "Action/Jev", id: "f57e051b0e194ff4b9276ae6c5e71291d")]
    public partial class JevDecideAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> ClientObject = new();
        [SerializeReference] public BlackboardVariable<JevBehaviorPolicy> Policy = new();
        [SerializeReference] public BlackboardVariable<string> StateJson = new();
        [SerializeReference] public BlackboardVariable<string> Revision = new();
        [SerializeReference] public BlackboardVariable<string> Outcome = new();
        [SerializeReference] public BlackboardVariable<string> Error = new();
        private CancellationTokenSource cancellation;
        private Task<JObject> pending;
        private string requestId, revision, packId;
        private string[] allowedOutcomes;
        protected override Status OnStart()
        {
            Outcome.Value = ""; Error.Value = "";
            try
            {
                var client = ClientObject.Value ? ClientObject.Value.GetComponent<JevDecisionClient>() : null;
                if (!client || !Policy.Value) throw new InvalidOperationException("Assign a client GameObject and Jev policy.");
                requestId = Guid.NewGuid().ToString("N"); revision = Revision.Value ?? "";
                packId = Policy.Value.PackId; allowedOutcomes = (string[])Policy.Value.AllowedOutcomes.Clone();
                cancellation = new CancellationTokenSource();
                pending = client.EvaluateAsync(packId, StateJson.Value, revision, requestId, cancellation.Token);
                return Status.Running;
            }
            catch (Exception exception) { return Fail(exception); }
        }
        protected override Status OnUpdate()
        {
            if (pending == null) return Status.Failure;
            if (!pending.IsCompleted) return Status.Running;
            try
            {
                Outcome.Value = JevDecisionGuard.Accept(pending.GetAwaiter().GetResult(), requestId, revision, Revision.Value ?? "", packId, allowedOutcomes);
                return Status.Success;
            }
            catch (Exception exception) { return Fail(exception); }
        }
        protected override void OnEnd()
        {
            cancellation?.Cancel(); cancellation?.Dispose(); cancellation = null;
            // Observe faults even when the graph exits before the asynchronous HTTP operation finishes.
            if (pending != null && !pending.IsCompleted) _ = ObserveStoppedTask(pending);
            else if (pending != null && pending.IsFaulted) _ = pending.Exception;
            pending = null;
        }
        private static async Task ObserveStoppedTask(Task<JObject> task)
        {
            try { await task; } catch (OperationCanceledException) { /* Expected when the graph cancels this node. */ }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        private Status Fail(Exception exception) { Error.Value = exception.Message; Outcome.Value = ""; Debug.LogException(exception); return Status.Failure; }
    }
}
