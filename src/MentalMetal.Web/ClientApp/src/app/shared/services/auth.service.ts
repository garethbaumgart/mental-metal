import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { AuthTokenResponse, UserProfile } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly accessToken = signal<string | null>(this.readStoredToken());
  readonly currentUser = signal<UserProfile | null>(null);
  readonly isAuthenticated = computed(() => this.accessToken() !== null);

  constructor() {
    this.extractTokenFromHash();
    if (this.isAuthenticated()) {
      this.loadCurrentUser();
    }
  }

  login(): void {
    window.location.href = '/api/auth/login?returnUrl=/';
  }

  async logout(): Promise<void> {
    try {
      await this.http.post('/api/auth/logout', {}).toPromise();
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
      const result = await this.http
        .post<AuthTokenResponse>('/api/auth/refresh', {})
        .toPromise();

      if (result?.accessToken) {
        this.accessToken.set(result.accessToken);
        this.storeToken(result.accessToken);
        return true;
      }
    } catch {
      // Refresh failed
    }

    this.accessToken.set(null);
    this.currentUser.set(null);
    this.removeStoredToken();
    return false;
  }

  loadCurrentUser(): void {
    this.http.get<UserProfile>('/api/users/me').subscribe({
      next: (user) => this.currentUser.set(user),
      error: () => {
        // Token might be invalid
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
        // Clean the hash from the URL
        window.history.replaceState(null, '', window.location.pathname);
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
