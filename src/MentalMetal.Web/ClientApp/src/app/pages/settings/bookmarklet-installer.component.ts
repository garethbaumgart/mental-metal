import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  PersonalAccessTokensService,
  PatSummary,
} from '../../shared/services/personal-access-tokens.service';
import { generateBookmarkletUrl } from './bookmarklet-template';

@Component({
  selector: 'app-bookmarklet-installer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, InputTextModule],
  template: `
    <section class="flex flex-col gap-4">
      <h2 class="text-xl font-semibold">Bookmarklet</h2>
      <p class="text-sm text-muted-color">
        Import Google Docs transcripts into Mental Metal with one click — no extension or OAuth needed.
        Desktop browsers only.
      </p>

      @if (loadError()) {
        <div
          class="p-4 rounded-lg text-sm"
          style="background: var(--p-surface-100); border: 1px solid var(--p-content-border-color)"
        >
          <p class="font-medium" style="color: var(--p-red-500)">Failed to load tokens. Check your connection and refresh the page.</p>
        </div>
      } @else if (!hasActiveTokens()) {
        <div
          class="p-4 rounded-lg text-sm"
          style="background: var(--p-surface-100); border: 1px solid var(--p-content-border-color)"
        >
          <p class="font-medium">No active tokens available.</p>
          <p class="text-muted-color mt-1">
            Generate a Personal Access Token with <strong>captures:write</strong> scope in the section above, then come back here.
          </p>
        </div>
      } @else {
        <div class="flex flex-col gap-4">
          <!-- Step 1: Paste token -->
          <div class="flex flex-col gap-2">
            <label for="patInput" class="text-sm font-medium text-muted-color">
              1. Paste your Personal Access Token
            </label>
            <input
              pInputText
              id="patInput"
              [ngModel]="patInput()"
              (ngModelChange)="patInput.set($event)"
              placeholder="mm_pat_..."
              class="w-full font-mono text-sm"
            />
            <span class="text-xs text-muted-color">
              The token you copied when you generated it above. It starts with <code>mm_pat_</code>.
            </span>
          </div>

          <!-- Step 2: Drag bookmarklet -->
          @if (bookmarkletSafeUrl()) {
            <div class="flex flex-col gap-2">
              <label class="text-sm font-medium text-muted-color">2. Drag this to your bookmarks bar</label>
              <div class="flex items-center gap-3">
                <a
                  [href]="bookmarkletSafeUrl()"
                  class="inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium cursor-grab no-underline"
                  style="background: var(--p-primary-color); color: var(--p-primary-contrast-color)"
                  (click)="$event.preventDefault()"
                  draggable="true"
                  title="Drag me to your bookmarks bar"
                >
                  <i class="pi pi-bookmark"></i>
                  Import to Mental Metal
                </a>
                <span class="text-xs text-muted-color italic">drag, don't click</span>
              </div>
              <p class="text-xs text-muted-color mt-1">
                Your token is embedded in the bookmark. If you revoke the token, you'll need to regenerate and re-drag.
              </p>
            </div>

            <!-- Step 3: Instructions -->
            <div class="flex flex-col gap-2">
              <label class="text-sm font-medium text-muted-color">3. Use it</label>
              <p class="text-sm text-muted-color">
                Open any Google Doc transcript (from your Drive or a calendar event link), then click the bookmarklet in your bookmarks bar. The transcript will be imported and queued for AI extraction.
              </p>
            </div>
          }
        </div>
      }
    </section>
  `,
})
export class BookmarkletInstallerComponent {
  private readonly patService = inject(PersonalAccessTokensService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly messageService = inject(MessageService);
  private readonly instanceUrl = window.location.origin;

  readonly loadError = signal(false);

  private readonly tokens = toSignal(
    this.patService.list().pipe(
      catchError(() => {
        this.loadError.set(true);
        return of([] as PatSummary[]);
      })
    ),
    { initialValue: [] as PatSummary[] }
  );

  protected readonly patInput = signal('');

  protected readonly hasActiveTokens = computed(() =>
    this.tokens().some((t) => !t.revokedAt && t.scopes.includes('captures:write'))
  );

  protected readonly bookmarkletSafeUrl = computed((): SafeUrl | null => {
    const pat = this.patInput().trim();
    if (!pat || !pat.startsWith('mm_pat_')) return null;
    const url = generateBookmarkletUrl(this.instanceUrl, pat);
    return this.sanitizer.bypassSecurityTrustUrl(url);
  });
}
