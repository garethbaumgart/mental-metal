import { Directive, HostListener, inject } from '@angular/core';
import { QuickCaptureUiService } from '../services/quick-capture-ui.service';
import { isMac } from '../utils/platform';

/**
 * Host-level keyboard shortcut for opening Quick Capture from anywhere in
 * the authenticated shell:
 *   - macOS: ⌘K
 *   - Other: Ctrl+K
 *
 * Applied to the authenticated shell so login/signup pages are unaffected.
 * Calls preventDefault() on the matched keystroke to stop the browser's
 * default handler (e.g. Chrome's address-bar focus) from firing.
 */
@Directive({
  selector: '[appQuickCaptureShortcut]',
  standalone: true,
})
export class QuickCaptureShortcutDirective {
  private readonly ui = inject(QuickCaptureUiService);

  @HostListener('window:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'k' && event.key !== 'K') return;

    const primaryModifier = isMac() ? event.metaKey : event.ctrlKey;
    if (!primaryModifier) return;
    // Ignore Cmd+Shift+K / Ctrl+Alt+K etc. so we don't clash with browser shortcuts.
    if (event.altKey || event.shiftKey) return;

    event.preventDefault();
    this.ui.open();
  }
}
