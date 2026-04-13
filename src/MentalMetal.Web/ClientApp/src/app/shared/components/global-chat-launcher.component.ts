import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../services/auth.service';
import { GlobalChatStateService } from '../services/global-chat-state.service';

/**
 * Always-visible (when signed-in) launcher for the global chat slide-over. Hides itself
 * on unauthenticated routes by gating on AuthService.isAuthenticated rather than wiring
 * route data — works in any layout the launcher is embedded into.
 */
@Component({
  selector: 'app-global-chat-launcher',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
  template: `
    @if (auth.isAuthenticated()) {
      <p-button
        icon="pi pi-sparkles"
        [text]="true"
        ariaLabel="Open chat"
        (onClick)="open()"
      />
    }
  `,
})
export class GlobalChatLauncherComponent {
  protected readonly auth = inject(AuthService);
  private readonly state = inject(GlobalChatStateService);

  protected open(): void {
    this.state.openSlideOver();
  }
}
