import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  UpdatePreferencesRequest,
  UpdateProfileRequest,
  UserProfile,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>('/api/users/me');
  }

  updateProfile(request: UpdateProfileRequest): Observable<void> {
    return this.http.put<void>('/api/users/me/profile', request);
  }

  updatePreferences(request: UpdatePreferencesRequest): Observable<void> {
    return this.http.put<void>('/api/users/me/preferences', request);
  }
}
