import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { from } from 'rxjs';
import { AuthService } from './auth.service';

let isRefreshing = false;

// Auth endpoints that must NOT receive a bearer token. `/api/auth/password`
// is deliberately excluded — it requires authentication. Matched against the
// URL path exactly so something like `/api/auth/login-extra` or a query
// string containing one of these values doesn't accidentally bypass auth.
const UNAUTHENTICATED_AUTH_PATHS = new Set<string>([
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh',
  '/api/auth/logout',
  '/api/auth/test-login',
]);

function isUnauthenticatedAuthRequest(url: string): boolean {
  try {
    // `new URL` handles both absolute (http://host/path) and the relative
    // form produced by HttpClient when no base is set; fall back to a
    // starts-with check if the URL is malformed.
    const pathname = new URL(url, 'http://_').pathname;
    return UNAUTHENTICATED_AUTH_PATHS.has(pathname);
  } catch {
    return false;
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (isUnauthenticatedAuthRequest(req.url)) {
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
