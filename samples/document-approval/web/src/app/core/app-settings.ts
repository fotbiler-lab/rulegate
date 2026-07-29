import { Injectable } from '@angular/core';

export interface AppConfiguration {
  readonly apiUrl: string;
  readonly keycloakUrl: string;
  readonly keycloakRealm: string;
  readonly keycloakClientId: string;
  readonly ruleGateClientId: string;
}

@Injectable({ providedIn: 'root' })
export class AppSettings {
  private configuration: AppConfiguration | null = null;

  get value(): AppConfiguration {
    if (this.configuration === null) {
      throw new Error('Application configuration has not been loaded.');
    }

    return this.configuration;
  }

  async load(): Promise<void> {
    const response = await fetch('/app-config.json', { cache: 'no-store' });

    if (!response.ok) {
      throw new Error(`Could not load app-config.json (${response.status}).`);
    }

    this.configuration = (await response.json()) as AppConfiguration;
  }
}
