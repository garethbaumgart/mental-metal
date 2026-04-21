import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import {
  catchError,
  filter,
  finalize,
  first,
  switchMap,
  throwError,
} from 'rxjs';
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

/**
 * Retry the original request with the current access token.
 * Extracted to avoid duplication between the "first 401" and
 * "queued while refresh in-flight" code paths.
 */
function retryWithToken(
  req: Parameters<HttpInterceptorFn>[0],
  next: Parameters<HttpInterceptorFn>[1],
  authService: AuthService,
  originalError: HttpErrorResponse,
) {
  const freshToken = authService.accessToken();
  if (freshToken) {
    const retryReq = req.clone({
      setHeaders: { Authorization: `Bearer ${freshToken}` },
    });
    return next(retryReq);
  }
  return throwError(() => originalError);
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
      if (error.status !== 401) {
        return throwError(() => error);
      }

      // A refresh is already in flight — wait for it to complete, then retry.
      if (isRefreshing) {
        return authService.refreshResult$.pipe(
          // refreshResult$ is a BehaviorSubject seeded with null.
          // Skip the current (null | stale) value and wait for the
          // next emission, which signals the in-flight refresh completed.
          filter((result) => result !== null),
          first(),
          switchMap(() => retryWithToken(req, next, authService, error)),
        );
      }

      // First 401 — initiate the refresh.
      isRefreshing = true;
      authService.beginRefresh();

      return from(authService.refreshToken()).pipe(
        switchMap((success) => {
          if (success) {
            return retryWithToken(req, next, authService, error);
          }
          return throwError(() => error);
        }),
        catchError((refreshError) => throwError(() => refreshError)),
        finalize(() => {
          isRefreshing = false;
        }),
      );
    }),
  );
};
