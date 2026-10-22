# Jev Behavior for Unity

## Rejouer une décision récente · Replay a recent decision · Reproducir una decisión reciente

**FR :** `cd gateway && npm run demo:fixture` vérifie maintenant qu’un deuxième envoi identique retourne le même résultat sans deuxième appel au fournisseur. Conserver `requestId`, `packId`, `revision` et le même état pour une reprise réseau ; les clés d’objet peuvent être dans un ordre différent. La réponse ajoute `cache: fresh` ou `cache: replay`. Un identifiant réutilisé avec un autre état, pack ou révision renvoie **409** ; une requête identique déjà en cours renvoie **425** sans lancer un autre calcul. Les échecs ne sont pas mis en cache. La capacité du gateway reste applicable : une saturation peut répondre 429 avant la lecture du corps.

Le cache de réponses réussies conserve au plus 128 entrées pendant 60 secondes par défaut. `createGateway({cacheLimit, cacheTtlMs, ...})` accepte 0–1024 entrées et 1–300000 ms ; `cacheLimit: 0` désactive la relecture terminée. L’éviction et l’expiration permettent une nouvelle évaluation. Ce cache est en mémoire, limité au processus et au jeton de développement partagé. Il ne garantit pas une exécution unique après redémarrage et ne représente pas une autorisation multijoueur ; un service de sessions doit définir son propre périmètre utilisateur. Le client Unity et le nœud Behavior ne sont pas modifiés ; cette évolution est vérifiée sur le gateway HTTP avec données synthétiques, sans appel Jev réel ni nouvelle compilation du jeu.

**EN:** `cd gateway && npm run demo:fixture` now verifies that an identical second request returns the same result without another provider call. Keep `requestId`, `packId`, `revision` and the same state for a network retry; object keys may have a different order. The response adds `cache: fresh` or `cache: replay`. Reusing an ID with another state, pack or revision returns **409**; an identical request still running returns **425** without starting another inference. Failures are not cached. Gateway capacity still applies: saturation can return 429 before the body is read.

Successful responses default to at most 128 entries for 60 seconds. `createGateway({cacheLimit, cacheTtlMs, ...})` accepts 0–1024 entries and 1–300000 ms; `cacheLimit: 0` disables completed replay. Eviction and expiry allow a new evaluation. This in-memory cache is scoped to the process and shared development token. It does not guarantee exactly-once execution after restart or provide multiplayer authorization; a session service must define its own user scope. Unity client and Behavior node are unchanged; this update is tested on the HTTP gateway with synthetic data, without live Jev inference or a new player build.

**ES:** `cd gateway && npm run demo:fixture` comprueba que una segunda solicitud idéntica devuelve el mismo resultado sin otra llamada al proveedor. Conservar `requestId`, `packId`, `revision` y el mismo estado al reintentar; el orden de claves de objetos puede cambiar. La respuesta añade `cache: fresh` o `cache: replay`. Reutilizar un identificador con otro estado, pack o revisión devuelve **409**; una solicitud idéntica todavía en curso devuelve **425** sin iniciar otro cálculo. Los errores no se guardan. La capacidad sigue aplicándose: una saturación puede devolver 429 antes de leer el cuerpo.

Las respuestas correctas conservan por defecto hasta 128 entradas durante 60 segundos. `createGateway({cacheLimit, cacheTtlMs, ...})` acepta 0–1024 entradas y 1–300000 ms; `cacheLimit: 0` desactiva la reproducción terminada. La expulsión y caducidad permiten una nueva evaluación. El caché está en memoria y limitado al proceso y token de desarrollo compartido. No garantiza ejecución única tras reiniciar ni autorización multijugador; un servicio de sesiones debe definir su ámbito por usuario. El cliente Unity y nodo Behavior no cambian; la evolución se prueba en el gateway HTTP con datos sintéticos, sin inferencia Jev real ni nueva compilación del juego.

A native Unity Behavior action node with finite outcomes, revision checks and an authenticated Node.js gateway. Jev credentials stay on the server; the game receives a decision record and chooses its own next action.

