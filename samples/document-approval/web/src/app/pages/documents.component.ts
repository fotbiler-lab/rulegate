import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { RuleGateCanDirective, RuleGateDisableDirective } from '@fotbiler/rulegate-angular';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { TableModule } from 'primeng/table';

import { DocumentApiService, DocumentRecord } from '../core/document-api.service';
import { RuleGateIdentifiers } from '../generated/rulegate';

@Component({
  imports: [
    ButtonDirective,
    Card,
    CommonModule,
    FormsModule,
    InputText,
    Message,
    RuleGateCanDirective,
    RuleGateDisableDirective,
    Select,
    TableModule,
    Tag,
  ],
  template: `
    <section
      class="flex flex-column gap-3 mb-4 md:flex-row md:align-items-center md:justify-content-between"
    >
      <div>
        <span class="text-primary text-sm font-semibold uppercase">{{
          approvalsOnly ? 'Workflow queue' : 'Resource workspace'
        }}</span>
        <h1 class="mt-2 mb-2 text-4xl md:text-5xl">
          {{ approvalsOnly ? 'Pending approvals' : 'Documents' }}
        </h1>
        <p class="m-0 text-color-secondary line-height-3">
          Actions are projected in the UI and enforced again against the resource by the API.
        </p>
      </div>
      <button
        *ruleGateCan="{ permission: permissions.docCreate }"
        pButton
        type="button"
        (click)="showCreate.update((value) => !value)"
      >
        <i class="pi pi-plus"></i><span>New document</span>
      </button>
    </section>

    @if (showCreate()) {
      <div class="mb-3">
        <p-card>
          <form class="formgrid grid align-items-end" (ngSubmit)="create()">
            <div class="field col-12 lg:col-8">
              <label for="document-title" class="block mb-2 font-semibold">Title</label>
              <input
                pInputText
                id="document-title"
                class="w-full"
                name="title"
                [(ngModel)]="title"
                required
                maxlength="200"
              />
            </div>
            <div class="field col-12 lg:col-2">
              <label for="classification" class="block mb-2 font-semibold">Classification</label>
              <p-select
                inputId="classification"
                styleClass="w-full"
                name="classification"
                [(ngModel)]="classification"
                [options]="classificationOptions"
                optionLabel="label"
                optionValue="value"
              />
            </div>
            <div class="field col-12 lg:col-2">
              <button
                pButton
                class="w-full"
                type="submit"
                [disabled]="busy() || title.trim().length === 0"
              >
                <i class="pi pi-save"></i><span>Create draft</span>
              </button>
            </div>
          </form>
        </p-card>
      </div>
    }

    @if (error()) {
      <div class="mb-3">
        <p-message severity="error" styleClass="w-full">{{ error() }}</p-message>
      </div>
    }

    <div class="mb-3">
      <p-message severity="info" styleClass="w-full">
        Results are resource-filtered by organization and clearance. Confidential access follows
        database-backed organization hours: records 08:00–18:00 and legal 06:00–20:00 on weekdays.
      </p-message>
    </div>

    <p-card>
      <p-table
        [value]="visibleDocuments"
        [loading]="loading()"
        [rowHover]="true"
        [scrollable]="true"
      >
        <ng-template #header>
          <tr>
            <th>Document</th>
            <th>Owner</th>
            <th>Organization</th>
            <th>Classification</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </ng-template>
        <ng-template #body let-document>
          <tr>
            <td>
              <strong>{{ document.title }}</strong>
            </td>
            <td>{{ document.ownerUsername }}</td>
            <td>{{ document.organizationId }}</td>
            <td>
              <p-tag
                [value]="document.classification"
                [severity]="classificationSeverity(document.classification)"
              />
            </td>
            <td><p-tag [value]="document.status" [severity]="severity(document.status)" /></td>
            <td>
              <div class="flex flex-wrap gap-1">
                @if (busy()) {
                  <span class="text-sm text-color-secondary">
                    <i class="pi pi-spin pi-spinner mr-1"></i>Working
                  </span>
                } @else if (document.status === 'draft') {
                  <button
                    pButton
                    type="button"
                    severity="secondary"
                    [text]="true"
                    [ruleGateDisable]="{ permission: permissions.wflStart }"
                    (click)="transition(document.id, 'submit')"
                  >
                    Submit
                  </button>
                } @else if (document.status === 'submitted') {
                  <button
                    pButton
                    type="button"
                    severity="success"
                    [text]="true"
                    [ruleGateDisable]="{ permission: permissions.wflApprove }"
                    (click)="transition(document.id, 'approve')"
                  >
                    Approve
                  </button>
                  <button
                    pButton
                    type="button"
                    severity="danger"
                    [text]="true"
                    [ruleGateDisable]="{ permission: permissions.wflReject }"
                    (click)="transition(document.id, 'reject')"
                  >
                    Reject
                  </button>
                } @else {
                  <span class="text-sm text-color-secondary">No actions</span>
                }
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template #emptymessage>
          <tr>
            <td colspan="6" class="py-6 text-center text-color-secondary">
              No documents are available for this view.
            </td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>
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
  readonly classificationOptions = [
    { label: 'Public', value: 'public' },
    { label: 'Internal', value: 'internal' },
    { label: 'Confidential', value: 'confidential' },
  ];
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
      error: (error: HttpErrorResponse) =>
        this.fail(
          error.status === 403
            ? 'Your permissions, clearance, or request context do not allow this classification.'
            : 'The document could not be created.',
        ),
    });
  }

  transition(id: number, action: 'submit' | 'approve' | 'reject'): void {
    this.busy.set(true);
    this.api.transition(id, action).subscribe({
      next: () => {
        this.busy.set(false);
        this.load();
      },
      error: () =>
        this.fail(
          `The document could not be ${
            {
              submit: 'submitted',
              approve: 'approved',
              reject: 'rejected',
            }[action]
          }.`,
        ),
    });
  }

  severity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    return { draft: 'secondary', submitted: 'warn', approved: 'success', rejected: 'danger' }[
      status
    ] as 'success' | 'info' | 'warn' | 'danger' | 'secondary';
  }

  classificationSeverity(classification: string): 'info' | 'warn' | 'danger' {
    return { public: 'info', internal: 'warn', confidential: 'danger' }[classification] as
      'info' | 'warn' | 'danger';
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
