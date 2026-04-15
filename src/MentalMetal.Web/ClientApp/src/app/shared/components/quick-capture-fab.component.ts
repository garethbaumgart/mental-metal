import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { QuickCaptureUiService } from '../services/quick-capture-ui.service';
import { isMac } from '../utils/platform';

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
      [pTooltip]="'Quick capture (' + shortcutGlyph() + ')'"
      tooltipPosition="left"
      [attr.aria-label]="'Quick capture (' + shortcutLabel() + ')'"
      (click)="ui.open()"
    ></button>
  `,
})
export class QuickCaptureFabComponent {
  protected readonly ui = inject(QuickCaptureUiService);

  /** Short glyph for the visible tooltip ("⌘K" / "Ctrl+K"). */
  protected readonly shortcutGlyph = computed(() => (isMac() ? '⌘K' : 'Ctrl+K'));

  /**
   * Spelled-out label for the aria-label. Screen readers announce
   * "Cmd + K" more reliably than the glyph "⌘K", so we keep the
   * accessible name as plain text even when the tooltip shows the glyph.
   */
  protected readonly shortcutLabel = computed(() => (isMac() ? 'Cmd+K' : 'Ctrl+K'));
}
