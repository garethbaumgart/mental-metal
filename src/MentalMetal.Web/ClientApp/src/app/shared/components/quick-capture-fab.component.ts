import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { QuickCaptureUiService } from '../services/quick-capture-ui.service';

/**
 * Persistent Floating Action Button for Quick Capture. Rendered once at
 * the authenticated shell level so it is visible on every page. The
 * tooltip advertises the keyboard shortcut so users discover the faster
 * path.
 */
@Component({
  selector: 'app-quick-capture-fab',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, TooltipModule],
  template: `
    <button
      type="button"
      pButton
      icon="pi pi-plus"
      [rounded]="true"
      [raised]="true"
      severity="primary"
      class="fixed bottom-6 right-6 z-40"
      [pTooltip]="'Quick capture (' + shortcutLabel() + ')'"
      tooltipPosition="left"
      [attr.aria-label]="'Quick capture (' + shortcutLabel() + ')'"
      (click)="ui.open()"
    ></button>
  `,
})
export class QuickCaptureFabComponent {
  protected readonly ui = inject(QuickCaptureUiService);

  /** macOS uses Cmd, everything else uses Ctrl. */
  protected readonly shortcutLabel = computed(() => (isMac() ? '⌘K' : 'Ctrl+K'));
}

export function isMac(): boolean {
  if (typeof navigator === 'undefined') return false;
  const platform = (navigator as Navigator & { userAgentData?: { platform?: string } }).userAgentData?.platform
    ?? navigator.platform
    ?? navigator.userAgent;
  return /mac/i.test(platform);
}
