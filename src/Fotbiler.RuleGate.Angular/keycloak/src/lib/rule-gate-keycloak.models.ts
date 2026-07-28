import { RuleGateAuthorizationSnapshot } from '@fotbiler/rulegate-angular';

/**
 * The small, structural subset of a keycloak-js session used by RuleGate.
 *
 * Applications can pass their keycloak-js instance directly. RuleGate does
 * not initialize Keycloak, store tokens, or own authentication callbacks.
 */
export interface RuleGateKeycloakSession {
  readonly authenticated?: boolean;
  readonly realmAccess?: unknown;
  readonly resourceAccess?: unknown;
  readonly tokenParsed?: unknown;
}

export interface RuleGateKeycloakSnapshotOptions {
  /** Includes effective realm roles. Defaults to `true`. */
  readonly includeRealmRoles?: boolean;

  /** Client IDs whose effective client roles are included. No client is selected by default. */
  readonly clientIds?: readonly string[];

  /** Top-level token claim containing one permission or an array of permissions. Defaults to `permission`. */
  readonly permissionClaim?: string | null;
}

export type RuleGateKeycloakSnapshot = RuleGateAuthorizationSnapshot;
