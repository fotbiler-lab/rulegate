import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RuleGateCanDirective } from '@fotbiler/rulegate-angular';
import { ButtonDirective } from 'primeng/button';

import { RuleGateIdentifiers } from '../generated/rulegate';

@Component({
  imports: [ButtonDirective, RouterLink, RuleGateCanDirective],
  template: `
    <section class="page-heading">
      <div>
        <span class="eyebrow">Official reference application</span>
        <h1>Document approval</h1>
        <p>
          Provider-independent authorization with local YAML policies and trusted application data.
        </p>
      </div>
      <a *ruleGateCan="{ permission: permissions.docRead }" pButton routerLink="/documents">
        <i class="pi pi-arrow-right"></i><span>Open workspace</span>
      </a>
    </section>

    <div class="metric-grid">
      <article class="metric-card accent-blue">
        <span class="metric-icon"><i class="pi pi-id-card"></i></span>
        <div><strong>Keycloak</strong><span>Authentication and effective roles</span></div>
      </article>
      <article class="metric-card accent-violet">
        <span class="metric-icon"><i class="pi pi-shield"></i></span>
        <div><strong>RuleGate</strong><span>Local, fail-closed authorization</span></div>
      </article>
      <article class="metric-card accent-green">
        <span class="metric-icon"><i class="pi pi-database"></i></span>
        <div><strong>SQLite</strong><span>Trusted profile and resource attributes</span></div>
      </article>
    </div>

    <section class="surface scenario-panel">
      <div>
        <span class="eyebrow">Separation of concerns</span>
        <h2>One identity, two authorization experiences</h2>
        <p>
          Angular guards and directives keep the interface clear. ASP.NET Core evaluates the same
          permission identifiers with ownership, organization, and workflow state before data
          changes.
        </p>
      </div>
      <div class="decision-flow">
        <span>Token</span><i class="pi pi-angle-right"></i><span>Trusted attributes</span>
        <i class="pi pi-angle-right"></i><span>YAML policy</span> <i class="pi pi-angle-right"></i
        ><strong>Allow or deny</strong>
      </div>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  readonly permissions = RuleGateIdentifiers.permissions;
}
