import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, firstValueFrom, Observable } from 'rxjs';
import {
  AuthTokenResponse,
  LoginWithPasswordRequest,
  PasswordAuthResponse,
  RegisterWithPasswordRequest,
  SetPasswordRequest,
  UserProfile,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly accessToken = signal<string | null>(this.readStoredToken());
  readonly currentUser = signal<UserProfile | null>(null);
  readonly isAuthenticated = computed(() => this.accessToken() !== null);

  /** Private subject that drives refresh coordination. Only AuthService
   *  should emit on this — the interceptor subscribes via the readonly
   *  `refreshResult$` observable. */
  private readonly _refreshResult = new BehaviorSubject<string | null>(null);

  /** Read-only projection the interceptor subscribes to. Emits the new token
   *  (or empty string on failure) after a token refresh completes so that
   *  concurrent 401 retries wait for a single refresh. */
  readonly refreshResult$: Observable<string | null> =
    this._refreshResult.asObservable();

  /** Reset the refresh result before starting a new refresh cycle. */
  beginRefresh(): void {
    this._refreshResult.next(null);
  }

  constructor() {
    this.extractTokenFromHash();
    if (this.isAuthenticated()) {
      // Defer to break the circular DI between AuthService and authInterceptor:
      // the interceptor calls inject(AuthService) for every outgoing request,
      // and firing the /api/users/me call synchronously from the constructor
      // lands inside that interceptor while AuthService is still being
      // constructed (NG0200 cyclic resolution -> subscribe errors synchronously
      // with no network request, which clears the just-restored token).
      // Queueing a microtask lets the constructor return first.
      queueMicrotask(() => this.loadCurrentUser());
    }
  }

  login(): void {
    window.location.href = '/api/auth/login?returnUrl=/';
  }

  async loginWithPassword(email: string, password: string): Promise<void> {
    const body: LoginWithPasswordRequest = { email, password };
    const response = await firstValueFrom(
      this.http.post<PasswordAuthResponse>('/api/auth/login', body),
    );
    this.handlePasswordAuthSuccess(response);
  }

  async registerWithPassword(
    email: string,
    password: string,
    name: string,
  ): Promise<void> {
    const body: RegisterWithPasswordRequest = { email, password, name };
    const response = await firstValueFrom(
      this.http.post<PasswordAuthResponse>('/api/auth/register', body),
    );
    this.handlePasswordAuthSuccess(response);
  }

  async setPassword(newPassword: string): Promise<void> {
    const body: SetPasswordRequest = { newPassword };
    await firstValueFrom(this.http.post('/api/auth/password', body));
    const current = this.currentUser();
    if (current) {
      this.currentUser.set({ ...current, hasPassword: true });
    }
  }

  private handlePasswordAuthSuccess(response: PasswordAuthResponse): void {
    this.accessToken.set(response.accessToken);
    this.storeToken(response.accessToken);
    this.currentUser.set(response.user);
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/auth/logout', {}));
    } catch {
      // Best effort — clear local state regardless
    }
    this.accessToken.set(null);
    this.currentUser.set(null);
    this.removeStoredToken();
    this.router.navigate(['/login']);
  }

  async refreshToken(): Promise<boolean> {
    try {
      const result = await firstValueFrom(
        this.http.post<AuthTokenResponse>('/api/auth/refresh', {}),
      );

      if (result?.accessToken) {
        this.accessToken.set(result.accessToken);
        this.storeToken(result.accessToken);
        this._refreshResult.next(result.accessToken);
        return true;
      }
    } catch {
      // Refresh failed
    }

    this.accessToken.set(null);
    this.currentUser.set(null);
    this.removeStoredToken();
    this._refreshResult.next('');
    return false;
  }

  loadCurrentUser(): void {
    this.http.get<UserProfile>('/api/users/me').subscribe({
      next: (user) => this.currentUser.set(user),
      error: () => {
        this.accessToken.set(null);
        this.currentUser.set(null);
        this.removeStoredToken();
      },
    });
  }

  private extractTokenFromHash(): void {
    if (typeof window === 'undefined') return;

    const hash = window.location.hash;
    if (hash.includes('access_token=')) {
      const token = hash.split('access_token=')[1]?.split('&')[0];
      if (token) {
        this.accessToken.set(token);
        this.storeToken(token);
        // Clean the hash from the URL, preserving query params
        window.history.replaceState(
          null,
          '',
          window.location.pathname + window.location.search,
        );
      }
    }
  }

  private readStoredToken(): string | null {
    try {
      return localStorage.getItem('access_token');
    } catch {
      return null;
    }
  }

  private storeToken(token: string): void {
    try {
      localStorage.setItem('access_token', token);
    } catch {
      // localStorage may be unavailable
    }
  }

  private removeStoredToken(): void {
    try {
      localStorage.removeItem('access_token');
    } catch {
      // localStorage may be unavailable
    }
  }
}
