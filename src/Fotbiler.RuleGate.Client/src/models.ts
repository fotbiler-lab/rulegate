/**
 * A frontend authorization projection supplied by the host application.
 *
 * The projection controls UI behavior only. It is not a replacement for a
 * backend RuleGate authorization decision.
 */
export interface RuleGateAuthorizationSnapshot {
  readonly permissions?: readonly string[];
  readonly policies?: readonly string[];
  readonly roles?: readonly string[];
}

export interface RuleGatePermissionRequirement {
  readonly permission: string;
  readonly policy?: never;
  readonly role?: never;
}

export interface RuleGatePolicyRequirement {
  readonly permission?: never;
  readonly policy: string;
  readonly role?: never;
}

export interface RuleGateRoleRequirement {
  readonly permission?: never;
  readonly policy?: never;
  readonly role: string;
}

/** A single permission, policy, or role check used by RuleGate UI helpers. */
export type RuleGateAuthorizationRequirement =
  RuleGatePermissionRequirement | RuleGatePolicyRequirement | RuleGateRoleRequirement;

/** Returns whether a runtime value is one valid RuleGate UI requirement. */
export function isRuleGateAuthorizationRequirement(
  requirement: unknown,
): requirement is RuleGateAuthorizationRequirement {
  if (!requirement || typeof requirement !== 'object') {
    return false;
  }

  const keys = Object.keys(requirement);
  const key = keys[0];

  if (keys.length !== 1 || (key !== 'permission' && key !== 'policy' && key !== 'role')) {
    return false;
  }

  const identifier = (requirement as Record<string, unknown>)[key];

  return isRuleGateIdentifier(identifier);
}

/** Returns whether a runtime value is one exact, non-empty RuleGate identifier. */
export function isRuleGateIdentifier(identifier: unknown): identifier is string {
  return (
    typeof identifier === 'string' && identifier.length > 0 && identifier.trim() === identifier
  );
}
