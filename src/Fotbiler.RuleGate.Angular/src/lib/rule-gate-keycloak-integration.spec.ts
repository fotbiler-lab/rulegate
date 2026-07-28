import { TestBed } from '@angular/core/testing';
import { RuleGateAuthorizationClient } from '@fotbiler/rulegate-angular';
import {
  createRuleGateSnapshotFromKeycloak,
  encodeRuleGateKeycloakComponent,
  ruleGateKeycloakClientRole,
  ruleGateKeycloakRealmRole,
  RuleGateKeycloakAdapter,
} from '@fotbiler/rulegate-angular/keycloak';

describe('RuleGate Keycloak integration', () => {
  it('uses canonical UTF-8 role identifiers', () => {
    expect(encodeRuleGateKeycloakComponent('mühendis')).toBe('m%C3%BChendis');
    expect(ruleGateKeycloakRealmRole('realm admin')).toBe('keycloak:realm:realm%20admin');
    expect(ruleGateKeycloakClientRole('web portal', 'documents/read')).toBe(
      'keycloak:client:web%20portal:documents%2Fread',
    );
  });

  it('maps realm roles, selected client roles, and explicit permissions', () => {
    const snapshot = createRuleGateSnapshotFromKeycloak(
      {
        authenticated: true,
        realmAccess: { roles: ['admin', 'admin'] },
        resourceAccess: {
          web: { roles: ['reader'] },
          ignored: { roles: ['owner'] },
        },
        tokenParsed: { permission: ['documents.read', 'documents.read'] },
      },
      { clientIds: ['web'] },
    );

    expect(snapshot).toEqual({
      permissions: ['documents.read'],
      policies: [],
      roles: ['keycloak:client:web:reader', 'keycloak:realm:admin'],
    });
  });

  it('fails closed for unauthenticated and malformed sessions', () => {
    expect(createRuleGateSnapshotFromKeycloak({ authenticated: false })).toBeNull();
    expect(
      createRuleGateSnapshotFromKeycloak({ authenticated: true, realmAccess: { roles: [' '] } }),
    ).toBeNull();
    expect(
      createRuleGateSnapshotFromKeycloak(
        { authenticated: true, resourceAccess: { web: { roles: 'reader' } } },
        { clientIds: ['web'] },
      ),
    ).toBeNull();
    expect(
      createRuleGateSnapshotFromKeycloak(
        { authenticated: true, resourceAccess: 'ignored' },
        { clientIds: [] },
      ),
    ).toEqual({ permissions: [], policies: [], roles: [] });
    expect(createRuleGateSnapshotFromKeycloak({ authenticated: true }, null as never)).toBeNull();
  });

  it('synchronizes and clears the provider-independent authorization client', () => {
    TestBed.configureTestingModule({});
    const client = TestBed.inject(RuleGateAuthorizationClient);
    const adapter = TestBed.inject(RuleGateKeycloakAdapter);

    expect(adapter.synchronize({ authenticated: true, realmAccess: { roles: ['admin'] } })).toBe(
      true,
    );
    expect(client.hasRole('keycloak:realm:admin')).toBe(true);

    expect(adapter.synchronize({ authenticated: false })).toBe(false);
    expect(client.isReady()).toBe(false);
  });
});
