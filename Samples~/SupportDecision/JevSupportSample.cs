// Purpose: Provide an importable sample component for testing a server decision from a Unity scene.
using System;
using System.Threading;
using UnityEngine;
namespace JevBehavior.Samples
{
    public sealed class JevSupportSample : MonoBehaviour
    {
        public JevDecisionClient Client;
        public JevBehaviorPolicy Policy;
        [TextArea] public string StateJson = "{\"text\":\"I was charged twice\"}";
        public string Revision = "1";
        public string Outcome;
        [ContextMenu("Evaluate Jev Decision")]
        public async void Evaluate()
        {
            try
            {
                if (!Client || !Policy) throw new InvalidOperationException("Assign Client and Policy.");
                // Local sample only: the token stays out of serialized assets and player builds.
                if (string.IsNullOrEmpty(Client.SessionToken)) Client.SessionToken = Environment.GetEnvironmentVariable("JEV_GATEWAY_TOKEN");
                var revision = Revision; var requestId = Guid.NewGuid().ToString("N");
                var response = await Client.EvaluateAsync(Policy.PackId, StateJson, revision, requestId, destroyCancellationToken);
                Outcome = JevDecisionGuard.Accept(response, requestId, revision, Revision, Policy.PackId, Policy.AllowedOutcomes);
                Debug.Log("Jev outcome: " + Outcome, this);
            }
            catch (OperationCanceledException) { Debug.Log("Jev sample request cancelled.", this); }
            catch (Exception error) { Outcome = ""; Debug.LogException(error, this); }
        }
    }
}
