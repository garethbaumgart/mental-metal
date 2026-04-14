import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { DividerModule } from 'primeng/divider';
import { MessageModule } from 'primeng/message';
import { AuthService } from '../../shared/services/auth.service';
import { Password } from '../../shared/models/password.constants';

type Mode = 'login' | 'register';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    DividerModule,
    MessageModule,
  ],
  template: `
    <div class="flex items-center justify-center min-h-screen p-6">
      <div class="flex flex-col items-center gap-6 w-full max-w-sm">
        <h1 class="text-4xl font-bold text-primary">Mental Metal</h1>
        <p class="text-muted-color text-center">
          AI-powered command centre for engineering managers
        </p>

        <p-button
          label="Sign in with Google"
          icon="pi pi-google"
          (onClick)="authService.login()"
          size="large"
          styleClass="w-full"
        />

        <p-divider align="center">
          <span class="text-muted-color text-sm">or</span>
        </p-divider>

        <form
          class="flex flex-col gap-3 w-full"
          (ngSubmit)="submit()"
          #form="ngForm"
        >
          <h2 class="text-xl font-semibold">
            {{ mode() === 'login' ? 'Sign in' : 'Create account' }}
          </h2>

          @if (mode() === 'register') {
            <div class="flex flex-col gap-1">
              <label for="name" class="text-sm font-medium text-muted-color">Name</label>
              <input
                pInputText
                id="name"
                name="name"
                type="text"
                [ngModel]="name()"
                (ngModelChange)="name.set($event)"
                required
                autocomplete="name"
              />
            </div>
          }

          <div class="flex flex-col gap-1">
            <label for="email" class="text-sm font-medium text-muted-color">Email</label>
            <input
              pInputText
              id="email"
              name="email"
              type="email"
              [ngModel]="email()"
              (ngModelChange)="email.set($event)"
              required
              autocomplete="email"
            />
          </div>

          <div class="flex flex-col gap-1">
            <label for="password" class="text-sm font-medium text-muted-color">Password</label>
            <p-password
              id="password"
              name="password"
              [ngModel]="password()"
              (ngModelChange)="password.set($event)"
              [feedback]="mode() === 'register'"
              [toggleMask]="true"
              styleClass="w-full"
              inputStyleClass="w-full"
              autocomplete="{{ mode() === 'register' ? 'new-password' : 'current-password' }}"
              required
            />
            @if (mode() === 'register') {
              <small class="text-muted-color">
                At least {{ minPasswordLength }} characters.
              </small>
            }
          </div>

          @if (errorMessage()) {
            <p-message severity="error" [text]="errorMessage()!" />
          }

          <p-button
            type="submit"
            [label]="mode() === 'login' ? 'Sign in' : 'Create account'"
            [loading]="submitting()"
            [disabled]="!canSubmit()"
            styleClass="w-full"
          />

          <button
            type="button"
            class="text-sm text-primary hover:underline self-center"
            (click)="toggleMode()"
          >
            @if (mode() === 'login') {
              <span>Need an account? Create one</span>
            } @else {
              <span>Already have an account? Sign in</span>
            }
          </button>
        </form>
      </div>
    </div>
  `,
})
export class LoginPage {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly mode = signal<Mode>('login');
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly minPasswordLength = Password.MinimumLength;

  protected readonly email = signal<string>('');
  protected readonly password = signal<string>('');
  protected readonly name = signal<string>('');

  protected readonly canSubmit = computed(() => {
    if (this.submitting()) return false;
    if (!this.email().trim() || !this.password()) return false;
    if (this.mode() === 'register') {
      if (!this.name().trim()) return false;
      if (this.password().length < Password.MinimumLength) return false;
    }
    return true;
  });

  toggleMode(): void {
    this.mode.update((m) => (m === 'login' ? 'register' : 'login'));
    this.errorMessage.set(null);
  }

  async submit(): Promise<void> {
    if (!this.canSubmit()) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    try {
      if (this.mode() === 'login') {
        await this.authService.loginWithPassword(this.email().trim(), this.password());
      } else {
        await this.authService.registerWithPassword(
          this.email().trim(),
          this.password(),
          this.name().trim(),
        );
      }
      await this.router.navigate(['/']);
    } catch (err: unknown) {
      this.errorMessage.set(this.resolveErrorMessage(err));
    } finally {
      this.submitting.set(false);
    }
  }

  private resolveErrorMessage(err: unknown): string {
    const status = (err as { status?: number })?.status;
    if (status === 401) return 'Invalid email or password.';
    if (status === 409) return 'That email is already in use.';
    if (status === 400) return 'Please check your details and try again.';
    return 'Something went wrong. Please try again.';
  }
}
