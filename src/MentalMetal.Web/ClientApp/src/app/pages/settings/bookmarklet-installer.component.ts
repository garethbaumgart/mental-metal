import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
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
      </p>

      @if (!hasActiveTokens()) {
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
          @if (bookmarkletUrl()) {
            <div class="flex flex-col gap-2">
              <label class="text-sm font-medium text-muted-color">2. Drag this to your bookmarks bar</label>
              <div class="flex items-center gap-3">
                <a
                  [href]="bookmarkletUrl()"
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
export class BookmarkletInstallerComponent implements OnInit {
  private readonly patService = inject(PersonalAccessTokensService);
  private readonly instanceUrl = window.location.origin;

  private readonly tokens = signal<PatSummary[]>([]);
  protected readonly patInput = signal('');

  protected readonly hasActiveTokens = computed(() =>
    this.tokens().some((t) => !t.revokedAt && t.scopes.includes('captures:write'))
  );

  protected readonly bookmarkletUrl = computed(() => {
    const pat = this.patInput().trim();
    if (!pat || !pat.startsWith('mm_pat_')) return null;
    return generateBookmarkletUrl(this.instanceUrl, pat);
  });

  ngOnInit(): void {
    this.patService.list().subscribe({
      next: (tokens) => this.tokens.set(tokens),
    });
  }
}
