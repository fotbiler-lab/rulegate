import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { RuleGateCanDirective, RuleGateDisableDirective } from '@fotbiler/rulegate-angular';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Tag } from 'primeng/tag';
import { TableModule } from 'primeng/table';

import { DocumentApiService, DocumentRecord } from '../core/document-api.service';
import { RuleGateIdentifiers } from '../generated/rulegate';

@Component({
  imports: [
    ButtonDirective,
    CommonModule,
    FormsModule,
    InputText,
    RuleGateCanDirective,
    RuleGateDisableDirective,
    TableModule,
    Tag,
  ],
  template: `
    <section class="page-heading">
      <div>
        <span class="eyebrow">{{ approvalsOnly ? 'Workflow queue' : 'Resource workspace' }}</span>
        <h1>{{ approvalsOnly ? 'Pending approvals' : 'Documents' }}</h1>
        <p>Actions are projected in the UI and enforced again against the resource by the API.</p>
      </div>
      <button
        *ruleGateCan="{ permission: permissions.docCreate }"
        pButton
        (click)="showCreate.update((value) => !value)"
      >
        <i class="pi pi-plus"></i><span>New document</span>
      </button>
    </section>

    @if (showCreate()) {
      <form class="surface create-form" (ngSubmit)="create()">
        <label
          >Title<input pInputText name="title" [(ngModel)]="title" required maxlength="200"
        /></label>
        <label>
          Classification
          <select name="classification" [(ngModel)]="classification">
            <option value="public">Public</option>
            <option value="internal">Internal</option>
            <option value="confidential">Confidential</option>
          </select>
        </label>
        <button pButton type="submit" [disabled]="busy() || title.trim().length === 0">
          <i class="pi pi-save"></i><span>Create draft</span>
        </button>
      </form>
    }

    @if (error()) {
      <div class="error-banner"><i class="pi pi-exclamation-triangle"></i>{{ error() }}</div>
    }

    <section class="surface table-surface">
      <p-table [value]="visibleDocuments" [loading]="loading()" [rowHover]="true">
        <ng-template #header>
          <tr>
            <th>Document</th>
            <th>Owner</th>
            <th>Organization</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </ng-template>
        <ng-template #body let-document>
          <tr>
            <td>
              <strong>{{ document.title }}</strong
              ><small>{{ document.classification }}</small>
            </td>
            <td>{{ document.ownerUsername }}</td>
            <td>{{ document.organizationId }}</td>
            <td><p-tag [value]="document.status" [severity]="severity(document.status)" /></td>
            <td class="actions">
              <button
                pButton
                class="table-action"
                [ruleGateDisable]="{ permission: permissions.wflStart }"
                [disabled]="document.status !== 'draft' || busy()"
                (click)="transition(document.id, 'submit')"
              >
                Submit
              </button>
              <button
                pButton
                class="table-action approve"
                [ruleGateDisable]="{ permission: permissions.wflApprove }"
                [disabled]="document.status !== 'submitted' || busy()"
                (click)="transition(document.id, 'approve')"
              >
                Approve
              </button>
              <button
                pButton
                class="table-action reject"
                [ruleGateDisable]="{ permission: permissions.wflReject }"
                [disabled]="document.status !== 'submitted' || busy()"
                (click)="transition(document.id, 'reject')"
              >
                Reject
              </button>
            </td>
          </tr>
        </ng-template>
        <ng-template #emptymessage>
          <tr>
            <td colspan="5" class="empty-cell">No documents are available for this view.</td>
          </tr>
        </ng-template>
      </p-table>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentsComponent implements OnInit {
  private readonly api = inject(DocumentApiService);
  private readonly route = inject(ActivatedRoute);
  readonly permissions = RuleGateIdentifiers.permissions;
  readonly documents = signal<readonly DocumentRecord[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly showCreate = signal(false);
  title = '';
  classification = 'internal';

  get approvalsOnly(): boolean {
    return this.route.snapshot.routeConfig?.path === 'approvals';
  }

  get visibleDocuments(): DocumentRecord[] {
    return this.approvalsOnly
      ? [...this.documents().filter((document) => document.status === 'submitted')]
      : [...this.documents()];
  }

  ngOnInit(): void {
    this.load();
  }

  create(): void {
    this.busy.set(true);
    this.api.create(this.title, this.classification).subscribe({
      next: () => {
        this.title = '';
        this.showCreate.set(false);
        this.busy.set(false);
        this.load();
      },
      error: () => this.fail('The document could not be created.'),
    });
  }

  transition(id: number, action: 'submit' | 'approve' | 'reject'): void {
    this.busy.set(true);
    this.api.transition(id, action).subscribe({
      next: () => {
        this.busy.set(false);
        this.load();
      },
      error: () => this.fail(`The document could not be ${action}ed.`),
    });
  }

  severity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    return { draft: 'secondary', submitted: 'warn', approved: 'success', rejected: 'danger' }[
      status
    ] as 'success' | 'info' | 'warn' | 'danger' | 'secondary';
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.list().subscribe({
      next: (documents) => {
        this.documents.set(documents);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set(
          'Documents could not be loaded. Check the API and your local profile mapping.',
        );
      },
    });
  }

  private fail(message: string): void {
    this.busy.set(false);
    this.error.set(message);
  }
}
