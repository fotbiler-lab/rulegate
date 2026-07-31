import { describe, expect, it } from 'vitest';

import { RuleGateAuthorizationStore } from './rule-gate-authorization-store.js';

describe('RuleGateAuthorizationStore', () => {
  it('defaults to deny and matches normalized identifiers exactly', () => {
    const store = new RuleGateAuthorizationStore();

    expect(store.hasPermission('documents.read')).toBe(false);
    expect(
      store.replaceSnapshot({
        permissions: ['documents.read', 'documents.read'],
        policies: ['documents-read'],
        roles: ['documents.reader'],
      }),
    ).toBe(true);
    expect(store.snapshot.permissions).toEqual(['documents.read']);
    expect(store.hasPermission('documents.read')).toBe(true);
    expect(store.hasPermission('Documents.Read')).toBe(false);
    expect(store.hasPolicy('documents-read')).toBe(true);
    expect(store.hasRole('documents.reader')).toBe(true);
  });

  it('clears all grants when runtime state is malformed', () => {
    const store = new RuleGateAuthorizationStore();
    store.replaceSnapshot({ permissions: ['documents.read'] });

    expect(store.replaceSnapshot({ permissions: [' '] })).toBe(false);
    expect(store.isReady).toBe(false);
    expect(store.hasPermission('documents.read')).toBe(false);
  });
});
