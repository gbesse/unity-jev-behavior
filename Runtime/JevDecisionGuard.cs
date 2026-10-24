// Purpose: Reject empty, coerced, stale or misrouted decisions before changing a Behavior blackboard.
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace JevBehavior
{
    public static class JevDecisionGuard
    {
        public static JObject Parse(string raw)
        {
            try {
                if (string.IsNullOrWhiteSpace(raw)) throw new JsonException();
                return JObject.Parse(raw, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            } catch (JsonException) {
                throw new InvalidOperationException("Objet JSON ambigu ou invalide / Ambiguous or invalid JSON object / Objeto JSON ambiguo o no válido");
            }
        }
        private static bool Text(JToken value) => value != null && value.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)value);
        private static bool Exact(JToken value, string expected) => Text(value) && string.Equals((string)value, expected, StringComparison.Ordinal);
        private static bool SchemaOne(JToken value) => value != null &&
            ((value.Type == JTokenType.Integer && value.ToString(Formatting.None) == "1") ||
             (value.Type == JTokenType.Float && (double)value == 1.0));
        public static string Accept(JObject response, string requestId, string capturedRevision, string currentRevision, string packId, string[] allowedOutcomes)
        {
            if (response == null || string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(capturedRevision) || string.IsNullOrWhiteSpace(currentRevision) || string.IsNullOrWhiteSpace(packId))
                throw new InvalidOperationException("Identite de decision vide / Empty decision identity / Identidad de decision vacia");
            if (allowedOutcomes == null || allowedOutcomes.Length == 0)
                throw new InvalidOperationException("Politique vide / Empty policy / Politica vacia");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var outcome in allowedOutcomes)
                if (string.IsNullOrWhiteSpace(outcome) || !seen.Add(outcome))
                    throw new InvalidOperationException("Issues de politique vides ou dupliquees / Empty or duplicate policy outcomes / Resultados de politica vacios o duplicados");
            if (capturedRevision != currentRevision) throw new InvalidOperationException("Revision du monde modifiee / World revision changed / Revision del mundo modificada");
            if (!Exact(response["requestId"], requestId) || !Exact(response["revision"], capturedRevision))
                throw new InvalidOperationException("Requete de decision differente / Decision request mismatch / Solicitud de decision distinta");
            var record = response["record"] as JObject;
            var pack = record?["pack"] as JObject;
            if (record == null || !SchemaOne(record["schemaVersion"]) || !Exact(pack?["name"], packId) || !Text(record["model"]))
                throw new InvalidOperationException("Provenance de decision invalide / Invalid decision provenance / Procedencia de decision no valida");
            var value = record["outcome"];
            if (!Text(value) || allowedOutcomes == null || Array.IndexOf(allowedOutcomes, (string)value) < 0)
                throw new InvalidOperationException("Issue de decision non autorisee / Decision outcome not allowed / Resultado de decision no permitido");
            return (string)value;
        }
    }
}
