// Run an authenticated, synthetic request against an ephemeral local gateway.
import {readFile} from 'node:fs/promises';
import {createGateway} from '../src/server.mjs';

const pack = JSON.parse(await readFile(new URL('../../packs/support-triage.json', import.meta.url)));
const fixture = JSON.parse(await readFile(new URL('../../examples/synthetic-billing-response.json', import.meta.url)));
const state = JSON.parse(await readFile(new URL('../../examples/billing-state.json', import.meta.url)));
const token = 'synthetic-local-token-only';
const server = createGateway({
  packs: {[pack.name]: pack},
  token,
  provider: async () => structuredClone(fixture),
  onError: error => { throw error; },
});
try {
  await new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', resolve);
  });
  const response = await fetch(`http://127.0.0.1:${server.address().port}/v1/decision`, {
    method: 'POST',
    headers: {'authorization': `Bearer ${token}`, 'content-type': 'application/json'},
    body: JSON.stringify({packId: pack.name, state: {text: state.text}, revision: 'demo-1', requestId: 'synthetic-request-1'}),
  });
  const body = await response.json();
  if (!response.ok || body.requestId !== 'synthetic-request-1' || body.record?.outcome !== 'billing') {
    throw new Error(`Unexpected synthetic gateway response: ${response.status}`);
  }
  console.log(JSON.stringify({source: 'synthetic fixture; no Jev call', status: response.status, outcome: body.record.outcome, revision: body.revision}));
} finally {
  await new Promise(resolve => server.close(resolve));
}
