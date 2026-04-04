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
    }

    nav a {
      transition: background-color 0.15s ease;
    }
  `],
  template: `
    <div class="flex items-center gap-3 p-5 border-b" style="border-color: var(--p-surface-200)">
      <i class="pi pi-bolt text-xl" style="color: var(--p-primary-color)"></i>
      <span class="text-lg font-semibold" style="color: var(--p-text-color)">Mental Metal</span>
    </div>

    <nav class="flex-1 p-3 flex flex-col gap-1">
      <a routerLink="/dashboard" routerLinkActive="font-semibold"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         style="color: var(--p-text-color)"
         (click)="navClick.emit()">
        <i class="pi pi-home"></i>
        <span>Dashboard</span>
      </a>
      <a routerLink="/capture" routerLinkActive="font-semibold"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         style="color: var(--p-text-color)"
         (click)="navClick.emit()">
        <i class="pi pi-pencil"></i>
        <span>Capture</span>
      </a>
      <a routerLink="/people" routerLinkActive="font-semibold"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         style="color: var(--p-text-color)"
         (click)="navClick.emit()">
        <i class="pi pi-users"></i>
        <span>People</span>
      </a>
      <a routerLink="/initiatives" routerLinkActive="font-semibold"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         style="color: var(--p-text-color)"
         (click)="navClick.emit()">
        <i class="pi pi-flag"></i>
        <span>Initiatives</span>
      </a>
      <a routerLink="/queue" routerLinkActive="font-semibold"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         style="color: var(--p-text-color)"
         (click)="navClick.emit()">
        <i class="pi pi-list-check"></i>
        <span>My Queue</span>
      </a>
    </nav>

    <div class="p-3 border-t" style="border-color: var(--p-surface-200)">
      <button
        class="flex items-center gap-3 px-3 py-2 rounded-md text-sm w-full"
        style="color: var(--p-text-color)"
        (click)="themeService.toggle()"
        [attr.aria-label]="themeService.isDark() ? 'Switch to light mode' : 'Switch to dark mode'">
        <i [class]="themeService.isDark() ? 'pi pi-sun' : 'pi pi-moon'"></i>
        <span>{{ themeService.isDark() ? 'Light mode' : 'Dark mode' }}</span>
      </button>
    </div>
  `,
})
export class SidebarComponent {
  protected readonly themeService = inject(ThemeService);
  readonly navClick = output<void>();
}
