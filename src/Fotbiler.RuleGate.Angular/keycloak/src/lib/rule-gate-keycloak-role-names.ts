import { isRuleGateIdentifier } from '@fotbiler/rulegate-angular';

const REALM_ROLE_PREFIX = 'keycloak:realm:';
const CLIENT_ROLE_PREFIX = 'keycloak:client:';

/** Encodes one role-name component with UTF-8 RFC 3986 percent encoding. */
export function encodeRuleGateKeycloakComponent(value: string): string {
  if (!isRuleGateIdentifier(value)) {
    throw new TypeError(
      'A Keycloak role-name component must be non-empty and have no surrounding whitespace.',
    );
  }

  let encoded = '';

  for (const byte of new TextEncoder().encode(value)) {
    if (isUnreserved(byte)) {
      encoded += String.fromCharCode(byte);
    } else {
      encoded += `%${byte.toString(16).toUpperCase().padStart(2, '0')}`;
    }
  }

  return encoded;
}

/** Creates the canonical RuleGate identifier for one effective Keycloak realm role. */
export function ruleGateKeycloakRealmRole(role: string): string {
  return `${REALM_ROLE_PREFIX}${encodeRuleGateKeycloakComponent(role)}`;
}

/** Creates the canonical RuleGate identifier for one effective Keycloak client role. */
export function ruleGateKeycloakClientRole(clientId: string, role: string): string {
  return `${CLIENT_ROLE_PREFIX}${encodeRuleGateKeycloakComponent(clientId)}:${encodeRuleGateKeycloakComponent(role)}`;
}

function isUnreserved(byte: number): boolean {
  return (
    (byte >= 0x41 && byte <= 0x5a) ||
    (byte >= 0x61 && byte <= 0x7a) ||
    (byte >= 0x30 && byte <= 0x39) ||
    byte === 0x2d ||
    byte === 0x2e ||
    byte === 0x5f ||
    byte === 0x7e
  );
}
