import { ChangeDetectionStrategy, Component, computed, effect, inject, output, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
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

    .sidebar-group-header {
      color: var(--p-text-muted-color, var(--p-text-color));
    }

    .sidebar-child {
      padding-left: 2.5rem;
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

    <nav class="flex-1 overflow-y-auto p-3 flex flex-col gap-1">
      <!-- Primary verbs -->
      <a routerLink="/dashboard" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-home"></i>
        <span>Today</span>
      </a>
      <a routerLink="/chat" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-sparkles"></i>
        <span>Chat</span>
      </a>
      <a routerLink="/capture" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-pencil"></i>
        <span>Capture</span>
      </a>
      <a routerLink="/people" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-users"></i>
        <span>People</span>
      </a>
      <a routerLink="/initiatives" routerLinkActive="font-semibold sidebar-link-active"
         class="flex items-center gap-3 px-3 py-2 rounded-md text-sm"
         (click)="navClick.emit()">
        <i class="pi pi-flag"></i>
        <span>Initiatives</span>
      </a>

      <!-- Work group (queue, commitments, delegations, nudges, close-out) -->
      <button type="button"
        class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-group-header"
        [attr.aria-expanded]="workOpen()"
        aria-controls="work-group"
        (click)="workOpen.set(!workOpen())">
        <i class="pi pi-check-square"></i>
        <span class="flex-1 text-left">Work</span>
        <i [class]="workOpen() ? 'pi pi-chevron-down' : 'pi pi-chevron-right'" class="text-xs"></i>
      </button>
      @if (workOpen()) {
        <div id="work-group" class="flex flex-col gap-1">
          <a routerLink="/my-queue" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-list-check"></i>
            <span>My Queue</span>
          </a>
          <a routerLink="/commitments" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-check-square"></i>
            <span>Commitments</span>
          </a>
          <a routerLink="/delegations" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-send"></i>
            <span>Delegations</span>
          </a>
          <a routerLink="/nudges" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-clock"></i>
            <span>Nudges</span>
          </a>
          <a routerLink="/close-out" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-inbox"></i>
            <span>Close-out</span>
          </a>
        </div>
      }

      <!-- More group (1:1s, observations, goals, interviews, weekly briefing) -->
      <button type="button"
        class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-group-header"
        [attr.aria-expanded]="moreOpen()"
        aria-controls="more-group"
        (click)="moreOpen.set(!moreOpen())">
        <i class="pi pi-ellipsis-h"></i>
        <span class="flex-1 text-left">More</span>
        <i [class]="moreOpen() ? 'pi pi-chevron-down' : 'pi pi-chevron-right'" class="text-xs"></i>
      </button>
      @if (moreOpen()) {
        <div id="more-group" class="flex flex-col gap-1">
          <a routerLink="/one-on-ones" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-comments"></i>
            <span>1:1s</span>
          </a>
          <a routerLink="/observations" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-eye"></i>
            <span>Observations</span>
          </a>
          <a routerLink="/goals" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-flag"></i>
            <span>Goals</span>
          </a>
          <a routerLink="/interviews" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-briefcase"></i>
            <span>Interviews</span>
          </a>
          <a routerLink="/briefings/weekly" routerLinkActive="font-semibold sidebar-link-active"
             class="flex items-center gap-3 px-3 py-2 rounded-md text-sm sidebar-child"
             (click)="navClick.emit()">
            <i class="pi pi-calendar"></i>
            <span>Weekly Briefing</span>
          </a>
        </div>
      }
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
  private readonly router = inject(Router);
  protected readonly themeService = inject(ThemeService);
  readonly navClick = output<void>();

  /** Current URL, updated on every NavigationEnd. */
  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  private readonly workActive = computed(() =>
    /^\/(my-queue|commitments|delegations|nudges|close-out)(\/|$)/.test(this.currentUrl()),
  );

  private readonly moreActive = computed(() =>
    /^\/(one-on-ones|observations|goals|interviews|briefings)(\/|$)/.test(this.currentUrl()),
  );

  protected readonly workOpen = signal(false);
  protected readonly moreOpen = signal(false);

  constructor() {
    // Auto-expand the group when the current route is inside it, so direct
    // navigation (or a page refresh) shows the active child highlighted.
    effect(() => {
      if (this.workActive()) this.workOpen.set(true);
      if (this.moreActive()) this.moreOpen.set(true);
    });
  }
}
