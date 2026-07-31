import { Directive, Input, OnDestroy, TemplateRef, ViewContainerRef } from '@angular/core';
import { RuleGateAuthorizationRequirement } from '@fotbiler/rulegate-client';
import { Subscription } from 'rxjs';

import { RuleGateLegacyAuthorizationClient } from './rule-gate-legacy-authorization-client';

@Directive({ selector: '[ruleGateLegacyCan]' })
export class RuleGateLegacyCanDirective implements OnDestroy {
  private requirement: RuleGateAuthorizationRequirement | null = null;
  private alternative: TemplateRef<unknown> | null = null;
  private renderedTemplate: TemplateRef<unknown> | null = null;
  private readonly subscription: Subscription;

  @Input()
  set ruleGateLegacyCan(requirement: RuleGateAuthorizationRequirement | null) {
    this.requirement = requirement;
    this.render();
  }

  @Input()
  set ruleGateLegacyCanElse(template: TemplateRef<unknown> | null) {
    this.alternative = template;
    this.render();
  }

  constructor(
    private readonly authorization: RuleGateLegacyAuthorizationClient,
    private readonly template: TemplateRef<unknown>,
    private readonly viewContainer: ViewContainerRef,
  ) {
    this.subscription = authorization.snapshot$.subscribe(() => this.render());
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  private render(): void {
    const template = this.authorization.isGranted(this.requirement)
      ? this.template
      : this.alternative;

    if (template === this.renderedTemplate && this.viewContainer.length !== 0) {
      return;
    }

    this.viewContainer.clear();
    this.renderedTemplate = template;

    if (template) {
      this.viewContainer.createEmbeddedView(template);
    }
  }
}
