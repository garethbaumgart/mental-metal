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
export class InitiativeChatService {
  private readonly http = inject(HttpClient);

  private base(initiativeId: string) {
    return `/api/initiatives/${initiativeId}/chat/threads`;
  }

  start(initiativeId: string): Observable<ChatThread> {
    return this.http.post<ChatThread>(this.base(initiativeId), {});
  }

  list(initiativeId: string, status?: ChatThreadStatus): Observable<ChatThreadSummary[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ChatThreadSummary[]>(this.base(initiativeId), { params });
  }

  get(initiativeId: string, threadId: string): Observable<ChatThread> {
    return this.http.get<ChatThread>(`${this.base(initiativeId)}/${threadId}`);
  }

  rename(initiativeId: string, threadId: string, request: RenameChatThreadRequest): Observable<ChatThread> {
    return this.http.put<ChatThread>(`${this.base(initiativeId)}/${threadId}`, request);
  }

  postMessage(initiativeId: string, threadId: string, request: PostChatMessageRequest): Observable<PostChatMessageResponse> {
    return this.http.post<PostChatMessageResponse>(`${this.base(initiativeId)}/${threadId}/messages`, request);
  }

  archive(initiativeId: string, threadId: string): Observable<ChatThread> {
    return this.http.post<ChatThread>(`${this.base(initiativeId)}/${threadId}/archive`, {});
  }

  unarchive(initiativeId: string, threadId: string): Observable<ChatThread> {
    return this.http.post<ChatThread>(`${this.base(initiativeId)}/${threadId}/unarchive`, {});
  }
}
