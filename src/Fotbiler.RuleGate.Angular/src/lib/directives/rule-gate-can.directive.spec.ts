import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';
import { RuleGateCanDirective } from './rule-gate-can.directive';

@Component({
  imports: [RuleGateCanDirective],
  template: `
    <span class="permission" *ruleGateCan="{ permission: permission }">permission content</span>
    <span class="policy" *ruleGateCan="{ policy: policy }">policy content</span>
  `,
})
class TestHostComponent {
  readonly permission = 'documents.read';
  readonly policy = 'documents-read';
}

describe('RuleGateCanDirective', () => {
  let authorization: RuleGateAuthorizationClient;
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();

    authorization = TestBed.inject(RuleGateAuthorizationClient);
    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('renders no protected content before state is ready', () => {
    expect(query('.permission')).toBeNull();
    expect(query('.policy')).toBeNull();
  });

  it('reacts independently to permission and policy grants', async () => {
    authorization.replaceSnapshot({ permissions: ['documents.read'] });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(query('.permission')?.textContent).toContain('permission content');
    expect(query('.policy')).toBeNull();

    authorization.replaceSnapshot({ policies: ['documents-read'] });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(query('.permission')).toBeNull();
    expect(query('.policy')?.textContent).toContain('policy content');
  });

  it('removes rendered content when state is cleared', async () => {
    authorization.replaceSnapshot({
      permissions: ['documents.read'],
      policies: ['documents-read'],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    authorization.clear();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(query('.permission')).toBeNull();
    expect(query('.policy')).toBeNull();
  });

  function query(selector: string): Element | null {
    return fixture.nativeElement.querySelector(selector) as Element | null;
  }
});
