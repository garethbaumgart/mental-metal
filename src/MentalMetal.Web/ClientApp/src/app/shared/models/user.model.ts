export interface UserProfile {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
  timezone: string;
  preferences: UserPreferences;
  createdAt: string;
  lastLoginAt: string;
}

export interface UserPreferences {
  theme: string;
  notificationsEnabled: boolean;
  briefingTime: string;
  // Required so callers cannot accidentally omit the flag and reset the persisted preference.
  livingBriefAutoApply: boolean;
}

export interface UpdateProfileRequest {
  name: string;
  avatarUrl: string | null;
  timezone: string;
}

export interface UpdatePreferencesRequest {
  theme: string;
  notificationsEnabled: boolean;
  briefingTime: string;
  // Required: omitting this on an update would silently reset the persisted preference.
  livingBriefAutoApply: boolean;
}

export interface AuthTokenResponse {
  accessToken: string;
}
