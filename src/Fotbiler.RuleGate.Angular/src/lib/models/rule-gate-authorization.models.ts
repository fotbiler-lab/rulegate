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
