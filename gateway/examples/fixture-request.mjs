// Run an authenticated, synthetic request against an ephemeral local gateway.
import { readFile } from 'node:fs/promises';
import { createGateway } from '../src/server.mjs';

const pack = JSON.parse(
  await readFile(new URL('../../packs/support-triage.json', import.meta.url)),
);
const fixture = JSON.parse(
  await readFile(new URL('../../examples/synthetic-billing-response.json', import.meta.url)),
);
const state = JSON.parse(
  await readFile(new URL('../../examples/billing-state.json', import.meta.url)),
);
const token = 'synthetic-local-token-only';
let providerCalls = 0;
const server = createGateway({
  packs: { [pack.name]: pack },
  token,
  provider: async () => {
    providerCalls++;
    return structuredClone(fixture);
  },
  onError: (error) => {
    throw error;
  },
});
try {
  await new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', resolve);
  });
  const response = await fetch(`http://127.0.0.1:${server.address().port}/v1/decision`, {
    method: 'POST',
    headers: { authorization: `Bearer ${token}`, 'content-type': 'application/json' },
    body: JSON.stringify({
      packId: pack.name,
      state: { text: state.text },
      revision: 'demo-1',
      requestId: 'synthetic-request-1',
    }),
  });
  const body = await response.json();
  if (
    !response.ok ||
    body.requestId !== 'synthetic-request-1' ||
    body.record?.outcome !== 'billing'
  ) {
    throw new Error(`Unexpected synthetic gateway response: ${response.status}`);
  }
  const replayed = await fetch(`http://127.0.0.1:${server.address().port}/v1/decision`, {
    method: 'POST',
    headers: { authorization: `Bearer ${token}`, 'content-type': 'application/json' },
    body: JSON.stringify({
      packId: pack.name,
      state: { text: state.text },
      revision: 'demo-1',
      requestId: 'synthetic-request-1',
    }),
  });
  const replay = await replayed.json();
  if (replayed.status !== 200 || replay.cache !== 'replay' || providerCalls !== 1)
    throw new Error(
      'FR : relecture synthétique incorrecte. EN: unexpected synthetic replay. ES: reproducción sintética inesperada.',
    );
  const conflict = await fetch(`http://127.0.0.1:${server.address().port}/v1/decision`, {
    method: 'POST',
    headers: { authorization: `Bearer ${token}`, 'content-type': 'application/json' },
    body: JSON.stringify({
      packId: pack.name,
      state: { text: state.text },
      revision: 'demo-new',
      requestId: 'synthetic-request-1',
    }),
  });
  if (conflict.status !== 409 || providerCalls !== 1)
    throw new Error(
      'FR : conflit synthétique incorrect. EN: unexpected synthetic conflict. ES: conflicto sintético inesperado.',
    );
  const rejected = await fetch(`http://127.0.0.1:${server.address().port}/v1/decision`, {
    method: 'POST',
    headers: { authorization: 'Bearer invalid-token', 'content-type': 'application/json' },
    body: JSON.stringify({
      packId: pack.name,
      state: { text: state.text },
      revision: 'demo-2',
      requestId: 'synthetic-request-2',
    }),
  });
  if (rejected.status !== 401)
    throw new Error(`Expected an unauthorized response; got ${rejected.status}`);
  const unknownPack = await fetch(`http://127.0.0.1:${server.address().port}/v1/decision`, {
    method: 'POST',
    headers: { authorization: `Bearer ${token}`, 'content-type': 'application/json' },
    body: JSON.stringify({
      packId: 'unregistered-pack',
      state: { text: state.text },
      revision: 'demo-3',
      requestId: 'synthetic-request-3',
    }),
  });
  if (unknownPack.status !== 400)
    throw new Error(`Expected an unknown-pack response; got ${unknownPack.status}`);
  console.log(
    JSON.stringify({
      source: 'synthetic fixture; no Jev call',
      status: response.status,
      outcome: body.record.outcome,
      revision: body.revision,
      unauthorized_status: rejected.status,
      unknown_pack_status: unknownPack.status,
      replay_status: replayed.status,
      replay_cache: replay.cache,
      conflict_status: conflict.status,
      provider_calls: providerCalls,
    }),
  );
} finally {
  await new Promise((resolve) => server.close(resolve));
}
