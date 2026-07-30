import { Inject, Injectable, InjectionToken } from '@angular/core';

export interface AppConfiguration {
  readonly apiUrl: string;
  readonly keycloakUrl: string;
  readonly keycloakRealm: string;
  readonly keycloakClientId: string;
  readonly ruleGateClientId: string;
  readonly primeNgLicense?: string;
}

export const APP_CONFIGURATION = new InjectionToken<AppConfiguration>('APP_CONFIGURATION');

export async function loadAppConfiguration(): Promise<AppConfiguration> {
  const response = await fetch('/app-config.json', { cache: 'no-store' });

  if (!response.ok) {
    throw new Error(`Could not load app-config.json (${response.status}).`);
  }

  const base = (await response.json()) as AppConfiguration;
  const local = await loadLocalConfiguration();
  return { ...base, ...local };
}

@Injectable({ providedIn: 'root' })
export class AppSettings {
  constructor(@Inject(APP_CONFIGURATION) readonly value: AppConfiguration) {}
}

async function loadLocalConfiguration(): Promise<Partial<AppConfiguration>> {
  const response = await fetch('/app-config.local.json', { cache: 'no-store' });

  if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) {
    return {};
  }

  return (await response.json()) as Partial<AppConfiguration>;
}
