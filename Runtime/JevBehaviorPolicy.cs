// Purpose: Declare the registered server pack and the finite outcomes a Behavior graph may accept.
using UnityEngine;
namespace JevBehavior
{
    [CreateAssetMenu(menuName = "Jev/Behavior Policy")]
    public sealed class JevBehaviorPolicy : ScriptableObject
    {
        public string ExpectedModel = "jev-1.13.0";
        public string PackId = "support/triage";
        public string[] AllowedOutcomes = { "billing", "technical", "sales", "review" };
    }
}
