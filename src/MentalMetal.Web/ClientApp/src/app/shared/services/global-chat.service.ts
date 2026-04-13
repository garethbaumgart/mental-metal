import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ChatThread,
  ChatThreadStatus,
  ChatThreadSummary,
  PostChatMessageRequest,
  PostChatMessageResponse,
  RenameChatThreadRequest,
} from '../models/chat-thread.model';

@Injectable({ providedIn: 'root' })
export class GlobalChatService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/chat/threads';

  start(): Observable<ChatThread> {
    return this.http.post<ChatThread>(this.base, {});
  }

  list(status?: ChatThreadStatus): Observable<ChatThreadSummary[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ChatThreadSummary[]>(this.base, { params });
  }

  get(threadId: string): Observable<ChatThread> {
    return this.http.get<ChatThread>(`${this.base}/${threadId}`);
  }

  rename(threadId: string, request: RenameChatThreadRequest): Observable<ChatThread> {
    return this.http.put<ChatThread>(`${this.base}/${threadId}`, request);
  }

  postMessage(threadId: string, request: PostChatMessageRequest): Observable<PostChatMessageResponse> {
    return this.http.post<PostChatMessageResponse>(`${this.base}/${threadId}/messages`, request);
  }

  archive(threadId: string): Observable<ChatThread> {
    return this.http.post<ChatThread>(`${this.base}/${threadId}/archive`, {});
  }

  unarchive(threadId: string): Observable<ChatThread> {
    return this.http.post<ChatThread>(`${this.base}/${threadId}/unarchive`, {});
  }
}
