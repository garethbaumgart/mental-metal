import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { from } from 'rxjs';
import { AuthService } from './auth.service';

let isRefreshing = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Don't attach token to unauthenticated auth endpoints (login, register,
  // refresh, logout). /api/auth/password requires auth and must get the token.
  const unauthenticatedAuthEndpoints = [
    '/api/auth/login',
    '/api/auth/register',
    '/api/auth/refresh',
    '/api/auth/logout',
    '/api/auth/test-login',
  ];
  if (unauthenticatedAuthEndpoints.some((p) => req.url.includes(p))) {
    return next(req);
  }

  const token = authService.accessToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isRefreshing) {
        isRefreshing = true;

        return from(authService.refreshToken()).pipe(
          switchMap((success) => {
            isRefreshing = false;
            if (success) {
              const retryReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${authService.accessToken()}`,
                },
              });
              return next(retryReq);
            }
            return throwError(() => error);
          }),
          catchError((refreshError) => {
            isRefreshing = false;
            return throwError(() => refreshError);
          }),
        );
      }

      return throwError(() => error);
    }),
  );
};
