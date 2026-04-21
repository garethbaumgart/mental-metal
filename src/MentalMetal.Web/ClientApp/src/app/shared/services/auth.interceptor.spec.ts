import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { firstValueFrom } from 'rxjs';

/**
 * Regression tests for the auth interceptor — specifically the 401 race
 * condition on first load (GitHub #168). When multiple requests receive 401s
 * concurrently, the interceptor must queue them behind a single token refresh
 * rather than dropping all but the first.
 */
describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    // Clear any module-level state from previous tests
    vi.stubGlobal('localStorage', {
      getItem: vi.fn().mockReturnValue(null),
      setItem: vi.fn(),
      removeItem: vi.fn(),
    });

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  it('attaches bearer token to authenticated requests', () => {
    authService.accessToken.set('test-token');

    http.get('/api/data').subscribe();

    const req = httpTesting.expectOne('/api/data');
    expect(req.request.headers.get('Authorization')).toBe(
      'Bearer test-token',
    );
    req.flush({});
  });

  it('does not attach token to unauthenticated auth endpoints', () => {
    authService.accessToken.set('test-token');

    http.post('/api/auth/login', {}).subscribe();

    const req = httpTesting.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('retries with new token after a 401 triggers refresh', async () => {
    authService.accessToken.set('expired-token');

    const data$ = firstValueFrom(http.get<{ ok: boolean }>('/api/data'));

    // First request goes out with expired token
    const firstReq = httpTesting.expectOne('/api/data');
    expect(firstReq.request.headers.get('Authorization')).toBe(
      'Bearer expired-token',
    );

    // Simulate refreshToken succeeding (async to mirror real behavior)
    vi.spyOn(authService, 'refreshToken').mockImplementation(async () => {
      authService.accessToken.set('fresh-token');
      authService.refreshResult$.next('fresh-token');
      return true;
    });

    // Respond with 401
    firstReq.flush(null, { status: 401, statusText: 'Unauthorized' });

    // Wait for the async refreshToken mock to resolve
    await new Promise((r) => setTimeout(r, 0));

    // The interceptor should retry with the fresh token
    const retryReq = httpTesting.expectOne('/api/data');
    expect(retryReq.request.headers.get('Authorization')).toBe(
      'Bearer fresh-token',
    );
    retryReq.flush({ ok: true });

    const result = await data$;
    expect(result).toEqual({ ok: true });
  });

  it('queues concurrent 401s behind a single refresh (regression #168)', async () => {
    authService.accessToken.set('expired-token');

    // Fire two requests concurrently
    const data1$ = firstValueFrom(http.get<{ id: 1 }>('/api/one'));
    const data2$ = firstValueFrom(http.get<{ id: 2 }>('/api/two'));

    const req1 = httpTesting.expectOne('/api/one');
    const req2 = httpTesting.expectOne('/api/two');

    // Mock refreshToken to simulate a real async refresh
    let resolveRefresh!: (value: boolean) => void;
    const refreshPromise = new Promise<boolean>((r) => (resolveRefresh = r));
    vi.spyOn(authService, 'refreshToken').mockImplementation(() => {
      authService.refreshResult$.next(null); // reset before refresh
      return refreshPromise.then((success) => {
        if (success) {
          authService.accessToken.set('fresh-token');
          authService.refreshResult$.next('fresh-token');
        } else {
          authService.refreshResult$.next('');
        }
        return success;
      });
    });

    // Both get 401
    req1.flush(null, { status: 401, statusText: 'Unauthorized' });
    req2.flush(null, { status: 401, statusText: 'Unauthorized' });

    // refreshToken should only be called once
    expect(authService.refreshToken).toHaveBeenCalledTimes(1);

    // Complete the refresh
    resolveRefresh(true);
    await refreshPromise;

    // Wait a tick for RxJS to process
    await new Promise((r) => setTimeout(r, 0));

    // Both requests should be retried with the fresh token
    const retries = httpTesting.match(
      (r) => r.url === '/api/one' || r.url === '/api/two',
    );
    expect(retries.length).toBe(2);

    const retryOne = retries.find((r) => r.request.url === '/api/one')!;
    const retryTwo = retries.find((r) => r.request.url === '/api/two')!;

    expect(retryOne.request.headers.get('Authorization')).toBe(
      'Bearer fresh-token',
    );
    expect(retryTwo.request.headers.get('Authorization')).toBe(
      'Bearer fresh-token',
    );

    retryOne.flush({ id: 1 });
    retryTwo.flush({ id: 2 });

    expect(await data1$).toEqual({ id: 1 });
    expect(await data2$).toEqual({ id: 2 });
  });
});
