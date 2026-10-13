import test from 'node:test';
import assert from 'node:assert/strict';
import { once } from 'node:events';
import { readFile } from 'node:fs/promises';
import { setTimeout as delay } from 'node:timers/promises';
import { createGateway } from '../src/server.mjs';
const pack = JSON.parse(
  await readFile(new URL('../../packs/support-triage.json', import.meta.url)),
);
const fixture = JSON.parse(
  await readFile(new URL('../../examples/synthetic-billing-response.json', import.meta.url)),
);
const token = 'synthetic-gateway-session';
const payload = {
  packId: pack.name,
  state: { text: 'Charged twice', category: 'billing' },
  revision: '42',
  requestId: 'one',
};
async function host(t, options = {}) {
  const server = createGateway({ packs: { [pack.name]: pack }, token, ...options });
  server.listen(0, '127.0.0.1');
  await once(server, 'listening');
  t.after(
    () =>
      new Promise((resolve) => {
        server.close(resolve);
        server.closeAllConnections();
      }),
  );
  const url = `http://127.0.0.1:${server.address().port}/v1/decision`;
  return (body = payload, key = token) =>
    fetch(url, {
      method: 'POST',
      headers: { authorization: 'Bearer ' + key, 'content-type': 'application/json' },
      body: JSON.stringify(body),
      signal: AbortSignal.timeout(3000),
    });
}
test('same request replays one decision, including equivalent object key order', async (t) => {
  let calls = 0;
  const post = await host(t, {
    provider: async () => {
      calls++;
      return structuredClone(fixture);
    },
  });
  const first = await post();
  const fresh = await first.json();
  assert.equal(first.status, 200);
  assert.equal(fresh.cache, 'fresh');
  const second = await post({ ...payload, state: { category: 'billing', text: 'Charged twice' } });
  const replay = await second.json();
  assert.equal(second.status, 200);
  assert.equal(replay.cache, 'replay');
  assert.deepEqual(replay.record, fresh.record);
  assert.equal(calls, 1);
  assert.equal((await post(payload, 'invalid')).status, 401);
});
test('changed world revision or state conflicts with a completed request ID', async (t) => {
  let calls = 0;
  const post = await host(t, {
    provider: async () => {
      calls++;
      return structuredClone(fixture);
    },
  });
  assert.equal((await post()).status, 200);
  for (const changed of [
    { ...payload, revision: '43' },
    { ...payload, state: { text: 'Different ticket' } },
  ]) {
    const response = await post(changed);
    assert.equal(response.status, 409);
    assert.equal((await response.json()).code, 'request_id_conflict');
  }
  assert.equal(calls, 1);
});
test('duplicate pending request never creates another inference', async (t) => {
  let release,
    entered,
    calls = 0;
  const ready = new Promise((resolve) => {
    entered = resolve;
  });
  const gate = new Promise((resolve) => {
    release = resolve;
  });
  const post = await host(t, {
    provider: async () => {
      calls++;
      entered();
      await gate;
      return structuredClone(fixture);
    },
  });
  const first = post();
  await ready;
  try {
    const pending = await post();
    assert.equal(pending.status, 425);
    assert.equal((await pending.json()).code, 'decision_pending');
    assert.equal((await post({ ...payload, revision: '43' })).status, 409);
    assert.equal(calls, 1);
  } finally {
    release();
  }
  assert.equal((await first).status, 200);
});
test('failed requests are not cached and may be retried', async (t) => {
  let calls = 0;
  const post = await host(t, {
    provider: async () => {
      calls++;
      if (calls === 1) throw new Error('synthetic failure');
      return structuredClone(fixture);
    },
    onError: () => {},
  });
  assert.equal((await post()).status, 502);
  assert.equal((await post()).status, 200);
  assert.equal(calls, 2);
});
test('completed cache expires and its capacity is bounded', async (t) => {
  let calls = 0;
  const provider = async () => {
    calls++;
    return structuredClone(fixture);
  };
  const post = await host(t, { provider, cacheLimit: 1 });
  await post();
  await post({ ...payload, requestId: 'two' });
  const evicted = await post();
  assert.equal((await evicted.json()).cache, 'fresh');
  assert.equal(calls, 3);
  const expiring = await host(t, { provider, cacheTtlMs: 50 });
  await expiring();
  await delay(100);
  const expired = await expiring();
  assert.equal((await expired.json()).cache, 'fresh');
  assert.equal(calls, 5);
});
test('completed replay can be disabled and invalid bounds are rejected', async (t) => {
  let calls = 0;
  const post = await host(t, {
    provider: async () => {
      calls++;
      return structuredClone(fixture);
    },
    cacheLimit: 0,
  });
  await post();
  await post();
  assert.equal(calls, 2);
  assert.throws(() => createGateway({ packs: { [pack.name]: pack }, token, cacheLimit: -1 }));
  assert.throws(() => createGateway({ packs: { [pack.name]: pack }, token, cacheTtlMs: 300001 }));
});
