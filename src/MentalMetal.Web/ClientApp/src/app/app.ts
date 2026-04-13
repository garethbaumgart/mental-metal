import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs/operators';
import { SidebarComponent } from './shared/components/sidebar.component';
import { GlobalChatLauncherComponent } from './shared/components/global-chat-launcher.component';
import { GlobalChatSlideOverComponent } from './shared/components/global-chat-slide-over.component';
import { AuthService } from './shared/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SidebarComponent, GlobalChatLauncherComponent, GlobalChatSlideOverComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  // Inject AuthService eagerly so its constructor runs during bootstrap —
  // before Angular's router processes the initial navigation and strips the
  // URL fragment via history.replaceState. This ensures extractTokenFromHash()
  // sees the access token returned in the OAuth callback redirect (#75 Bug 5).
  private readonly _auth = inject(AuthService);

  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);

  protected readonly sidebarOpen = signal(false);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  protected readonly isLoginRoute = computed(() => this.currentUrl().startsWith('/login'));

  protected readonly pageTitle = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.resolveTitle()),
      startWith(this.resolveTitle()),
    ),
    { initialValue: this.resolveTitle() },
  );

  private resolveTitle(): string {
    let route = this.activatedRoute;
    while (route.firstChild) {
      route = route.firstChild;
    }
    const title = route.snapshot.data?.['title'];
    return typeof title === 'string' ? title : '';
  }
}
