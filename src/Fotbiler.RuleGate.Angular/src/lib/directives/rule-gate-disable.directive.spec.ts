import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';
import { RuleGateDisableDirective } from './rule-gate-disable.directive';

@Component({
  imports: [RuleGateDisableDirective],
  template: `
    <button class="button" [ruleGateDisable]="{ permission: permission }">Edit</button>
    <a class="link" href="/documents" [ruleGateDisable]="{ permission: permission }"> Documents </a>
  `,
})
class TestHostComponent {
  readonly permission = 'documents.write';
}

describe('RuleGateDisableDirective', () => {
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

  it('disables native controls before authorization state is ready', () => {
    const button = query<HTMLButtonElement>('.button');

    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-disabled')).toBe('true');
    expect(button.hasAttribute('data-rulegate-disabled')).toBe(true);
  });

  it('reacts to granted and cleared authorization state', async () => {
    authorization.replaceSnapshot({ permissions: ['documents.write'] });
    await render();

    const button = query<HTMLButtonElement>('.button');
    expect(button.disabled).toBe(false);
    expect(button.hasAttribute('aria-disabled')).toBe(false);

    authorization.clear();
    await render();

    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-disabled')).toBe('true');
  });

  it('blocks activation for non-native interactive hosts while denied', () => {
    const link = query<HTMLAnchorElement>('.link');
    const event = new MouseEvent('click', { bubbles: true, cancelable: true });

    expect(link.dispatchEvent(event)).toBe(false);
    expect(event.defaultPrevented).toBe(true);
    expect(link.getAttribute('aria-disabled')).toBe('true');
  });

  async function render(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function query<T extends Element>(selector: string): T {
    const element = fixture.nativeElement.querySelector(selector) as T | null;

    if (!element) {
      throw new Error(`Missing test element: ${selector}`);
    }

    return element;
  }
});
