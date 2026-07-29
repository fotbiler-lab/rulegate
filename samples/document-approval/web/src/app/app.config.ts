import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { RULE_GATE_DENIED_NAVIGATION_HANDLER } from '@fotbiler/rulegate-angular';
import Aura from '@primeuix/themes/aura';
import { providePrimeNG } from 'primeng/config';

import { routes } from './app.routes';
import { AuthService } from './core/auth.service';
import { authInterceptor } from './core/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter(routes),
    providePrimeNG({
      ripple: true,
      theme: {
        preset: Aura,
        options: { darkModeSelector: '.app-dark', cssLayer: false },
      },
    }),
    provideAppInitializer(() => inject(AuthService).initialize()),
    {
      provide: RULE_GATE_DENIED_NAVIGATION_HANDLER,
      useFactory: () => {
        const router = inject(Router);
        return () => router.parseUrl('/access-denied');
      },
    },
  ],
};
