import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectButtonModule } from 'primeng/selectbutton';
import { MyQueueItemComponent } from './my-queue-item.component';
import { MyQueueService } from './my-queue.service';
import { QueueItem, QueueItemType, QueueScope } from './my-queue.models';

interface ScopeOption { label: string; value: QueueScope; }
interface TypeOption { label: string; value: QueueItemType; }

@Component({
  selector: 'app-my-queue-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, SelectButtonModule, MyQueueItemComponent],
  template: `
    <div class="flex flex-col gap-6 max-w-3xl mx-auto">
      <header class="flex flex-col gap-2">
        <h1 class="text-2xl font-bold">My Queue</h1>
        <p class="text-sm text-muted-color">
          Everything pulling on your attention right now, ranked by priority.
        </p>
      </header>

      <section class="flex flex-col gap-3 p-4 rounded-md bg-surface-50">
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p-selectButton
            [options]="scopeOptions"
            [(ngModel)]="scope"
            optionLabel="label"
            optionValue="value"
            (ngModelChange)="reload()"
          />
          <p-selectButton
            [options]="typeOptions"
            [(ngModel)]="itemTypes"
            optionLabel="label"
            optionValue="value"
            [multiple]="true"
            (ngModelChange)="reload()"
          />
        </div>
        @if (counts(); as c) {
          <div class="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-color">
            <span>Total {{ c.total }}</span>
            <span>Overdue {{ c.overdue }}</span>
            <span>Due soon {{ c.dueSoon }}</span>
            <span>Stale captures {{ c.staleCaptures }}</span>
            <span>Stale delegations {{ c.staleDelegations }}</span>
          </div>
        }
      </section>

      @if (service.loading()) {
        <div class="flex justify-center p-8">
          <i class="pi pi-spinner pi-spin text-2xl"></i>
        </div>
      } @else if (service.error()) {
        <div class="flex flex-col items-center gap-3 p-8 bg-surface-50 rounded-md">
          <p class="text-sm text-muted-color">{{ service.error() }}</p>
          <p-button label="Retry" icon="pi pi-refresh" (onClick)="reload()" />
        </div>
      } @else if (items().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12 bg-surface-50 rounded-md">
          <i class="pi pi-check-circle text-4xl text-primary"></i>
          <p class="text-sm text-muted-color">All clear — your queue is empty.</p>
        </div>
      } @else {
        <div class="flex flex-col gap-3">
          @for (item of items(); track item.id) {
            <app-my-queue-item [item]="item" (delegate)="onDelegate($event)" />
          }
        </div>
      }
    </div>
  `,
})
export class MyQueuePageComponent implements OnInit {
  protected readonly service = inject(MyQueueService);
  private readonly router = inject(Router);

  protected readonly scope = signal<QueueScope>('All');
  protected readonly itemTypes = signal<QueueItemType[]>([]);

  protected readonly items = computed(() => this.service.response()?.items ?? []);
  protected readonly counts = computed(() => this.service.response()?.counts ?? null);

  protected readonly scopeOptions: ScopeOption[] = [
    { label: 'All', value: 'All' },
    { label: 'Overdue', value: 'Overdue' },
    { label: 'Today', value: 'Today' },
    { label: 'This Week', value: 'ThisWeek' },
  ];

  protected readonly typeOptions: TypeOption[] = [
    { label: 'Commitments', value: 'Commitment' },
    { label: 'Delegations', value: 'Delegation' },
    { label: 'Captures', value: 'Capture' },
  ];

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.service.load({
      scope: this.scope(),
      itemType: this.itemTypes().length > 0 ? this.itemTypes() : undefined,
    });
  }

  protected onDelegate(item: QueueItem): void {
    const queryParams: Record<string, string> = {
      description: item.title,
      sourceCommitmentId: item.id,
    };
    if (item.personId) queryParams['personId'] = item.personId;
    if (item.initiativeId) queryParams['initiativeId'] = item.initiativeId;

    this.router.navigate(['/delegations'], { queryParams });
  }
}
