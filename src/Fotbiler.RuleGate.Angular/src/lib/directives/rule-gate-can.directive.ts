import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';
import { RuleGateAuthorizationRequirement } from '../models/rule-gate-authorization.models';

/**
 * Renders its template when one permission, policy, or role requirement is granted.
 *
 * The directive removes the view when state is missing, malformed, or denied.
 */
@Directive({
  selector: '[ruleGateCan]',
  standalone: true,
})
export class RuleGateCanDirective {
  private readonly authorization = inject(RuleGateAuthorizationClient);
  private readonly template = inject<TemplateRef<unknown>>(TemplateRef);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly ruleGateCan = input<RuleGateAuthorizationRequirement | null>(null);
  readonly ruleGateCanElse = input<TemplateRef<unknown> | null>(null);

  private renderedTemplate: TemplateRef<unknown> | null = null;

  constructor() {
    effect(() => {
      const template = this.authorization.isGranted(this.ruleGateCan())
        ? this.template
        : this.ruleGateCanElse();

      this.render(template);
    });
  }

  private render(template: TemplateRef<unknown> | null): void {
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
