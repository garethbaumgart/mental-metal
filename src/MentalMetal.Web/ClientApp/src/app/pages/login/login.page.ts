import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../shared/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
  template: `
    <div class="flex items-center justify-center h-screen">
      <div class="flex flex-col items-center gap-6 p-8">
        <h1 class="text-4xl font-bold text-primary">Mental Metal</h1>
        <p class="text-muted-color text-lg">
          AI-powered command centre for engineering managers
        </p>
        <p-button
          label="Sign in with Google"
          icon="pi pi-google"
          (onClick)="authService.login()"
          size="large"
        />
      </div>
    </div>
  `,
})
export class LoginPage {
  protected readonly authService = inject(AuthService);
}
