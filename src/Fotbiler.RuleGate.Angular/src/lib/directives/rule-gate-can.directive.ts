import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';
import { RuleGateAuthorizationRequirement } from '../models/rule-gate-authorization.models';

/**
 * Renders its template when one permission or policy requirement is granted.
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

  constructor() {
    effect(() => {
      if (this.authorization.isGranted(this.ruleGateCan())) {
        if (this.viewContainer.length === 0) {
          this.viewContainer.createEmbeddedView(this.template);
        }

        return;
      }

      this.viewContainer.clear();
    });
  }
}
