import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { RuleGateCanDirective } from '@fotbiler/rulegate-angular';
import { Ripple } from 'primeng/ripple';

import { AuthService } from '../core/auth.service';
import { RuleGateIdentifiers } from '../generated/rulegate';
import { LayoutService } from './layout.service';

@Component({
  selector: 'app-layout',
  imports: [NgClass, Ripple, RouterLink, RouterLinkActive, RuleGateCanDirective],
  template: `
    <div class="layout-wrapper layout-static" [ngClass]="layout.containerClass()">
      <header class="layout-topbar">
        <div class="layout-topbar-logo-container">
          <button
            type="button"
            class="layout-menu-button layout-topbar-action"
            aria-label="Toggle menu"
            (click)="layout.toggleMenu()"
          >
            <i class="pi pi-bars"></i>
          </button>
          <a class="layout-topbar-logo" routerLink="/" aria-label="RuleGate home">
            <img src="/rulegate-logo.svg" alt="RuleGate" />
          </a>
        </div>

        <div class="layout-topbar-actions">
          <span class="layout-user-name">{{ auth.username() }}</span>
          <button
            type="button"
            class="layout-topbar-action"
            aria-label="Toggle dark mode"
            (click)="layout.toggleDarkMode()"
          >
            <i
              class="pi"
              [class.pi-moon]="!layout.darkMode()"
              [class.pi-sun]="layout.darkMode()"
            ></i>
          </button>
          <button type="button" class="layout-topbar-action logout-action" (click)="logout()">
            <i class="pi pi-sign-out"></i><span>Sign out</span>
          </button>
        </div>
      </header>

      <aside class="layout-sidebar">
        <nav aria-label="Primary navigation">
          <ul class="layout-menu">
            <li class="layout-root-menuitem">
              <div class="layout-menuitem-root-text">Workspace</div>
              <ul>
                <li>
                  <a
                    pRipple
                    routerLink="/"
                    routerLinkActive="active-route"
                    [routerLinkActiveOptions]="{ exact: true }"
                    (click)="layout.closeMobileMenu()"
                  >
                    <i class="pi pi-fw pi-home layout-menuitem-icon"></i>
                    <span class="layout-menuitem-text">Overview</span>
                  </a>
                </li>
                <li *ruleGateCan="{ permission: permissions.docRead }">
                  <a
                    pRipple
                    routerLink="/documents"
                    routerLinkActive="active-route"
                    (click)="layout.closeMobileMenu()"
                  >
                    <i class="pi pi-fw pi-file layout-menuitem-icon"></i>
                    <span class="layout-menuitem-text">Documents</span>
                  </a>
                </li>
                <li *ruleGateCan="{ permission: permissions.wflApprove }">
                  <a
                    pRipple
                    routerLink="/approvals"
                    routerLinkActive="active-route"
                    (click)="layout.closeMobileMenu()"
                  >
                    <i class="pi pi-fw pi-check-square layout-menuitem-icon"></i>
                    <span class="layout-menuitem-text">Approvals</span>
                  </a>
                </li>
              </ul>
            </li>
          </ul>
        </nav>

        <div class="security-note">
          <i class="pi pi-lock"></i>
          <div>
            <strong>Local decisions</strong>
            <span>Keycloak authenticates. RuleGate authorizes.</span>
          </div>
        </div>
      </aside>

      <div class="layout-main-container">
        <main class="layout-main"><ng-content /></main>
        <footer class="layout-footer">RuleGate document approval reference application</footer>
      </div>

      @if (layout.state().mobileMenuActive) {
        <button
          type="button"
          class="layout-mask"
          aria-label="Close menu"
          (click)="layout.closeMobileMenu()"
        ></button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppLayoutComponent {
  readonly auth = inject(AuthService);
  readonly layout = inject(LayoutService);
  readonly permissions = RuleGateIdentifiers.permissions;

  logout(): void {
    void this.auth.logout();
  }
}
