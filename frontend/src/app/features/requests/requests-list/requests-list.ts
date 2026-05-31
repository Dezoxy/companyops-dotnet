import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';

import { RequestsService } from '../requests.service';
import {
  REQUEST_STATUS_META,
  REQUEST_TYPE_LABEL,
  RequestStatus,
  RequestType,
} from '../requests.models';
import { StatusChip } from '../../../shared/status-chip/status-chip';

/** Requests list: a filterable, paged table over GET /requests. Read-only — creating and
 *  acting on requests land in Phase 14b. Filtering/paging is client-side over the loaded list. */
@Component({
  selector: 'app-requests-list',
  imports: [
    DatePipe,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatCardModule,
    MatTooltipModule,
    StatusChip,
  ],
  templateUrl: './requests-list.html',
  styleUrl: './requests-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsList {
  private readonly service = inject(RequestsService);

  protected readonly requests = this.service.requests;
  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;

  protected readonly columns = ['id', 'title', 'type', 'status', 'created', 'actions'];

  // Filter options for the dropdowns (label + value), built from the display metadata.
  protected readonly statusOptions = Object.entries(REQUEST_STATUS_META).map(([value, meta]) => ({
    value: value as RequestStatus,
    label: meta.label,
  }));
  protected readonly typeOptions = Object.entries(REQUEST_TYPE_LABEL).map(([value, label]) => ({
    value: value as RequestType,
    label,
  }));

  protected readonly search = signal('');
  protected readonly statusFilter = signal<RequestStatus | 'all'>('all');
  protected readonly typeFilter = signal<RequestType | 'all'>('all');
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.statusFilter();
    const type = this.typeFilter();
    return this.requests().filter(
      (r) =>
        (status === 'all' || r.status === status) &&
        (type === 'all' || r.type === type) &&
        (term === '' || r.title.toLowerCase().includes(term) || r.id.toLowerCase().includes(term)),
    );
  });

  protected readonly paged = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    return this.filtered().slice(start, start + this.pageSize());
  });

  constructor() {
    this.service.loadAll();
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    this.pageIndex.set(0);
  }

  protected onStatus(value: RequestStatus | 'all'): void {
    this.statusFilter.set(value);
    this.pageIndex.set(0);
  }

  protected onType(value: RequestType | 'all'): void {
    this.typeFilter.set(value);
    this.pageIndex.set(0);
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  protected refresh(): void {
    this.service.loadAll();
  }
}
