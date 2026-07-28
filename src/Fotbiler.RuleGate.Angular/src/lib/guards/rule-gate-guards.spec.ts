import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';
import { ruleGatePermissionGuard } from './rule-gate-permission.guard';
import { ruleGatePolicyGuard } from './rule-gate-policy.guard';

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
});

function runGuard(guard: ReturnType<typeof ruleGatePermissionGuard>): unknown {
  return TestBed.runInInjectionContext(() =>
    guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  );
}
