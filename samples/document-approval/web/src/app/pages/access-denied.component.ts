import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonDirective } from 'primeng/button';

@Component({
  imports: [ButtonDirective, RouterLink],
  template: `
    <section class="surface state-page">
      <span class="state-icon"><i class="pi pi-lock"></i></span>
      <span class="eyebrow">Authorization denied</span>
      <h1>This workspace is not available</h1>
      <p>
        Your current permission projection does not grant this route. The API remains the security
        boundary.
      </p>
      <a pButton routerLink="/"><i class="pi pi-home"></i><span>Return home</span></a>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessDeniedComponent {}
