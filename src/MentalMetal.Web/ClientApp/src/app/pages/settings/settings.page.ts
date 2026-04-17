import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../shared/services/auth.service';
import { UserService } from '../../shared/services/user.service';
import { ThemeService } from '../../shared/services/theme.service';
import { AiProviderSettingsComponent } from './ai-provider-settings.component';
import { PasswordSettingsComponent } from './password-settings.component';
import { PersonalAccessTokensComponent } from './personal-access-tokens.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    ToggleSwitchModule,
    ToastModule,
    AiProviderSettingsComponent,
    PasswordSettingsComponent,
    PersonalAccessTokensComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="p-6 max-w-2xl mx-auto flex flex-col gap-8">
      <h1 class="text-2xl font-bold">Settings</h1>

      <!-- Profile Section -->
      <section class="flex flex-col gap-4">
        <h2 class="text-xl font-semibold">Profile</h2>

        <div class="flex flex-col gap-2">
          <label for="email" class="text-sm font-medium text-muted-color">Email</label>
          <input pInputText id="email" [value]="email()" disabled class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="name" class="text-sm font-medium text-muted-color">Name</label>
          <input pInputText id="name" [(ngModel)]="name" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="timezone" class="text-sm font-medium text-muted-color">Timezone</label>
          <p-select
            id="timezone"
            [options]="timezones"
            [(ngModel)]="timezone"
            [filter]="true"
            filterBy="label"
            placeholder="Select timezone"
            class="w-full"
          />
        </div>

        <p-button
          label="Save Profile"
          (onClick)="saveProfile()"
          [loading]="savingProfile()"
        />
      </section>

      <!-- Preferences Section -->
      <section class="flex flex-col gap-4">
        <h2 class="text-xl font-semibold">Preferences</h2>

        <div class="flex items-center justify-between">
          <label for="theme" class="text-sm font-medium">Dark Mode</label>
          <p-toggleSwitch
            id="theme"
            [ngModel]="darkMode()"
            (ngModelChange)="onThemeChange($event)"
          />
        </div>

        <div class="flex items-center justify-between">
          <label for="notifications" class="text-sm font-medium">Notifications</label>
          <p-toggleSwitch id="notifications" [(ngModel)]="notificationsEnabled" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="briefingTime" class="text-sm font-medium text-muted-color">Briefing Time</label>
          <input pInputText id="briefingTime" type="time" [(ngModel)]="briefingTime" class="w-full" />
        </div>

        <div class="flex items-center justify-between">
          <label for="livingBriefAutoApply" class="text-sm font-medium">Auto-apply AI brief updates</label>
          <p-toggleSwitch id="livingBriefAutoApply" [(ngModel)]="livingBriefAutoApply" />
        </div>

        <p-button
          label="Save Preferences"
          (onClick)="savePreferences()"
          [loading]="savingPreferences()"
        />
      </section>

      <!-- Password Section -->
      <app-password-settings />

      <!-- Personal Access Tokens Section -->
      <app-personal-access-tokens />

      <!-- AI Provider Section -->
      <app-ai-provider-settings />
    </div>
  `,
})
export class SettingsPage implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly themeService = inject(ThemeService);
  private readonly messageService = inject(MessageService);

  readonly email = signal('');
  readonly savingProfile = signal(false);
  readonly savingPreferences = signal(false);

  // Mirrors the live theme state so the toggle always reflects the actual
  // theme, not whatever was saved in the user's preferences last.
  protected readonly darkMode = this.themeService.isDark;

  protected name = '';
  protected timezone = '';
  protected notificationsEnabled = true;
  protected briefingTime = '08:00';
  protected livingBriefAutoApply = false;

  protected readonly timezones = Intl.supportedValuesOf('timeZone').map((tz) => ({
    label: tz,
    value: tz,
  }));

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.email.set(user.email);
      this.name = user.name;
      this.timezone = user.timezone;
      this.notificationsEnabled = user.preferences.notificationsEnabled;
      this.briefingTime = user.preferences.briefingTime;
      this.livingBriefAutoApply = user.preferences.livingBriefAutoApply;
    }
  }

  onThemeChange(dark: boolean): void {
    if (this.themeService.isDark() !== dark) {
      this.themeService.toggle();
    }
  }

  saveProfile(): void {
    this.savingProfile.set(true);
    this.userService
      .updateProfile({
        name: this.name,
        avatarUrl: this.authService.currentUser()?.avatarUrl ?? null,
        timezone: this.timezone,
      })
      .subscribe({
        next: () => {
          this.savingProfile.set(false);
          this.authService.loadCurrentUser();
          this.messageService.add({
            severity: 'success',
            summary: 'Profile updated',
          });
        },
        error: () => {
          this.savingProfile.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Failed to update profile',
          });
        },
      });
  }

  savePreferences(): void {
    this.savingPreferences.set(true);
    this.userService
      .updatePreferences({
        theme: this.darkMode() ? 'Dark' : 'Light',
        notificationsEnabled: this.notificationsEnabled,
        briefingTime: this.briefingTime,
        livingBriefAutoApply: this.livingBriefAutoApply,
      })
      .subscribe({
        next: () => {
          this.savingPreferences.set(false);
          this.authService.loadCurrentUser();
          this.messageService.add({
            severity: 'success',
            summary: 'Preferences updated',
          });
        },
        error: () => {
          this.savingPreferences.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Failed to update preferences',
          });
        },
      });
  }
}
