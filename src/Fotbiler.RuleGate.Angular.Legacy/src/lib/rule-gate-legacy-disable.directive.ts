import { Directive, ElementRef, Input, OnDestroy, Renderer2 } from '@angular/core';
import { RuleGateAuthorizationRequirement } from '@fotbiler/rulegate-client';
import { Subscription } from 'rxjs';

import { RuleGateLegacyAuthorizationClient } from './rule-gate-legacy-authorization-client';

@Directive({ selector: '[ruleGateLegacyDisable]' })
export class RuleGateLegacyDisableDirective implements OnDestroy {
  private requirement: RuleGateAuthorizationRequirement | null = null;
  private readonly subscription: Subscription;

  @Input()
  set ruleGateLegacyDisable(requirement: RuleGateAuthorizationRequirement | null) {
    this.requirement = requirement;
    this.applyState();
  }

  constructor(
    private readonly authorization: RuleGateLegacyAuthorizationClient,
    private readonly element: ElementRef<HTMLElement>,
    private readonly renderer: Renderer2,
  ) {
    this.subscription = authorization.snapshot$.subscribe(() => this.applyState());
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  private applyState(): void {
    const denied = !this.authorization.isGranted(this.requirement);
    const nativeElement = this.element.nativeElement;

    this.renderer.setAttribute(nativeElement, 'aria-disabled', denied ? 'true' : 'false');

    if ('disabled' in nativeElement) {
      this.renderer.setProperty(nativeElement, 'disabled', denied);
    }
  }
}
