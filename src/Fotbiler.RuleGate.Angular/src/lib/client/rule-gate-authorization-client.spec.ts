import { RuleGateAuthorizationClient } from './rule-gate-authorization-client';

describe('RuleGateAuthorizationClient', () => {
  let client: RuleGateAuthorizationClient;

  beforeEach(() => {
    client = new RuleGateAuthorizationClient();
  });

  it('denies all checks before a snapshot is supplied', () => {
    expect(client.isReady()).toBe(false);
    expect(client.hasPermission('documents.read')).toBe(false);
    expect(client.hasPolicy('documents-read')).toBe(false);
    expect(client.hasRole('documents.reader')).toBe(false);
  });

  it('matches permissions, policies, and roles exactly', () => {
    expect(
      client.replaceSnapshot({
        permissions: ['documents.read'],
        policies: ['documents-read'],
        roles: ['documents.reader'],
      }),
    ).toBe(true);

    expect(client.isReady()).toBe(true);
    expect(client.hasPermission('documents.read')).toBe(true);
    expect(client.hasPermission('Documents.Read')).toBe(false);
    expect(client.hasPolicy('documents-read')).toBe(true);
    expect(client.hasPolicy('documents-write')).toBe(false);
    expect(client.hasRole('documents.reader')).toBe(true);
    expect(client.hasRole('Documents.Reader')).toBe(false);
  });

  it('accepts string-valued generated constants', () => {
    const generated = {
      policies: {
        documentsRead: 'documents-read',
      },
      permissions: {
        documentsRead: 'documents.read',
      },
      roles: {
        documentsReader: 'documents.reader',
      },
    } as const;

    client.replaceSnapshot({
      permissions: [generated.permissions.documentsRead],
      policies: [generated.policies.documentsRead],
      roles: [generated.roles.documentsReader],
    });

    expect(client.hasPermission(generated.permissions.documentsRead)).toBe(true);
    expect(client.hasPolicy(generated.policies.documentsRead)).toBe(true);
    expect(client.hasRole(generated.roles.documentsReader)).toBe(true);
  });

  it('copies and deduplicates supplied identifiers', () => {
    const permissions = ['documents.read', 'documents.read'];

    client.replaceSnapshot({ permissions });
    permissions.push('documents.write');

    expect(client.snapshot().permissions).toEqual(['documents.read']);
    expect(client.hasPermission('documents.write')).toBe(false);
  });

  it('clears every grant when a snapshot is malformed', () => {
    client.replaceSnapshot({ permissions: ['documents.read'] });

    const accepted = client.replaceSnapshot({
      permissions: ['documents.read', ' '],
    });

    expect(accepted).toBe(false);
    expect(client.isReady()).toBe(false);
    expect(client.hasPermission('documents.read')).toBe(false);
  });

  it('rejects malformed runtime snapshot shapes', () => {
    client.replaceSnapshot({ permissions: ['documents.read'] });

    expect(client.replaceSnapshot({ permissions: 'documents.read' } as never)).toBe(false);
    expect(client.isReady()).toBe(false);

    expect(client.replaceSnapshot(null as never)).toBe(false);
    expect(client.snapshot()).toEqual({ permissions: [], policies: [], roles: [] });
  });

  it('requires exactly one valid requirement kind', () => {
    client.replaceSnapshot({
      permissions: ['documents.read'],
      policies: ['documents-read'],
      roles: ['documents.reader'],
    });

    expect(client.isGranted({ permission: 'documents.read' })).toBe(true);
    expect(client.isGranted({ policy: 'documents-read' })).toBe(true);
    expect(client.isGranted({ role: 'documents.reader' })).toBe(true);
    expect(
      client.isGranted({
        permission: 'documents.read',
        policy: 'documents-read',
      } as never),
    ).toBe(false);
    expect(client.isGranted({ permission: 'documents.read', unexpected: true } as never)).toBe(
      false,
    );
    expect(client.isGranted({} as never)).toBe(false);
  });

  it('returns to fail-closed state when cleared', () => {
    client.replaceSnapshot({ permissions: ['documents.read'] });

    client.clear();

    expect(client.isReady()).toBe(false);
    expect(client.snapshot()).toEqual({ permissions: [], policies: [], roles: [] });
    expect(client.hasPermission('documents.read')).toBe(false);
  });
});
