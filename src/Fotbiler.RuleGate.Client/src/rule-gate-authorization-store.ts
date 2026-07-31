import {
  isRuleGateAuthorizationRequirement,
  isRuleGateIdentifier,
  RuleGateAuthorizationRequirement,
  RuleGateAuthorizationSnapshot,
} from './models.js';

const EMPTY_IDENTIFIERS: readonly string[] = Object.freeze([]);

const EMPTY_SNAPSHOT: RuleGateAuthorizationSnapshot = Object.freeze({
  permissions: EMPTY_IDENTIFIERS,
  policies: EMPTY_IDENTIFIERS,
  roles: EMPTY_IDENTIFIERS,
});

interface RuleGateAuthorizationState {
  readonly isReady: boolean;
  readonly permissionSet: ReadonlySet<string>;
  readonly policySet: ReadonlySet<string>;
  readonly roleSet: ReadonlySet<string>;
  readonly snapshot: RuleGateAuthorizationSnapshot;
}

const EMPTY_STATE: RuleGateAuthorizationState = Object.freeze({
  isReady: false,
  permissionSet: new Set<string>(),
  policySet: new Set<string>(),
  roleSet: new Set<string>(),
  snapshot: EMPTY_SNAPSHOT,
});

/** Framework-independent, fail-closed RuleGate frontend authorization state. */
export class RuleGateAuthorizationStore {
  private state: RuleGateAuthorizationState = EMPTY_STATE;

  get isReady(): boolean {
    return this.state.isReady;
  }

  get snapshot(): RuleGateAuthorizationSnapshot {
    return this.state.snapshot;
  }

  replaceSnapshot(snapshot: RuleGateAuthorizationSnapshot): boolean {
    if (!snapshot || typeof snapshot !== 'object') {
      this.clear();
      return false;
    }

    const permissions = normalizeIdentifiers(snapshot.permissions);
    const policies = normalizeIdentifiers(snapshot.policies);
    const roles = normalizeIdentifiers(snapshot.roles);

    if (permissions === null || policies === null || roles === null) {
      this.clear();
      return false;
    }

    const normalizedSnapshot: RuleGateAuthorizationSnapshot = Object.freeze({
      permissions,
      policies,
      roles,
    });

    this.state = Object.freeze({
      isReady: true,
      permissionSet: new Set(permissions),
      policySet: new Set(policies),
      roleSet: new Set(roles),
      snapshot: normalizedSnapshot,
    });

    return true;
  }

  clear(): void {
    this.state = EMPTY_STATE;
  }

  hasPermission(permission: string): boolean {
    return (
      this.state.isReady &&
      isRuleGateIdentifier(permission) &&
      this.state.permissionSet.has(permission)
    );
  }

  hasPolicy(policy: string): boolean {
    return this.state.isReady && isRuleGateIdentifier(policy) && this.state.policySet.has(policy);
  }

  hasRole(role: string): boolean {
    return this.state.isReady && isRuleGateIdentifier(role) && this.state.roleSet.has(role);
  }

  isGranted(requirement: RuleGateAuthorizationRequirement | null | undefined): boolean {
    if (!isRuleGateAuthorizationRequirement(requirement)) {
      return false;
    }

    if (requirement.permission !== undefined) {
      return this.hasPermission(requirement.permission);
    }

    if (requirement.policy !== undefined) {
      return this.hasPolicy(requirement.policy);
    }

    return this.hasRole(requirement.role);
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
    if (!isRuleGateIdentifier(value)) {
      return null;
    }

    identifiers.add(value);
  }

  return Object.freeze(Array.from(identifiers));
}
