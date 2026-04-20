import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs/operators';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { SidebarComponent } from './shared/components/sidebar.component';
import { QuickCaptureFabComponent } from './shared/components/quick-capture-fab.component';
import { QuickCaptureShortcutDirective } from './shared/directives/quick-capture-shortcut.directive';
import { QuickCaptureDialogComponent } from './pages/captures/quick-capture-dialog/quick-capture-dialog.component';
import { QuickCaptureUiService } from './shared/services/quick-capture-ui.service';
import { CaptureProcessingTrackerService } from './shared/services/capture-processing-tracker.service';
import { Capture } from './shared/models/capture.model';
import { AuthService } from './shared/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    ToastModule,
    SidebarComponent,
    QuickCaptureFabComponent,
    QuickCaptureShortcutDirective,
    QuickCaptureDialogComponent,
  ],
  providers: [MessageService],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  // AuthService is injected for its constructor side effect: it calls
  // extractTokenFromHash() during bootstrap, before Angular's router processes
  // the initial navigation and strips the URL fragment via history.replaceState.
  // The reference is intentionally unused — holding it keeps the DI instance
  // alive and guarantees construction order (#75 Bug 5).
  protected readonly eagerAuthInit = inject(AuthService);

  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  protected readonly quickCapture = inject(QuickCaptureUiService);
  private readonly processingTracker = inject(CaptureProcessingTrackerService);
  private readonly messageService = inject(MessageService);

  protected readonly sidebarOpen = signal(false);

  ngOnInit(): void {
    this.processingTracker.completions$.subscribe((completion) => {
      const title = completion.capture.title
        || completion.capture.rawContent.substring(0, 40) + (completion.capture.rawContent.length > 40 ? '...' : '');

      if (completion.status === 'Processed') {
        const extraction = completion.capture.aiExtraction;
        const parts: string[] = [];
        if (extraction?.commitments?.length) parts.push(`${extraction.commitments.length} commitment(s)`);
        if (extraction?.peopleMentioned?.length) parts.push(`${extraction.peopleMentioned.length} people`);
        const detail = parts.length > 0 ? parts.join(' \u00b7 ') : 'Processing complete';

        this.messageService.add({
          severity: 'success',
          summary: `"${title}" ready`,
          detail,
          life: 8000,
        });
      } else {
        this.messageService.add({
          severity: 'error',
          summary: `"${title}" failed`,
          detail: completion.capture.failureReason ?? 'Extraction failed',
          life: 10000,
        });
      }
    });
  }

  /** Shell-level callback wired to the single global Quick Capture dialog. */
  protected onQuickCaptureCreated(capture: Capture): void {
    this.quickCapture.notifyCreated(capture);
  }

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
