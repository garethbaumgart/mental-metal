import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { PasswordModule } from 'primeng/password';
import { MessageModule } from 'primeng/message';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../shared/services/auth.service';
import { Password } from '../../shared/models/password.constants';

@Component({
  selector: 'app-password-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, PasswordModule, MessageModule],
  template: `
    <section class="flex flex-col gap-4">
      <h2 class="text-xl font-semibold">{{ heading() }}</h2>

      @if (!hasPassword()) {
        <p class="text-muted-color text-sm">
          Add a password so you can sign in without Google.
        </p>
      }

      <div class="flex flex-col gap-2">
        <label for="newPassword" class="text-sm font-medium text-muted-color">
          New password
        </label>
        <p-password
          id="newPassword"
          name="newPassword"
          [(ngModel)]="newPassword"
          [feedback]="true"
          [toggleMask]="true"
          styleClass="w-full"
          inputStyleClass="w-full"
          autocomplete="new-password"
        />
        <small class="text-muted-color">
          At least {{ minPasswordLength }} characters.
        </small>
      </div>

      @if (validationMessage()) {
        <p-message severity="error" [text]="validationMessage()!" />
      }

      <div>
        <p-button
          [label]="heading()"
          (onClick)="submit()"
          [loading]="submitting()"
          [disabled]="!canSubmit()"
        />
      </div>
    </section>
  `,
})
export class PasswordSettingsComponent {
  private readonly authService = inject(AuthService);
  private readonly messageService = inject(MessageService);

  protected readonly minPasswordLength = Password.MinimumLength;
  protected readonly submitting = signal(false);
  protected readonly validationMessage = signal<string | null>(null);
  protected readonly hasPassword = computed(
    () => this.authService.currentUser()?.hasPassword ?? false,
  );
  protected readonly heading = computed(() =>
    this.hasPassword() ? 'Change password' : 'Set a password',
  );

  protected newPassword = '';

  canSubmit(): boolean {
    return (
      !this.submitting() &&
      this.newPassword.length >= Password.MinimumLength
    );
  }

  async submit(): Promise<void> {
    if (this.newPassword.length < Password.MinimumLength) {
      this.validationMessage.set(
        `Password must be at least ${Password.MinimumLength} characters.`,
      );
      return;
    }

    this.submitting.set(true);
    this.validationMessage.set(null);

    try {
      await this.authService.setPassword(this.newPassword);
      this.newPassword = '';
      this.messageService.add({
        severity: 'success',
        summary: this.hasPassword() ? 'Password updated' : 'Password set',
      });
    } catch {
      this.messageService.add({
        severity: 'error',
        summary: 'Failed to save password',
      });
    } finally {
      this.submitting.set(false);
    }
  }
}
