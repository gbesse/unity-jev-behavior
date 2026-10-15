# Verification record

This records checks performed locally on 2026-09-21 for v0.1.0.

Unity 6000.3.23f1 EditMode: 7 tests, including a real UnityWebRequest loopback request. Node gateway: 5 tests. No player build.

Provider judgments were synthetic. These checks do not establish live Jev accuracy, calibration, production capacity or an upstream partnership. The GitHub workflow contains reproducible checks; Unity editor tests require an installed licensed editor and are separate from gateway CI.

## Calls that outlive a deadline / Appels dépassant le délai / Llamadas que exceden el plazo

EN: Gateway capacity and request-ID reservations now remain occupied while a provider ignores cancellation and keeps running. New work returns 429 when active requests plus unfinished calls reach `maxConcurrent`; with spare capacity, the same pending ID returns 425 and different content returns 409. Capacity is released when that provider promise settles. A late result is never cached or returned to the timed-out game. An indefinitely hanging provider can occupy a slot until process restart; the gateway cannot terminate external side effects or synchronous blocking JavaScript.

FR : La capacité et les réservations d’identifiants restent désormais occupées lorsqu’un fournisseur ignore l’annulation et continue. Un nouveau travail reçoit 429 lorsque les requêtes actives et appels non terminés atteignent `maxConcurrent` ; avec de la capacité libre, le même identifiant reçoit 425 et un contenu différent 409. La résolution de la promesse libère la capacité. Aucun résultat tardif n’est mis en cache ou rendu au jeu après expiration. Un fournisseur indéfiniment bloqué peut occuper une place jusqu’au redémarrage ; la passerelle ne peut arrêter les effets externes ni du JavaScript synchrone bloquant.

ES: La capacidad y las reservas de identificadores permanecen ocupadas mientras un proveedor ignora la cancelación y sigue ejecutándose. Trabajo nuevo recibe 429 cuando solicitudes activas y llamadas sin terminar alcanzan `maxConcurrent`; con capacidad libre, el mismo identificador recibe 425 y contenido distinto 409. La finalización de la promesa libera capacidad. Nunca almacena ni devuelve resultados tardíos al juego caducado. Un proveedor bloqueado indefinidamente puede ocupar una plaza hasta reiniciar; la pasarela no puede detener efectos externos ni JavaScript síncrono bloqueante.
