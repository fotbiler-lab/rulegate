import { computed, Injectable, signal } from '@angular/core';

import {
  RuleGateAuthorizationRequirement,
  RuleGateAuthorizationSnapshot,
} from '../models/rule-gate-authorization.models';

const EMPTY_IDENTIFIERS: readonly string[] = Object.freeze([]);

const EMPTY_SNAPSHOT: RuleGateAuthorizationSnapshot = Object.freeze({
  permissions: EMPTY_IDENTIFIERS,
  policies: EMPTY_IDENTIFIERS,
});

interface RuleGateAuthorizationState {
  readonly isReady: boolean;
  readonly permissionSet: ReadonlySet<string>;
  readonly policySet: ReadonlySet<string>;
  readonly snapshot: RuleGateAuthorizationSnapshot;
}

const EMPTY_STATE: RuleGateAuthorizationState = Object.freeze({
  isReady: false,
  permissionSet: new Set<string>(),
  policySet: new Set<string>(),
  snapshot: EMPTY_SNAPSHOT,
});

/**
 * Holds the host application's frontend authorization projection.
 *
 * Missing and malformed state denies every check. Matching is exact,
 * ordinal, and case-sensitive.
 */
@Injectable({ providedIn: 'root' })
export class RuleGateAuthorizationClient {
  private readonly state = signal<RuleGateAuthorizationState>(EMPTY_STATE);

  readonly isReady = computed(() => this.state().isReady);
  readonly snapshot = computed(() => this.state().snapshot);

  /**
   * Replaces the complete frontend authorization projection.
   *
   * Returns `false` and clears all grants when any identifier is malformed.
   */
  replaceSnapshot(snapshot: RuleGateAuthorizationSnapshot): boolean {
    if (!snapshot || typeof snapshot !== 'object') {
      this.clear();
      return false;
    }

    const permissions = normalizeIdentifiers(snapshot.permissions);
    const policies = normalizeIdentifiers(snapshot.policies);

    if (permissions === null || policies === null) {
      this.clear();
      return false;
    }

    const normalizedSnapshot: RuleGateAuthorizationSnapshot = Object.freeze({
      permissions,
      policies,
    });

    this.state.set(
      Object.freeze({
        isReady: true,
        permissionSet: new Set(permissions),
        policySet: new Set(policies),
        snapshot: normalizedSnapshot,
      }),
    );

    return true;
  }

  /** Clears the projection and returns the client to fail-closed state. */
  clear(): void {
    this.state.set(EMPTY_STATE);
  }

  hasPermission(permission: string): boolean {
    const currentState = this.state();

    return (
      currentState.isReady &&
      isValidIdentifier(permission) &&
      currentState.permissionSet.has(permission)
    );
  }

  hasPolicy(policy: string): boolean {
    const currentState = this.state();

    return currentState.isReady && isValidIdentifier(policy) && currentState.policySet.has(policy);
  }

  isGranted(requirement: RuleGateAuthorizationRequirement | null | undefined): boolean {
    if (!requirement || typeof requirement !== 'object') {
      return false;
    }

    const hasPermission = Object.prototype.hasOwnProperty.call(requirement, 'permission');
    const hasPolicy = Object.prototype.hasOwnProperty.call(requirement, 'policy');

    if (hasPermission === hasPolicy) {
      return false;
    }

    if (hasPermission) {
      return this.hasPermission((requirement as { readonly permission: string }).permission);
    }

    return this.hasPolicy((requirement as { readonly policy: string }).policy);
  }
}

function normalizeIdentifiers(values: readonly string[] | undefined): readonly string[] | null {
  if (values === undefined) {
    return EMPTY_IDENTIFIERS;
  }

  if (!Array.isArray(values)) {
    return null;
  }

  const identifiers = new Set<string>();

  for (const value of values) {
    if (!isValidIdentifier(value)) {
      return null;
    }

    identifiers.add(value);
  }

  return Object.freeze([...identifiers]);
}

function isValidIdentifier(identifier: unknown): identifier is string {
  return (
    typeof identifier === 'string' && identifier.length > 0 && identifier.trim() === identifier
  );
}
