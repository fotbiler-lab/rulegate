import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';

/** Creates a fail-closed route guard for one exact permission identifier. */
export function ruleGatePermissionGuard(permission: string): CanActivateFn {
  return () => inject(RuleGateAuthorizationClient).hasPermission(permission);
}
