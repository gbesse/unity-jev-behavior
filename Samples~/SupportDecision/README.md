# Support decision sample

This component demonstrates a local, authenticated request and a guarded finite outcome. Import it from Package Manager → Jev Behavior → Samples. Add `JevDecisionClient` and `JevSupportSample` to a GameObject, create a Jev Behavior Policy asset, and assign both references. Set `Client.SessionToken` at runtime to the local gateway token (or launch the editor with `JEV_GATEWAY_TOKEN` set). Start the fixture gateway from the root README, enter Play Mode, and use the sample component's **Evaluate Jev Decision** context menu. The Console and `Outcome` field show `billing`. Change `Revision` during a slow request to test stale-response rejection.

A Behavior graph can use **Action / Jev / Jev Decision** instead: bind the client GameObject, policy, JSON state, revision, outcome and error blackboard variables. Branch on `Outcome`. The addon never executes a model-authored action.
