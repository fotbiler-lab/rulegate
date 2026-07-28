import { Injectable, inject } from '@angular/core';
import {
  isRuleGateIdentifier,
  RuleGateAuthorizationClient,
  RuleGateAuthorizationSnapshot,
} from '@fotbiler/rulegate-angular';

import {
  RuleGateKeycloakSession,
  RuleGateKeycloakSnapshotOptions,
} from './rule-gate-keycloak.models';
import {
  ruleGateKeycloakClientRole,
  ruleGateKeycloakRealmRole,
} from './rule-gate-keycloak-role-names';

const DEFAULT_PERMISSION_CLAIM = 'permission';

/**
 * Creates a provider-independent UI projection from an authenticated keycloak-js session.
 *
 * Returns `null` for unauthenticated, malformed, or ambiguously configured input.
 */
export function createRuleGateSnapshotFromKeycloak(
  session: RuleGateKeycloakSession,
  options: RuleGateKeycloakSnapshotOptions = {},
): RuleGateAuthorizationSnapshot | null {
  if (
    typeof session !== 'object' ||
    session === null ||
    Array.isArray(session) ||
    session.authenticated !== true
  ) {
    return null;
  }

  if (typeof options !== 'object' || options === null || Array.isArray(options)) {
    return null;
  }

  const includeRealmRoles = options.includeRealmRoles ?? true;
  const clientIds = normalizeIdentifiers(options.clientIds ?? []);
  const permissionClaim =
    options.permissionClaim === undefined ? DEFAULT_PERMISSION_CLAIM : options.permissionClaim;

  if (
    typeof includeRealmRoles !== 'boolean' ||
    clientIds === null ||
    (permissionClaim !== null && !isRuleGateIdentifier(permissionClaim))
  ) {
    return null;
  }

  const roles = new Set<string>();

  if (includeRealmRoles) {
    const realmRoles = readAccessRoles(session.realmAccess);

    if (realmRoles === null) {
      return null;
    }

    for (const role of realmRoles) {
      roles.add(ruleGateKeycloakRealmRole(role));
    }
  }

  if (clientIds.length !== 0 && !isPlainObjectOrUndefined(session.resourceAccess)) {
    return null;
  }

  for (const clientId of clientIds) {
    const clientAccess = isPlainObject(session.resourceAccess)
      ? session.resourceAccess[clientId]
      : undefined;
    const clientRoles = readAccessRoles(clientAccess);

    if (clientRoles === null) {
      return null;
    }

    for (const role of clientRoles) {
      roles.add(ruleGateKeycloakClientRole(clientId, role));
    }
  }

  const permissions = readPermissions(session.tokenParsed, permissionClaim);

  if (permissions === null) {
    return null;
  }

  return Object.freeze({
    permissions: Object.freeze([...permissions].sort()),
    policies: Object.freeze([]),
    roles: Object.freeze([...roles].sort()),
  });
}

/** Synchronizes the generic RuleGate client without taking ownership of the Keycloak lifecycle. */
@Injectable({ providedIn: 'root' })
export class RuleGateKeycloakAdapter {
  private readonly authorization = inject(RuleGateAuthorizationClient);

  synchronize(
    session: RuleGateKeycloakSession,
    options: RuleGateKeycloakSnapshotOptions = {},
  ): boolean {
    const snapshot = createRuleGateSnapshotFromKeycloak(session, options);

    if (snapshot === null) {
      this.authorization.clear();
      return false;
    }

    return this.authorization.replaceSnapshot(snapshot);
  }

  clear(): void {
    this.authorization.clear();
  }
}

function readAccessRoles(access: unknown): readonly string[] | null {
  if (access === undefined) {
    return [];
  }

  if (!isPlainObject(access)) {
    return null;
  }

  const roles = access['roles'];

  if (roles === undefined) {
    return [];
  }

  return normalizeIdentifiers(roles);
}

function readPermissions(tokenParsed: unknown, claimType: string | null): readonly string[] | null {
  if (claimType === null || tokenParsed === undefined) {
    return [];
  }

  if (!isPlainObject(tokenParsed)) {
    return null;
  }

  const value = tokenParsed[claimType];

  if (value === undefined) {
    return [];
  }

  if (typeof value === 'string') {
    return normalizeIdentifiers([value]);
  }

  return normalizeIdentifiers(value);
}

function normalizeIdentifiers(values: unknown): readonly string[] | null {
  if (!Array.isArray(values)) {
    return null;
  }

  const identifiers = new Set<string>();

  for (const value of values) {
    if (!isRuleGateIdentifier(value)) {
      return null;
    }

    identifiers.add(value);
  }

  return [...identifiers];
}

function isPlainObject(value: unknown): value is Readonly<Record<string, unknown>> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isPlainObjectOrUndefined(
  value: unknown,
): value is Readonly<Record<string, unknown>> | undefined {
  return value === undefined || isPlainObject(value);
}
