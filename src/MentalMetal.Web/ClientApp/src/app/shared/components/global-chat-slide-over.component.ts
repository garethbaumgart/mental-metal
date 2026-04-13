import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal, viewChild, ElementRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { SkeletonModule } from 'primeng/skeleton';
import { MessageModule } from 'primeng/message';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { GlobalChatService } from '../services/global-chat.service';
import { GlobalChatStateService } from '../services/global-chat-state.service';
import { ChatMessage, ChatThread } from '../models/chat-thread.model';
import { SourceReferenceChipComponent } from '../../pages/initiatives/chat/source-reference-chip.component';

/**
 * Quick-question slide-over panel rendered into the app shell. On open it preselects the
 * user's most-recent active thread (or fires a Start request when none exist on the very
 * first message). Shares the active-thread signal with the full-page route so navigating
 * to `/chat` keeps the user where they were.
 */
@Component({
  selector: 'app-global-chat-slide-over',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DrawerModule,
    ButtonModule,
    TextareaModule,
    SkeletonModule,
    MessageModule,
    ToastModule,
    SourceReferenceChipComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <p-drawer
      [visible]="state.slideOverOpen()"
      (visibleChange)="onVisibleChange($event)"
      position="right"
      styleClass="!w-full md:!w-[480px]"
      header="Chat"
    >
      <div class="flex flex-col h-full gap-3">
        <div class="flex items-center justify-between">
          <span class="text-sm text-muted-color truncate">
            {{ state.activeThread()?.title || 'New thread' }}
          </span>
          <p-button
            label="Open in full view"
            icon="pi pi-external-link"
            [text]="true"
            size="small"
            (onClick)="openFullView()"
          />
        </div>

        <div #messageList class="flex-1 overflow-y-auto flex flex-col gap-2 pr-1">
          @if (state.activeThread()) {
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
          } @else {
            <div class="flex items-center justify-center h-full text-sm text-muted-color">
              Ask anything about your work.
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
            [disabled]="awaitingReply() || composerDisabled()"
            (keydown.enter)="onEnter($event)"
            placeholder="Ask anything..."
          ></textarea>
          <p-button
            label="Send"
            icon="pi pi-send"
            [disabled]="!composer().trim() || composerDisabled()"
            [loading]="awaitingReply()"
            (onClick)="send()"
          />
        </div>
      </div>
    </p-drawer>
  `,
  styles: [`
    .message-bubble { border: 1px solid var(--p-surface-200); }
    .user-bubble { background: var(--p-primary-50); }
    .assistant-bubble { background: var(--p-surface-50); }
  `],
})
export class GlobalChatSlideOverComponent {
  private readonly chatService = inject(GlobalChatService);
  protected readonly state = inject(GlobalChatStateService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly messageList = viewChild<ElementRef<HTMLDivElement>>('messageList');

  protected readonly composer = signal('');
  protected readonly awaitingReply = signal(false);
  protected readonly composerDisabled = computed(() => {
    const status = this.state.activeThread()?.status;
    // Disable when a thread exists but isn't Active. When no thread exists yet the
    // drawer lazily starts one on first send, so keep the composer enabled.
    return status !== undefined && status !== 'Active';
  });

  // Monotonic token so late preload responses can't clobber a newer selection.
  private preloadRequestToken = 0;

  constructor() {
    effect(() => {
      // First open with no active thread → load the most-recent or create a fresh one.
      if (this.state.slideOverOpen() && !this.state.activeThread()) {
        const requestToken = ++this.preloadRequestToken;
        this.chatService.list('Active').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (list) => {
            if (requestToken !== this.preloadRequestToken) return;
            this.state.activeThreads.set(list);
            // Only preselect if the user hasn't already selected or started something.
            if (this.state.activeThread()) return;
            const mostRecent = list[0];
            if (mostRecent) {
              this.chatService.get(mostRecent.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
                next: (t) => {
                  if (requestToken !== this.preloadRequestToken) return;
                  if (this.state.activeThread()) return;
                  this.state.activeThread.set(t);
                },
              });
            }
            // Otherwise leave activeThread null — start happens lazily on first send.
          },
        });
      }
    });

    effect(() => {
      // Auto-scroll on message list updates.
      if (this.state.activeThread()) {
        queueMicrotask(() => {
          const el = this.messageList()?.nativeElement;
          if (el) el.scrollTop = el.scrollHeight;
        });
      }
    });
  }

  protected onVisibleChange(open: boolean): void {
    if (!open) this.state.closeSlideOver();
  }

  protected openFullView(): void {
    this.state.closeSlideOver();
    this.router.navigate(['/chat']);
  }

  protected onEnter(event: Event): void {
    const ke = event as KeyboardEvent;
    if (ke.shiftKey) return;
    ke.preventDefault();
    this.send();
  }

  protected send(): void {
    const draft = this.composer().trim();
    if (!draft) return;

    const proceed = (thread: ChatThread) => {
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

      this.chatService.postMessage(thread.id, { content: draft }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (resp) => {
          this.awaitingReply.set(false);
          this.state.activeThread.update((t) => {
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
        },
        error: (err) => {
          this.awaitingReply.set(false);
          const status = err?.status as number | undefined;
          this.messageService.add({
            severity: 'error',
            summary: status === 409 ? 'This thread is archived' : 'Failed to send message',
          });
          this.state.activeThread.update((t) => t ? { ...t, messages: t.messages.filter((m) => m !== optimistic) } : t);
          if (status === 409) {
            // Server archived this thread concurrently: mirror that locally so the composer
            // disables itself via [disabled]="composerDisabled()" and the rail is consistent.
            this.state.activeThread.update((t) => t && t.id === thread.id ? { ...t, status: 'Archived' } : t);
            const moved = this.state.activeThreads().find((t) => t.id === thread.id);
            this.state.activeThreads.update((list) => list.filter((t) => t.id !== thread.id));
            if (moved && !this.state.archivedThreads().some((t) => t.id === thread.id)) {
              this.state.archivedThreads.update((list) => [{ ...moved, status: 'Archived' }, ...list]);
            }
          }
        },
      });
    };

    const current = this.state.activeThread();
    if (current) {
      proceed(current);
    } else {
      // Lazy thread creation on first send.
      this.chatService.start().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (thread) => {
          this.state.activeThread.set(thread);
          proceed(thread);
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Failed to start thread' });
        },
      });
    }
  }
}
