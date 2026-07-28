import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';

/** Creates a fail-closed route guard for one exact policy identifier. */
export function ruleGatePolicyGuard(policy: string): CanActivateFn {
  return () => inject(RuleGateAuthorizationClient).hasPolicy(policy);
}
