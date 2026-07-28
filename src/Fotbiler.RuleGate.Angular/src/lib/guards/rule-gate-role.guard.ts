import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';

/** Creates a fail-closed route guard for one exact role identifier. */
export function ruleGateRoleGuard(role: string): CanActivateFn {
  return () => inject(RuleGateAuthorizationClient).hasRole(role);
}
