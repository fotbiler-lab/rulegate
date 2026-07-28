import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  RedirectCommand,
  Router,
  RouterStateSnapshot,
} from '@angular/router';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';
import { ruleGatePermissionGuard } from './rule-gate-permission.guard';
import { ruleGatePolicyGuard } from './rule-gate-policy.guard';
import { ruleGateRoleGuard } from './rule-gate-role.guard';
import {
  provideRuleGateDeniedNavigation,
  ruleGateGuard,
  ruleGateRouteData,
} from '../routing/rule-gate-route-authorization';

describe('RuleGate route guards', () => {
  let client: RuleGateAuthorizationClient;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    client = TestBed.inject(RuleGateAuthorizationClient);
  });

  it('denies permission navigation before state is ready', () => {
    expect(runGuard(ruleGatePermissionGuard('documents.read'))).toBe(false);
  });

  it('allows only an exact granted permission', () => {
    client.replaceSnapshot({ permissions: ['documents.read'] });

    expect(runGuard(ruleGatePermissionGuard('documents.read'))).toBe(true);
    expect(runGuard(ruleGatePermissionGuard('Documents.Read'))).toBe(false);
  });

  it('denies policy navigation before state is ready', () => {
    expect(runGuard(ruleGatePolicyGuard('documents-read'))).toBe(false);
  });

  it('allows only an exact granted policy', () => {
    client.replaceSnapshot({ policies: ['documents-read'] });

    expect(runGuard(ruleGatePolicyGuard('documents-read'))).toBe(true);
    expect(runGuard(ruleGatePolicyGuard('documents-write'))).toBe(false);
  });

  it('allows only an exact granted role', () => {
    client.replaceSnapshot({ roles: ['documents.reader'] });

    expect(runGuard(ruleGateRoleGuard('documents.reader'))).toBe(true);
    expect(runGuard(ruleGateRoleGuard('Documents.Reader'))).toBe(false);
  });

  it('reads a requirement from declarative route metadata', () => {
    client.replaceSnapshot({ permissions: ['documents.read'] });

    expect(
      runGuard(ruleGateGuard, {
        data: ruleGateRouteData({ permission: 'documents.read' }),
      }),
    ).toBe(true);
  });

  it('denies missing and malformed route metadata without invoking a handler', () => {
    const deniedHandler = vi.fn(() => true);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRuleGateDeniedNavigation(deniedHandler)],
    });

    expect(runGuard(ruleGateGuard)).toBe(false);
    expect(
      runGuard(ruleGateGuard, {
        data: { ruleGate: { permission: ' documents.read' } },
      }),
    ).toBe(false);
    expect(
      runGuard(ruleGateGuard, {
        data: { ruleGate: { permission: 'documents.read', unexpected: true } },
      }),
    ).toBe(false);
    expect(deniedHandler).not.toHaveBeenCalled();
  });

  it('delegates valid denials to a framework-aware navigation handler', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideRuleGateDeniedNavigation(({ state }) => {
          const redirect = TestBed.inject(Router).parseUrl('/forbidden');

          return new RedirectCommand(redirect, {
            state: { returnUrl: state.url },
          });
        }),
      ],
    });

    const result = runGuard(
      ruleGateGuard,
      {
        data: ruleGateRouteData({ policy: 'documents-read' }),
      },
      { url: '/documents' },
    );

    expect(result).toBeInstanceOf(RedirectCommand);
    expect((result as RedirectCommand).redirectTo.toString()).toBe('/forbidden');
  });
});

function runGuard(
  guard: ReturnType<typeof ruleGatePermissionGuard>,
  route: Partial<ActivatedRouteSnapshot> = {},
  state: Partial<RouterStateSnapshot> = {},
): unknown {
  return TestBed.runInInjectionContext(() =>
    guard(
      {
        data: {},
        ...route,
      } as ActivatedRouteSnapshot,
      {
        url: '/',
        ...state,
      } as RouterStateSnapshot,
    ),
  );
}