**v0.1.6 · FR : évolution source expérimentale · EN: experimental source update · ES: actualización fuente experimental · MIT · Unity 6**. FR : la validation Editor 6000.3.23f1 / Behavior 1.0.16 concernait la version 0.1.4 ; cette évolution attend une nouvelle validation Editor, bloquée ici par la licence. EN: Editor 6000.3.23f1 / Behavior 1.0.16 validation applied to 0.1.4; this update awaits a new Editor run, blocked here by licensing. ES: la validación Editor 6000.3.23f1 / Behavior 1.0.16 correspondía a 0.1.4; esta actualización espera nueva validación Editor, bloqueada aquí por licencia.

## Install

In Unity Package Manager → Add package from Git URL:

```text
https://github.com/gbesse/unity-jev-behavior.git#v0.1.4
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

## October 2026 improvement · Amélioration d’octobre 2026 · Mejora de octubre de 2026

`cd gateway && npm run demo:fixture` now checks both an invalid token (401) and an unregistered pack (400) alongside the successful synthetic decision. It does not validate a Unity player build.

`cd gateway && npm run demo:fixture` vérifie désormais un jeton invalide (401), un pack non enregistré (400) et la décision synthétique valide. Cela ne valide pas une compilation Unity.

`cd gateway && npm run demo:fixture` ahora comprueba un token inválido (401), un pack no registrado (400) y la decisión sintética válida. Esto no valida una compilación de Unity.

## Contrôle d’adoption · Adoption check · Comprobación de adopción

[Français : essayer un cas concret](examples/adoption-check.md) · [English: try a concrete case](examples/adoption-check.md) · [Español: pruebe un caso concreto](examples/adoption-check.md).

## Identités typées et annulation / Typed identities and cancellation / Identidades tipadas y cancelación

FR — Le garde refuse les identités vides, blanches ou converties depuis des nombres, ainsi que les versions de schéma fractionnaires. Une valeur numérique exactement égale à 1 reste compatible. Le modèle doit être un texte non vide ; son identité reste déclarée par la passerelle. Une requête déjà annulée échoue avant le transport UnityWebRequest. Les décisions restent limitées aux issues de la politique, sans nouvelle logique autonome.

EN — The guard rejects empty, whitespace or number-coerced identities and fractional schema versions. A numeric value exactly equal to 1 remains compatible. Model identity must be nonempty text and remains gateway-declared. Already-cancelled requests fail before UnityWebRequest transport. Decisions remain restricted to policy outcomes, without new autonomous behavior.

ES — El guard rechaza identidades vacías, blancas o convertidas desde números, y versiones fraccionarias. Un valor numérico exactamente igual a 1 sigue siendo compatible. El modelo debe ser texto no vacío y su identidad sigue declarada por la pasarela. Peticiones ya canceladas fallan antes del transporte UnityWebRequest. Decisiones siguen limitadas a resultados de la política, sin nueva autonomía.

FR — Le garde de production peut être compilé et testé séparément avec `dotnet run --project PortableGuardTests~/GuardTests.csproj`. La CI utilise ce contrôle en plus de la passerelle ; cela ne valide ni `UnityWebRequest` ni Behavior dans Editor. Aucun nouveau tag natif avant cette validation.

EN — Compile and test the production guard independently with `dotnet run --project PortableGuardTests~/GuardTests.csproj`. CI runs this alongside gateway checks; it validates neither `UnityWebRequest` nor Behavior in Editor. No new native tag before that validation.

ES — Compila y prueba el guard de producción por separado con `dotnet run --project PortableGuardTests~/GuardTests.csproj`. CI lo ejecuta junto a la pasarela; no valida `UnityWebRequest` ni Behavior en Editor. Sin nuevo tag nativo hasta esa validación.

## Politique / Policy / Política

FR — Le garde refuse les politiques vides, les issues blanches et les doublons avant tout verdict. Cette évolution est publiée en source ; les tests Editor et player build restent non validés (licence indisponible lors du précédent essai, code 198).

EN — The guard rejects empty policies, blank outcomes and duplicates before accepting any verdict. This change is source-only; Editor tests and player build remain unvalidated (previous attempt lacked a valid license, exit 198).

ES — El guard rechaza políticas vacías, resultados en blanco y duplicados antes de aceptar un veredicto. Cambio publicado solo en fuente; pruebas Editor y player build sin validar (intento previo sin licencia válida, código 198).
