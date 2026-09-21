// Purpose: Request server-side Jev decisions without embedding a provider credential in the game.
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
namespace JevBehavior
{
    public sealed class JevDecisionClient : MonoBehaviour
    {
        public string GatewayUrl = "http://127.0.0.1:8787/v1/decision";
        [Range(1, 60)] public int TimeoutSeconds = 35;
        // Set this from your session service. It is intentionally not serialized in a scene or prefab.
        [NonSerialized] public string SessionToken;
        public async Task<JObject> EvaluateAsync(string packId, string stateJson, string revision, string requestId, CancellationToken cancellation)
        {
            if (!Uri.TryCreate(GatewayUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
                throw new InvalidOperationException("Gateway requires HTTPS, except on loopback.");
            if (string.IsNullOrWhiteSpace(SessionToken)) throw new InvalidOperationException("Set a gateway session token at runtime.");
            if (TimeoutSeconds < 1 || TimeoutSeconds > 60) throw new InvalidOperationException("Invalid timeout.");
            var state = JObject.Parse(stateJson);
            var payload = new JObject { ["packId"] = packId, ["state"] = state, ["revision"] = revision, ["requestId"] = requestId };
            var bytes = Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None));
            if (bytes.Length > 100000) throw new InvalidOperationException("Decision request is too large.");
            using var request = new UnityWebRequest(GatewayUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bytes); request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json"); request.SetRequestHeader("Authorization", "Bearer " + SessionToken);
            request.timeout = TimeoutSeconds; request.redirectLimit = 0;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            deadline.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
            var operation = request.SendWebRequest();
            try
            {
                // Yield to Unity's synchronization context so all UnityWebRequest access stays on its main thread.
                while (!operation.isDone) { deadline.Token.ThrowIfCancellationRequested(); await Task.Yield(); }
                deadline.Token.ThrowIfCancellationRequested();
                if (request.result != UnityWebRequest.Result.Success) throw new InvalidOperationException($"Gateway request failed ({request.responseCode}): {request.error}");
                if (request.downloadHandler.data.Length > 1000000) throw new InvalidOperationException("Gateway response too large.");
                return JObject.Parse(request.downloadHandler.text);
            }
            catch { request.Abort(); throw; }
        }
    }
}
