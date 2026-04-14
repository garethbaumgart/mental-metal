import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../shared/services/auth.service';
import { UserProfile } from '../../shared/models/user.model';
import { PasswordSettingsComponent } from './password-settings.component';

function makeUser(hasPassword: boolean): UserProfile {
  return {
    id: '1',
    email: 'user@example.com',
    name: 'User',
    avatarUrl: null,
    timezone: 'UTC',
    preferences: {
      theme: 'Light',
      notificationsEnabled: true,
      briefingTime: '08:00',
      livingBriefAutoApply: false,
    },
    hasAiProvider: false,
    hasPassword,
    createdAt: '2026-01-01T00:00:00Z',
    lastLoginAt: '2026-01-01T00:00:00Z',
  };
}

describe('PasswordSettingsComponent', () => {
  let httpMock: HttpTestingController;
  let authService: AuthService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PasswordSettingsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        MessageService,
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows "Set a password" when the user has no password', () => {
    authService.currentUser.set(makeUser(false));

    const fixture = TestBed.createComponent(PasswordSettingsComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement;
    expect(host.textContent).toContain('Set a password');
  });

  it('shows "Change password" when the user has a password', () => {
    authService.currentUser.set(makeUser(true));

    const fixture = TestBed.createComponent(PasswordSettingsComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement;
    expect(host.textContent).toContain('Change password');
  });

  it('submits the new password and flips hasPassword to true on success', async () => {
    authService.currentUser.set(makeUser(false));

    const fixture = TestBed.createComponent(PasswordSettingsComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance as unknown as {
      newPassword: string;
      submit: () => Promise<void>;
    };
    component.newPassword = 'brand-new-pw';

    const pending = component.submit();

    const req = httpMock.expectOne('/api/auth/password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ newPassword: 'brand-new-pw' });
    req.flush(null, { status: 204, statusText: 'No Content' });

    await pending;
    expect(authService.currentUser()?.hasPassword).toBe(true);
  });
});
