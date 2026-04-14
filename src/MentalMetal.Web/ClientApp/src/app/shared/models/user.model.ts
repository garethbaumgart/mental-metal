export interface UserProfile {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
  timezone: string;
  preferences: UserPreferences;
  hasAiProvider: boolean;
  hasPassword: boolean;
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

export interface PasswordAuthResponse {
  accessToken: string;
  user: UserProfile;
}

export interface LoginWithPasswordRequest {
  email: string;
  password: string;
}

export interface RegisterWithPasswordRequest {
  email: string;
  password: string;
  name: string;
}

export interface SetPasswordRequest {
  newPassword: string;
}
