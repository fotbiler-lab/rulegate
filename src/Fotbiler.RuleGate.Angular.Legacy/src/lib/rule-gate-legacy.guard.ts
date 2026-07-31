import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, RouterStateSnapshot } from '@angular/router';
import {
  isRuleGateAuthorizationRequirement,
  RuleGateAuthorizationRequirement,
} from '@fotbiler/rulegate-client';

import { RuleGateLegacyAuthorizationClient } from './rule-gate-legacy-authorization-client';

export const RULE_GATE_LEGACY_ROUTE_DATA_KEY = 'ruleGate';

@Injectable({ providedIn: 'root' })
export class RuleGateLegacyGuard implements CanActivate {
  constructor(private readonly authorization: RuleGateLegacyAuthorizationClient) {}

  canActivate(route: ActivatedRouteSnapshot, _state: RouterStateSnapshot): boolean {
    const requirement = route.data[RULE_GATE_LEGACY_ROUTE_DATA_KEY];

    return (
      isRuleGateAuthorizationRequirement(requirement) && this.authorization.isGranted(requirement)
    );
  }
}

export function ruleGateLegacyRouteData(requirement: RuleGateAuthorizationRequirement): {
  readonly ruleGate: RuleGateAuthorizationRequirement;
} {
  return { ruleGate: requirement };
}
