import { Injectable, signal } from '@angular/core';
import { ChatThread, ChatThreadSummary } from '../models/chat-thread.model';

/**
 * Shared signal state between the global chat slide-over (in the app shell) and the
 * full-page route at /chat. Keeping the active-thread reference in one place lets the
 * slide-over hand control off to the full page (and vice versa) without an extra fetch.
 */
@Injectable({ providedIn: 'root' })
export class GlobalChatStateService {
  readonly activeThreads = signal<ChatThreadSummary[]>([]);
  readonly archivedThreads = signal<ChatThreadSummary[]>([]);
  readonly activeThread = signal<ChatThread | null>(null);
  readonly slideOverOpen = signal(false);

  openSlideOver(): void { this.slideOverOpen.set(true); }
  closeSlideOver(): void { this.slideOverOpen.set(false); }
}
