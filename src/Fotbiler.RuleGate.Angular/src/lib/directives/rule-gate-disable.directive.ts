import { computed, Directive, effect, ElementRef, inject, input, Renderer2 } from '@angular/core';

import { RuleGateAuthorizationClient } from '../client/rule-gate-authorization-client';
import { RuleGateAuthorizationRequirement } from '../models/rule-gate-authorization.models';

/**
 * Disables an interactive host while its RuleGate requirement is denied.
 *
 * This directive owns the native `disabled` property when the host provides
 * one. It also exposes `aria-disabled` and blocks click activation for other
 * interactive elements.
 */
@Directive({
  selector: '[ruleGateDisable]',
  standalone: true,
  host: {
    '[attr.aria-disabled]': "denied() ? 'true' : null",
    '[attr.data-rulegate-disabled]': "denied() ? '' : null",
    '(click)': 'blockDeniedInteraction($event)',
  },
})
export class RuleGateDisableDirective {
  private readonly authorization = inject(RuleGateAuthorizationClient);
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly renderer = inject(Renderer2);

  readonly ruleGateDisable = input<RuleGateAuthorizationRequirement | null>(null);
  readonly denied = computed(() => !this.authorization.isGranted(this.ruleGateDisable()));

  constructor() {
    effect(() => {
      const nativeElement = this.element.nativeElement;

      if ('disabled' in nativeElement) {
        this.renderer.setProperty(nativeElement, 'disabled', this.denied());
      }
    });
  }

  protected blockDeniedInteraction(event: Event): void {
    if (!this.denied()) {
      return;
    }

    event.preventDefault();
    event.stopImmediatePropagation();
  }
}
