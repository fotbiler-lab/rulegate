import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RuleGateCanDirective } from '@fotbiler/rulegate-angular';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';

import { RuleGateIdentifiers } from '../generated/rulegate';

@Component({
  imports: [ButtonDirective, Card, RouterLink, RuleGateCanDirective],
  template: `
    <section
      class="flex flex-column gap-3 md:flex-row md:align-items-center md:justify-content-between"
    >
      <div>
        <span class="text-primary text-sm font-semibold uppercase"
          >Official reference application</span
        >
        <h1 class="mt-2 mb-2 text-4xl md:text-5xl">Document approval</h1>
        <p class="m-0 text-color-secondary line-height-3">
          Provider-independent authorization with local YAML policies and trusted application data.
        </p>
      </div>
      <a *ruleGateCan="{ permission: permissions.docRead }" pButton routerLink="/documents">
        <i class="pi pi-arrow-right"></i><span>Open workspace</span>
      </a>
    </section>

    <div class="grid mt-4">
      <div class="col-12 md:col-4">
        <p-card styleClass="h-full">
          <div class="flex align-items-center gap-3">
            <span
              class="inline-flex align-items-center justify-content-center w-3rem h-3rem border-round-xl bg-blue-500 text-white"
              ><i class="pi pi-id-card text-xl"></i
            ></span>
            <div class="flex flex-column gap-1">
              <strong>Keycloak</strong
              ><span class="text-color-secondary text-sm">Authentication and effective roles</span>
            </div>
          </div>
        </p-card>
      </div>
      <div class="col-12 md:col-4">
        <p-card styleClass="h-full">
          <div class="flex align-items-center gap-3">
            <span
              class="inline-flex align-items-center justify-content-center w-3rem h-3rem border-round-xl bg-purple-500 text-white"
              ><i class="pi pi-shield text-xl"></i
            ></span>
            <div class="flex flex-column gap-1">
              <strong>RuleGate</strong
              ><span class="text-color-secondary text-sm">Local, fail-closed authorization</span>
            </div>
          </div>
        </p-card>
      </div>
      <div class="col-12 md:col-4">
        <p-card styleClass="h-full">
          <div class="flex align-items-center gap-3">
            <span
              class="inline-flex align-items-center justify-content-center w-3rem h-3rem border-round-xl bg-green-500 text-white"
              ><i class="pi pi-database text-xl"></i
            ></span>
            <div class="flex flex-column gap-1">
              <strong>SQLite</strong
              ><span class="text-color-secondary text-sm"
                >Trusted profile and resource attributes</span
              >
            </div>
          </div>
        </p-card>
      </div>
    </div>

    <p-card styleClass="mt-3">
      <div class="grid align-items-center">
        <div class="col-12 lg:col-5">
          <span class="text-primary text-sm font-semibold uppercase">Separation of concerns</span>
          <h2 class="mt-2 mb-2">One identity, two authorization experiences</h2>
          <p class="m-0 text-color-secondary line-height-3">
            Angular guards and directives keep the interface clear. ASP.NET Core evaluates the same
            permission identifiers with ownership, organization, and workflow state before data
            changes.
          </p>
        </div>
        <div class="col-12 lg:col-7">
          <div
            class="flex flex-wrap align-items-center justify-content-center gap-2 p-4 border-round-xl surface-ground text-color-secondary"
          >
            <span>Token</span><i class="pi pi-angle-right"></i><span>Trusted attributes</span>
            <i class="pi pi-angle-right"></i><span>YAML policy</span>
            <i class="pi pi-angle-right"></i><strong class="text-primary">Allow or deny</strong>
          </div>
        </div>
      </div>
    </p-card>

    <div class="grid mt-3">
      <div class="col-12 sm:col-6 xl:col-3">
        <p-card styleClass="h-full">
          <span class="text-primary text-sm font-semibold">PBAC</span>
          <h3 class="mt-2 mb-2">Permissions</h3>
          <p class="m-0 text-color-secondary line-height-3">
            Explicit document and workflow permissions define the operation boundary.
          </p>
        </p-card>
      </div>
      <div class="col-12 sm:col-6 xl:col-3">
        <p-card styleClass="h-full">
          <span class="text-primary text-sm font-semibold">RBAC</span>
          <h3 class="mt-2 mb-2">Effective roles</h3>
          <p class="m-0 text-color-secondary line-height-3">
            Approval requires the effective Keycloak approver realm role.
          </p>
        </p-card>
      </div>
      <div class="col-12 sm:col-6 xl:col-3">
        <p-card styleClass="h-full">
          <span class="text-primary text-sm font-semibold">ABAC</span>
          <h3 class="mt-2 mb-2">Trusted attributes</h3>
          <p class="m-0 text-color-secondary line-height-3">
            Organization, owner, state, clearance, and classification constrain each resource.
          </p>
        </p-card>
      </div>
      <div class="col-12 sm:col-6 xl:col-3">
        <p-card styleClass="h-full">
          <span class="text-primary text-sm font-semibold">CBAC</span>
          <h3 class="mt-2 mb-2">Request context</h3>
          <p class="m-0 text-color-secondary line-height-3">
            Trusted request channel and database-backed organization hours constrain decisions.
          </p>
        </p-card>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  readonly permissions = RuleGateIdentifiers.permissions;
}
