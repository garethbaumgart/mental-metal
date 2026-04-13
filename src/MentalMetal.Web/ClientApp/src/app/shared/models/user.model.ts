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
  livingBriefAutoApply?: boolean;
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
  livingBriefAutoApply?: boolean;
}

export interface AuthTokenResponse {
  accessToken: string;
}
