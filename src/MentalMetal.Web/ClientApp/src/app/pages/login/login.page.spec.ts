import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { LoginPage } from './login.page';

describe('LoginPage', () => {
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    // Drain any eager /api/users/me call from AuthService constructor if a prior test
    // stored a token. Tests themselves clear localStorage in beforeEach.
    httpMock.match('/api/users/me').forEach((r) => r.flush(null, { status: 401, statusText: 'Unauthorized' }));
    httpMock.verify();
    localStorage.clear();
  });

  it('renders the login form by default', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement;
    expect(host.textContent).toContain('Sign in');
    // No Name field in login mode
    expect(host.querySelector('input#name')).toBeNull();
  });

  it('toggles to register mode and shows the Name field', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement;
    const toggleButton = Array.from(host.querySelectorAll('button')).find((b) =>
      (b.textContent ?? '').includes('Need an account'),
    ) as HTMLButtonElement;
    expect(toggleButton).toBeTruthy();
    toggleButton.click();
    fixture.detectChanges();

    expect(host.querySelector('input#name')).not.toBeNull();
    expect(host.textContent).toContain('Create account');
  });

  it('posts login credentials and stores the access token', async () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const component = fixture.componentInstance as unknown as {
      email: string;
      password: string;
      submit: () => Promise<void>;
    };
    component.email = 'user@example.com';
    component.password = 'secret-pw';

    const pending = component.submit();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com', password: 'secret-pw' });
    req.flush({
      accessToken: 'at',
      user: {
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
        hasPassword: true,
        createdAt: '2026-01-01T00:00:00Z',
        lastLoginAt: '2026-01-01T00:00:00Z',
      },
    });

    await pending;
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('shows an invalid-credentials message on 401', async () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const component = fixture.componentInstance as unknown as {
      email: string;
      password: string;
      submit: () => Promise<void>;
    };
    component.email = 'user@example.com';
    component.password = 'wrong-pw';

    const pending = component.submit();

    const req = httpMock.expectOne('/api/auth/login');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    await pending;
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.textContent).toContain('Invalid email or password');
  });

  it('shows an email-in-use message on 409 during register', async () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const component = fixture.componentInstance as unknown as {
      email: string;
      password: string;
      name: string;
      mode: () => string;
      toggleMode: () => void;
      submit: () => Promise<void>;
    };
    component.toggleMode();
    component.email = 'taken@example.com';
    component.password = 'secret-pw';
    component.name = 'New User';

    const pending = component.submit();

    const req = httpMock.expectOne('/api/auth/register');
    req.flush({}, { status: 409, statusText: 'Conflict' });

    await pending;
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.textContent).toContain('already in use');
  });
});
