# unity-jev-behavior — contrôle d’adoption · adoption check · comprobación de adopción

## Français

Point de départ local, après la préparation indiquée dans le README :

```sh
cd gateway && npm run demo:fixture
```

Un pack absent du registre doit être rejeté par la passerelle avant l’appel au fournisseur. Vérifiez ensuite la liaison du nœud Behavior dans Unity Editor ; le contrôle Node seul ne prouve pas l’intégration.

## English

Local starting point, after the setup described in the README:

```sh
cd gateway && npm run demo:fixture
```

A pack missing from the registry should be rejected by the gateway before a provider call. Then check the Behavior node binding in Unity Editor; the Node check alone does not prove integration.

## Español

Punto de partida local, después de la preparación descrita en el README:

```sh
cd gateway && npm run demo:fixture
```

Un paquete ausente del registro debe rechazarse en la pasarela antes de llamar al proveedor. Compruebe después el enlace del nodo Behavior en Unity Editor; la prueba de Node por sí sola no demuestra la integración.
## Variante synthétique · Synthetic variation · Variante sintética

```text
packId="unknown/pack"; expected_http_status=400
```

FR : adaptez une copie de la fixture locale à cette situation, puis vérifiez le comportement décrit ci-dessus. Les valeurs sont illustratives, pas des résultats Jev mesurés.

EN: adapt a copy of the local fixture to this situation, then check the behavior described above. Values are illustrative, not measured Jev output.

ES: adapte una copia de la fixture local a esta situación y compruebe el comportamiento descrito arriba. Los valores son ilustrativos, no resultados Jev medidos.
