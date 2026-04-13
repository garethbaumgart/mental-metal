import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
  ElementRef,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { InitiativeChatService } from '../../../shared/services/initiative-chat.service';
import {
  ChatMessage,
  ChatThread,
  ChatThreadSummary,
} from '../../../shared/models/chat-thread.model';
import { SourceReferenceChipComponent } from './source-reference-chip.component';

@Component({
  selector: 'app-initiative-chat-tab',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    DatePipe,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    TagModule,
    MessageModule,
    SkeletonModule,
    ToastModule,
    SourceReferenceChipComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast />

    <div class="grid grid-cols-12 gap-4 h-[600px]">
      <!-- Left rail -->
      <aside class="col-span-4 flex flex-col gap-3 border rounded p-3" style="border-color: var(--p-surface-200)">
        <div class="flex items-center justify-between">
          <h3 class="text-lg font-semibold">Threads</h3>
          <p-button
            icon="pi pi-plus"
            [text]="true"
            [loading]="startingThread()"
            (onClick)="startThread()"
            ariaLabel="New thread"
          />
        </div>

        @if (activeThreads().length === 0) {
          <p class="text-sm text-muted-color">No active threads yet.</p>
        } @else {
          <ul class="flex flex-col gap-1">
            @for (t of activeThreads(); track t.id) {
              <li>
                <button
                  type="button"
                  class="thread-row w-full text-left p-2 rounded"
                  [class.selected]="t.id === activeThreadId()"
                  (click)="selectThread(t.id)"
                >
                  <div class="text-sm font-medium truncate">{{ t.title || 'New thread' }}</div>
                  <div class="text-xs text-muted-color">
                    {{ t.messageCount }} message{{ t.messageCount === 1 ? '' : 's' }}
                    @if (t.lastMessageAt) {
                      · {{ t.lastMessageAt | date:'short' }}
                    }
                  </div>
                </button>
              </li>
            }
          </ul>
        }

        <div class="mt-auto flex flex-col gap-2">
          <button
            type="button"
            class="text-xs text-muted-color text-left"
            (click)="toggleArchived()"
          >
            {{ showArchived() ? '▾' : '▸' }} Archived ({{ archivedThreads().length }})
          </button>
          @if (showArchived()) {
            <ul class="flex flex-col gap-1">
              @for (t of archivedThreads(); track t.id) {
                <li class="flex items-center gap-1">
                  <button
                    type="button"
                    class="thread-row flex-1 text-left p-1 rounded opacity-70"
                    [class.selected]="t.id === activeThreadId()"
                    (click)="selectThread(t.id)"
                  >
                    <div class="text-xs truncate">{{ t.title || 'Untitled' }}</div>
                  </button>
                  <p-button
                    icon="pi pi-refresh"
                    [text]="true"
                    size="small"
                    ariaLabel="Unarchive"
                    (onClick)="unarchive(t.id)"
                  />
                </li>
              }
            </ul>
          }
        </div>
      </aside>

      <!-- Main panel -->
      <section class="col-span-8 flex flex-col gap-3 border rounded p-3" style="border-color: var(--p-surface-200)">
        @if (!activeThread()) {
          <div class="flex items-center justify-center flex-1 text-sm text-muted-color">
            Select or start a thread.
          </div>
        } @else {
          <div class="flex items-center gap-2">
            @if (renaming()) {
              <input
                pInputText
                [(ngModel)]="renameDraft"
                class="flex-1"
                (keyup.enter)="commitRename()"
              />
              <p-button label="Save" size="small" (onClick)="commitRename()" [loading]="savingRename()" />
              <p-button label="Cancel" size="small" [text]="true" (onClick)="cancelRename()" />
            } @else {
              <h4 class="text-base font-semibold flex-1 truncate">{{ activeThread()!.title || 'New thread' }}</h4>
              <p-button icon="pi pi-pencil" [text]="true" size="small" ariaLabel="Rename" (onClick)="beginRename()" />
              @if (activeThread()!.status === 'Active') {
                <p-button icon="pi pi-inbox" [text]="true" size="small" ariaLabel="Archive" (onClick)="archive()" />
              }
            }
          </div>

          <div #messageList class="flex-1 overflow-y-auto flex flex-col gap-2 p-2">
            @for (m of activeThread()!.messages; track m.messageOrdinal) {
              <div
                class="rounded p-2 message-bubble"
                [class.self-end]="m.role === 'User'"
                [class.self-start]="m.role === 'Assistant'"
                [class.self-center]="m.role === 'System'"
                [class.bubble-capped]="m.role !== 'System'"
                [class.user-bubble]="m.role === 'User'"
                [class.assistant-bubble]="m.role === 'Assistant'"
              >
                @if (m.role === 'System') {
                  <p-message severity="warn" [text]="m.content" />
                } @else {
                  <div class="whitespace-pre-wrap text-sm">{{ m.content }}</div>

                  @if (m.sourceReferences.length > 0) {
                    <div class="flex flex-wrap gap-1 mt-2">
                      @for (r of m.sourceReferences; track r.entityId + '-' + $index) {
                        <app-source-reference-chip [reference]="r" [initiativeId]="initiativeId()" />
                      }
                    </div>
                  }

                  <div class="text-xs text-muted-color mt-1"
                    [title]="m.tokenUsage ? ('Tokens: ' + m.tokenUsage.promptTokens + ' prompt / ' + m.tokenUsage.completionTokens + ' completion') : ''">
                    {{ m.createdAt | date:'short' }}
                  </div>
                }
              </div>
            }

            @if (awaitingReply()) {
              <div class="self-start rounded p-2 assistant-bubble max-w-[80%]">
                <p-skeleton width="12rem" height="1rem" styleClass="mb-2" />
                <p-skeleton width="18rem" height="1rem" />
              </div>
            }
          </div>

          <div class="flex gap-2">
            <textarea
              pTextarea
              [(ngModel)]="composerText"
              rows="2"
              class="flex-1"
              [disabled]="awaitingReply() || activeThread()!.status !== 'Active'"
              (keydown.enter)="onComposerEnter($event)"
              placeholder="Ask about this initiative..."
            ></textarea>
            <p-button
              label="Send"
              icon="pi pi-send"
              [disabled]="!composerText.trim() || activeThread()!.status !== 'Active'"
              [loading]="awaitingReply()"
              (onClick)="sendMessage()"
            />
          </div>
        }
      </section>
    </div>
  `,
  styles: [`
    .thread-row {
      border: 1px solid transparent;
    }
    .thread-row:hover {
      background: var(--p-surface-100);
    }
    .thread-row.selected {
      background: var(--p-surface-100);
      border-color: var(--p-primary-color);
    }
    .message-bubble {
      border: 1px solid var(--p-surface-200);
    }
    .bubble-capped {
      max-width: 80%;
    }
    .user-bubble {
      background: var(--p-primary-50);
    }
    .assistant-bubble {
      background: var(--p-surface-50);
    }
  `],
})
export class InitiativeChatTabComponent {
  readonly initiativeId = input.required<string>();

  private readonly chatService = inject(InitiativeChatService);
  private readonly messageService = inject(MessageService);

  private readonly messageList = viewChild<ElementRef<HTMLDivElement>>('messageList');

  private readonly threadSummaries = signal<ChatThreadSummary[]>([]);
  private readonly archivedSummaries = signal<ChatThreadSummary[]>([]);
  protected readonly activeThread = signal<ChatThread | null>(null);
  protected readonly activeThreadId = computed(() => this.activeThread()?.id ?? null);

  protected readonly startingThread = signal(false);
  protected readonly awaitingReply = signal(false);
  protected readonly showArchived = signal(false);
  protected readonly renaming = signal(false);
  protected readonly savingRename = signal(false);

  protected renameDraft = '';
  protected composerText = '';

  protected readonly activeThreads = computed(() =>
    [...this.threadSummaries()].sort((a, b) => this.sortByRecency(a, b))
  );
  protected readonly archivedThreads = computed(() =>
    [...this.archivedSummaries()].sort((a, b) => this.sortByRecency(a, b))
  );

  constructor() {
    effect(() => {
      const id = this.initiativeId();
      if (id) this.refreshThreads();
    });

    effect(() => {
      // When the active thread (or its messages) changes, scroll to bottom.
      if (this.activeThread()) {
        queueMicrotask(() => this.scrollToBottom());
      }
    });
  }

  protected startThread(): void {
    this.startingThread.set(true);
    this.chatService.start(this.initiativeId()).subscribe({
      next: (t) => {
        this.startingThread.set(false);
        this.appendThreadSummary(t);
        this.activeThread.set(t);
      },
      error: () => {
        this.startingThread.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to start thread' });
      },
    });
  }

  protected selectThread(threadId: string): void {
    this.chatService.get(this.initiativeId(), threadId).subscribe({
      next: (t) => this.activeThread.set(t),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to load thread' }),
    });
  }

  protected sendMessage(): void {
    const thread = this.activeThread();
    if (!thread || !this.composerText.trim()) return;

    const content = this.composerText.trim();
    this.composerText = '';
    this.awaitingReply.set(true);

    // Optimistically append the user message so the UI is responsive while the request is in flight.
    const optimistic: ChatMessage = {
      messageOrdinal: thread.messageCount + 1,
      role: 'User',
      content,
      createdAt: new Date().toISOString(),
      sourceReferences: [],
      tokenUsage: null,
    };
    this.activeThread.update((t) => t ? { ...t, messages: [...t.messages, optimistic] } : t);

    this.chatService.postMessage(this.initiativeId(), thread.id, { content }).subscribe({
      next: (resp) => {
        this.awaitingReply.set(false);
        // Replace the optimistic message with the canonical ones from the server. The backend
        // auto-titles the thread on the first user message, so adopt the server's title here.
        this.activeThread.update((t) => {
          if (!t) return t;
          const base = t.messages.filter((m) => m !== optimistic);
          return {
            ...t,
            title: resp.thread.title,
            messages: [...base, resp.userMessage, resp.assistantMessage],
            messageCount: resp.thread.messageCount,
            lastMessageAt: resp.thread.lastMessageAt ?? resp.assistantMessage.createdAt,
          };
        });
        this.syncThreadSummary(resp.thread);
      },
      error: (err) => {
        this.awaitingReply.set(false);
        const status = err?.status as number | undefined;
        if (status === 409) {
          this.messageService.add({ severity: 'error', summary: 'This thread is archived' });
        } else {
          this.messageService.add({ severity: 'error', summary: 'Failed to send message' });
        }
        // Roll back optimistic append.
        this.activeThread.update((t) => t ? { ...t, messages: t.messages.filter((m) => m !== optimistic) } : t);
      },
    });
  }

  protected onComposerEnter(event: Event): void {
    const ke = event as KeyboardEvent;
    if (ke.shiftKey) return; // Shift+Enter newline.
    ke.preventDefault();
    this.sendMessage();
  }

  protected beginRename(): void {
    this.renameDraft = this.activeThread()?.title || '';
    this.renaming.set(true);
  }

  protected cancelRename(): void {
    this.renaming.set(false);
  }

  protected commitRename(): void {
    const thread = this.activeThread();
    if (!thread || !this.renameDraft.trim()) return;
    this.savingRename.set(true);
    this.chatService.rename(this.initiativeId(), thread.id, { title: this.renameDraft.trim() }).subscribe({
      next: (t) => {
        this.savingRename.set(false);
        this.renaming.set(false);
        this.activeThread.update((curr) => curr ? { ...curr, title: t.title } : curr);
        this.renameThreadSummary(t.id, t.title);
      },
      error: () => {
        this.savingRename.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to rename thread' });
      },
    });
  }

  protected archive(): void {
    const thread = this.activeThread();
    if (!thread) return;
    this.chatService.archive(this.initiativeId(), thread.id).subscribe({
      next: () => {
        this.moveToArchived(thread.id);
        this.activeThread.set(null);
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to archive' }),
    });
  }

  protected unarchive(threadId: string): void {
    this.chatService.unarchive(this.initiativeId(), threadId).subscribe({
      next: () => this.refreshThreads(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to unarchive' }),
    });
  }

  protected toggleArchived(): void {
    this.showArchived.update((v) => !v);
  }

  private refreshThreads(): void {
    this.chatService.list(this.initiativeId(), 'Active').subscribe({
      next: (list) => this.threadSummaries.set(list),
    });
    this.chatService.list(this.initiativeId(), 'Archived').subscribe({
      next: (list) => this.archivedSummaries.set(list),
    });
  }

  private appendThreadSummary(thread: ChatThread): void {
    const summary: ChatThreadSummary = {
      id: thread.id,
      title: thread.title,
      status: thread.status,
      createdAt: thread.createdAt,
      lastMessageAt: thread.lastMessageAt ?? null,
      messageCount: thread.messageCount,
    };
    this.threadSummaries.update((list) => [summary, ...list]);
  }

  private syncThreadSummary(summary: ChatThreadSummary): void {
    this.threadSummaries.update((list) =>
      list.map((t) => t.id === summary.id ? { ...t, ...summary } : t)
    );
  }

  private renameThreadSummary(threadId: string, title: string): void {
    this.threadSummaries.update((list) => list.map((t) => t.id === threadId ? { ...t, title } : t));
    this.archivedSummaries.update((list) => list.map((t) => t.id === threadId ? { ...t, title } : t));
  }

  private moveToArchived(threadId: string): void {
    const moved = this.threadSummaries().find((t) => t.id === threadId);
    if (!moved) return;
    this.threadSummaries.update((list) => list.filter((t) => t.id !== threadId));
    this.archivedSummaries.update((list) => [{ ...moved, status: 'Archived' }, ...list]);
  }

  private sortByRecency(a: ChatThreadSummary, b: ChatThreadSummary): number {
    const aTime = a.lastMessageAt ? Date.parse(a.lastMessageAt) : Date.parse(a.createdAt);
    const bTime = b.lastMessageAt ? Date.parse(b.lastMessageAt) : Date.parse(b.createdAt);
    return bTime - aTime;
  }

  private scrollToBottom(): void {
    const el = this.messageList()?.nativeElement;
    if (!el) return;
    el.scrollTop = el.scrollHeight;
  }
}
