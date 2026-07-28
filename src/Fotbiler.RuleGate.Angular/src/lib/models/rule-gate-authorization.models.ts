/**
 * A frontend authorization projection supplied by the host application.
 *
 * The projection controls UI behavior only. It is not a replacement for a
 * backend RuleGate authorization decision.
 */
export interface RuleGateAuthorizationSnapshot {
  readonly permissions?: readonly string[];
  readonly policies?: readonly string[];
}

export interface RuleGatePermissionRequirement {
  readonly permission: string;
  readonly policy?: never;
}

export interface RuleGatePolicyRequirement {
  readonly permission?: never;
  readonly policy: string;
}

/** A single permission or policy check used by RuleGate Angular helpers. */
export type RuleGateAuthorizationRequirement =
  | RuleGatePermissionRequirement
  | RuleGatePolicyRequirement;

/** Returns whether a runtime value is one valid RuleGate UI requirement. */
export function isRuleGateAuthorizationRequirement(
  requirement: unknown,
): requirement is RuleGateAuthorizationRequirement {
  if (!requirement || typeof requirement !== 'object') {
    return false;
  }

  const keys = Object.keys(requirement);

  if (keys.length !== 1 || (keys[0] !== 'permission' && keys[0] !== 'policy')) {
    return false;
  }

  const identifier =
    keys[0] === 'permission'
      ? (requirement as { readonly permission: unknown }).permission
      : (requirement as { readonly policy: unknown }).policy;

  return isRuleGateIdentifier(identifier);
}

/** Returns whether a runtime value is one exact, non-empty RuleGate identifier. */
export function isRuleGateIdentifier(identifier: unknown): identifier is string {
  return (
    typeof identifier === 'string' && identifier.length > 0 && identifier.trim() === identifier
  );
}
