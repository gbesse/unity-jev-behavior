// Purpose: Reject stale, misrouted or unknown decisions before changing a Behavior blackboard.
using System;
using Newtonsoft.Json.Linq;
namespace JevBehavior
{
    public static class JevDecisionGuard
    {
        public static string Accept(JObject response, string requestId, string capturedRevision, string currentRevision, string packId, string[] allowedOutcomes)
        {
            if (capturedRevision != currentRevision) throw new InvalidOperationException("World revision changed during evaluation.");
            if ((string)response["requestId"] != requestId || (string)response["revision"] != capturedRevision) throw new InvalidOperationException("Decision request mismatch.");
            var record = response["record"] as JObject;
            if (record == null || (int?)record["schemaVersion"] != 1 || (string)record["pack"]?["name"] != packId || string.IsNullOrEmpty((string)record["model"]))
                throw new InvalidOperationException("Invalid decision provenance.");
            var outcome = (string)record["outcome"];
            if (string.IsNullOrEmpty(outcome) || allowedOutcomes == null || Array.IndexOf(allowedOutcomes, outcome) < 0)
                throw new InvalidOperationException("Decision outcome is not allowed by this policy.");
            return outcome;
        }
    }
}
