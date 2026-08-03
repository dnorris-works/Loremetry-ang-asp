import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';

import { AuthTokenService } from './auth-token.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authTokenService = inject(AuthTokenService);

  return from(authTokenService.buildAuthHeaders()).pipe(
    switchMap((headers) => {
      let authRequest = request;

      const bypass = headers.get('X-Loremetry-Admin-Bypass');
      if (bypass) {
        authRequest = authRequest.clone({
          setHeaders: { 'X-Loremetry-Admin-Bypass': bypass },
        });
      }

      const authorization = headers.get('Authorization');
      if (authorization) {
        authRequest = authRequest.clone({
          setHeaders: { Authorization: authorization },
        });
      }

      return next(authRequest).pipe(
        catchError((error) => {
          const message =
            typeof error?.error?.message === 'string'
              ? error.error.message
              : typeof error?.error?.reason === 'string'
                ? error.error.reason
                : '';

          if (error.status === 401 || authTokenService.isAuthFailureMessage(message)) {
            authTokenService.notifyAuthRequired();
          }

          return throwError(() => error);
        }),
      );
    }),
  );
};
