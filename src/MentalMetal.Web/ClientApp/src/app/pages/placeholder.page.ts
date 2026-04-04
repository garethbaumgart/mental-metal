import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Component({
  selector: 'app-placeholder',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center py-16">
      <i class="pi pi-cog text-4xl mb-4" style="color: var(--p-text-muted-color)"></i>
      <p class="text-lg font-semibold" style="color: var(--p-text-color)">{{ title() }}</p>
      <p class="text-sm mt-2" style="color: var(--p-text-muted-color)">Coming soon</p>
    </div>
  `,
})
export class PlaceholderPage {
  private readonly route = inject(ActivatedRoute);
  readonly title = toSignal(this.route.data.pipe(map(d => d['title'] ?? 'Page')), { initialValue: 'Page' });
}
