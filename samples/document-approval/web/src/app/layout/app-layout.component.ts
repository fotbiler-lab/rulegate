import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { RuleGateCanDirective } from '@fotbiler/rulegate-angular';
import { ButtonDirective } from 'primeng/button';

import { AuthService } from '../core/auth.service';
import { RuleGateIdentifiers } from '../generated/rulegate';

@Component({
  selector: 'app-layout',
  imports: [ButtonDirective, RouterLink, RouterLinkActive, RuleGateCanDirective],
  template: `
    <div class="app-shell" [class.sidebar-open]="sidebarOpen()">
      <header class="topbar">
        <button
          pButton
          class="icon-button menu-button"
          aria-label="Toggle menu"
          (click)="toggleMenu()"
        >
          <i class="pi pi-bars"></i>
        </button>
        <a class="brand" routerLink="/" aria-label="RuleGate home">
          <span class="brand-mark"><i class="pi pi-shield"></i></span>
          <span>RuleGate</span>
        </a>
        <span class="topbar-spacer"></span>
        <span class="user-name">{{ auth.username() }}</span>
        <button
          pButton
          class="icon-button"
          aria-label="Toggle dark mode"
          (click)="toggleDarkMode()"
        >
          <i class="pi" [class.pi-moon]="!darkMode()" [class.pi-sun]="darkMode()"></i>
        </button>
        <button pButton class="logout-button" (click)="logout()">
          <i class="pi pi-sign-out"></i><span>Sign out</span>
        </button>
      </header>

      <aside class="sidebar">
        <div class="menu-label">Workspace</div>
        <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
          <i class="pi pi-home"></i><span>Overview</span>
        </a>
        <a
          *ruleGateCan="{ permission: permissions.docRead }"
          routerLink="/documents"
          routerLinkActive="active"
        >
          <i class="pi pi-file"></i><span>Documents</span>
        </a>
        <a
          *ruleGateCan="{ permission: permissions.wflApprove }"
          routerLink="/approvals"
          routerLinkActive="active"
        >
          <i class="pi pi-check-square"></i><span>Approvals</span>
        </a>
        <div class="security-note">
          <i class="pi pi-lock"></i>
          <div>
            <strong>Local decisions</strong
            ><span>Keycloak authenticates. RuleGate authorizes.</span>
          </div>
        </div>
      </aside>

      <main class="content"><ng-content /></main>
      @if (sidebarOpen()) {
        <button class="mask" aria-label="Close menu" (click)="toggleMenu()"></button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppLayoutComponent {
  readonly auth = inject(AuthService);
  readonly permissions = RuleGateIdentifiers.permissions;
  readonly sidebarOpen = signal(false);
  readonly darkMode = signal(false);
  private readonly document = inject(DOCUMENT);

  toggleMenu(): void {
    this.sidebarOpen.update((value) => !value);
  }

  toggleDarkMode(): void {
    this.darkMode.update((value) => !value);
    this.document.documentElement.classList.toggle('app-dark', this.darkMode());
  }

  logout(): void {
    void this.auth.logout();
  }
}
