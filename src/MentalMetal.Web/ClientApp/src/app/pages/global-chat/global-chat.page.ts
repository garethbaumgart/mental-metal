import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  signal,
  viewChild,
  ElementRef,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { GlobalChatService } from '../../shared/services/global-chat.service';
import { GlobalChatStateService } from '../../shared/services/global-chat-state.service';
import { ChatMessage, ChatThread, ChatThreadSummary } from '../../shared/models/chat-thread.model';
import { SourceReferenceChipComponent } from '../initiatives/chat/source-reference-chip.component';

interface ThreadGroup { label: string; threads: ChatThreadSummary[]; }

/**
 * Full-page global chat view at `/chat`. Mirrors the slide-over but with a left rail of
 * threads grouped by date and per-thread rename / archive controls. Shares state via
 * GlobalChatStateService so navigating back and forth between this page and the slide-over
 * keeps the active thread selected.
 */
@Component({
  selector: 'app-global-chat-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    MessageModule,
    SkeletonModule,
    ToastModule,
    SourceReferenceChipComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="grid grid-cols-12 gap-4 h-[calc(100vh-7rem)]">
      <!-- Left rail -->
      <aside class="col-span-12 md:col-span-4 lg:col-span-3 flex flex-col gap-3 border rounded p-3" style="border-color: var(--p-content-border-color)">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-semibold">Threads</h2>
          <p-button icon="pi pi-plus" [text]="true" [loading]="startingThread()" (onClick)="startThread()" ariaLabel="New thread" />
        </div>

        @for (group of activeGroups(); track group.label) {
          @if (group.threads.length > 0) {
            <div class="flex flex-col gap-1">
              <div class="text-xs uppercase tracking-wide text-muted-color">{{ group.label }}</div>
              <ul class="flex flex-col gap-1">
                @for (t of group.threads; track t.id) {
                  <li class="flex items-center gap-1">
                    @if (renamingId() === t.id) {
                      <input
                        pInputText
                        [value]="renameDraft()"
                        (input)="renameDraft.set($any($event.target).value)"
                        class="flex-1"
                        (keyup.enter)="commitRename(t.id)"
                      />
                      <p-button label="Save" size="small" (onClick)="commitRename(t.id)" />
                    } @else {
                      <button
                        type="button"
                        class="thread-row flex-1 text-left p-2 rounded"
                        [class.selected]="t.id === state.activeThread()?.id"
                        (click)="selectThread(t.id)"
                      >
                        <div class="text-sm font-medium truncate">{{ t.title || 'New thread' }}</div>
                        <div class="text-xs text-muted-color">
                          {{ t.messageCount }} message{{ t.messageCount === 1 ? '' : 's' }}
                          @if (t.lastMessageAt) { · {{ t.lastMessageAt | date:'short' }} }
                        </div>
                      </button>
                      <p-button icon="pi pi-pencil" [text]="true" size="small" ariaLabel="Rename" (onClick)="beginRename(t.id, t.title)" />
                      <p-button icon="pi pi-inbox" [text]="true" size="small" ariaLabel="Archive" (onClick)="archive(t.id)" />
                    }
                  </li>
                }
              </ul>
            </div>
          }
        }

        <div class="mt-auto flex flex-col gap-2">
          <button type="button" class="text-xs text-muted-color text-left" (click)="toggleArchived()">
            {{ showArchived() ? '▾' : '▸' }} Archived ({{ state.archivedThreads().length }})
          </button>
          @if (showArchived()) {
            <ul class="flex flex-col gap-1">
              @for (t of state.archivedThreads(); track t.id) {
                <li class="flex items-center gap-1">
                  <button
                    type="button"
                    class="thread-row flex-1 text-left p-1 rounded opacity-70"
                    [class.selected]="t.id === state.activeThread()?.id"
                    (click)="selectThread(t.id)"
                  >
                    <div class="text-xs truncate">{{ t.title || 'Untitled' }}</div>
                  </button>
                  <p-button icon="pi pi-refresh" [text]="true" size="small" ariaLabel="Unarchive" (onClick)="unarchive(t.id)" />
                </li>
              }
            </ul>
          }
        </div>
      </aside>

      <!-- Conversation pane -->
      <section class="col-span-12 md:col-span-8 lg:col-span-9 flex flex-col gap-3 border rounded p-3" style="border-color: var(--p-content-border-color)">
        @if (!state.activeThread()) {
          <div class="flex items-center justify-center flex-1 text-sm text-muted-color">
            Select or start a thread.
          </div>
        } @else {
          <div #messageList class="flex-1 overflow-y-auto flex flex-col gap-2 p-2">
            @for (m of state.activeThread()!.messages; track m.messageOrdinal) {
              <div
                class="rounded p-2 message-bubble"
                [class.self-end]="m.role === 'User'"
                [class.self-start]="m.role !== 'User'"
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
                        <app-source-reference-chip [reference]="r" />
                      }
                    </div>
                  }
                  <div class="text-xs text-muted-color mt-1">{{ m.createdAt | date:'short' }}</div>
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
              [value]="composer()"
              (input)="composer.set($any($event.target).value)"
              rows="2"
              class="flex-1"
              [disabled]="awaitingReply() || state.activeThread()!.status !== 'Active'"
              (keydown.enter)="onEnter($event)"
              placeholder="Ask anything..."
            ></textarea>
            <p-button
              label="Send"
              icon="pi pi-send"
              [disabled]="!composer().trim() || state.activeThread()!.status !== 'Active'"
              [loading]="awaitingReply()"
              (onClick)="send()"
            />
          </div>
        }
      </section>
    </div>
  `,
  styles: [`
    .thread-row { border: 1px solid transparent; }
    .thread-row:hover { background: var(--p-surface-100); }
    .thread-row.selected { background: var(--p-surface-100); border-color: var(--p-primary-color); }
    .message-bubble { border: 1px solid var(--p-content-border-color); }
    .user-bubble { background: var(--p-primary-50); }
    .assistant-bubble { background: var(--p-surface-50); }
  `],
})
export class GlobalChatPageComponent {
  protected readonly state = inject(GlobalChatStateService);
  private readonly chatService = inject(GlobalChatService);
  private readonly messageService = inject(MessageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly messageList = viewChild<ElementRef<HTMLDivElement>>('messageList');

  protected readonly composer = signal('');
  protected readonly awaitingReply = signal(false);
  protected readonly startingThread = signal(false);
  protected readonly showArchived = signal(false);
  protected readonly renamingId = signal<string | null>(null);
  protected readonly renameDraft = signal('');

  protected readonly activeGroups = computed<ThreadGroup[]>(() => this.groupByDate(this.state.activeThreads()));

  // Monotonic token to discard stale selectThread() responses when clicks race.
  private selectThreadRequestToken = 0;

  constructor() {
    this.refresh();

    effect(() => {
      if (this.state.activeThread()) {
        queueMicrotask(() => {
          const el = this.messageList()?.nativeElement;
          if (el) el.scrollTop = el.scrollHeight;
        });
      }
    });
  }

  protected toggleArchived(): void { this.showArchived.update((v) => !v); }

  protected startThread(): void {
    this.startingThread.set(true);
    this.chatService.start().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (t) => {
        this.startingThread.set(false);
        this.state.activeThreads.update((list) => [{ id: t.id, title: t.title, status: t.status, createdAt: t.createdAt, lastMessageAt: t.lastMessageAt ?? null, messageCount: t.messageCount }, ...list]);
        this.state.activeThread.set(t);
      },
      error: () => {
        this.startingThread.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to start thread' });
      },
    });
  }

  protected selectThread(threadId: string): void {
    // Guard against stale responses when the user clicks between threads rapidly: tag each
    // request with a monotonic token and only apply the response if it still matches.
    const requestToken = ++this.selectThreadRequestToken;
    this.chatService.get(threadId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (t) => {
        if (requestToken !== this.selectThreadRequestToken) return;
        this.state.activeThread.set(t);
      },
      error: () => {
        if (requestToken !== this.selectThreadRequestToken) return;
        this.messageService.add({ severity: 'error', summary: 'Failed to load thread' });
      },
    });
  }

  protected beginRename(threadId: string, currentTitle: string): void {
    this.renamingId.set(threadId);
    this.renameDraft.set(currentTitle);
  }

  protected commitRename(threadId: string): void {
    const draft = this.renameDraft().trim();
    if (!draft) return;
    this.chatService.rename(threadId, { title: draft }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (t) => {
        this.renamingId.set(null);
        this.state.activeThreads.update((list) => list.map((x) => x.id === threadId ? { ...x, title: t.title } : x));
        this.state.activeThread.update((curr) => curr && curr.id === threadId ? { ...curr, title: t.title } : curr);
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to rename thread' }),
    });
  }

  protected archive(threadId: string): void {
    this.chatService.archive(threadId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        const moved = this.state.activeThreads().find((t) => t.id === threadId);
        this.state.activeThreads.update((list) => list.filter((t) => t.id !== threadId));
        if (moved) this.state.archivedThreads.update((list) => [{ ...moved, status: 'Archived' }, ...list]);
        if (this.state.activeThread()?.id === threadId) this.state.activeThread.set(null);
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to archive' }),
    });
  }

  protected unarchive(threadId: string): void {
    this.chatService.unarchive(threadId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.refresh(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to unarchive' }),
    });
  }

  protected onEnter(event: Event): void {
    const ke = event as KeyboardEvent;
    if (ke.shiftKey) return;
    ke.preventDefault();
    this.send();
  }

  protected send(): void {
    const thread = this.state.activeThread();
    const draft = this.composer().trim();
    if (!thread || !draft) return;

    const optimistic: ChatMessage = {
      messageOrdinal: thread.messageCount + 1,
      role: 'User',
      content: draft,
      createdAt: new Date().toISOString(),
      sourceReferences: [],
      tokenUsage: null,
    };
    this.state.activeThread.update((t) => t ? { ...t, messages: [...t.messages, optimistic] } : t);
    this.composer.set('');
    this.awaitingReply.set(true);

    const sendingThreadId = thread.id;
    this.chatService.postMessage(thread.id, { content: draft }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (resp) => {
        // Always update the rail summary so background threads still reflect server state.
        this.state.activeThreads.update((list) => list.map((x) => x.id === resp.thread.id ? { ...x, ...resp.thread } : x));
        // But only mutate activeThread / awaitingReply if the user is still viewing the originating thread.
        if (this.state.activeThread()?.id !== sendingThreadId) return;
        this.awaitingReply.set(false);
        this.state.activeThread.update((t) => {
          if (!t || t.id !== sendingThreadId) return t;
          const base = t.messages.filter((m) => m !== optimistic);
          return { ...t, title: resp.thread.title, messages: [...base, resp.userMessage, resp.assistantMessage], messageCount: resp.thread.messageCount, lastMessageAt: resp.thread.lastMessageAt ?? resp.assistantMessage.createdAt };
        });
      },
      error: (err) => {
        const status = err?.status as number | undefined;
        this.messageService.add({ severity: 'error', summary: status === 409 ? 'This thread is archived' : 'Failed to send message' });
        // Only roll back optimistic state / clear awaitingReply / reconcile if still on the originating thread.
        if (this.state.activeThread()?.id !== sendingThreadId) return;
        this.awaitingReply.set(false);
        this.state.activeThread.update((t) => t && t.id === sendingThreadId ? { ...t, messages: t.messages.filter((m) => m !== optimistic) } : t);
        if (status === 409) this.reconcileArchivedThread(sendingThreadId);
      },
    });
  }

  private reconcileArchivedThread(threadId: string): void {
    // Server rejected send with 409 → thread was archived concurrently. Mirror that locally
    // so the rail moves it into archived, activeThread.status becomes Archived, and the
    // composer disables itself via the [disabled] bindings.
    this.state.activeThread.update((curr) => curr && curr.id === threadId ? { ...curr, status: 'Archived' } : curr);
    const moved = this.state.activeThreads().find((t) => t.id === threadId);
    this.state.activeThreads.update((list) => list.filter((t) => t.id !== threadId));
    if (moved && !this.state.archivedThreads().some((t) => t.id === threadId)) {
      this.state.archivedThreads.update((list) => [{ ...moved, status: 'Archived' }, ...list]);
    }
  }

  private refresh(): void {
    this.chatService.list('Active').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: (l) => this.state.activeThreads.set(l) });
    this.chatService.list('Archived').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: (l) => this.state.archivedThreads.set(l) });
  }

  private groupByDate(threads: ChatThreadSummary[]): ThreadGroup[] {
    const sorted = [...threads].sort((a, b) => this.recency(b) - this.recency(a));
    const today = this.startOfDay(new Date());
    const yesterday = today - 86400000;
    const startOfWeek = this.startOfWeek(new Date());

    const today_: ChatThreadSummary[] = [];
    const yesterday_: ChatThreadSummary[] = [];
    const thisWeek_: ChatThreadSummary[] = [];
    const older_: ChatThreadSummary[] = [];

    for (const t of sorted) {
      const r = this.recency(t);
      if (r >= today) today_.push(t);
      else if (r >= yesterday) yesterday_.push(t);
      else if (r >= startOfWeek) thisWeek_.push(t);
      else older_.push(t);
    }

    return [
      { label: 'Today', threads: today_ },
      { label: 'Yesterday', threads: yesterday_ },
      { label: 'This Week', threads: thisWeek_ },
      { label: 'Older', threads: older_ },
    ];
  }

  private recency(t: ChatThreadSummary): number {
    return t.lastMessageAt ? Date.parse(t.lastMessageAt) : Date.parse(t.createdAt);
  }
  private startOfDay(d: Date): number {
    const c = new Date(d);
    c.setHours(0, 0, 0, 0);
    return c.getTime();
  }
  private startOfWeek(d: Date): number {
    const c = new Date(d);
    c.setDate(c.getDate() - c.getDay());
    c.setHours(0, 0, 0, 0);
    return c.getTime();
  }
}
