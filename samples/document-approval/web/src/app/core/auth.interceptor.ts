import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';

import { AppSettings } from './app-settings';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const settings = inject(AppSettings).value;

  if (!request.url.startsWith(settings.apiUrl)) {
    return next(request);
  }

  return from(inject(AuthService).validToken()).pipe(
    switchMap((token) =>
      next(
        token === null
          ? request
          : request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }),
      ),
    ),
  );
};
