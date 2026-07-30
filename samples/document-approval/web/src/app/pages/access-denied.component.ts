import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';

@Component({
  imports: [ButtonDirective, Card, RouterLink],
  template: `
    <div class="flex justify-content-center py-6">
      <p-card styleClass="w-full text-center" [style]="{ 'max-width': '42rem' }">
        <span
          class="inline-flex align-items-center justify-content-center w-4rem h-4rem mb-3 border-circle bg-primary text-primary-contrast"
          ><i class="pi pi-lock text-2xl"></i
        ></span>
        <span class="block text-primary text-sm font-semibold uppercase">Authorization denied</span>
        <h1 class="mt-2 mb-2 text-4xl">This workspace is not available</h1>
        <p class="m-0 text-color-secondary line-height-3">
          Your current permission projection does not grant this route. The API remains the security
          boundary.
        </p>
        <a pButton routerLink="/" class="mt-4">
          <i class="pi pi-home"></i><span>Return home</span>
        </a>
      </p-card>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessDeniedComponent {}
