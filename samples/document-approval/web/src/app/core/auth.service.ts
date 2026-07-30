import { computed, inject, Injectable, signal } from '@angular/core';
import { RuleGateKeycloakAdapter } from '@fotbiler/rulegate-angular/keycloak';
import Keycloak from 'keycloak-js';

import { AppSettings } from './app-settings';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly settings = inject(AppSettings);
  private readonly adapter = inject(RuleGateKeycloakAdapter);
  private keycloak: Keycloak | null = null;
  private readonly usernameState = signal('');

  readonly username = computed(() => this.usernameState());

  async initialize(): Promise<void> {
    const config = this.settings.value;
    this.keycloak = new Keycloak({
      url: config.keycloakUrl,
      realm: config.keycloakRealm,
      clientId: config.keycloakClientId,
    });

    const authenticated = await this.keycloak.init({
      onLoad: 'login-required',
      pkceMethod: 'S256',
      checkLoginIframe: false,
    });

    if (!authenticated || !this.synchronize()) {
      this.adapter.clear();
      throw new Error('The authenticated Keycloak session could not be synchronized.');
    }
  }

  async validToken(): Promise<string | null> {
    if (this.keycloak === null || !this.keycloak.authenticated) {
      this.adapter.clear();
      return null;
    }

    try {
      if (await this.keycloak.updateToken(30)) {
        this.synchronize();
      }
      return this.keycloak.token ?? null;
    } catch {
      this.adapter.clear();
      return null;
    }
  }

  logout(): Promise<void> {
    this.adapter.clear();
    return this.keycloak?.logout({ redirectUri: window.location.origin }) ?? Promise.resolve();
  }

  private synchronize(): boolean {
    if (this.keycloak === null) {
      return false;
    }

    const synchronized = this.adapter.synchronize(this.keycloak, {
      clientIds: [this.settings.value.ruleGateClientId],
    });
    const parsed = this.keycloak.tokenParsed;
    this.usernameState.set(
      typeof parsed?.['preferred_username'] === 'string' ? parsed['preferred_username'] : '',
    );
    return synchronized;
  }
}
