import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ThemeService } from '../services/theme.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive],
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      color: var(--p-text-color);
    }

    .sidebar-border {
      border-color: var(--p-content-border-color);
    }

    .sidebar-brand {
      color: var(--p-primary-color);
    }

    nav a, nav button {
      color: var(--p-text-color);
      transition: background-color 0.15s ease, border-color 0.15s ease;
    }

    .sidebar-link-active {
      background-color: var(--p-primary-50);
      border-left: 3px solid var(--p-primary-color);
    }

    :host-context(.dark) .sidebar-link-active {
      background-color: var(--p-primary-950);
    }

    .theme-toggle {
      color: var(--p-text-color);
    }
  `],
  template: `
    <div class="flex items-center gap-3 p-5 border-b sidebar-border">
      <i class="pi pi-bolt text-xl sidebar-brand"></i>
      <span class="text-lg font-semibold">Mental Metal</span>
    </div>

    <nav class="flex-1 overflow-y-auto p-3 flex flex-col gap-1" aria-label="Primary">
      <a routerLink="/dashboard" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-home"></i>
        <span>Dashboard</span>
      </a>
      <a routerLink="/capture" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-pencil"></i>
        <span>Captures</span>
      </a>
      <a routerLink="/people" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-users"></i>
        <span>People</span>
      </a>
      <a routerLink="/commitments" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-check-square"></i>
        <span>Commitments</span>
      </a>
      <a routerLink="/initiatives" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-flag"></i>
        <span>Initiatives</span>
      </a>
    </nav>

    <nav class="p-3 border-t sidebar-border flex flex-col gap-1" aria-label="Secondary">
      <a routerLink="/settings" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-cog"></i>
        <span>Settings</span>
      </a>
      <button type="button"
        class="flex items-center gap-3 px-3 py-2 rounded-md text-sm w-full theme-toggle"
        (click)="themeService.toggle()"
        [attr.aria-label]="themeService.isDark() ? 'Switch to light mode' : 'Switch to dark mode'">
        <i [class]="themeService.isDark() ? 'pi pi-sun' : 'pi pi-moon'"></i>
        <span>{{ themeService.isDark() ? 'Light mode' : 'Dark mode' }}</span>
      </button>
    </nav>
  `,
})
export class SidebarComponent {
  protected readonly themeService = inject(ThemeService);
  readonly navClick = output<void>();
}
