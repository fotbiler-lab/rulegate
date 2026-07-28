import { InjectionToken, Provider, inject } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  GuardResult,
  MaybeAsync,
  RouterStateSnapshot,
} from '@angular/router';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';
import {
  isRuleGateAuthorizationRequirement,
  RuleGateAuthorizationRequirement,
} from '../models/rule-gate-authorization.models';

export const RULE_GATE_ROUTE_DATA_KEY = 'ruleGate';

export interface RuleGateRouteData {
  readonly [RULE_GATE_ROUTE_DATA_KEY]: RuleGateAuthorizationRequirement;
}

export interface RuleGateDeniedNavigationContext {
  readonly requirement: RuleGateAuthorizationRequirement;
  readonly route: ActivatedRouteSnapshot;
  readonly state: RouterStateSnapshot;
}

export type RuleGateDeniedNavigationHandler = (
  context: RuleGateDeniedNavigationContext,
) => MaybeAsync<GuardResult>;

const denyNavigation: RuleGateDeniedNavigationHandler = () => false;

export const RULE_GATE_DENIED_NAVIGATION_HANDLER =
  new InjectionToken<RuleGateDeniedNavigationHandler>('RULE_GATE_DENIED_NAVIGATION_HANDLER', {
    providedIn: 'root',
    factory: () => denyNavigation,
  });

/** Creates typed Angular route data for the shared RuleGate route guard. */
export function ruleGateRouteData(
  requirement: RuleGateAuthorizationRequirement,
): RuleGateRouteData {
  return Object.freeze({
    [RULE_GATE_ROUTE_DATA_KEY]: Object.freeze({ ...requirement }),
  });
}

/** Registers application-specific denied-navigation behavior. */
export function provideRuleGateDeniedNavigation(
  handler: RuleGateDeniedNavigationHandler,
): Provider {
  return {
    provide: RULE_GATE_DENIED_NAVIGATION_HANDLER,
    useValue: handler,
  };
}

/**
 * Authorizes a route from its declarative `ruleGate` metadata.
 *
 * Missing or malformed metadata denies directly. A valid but ungranted
 * requirement delegates to the configured denied-navigation handler.
 */
export const ruleGateGuard: CanActivateFn = (route, state) => {
  const requirement = route.data[RULE_GATE_ROUTE_DATA_KEY];

  if (!isRuleGateAuthorizationRequirement(requirement)) {
    return false;
  }

  if (inject(RuleGateAuthorizationClient).isGranted(requirement)) {
    return true;
  }

  return inject(RULE_GATE_DENIED_NAVIGATION_HANDLER)({
    requirement,
    route,
    state,
  });
};
