import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Clipboard } from '@angular/cdk/clipboard';
import {
  PersonalAccessTokensService,
  PatSummary,
} from '../../shared/services/personal-access-tokens.service';

@Component({
  selector: 'app-personal-access-tokens',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TableModule,
    TagModule,
    TooltipModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  template: `
    <p-confirmDialog />
    <section class="flex flex-col gap-4">
      <div class="flex items-center justify-between">
        <h2 class="text-xl font-semibold">Personal Access Tokens</h2>
        <p-button
          label="Generate Token"
          icon="pi pi-plus"
          size="small"
          (onClick)="showGenerateDialog.set(true)"
        />
      </div>

      <p class="text-sm text-muted-color">
        Tokens allow external tools (like a bookmarklet) to import captures on your behalf.
      </p>

      @if (tokens().length > 0) {
        <p-table [value]="tokens()" [tableStyle]="{ 'min-width': '100%' }">
          <ng-template #header>
            <tr>
              <th>Name</th>
              <th>Scopes</th>
              <th>Created</th>
              <th>Last Used</th>
              <th>Status</th>
              <th></th>
            </tr>
          </ng-template>
          <ng-template #body let-t>
            <tr>
              <td>{{ t.name }}</td>
              <td>
                @for (scope of t.scopes; track scope) {
                  <p-tag [value]="scope" severity="info" class="mr-1" />
                }
              </td>
              <td>{{ t.createdAt | date:'short' }}</td>
              <td>{{ t.lastUsedAt ? (t.lastUsedAt | date:'short') : 'Never' }}</td>
              <td>
                @if (t.revokedAt) {
                  <p-tag value="Revoked" severity="danger" />
                } @else {
                  <p-tag value="Active" severity="success" />
                }
              </td>
              <td>
                @if (!t.revokedAt) {
                  <p-button
                    label="Revoke"
                    severity="danger"
                    size="small"
                    [text]="true"
                    (onClick)="confirmRevoke(t)"
                  />
                }
              </td>
            </tr>
          </ng-template>
        </p-table>
      } @else {
        <p class="text-sm text-muted-color italic">No tokens yet.</p>
      }
    </section>

    <!-- Generate Dialog -->
    <p-dialog
      header="Generate Personal Access Token"
      [visible]="showGenerateDialog()"
      (visibleChange)="showGenerateDialog.set($event)"
      [modal]="true"
      [style]="{ width: '28rem' }"
    >
      @if (createdToken()) {
        <div class="flex flex-col gap-4 pt-4">
          <p class="text-sm font-semibold" style="color: var(--p-orange-500)">
            Copy this token now. You will not see it again.
          </p>
          <div
            class="p-3 rounded-lg font-mono text-sm break-all"
            style="background: var(--p-surface-100); border: 1px solid var(--p-content-border-color)"
          >
            {{ createdToken() }}
          </div>
          <p-button
            label="Copy to Clipboard"
            icon="pi pi-copy"
            (onClick)="copyToken()"
          />
        </div>
        <ng-template #footer>
          <p-button
            label="Done"
            severity="secondary"
            (onClick)="closeGenerateDialog()"
          />
        </ng-template>
      } @else {
        <div class="flex flex-col gap-4 pt-4">
          <div class="flex flex-col gap-2">
            <label for="tokenName" class="text-sm font-medium text-muted-color">Name *</label>
            <input
              pInputText
              id="tokenName"
              [(ngModel)]="tokenName"
              placeholder="e.g. Import bookmarklet"
              class="w-full"
            />
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-sm font-medium text-muted-color">Scopes</label>
            <label class="flex items-center gap-2 text-sm">
              <input type="checkbox" checked disabled />
              captures:write
            </label>
          </div>
        </div>
        <ng-template #footer>
          <div class="flex justify-end gap-2">
            <p-button
              label="Cancel"
              severity="secondary"
              (onClick)="showGenerateDialog.set(false)"
            />
            <p-button
              label="Generate"
              icon="pi pi-key"
              (onClick)="generateToken()"
              [loading]="generating()"
              [disabled]="!tokenName.trim()"
            />
          </div>
        </ng-template>
      }
    </p-dialog>
  `,
})
export class PersonalAccessTokensComponent implements OnInit {
  private readonly patService = inject(PersonalAccessTokensService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly clipboard = inject(Clipboard);

  readonly tokens = signal<PatSummary[]>([]);
  readonly showGenerateDialog = signal(false);
  readonly createdToken = signal<string | null>(null);
  readonly generating = signal(false);

  protected tokenName = '';

  ngOnInit(): void {
    this.loadTokens();
  }

  private loadTokens(): void {
    this.patService.list().subscribe({
      next: (tokens) => this.tokens.set(tokens),
    });
  }

  generateToken(): void {
    this.generating.set(true);
    this.patService
      .create({ name: this.tokenName.trim(), scopes: ['captures:write'] })
      .subscribe({
        next: (result) => {
          this.generating.set(false);
          this.createdToken.set(result.token);
          this.loadTokens();
        },
        error: () => {
          this.generating.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Failed to generate token',
          });
        },
      });
  }

  copyToken(): void {
    const token = this.createdToken();
    if (token) {
      this.clipboard.copy(token);
      this.messageService.add({
        severity: 'success',
        summary: 'Token copied to clipboard',
      });
    }
  }

  closeGenerateDialog(): void {
    this.showGenerateDialog.set(false);
    this.createdToken.set(null);
    this.tokenName = '';
  }

  confirmRevoke(token: PatSummary): void {
    this.confirmationService.confirm({
      message: `Revoke token "${token.name}"? Any tools using this token will stop working.`,
      header: 'Revoke Token',
      acceptLabel: 'Revoke',
      rejectLabel: 'Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.patService.revoke(token.id).subscribe({
          next: () => {
            this.loadTokens();
            this.messageService.add({
              severity: 'success',
              summary: 'Token revoked',
            });
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Failed to revoke token',
            });
          },
        });
      },
    });
  }
}
