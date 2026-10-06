# Jev Behavior for Unity

A native Unity Behavior action node with finite outcomes, revision checks and an authenticated Node.js gateway. Jev credentials stay on the server; the game receives a decision record and chooses its own next action.

**v0.1.2 experimental alpha · MIT · Unity 6**. Tested in editor 6000.3.23f1 with Unity Behavior 1.0.16. Independent community integration.

## Install

In Unity Package Manager → Add package from Git URL:

```text
https://github.com/gbesse/unity-jev-behavior.git#v0.1.2
```

Create a **Jev / Behavior Policy** asset. Add `JevDecisionClient` to a GameObject. In a Behavior graph add **Action / Jev / Jev Decision**, then bind `ClientObject`, `Policy`, `StateJson`, `Revision`, `Outcome` and `Error` blackboard variables. A valid result sets `Outcome` and succeeds; failure clears the outcome and reports the error. Changing `Revision` while evaluating rejects the response. Increment this revision whenever relevant world state changes.

The policy declares a registered pack ID and allowed outcomes. It does not contain the provider key. `SessionToken` must be set by trusted application code at runtime. Import the **Support Decision** sample for a runnable component and instructions.

## Local gateway demo

Clone this repository and run:

```sh
cd gateway
npm ci
export JEV_GATEWAY_TOKEN='replace-with-a-random-local-token'
npm start -- --fixture
```

Set the client URL to `http://127.0.0.1:8787/v1/decision` and its runtime `SessionToken` to the same token. The fixture is explicitly synthetic and returns the billing judgment. For real inference omit `--fixture` and set `TYPESAFE_API_KEY` on the gateway. The server registers only the bundled support pack and uses the existing DecisionPacks runtime.

The supplied gateway is a loopback development server with a shared development token, four concurrent calls, body limits and total deadlines. A production multiplayer service needs your user/session authorization, per-user limits and HTTPS deployment. Do not embed the shared development token or a Typesafe key into a distributed player. Browser/WebGL deployment, mobile/console builds, IL2CPP and production gateway authentication have not been validated.

## Verify the gateway contract before opening Unity

`cd gateway && npm ci && npm run demo:fixture` starts an ephemeral loopback gateway, sends the bundled synthetic support ticket and prints the finite outcome. It needs no API key or Unity editor. The example confirms the HTTP contract only; use the Unity sample above to verify the Behavior node and revision handling in Editor.

The same fixture now sends a second request with an invalid token and requires HTTP 401. / La même fixture envoie une seconde requête avec un jeton invalide et exige HTTP 401. / La misma fixture envía una segunda solicitud con un token no válido y exige HTTP 401. This exercises the gateway's authentication boundary; it does not validate a Unity player build. / Elle vérifie la frontière d'authentification de la passerelle, pas une compilation du jeu Unity. / Comprueba el límite de autenticación de la pasarela, no una compilación del juego Unity.

## Verification

```sh
cd gateway
npm ci
npm test
```

Open `TestProject~` in the specified Unity editor and run EditMode tests in Test Runner. The host project refers to the root package through a local UPM dependency. The editor tests cover native Behavior types, finite outcomes, stale/misrouted responses and an actual `UnityWebRequest` against a loopback HTTP fixture. Gateway tests exercise authenticated real HTTP, records, unknown packs, errors and deadlines. No player build or live Jev inference was run.

See [reuse and provenance](docs/reuse.md), [contributing](CONTRIBUTING.md) and [security](SECURITY.md).

[Recorded verification scope](docs/verification.md).
