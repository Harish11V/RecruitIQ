import { inject, Injectable, signal, effect } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { CandidateApiService } from '../services/candidate-api.service';
import { CandidateSummaryResponse } from '../models/candidate.models';
import { NotificationService } from '../../../core/services/notification.service';
import { mapBusinessError } from '../../../core/errors/business-error-mapper';

@Injectable()
export class CandidateStore {
  private readonly api = inject(CandidateApiService);
  private readonly notification = inject(NotificationService);

  readonly candidates = signal<CandidateSummaryResponse[]>([]);
  readonly totalRecords = signal<number>(0);
  readonly totalPages = signal<number>(0);
  readonly page = signal<number>(1);
  readonly pageSize = signal<number>(10);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  readonly searchTerm = signal<string>('');
  readonly statusFilter = signal<string>('');

  constructor() {
    // Automatically trigger candidates reload when filters or paging state changes
    effect(() => {
      this.loadCandidates();
    });
  }

  loadCandidates(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const search = this.searchTerm();
    const status = this.statusFilter();
    const page = this.page();
    const pageSize = this.pageSize();

    this.api.getCandidates({
      search,
      status: status || undefined,
      page,
      pageSize,
      sortBy: 'CreatedAt',
      sortOrder: 'desc'
    }).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const pagedData = response.data;
          this.candidates.set(pagedData.items);
          this.totalRecords.set(pagedData.totalRecords);
          this.totalPages.set(pagedData.totalPages);
        } else {
          this.error.set(response.message || 'Failed to retrieve candidates.');
          this.notification.error(response.message || 'Failed to retrieve candidates.');
        }
      },
      error: (err: HttpErrorResponse) => {
        const msg = err.error?.message || 'A network error occurred. Please try again.';
        this.error.set(msg);
        this.notification.error(msg);
      }
    });
  }

  setSearchTerm(term: string): void {
    this.searchTerm.set(term);
    this.page.set(1); // reset to page 1 on filter change
  }

  setStatusFilter(status: string): void {
    this.statusFilter.set(status);
    this.page.set(1); // reset to page 1 on filter change
  }

  setPage(pageIndex: number): void {
    this.page.set(pageIndex);
  }

  setPageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
  }

  changeStatus(id: string, status: number, rowVersion: string, onSuccess: (newRowVersion: string) => void): void {
    this.isLoading.set(true);
    this.api.changeStatus(id, status, rowVersion).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success && response.data) {
          this.notification.success('Status updated successfully.');
          onSuccess(response.data);
        } else {
          const msg = mapBusinessError(response.message);
          this.notification.error(msg);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        const errorCode = err.error?.message || err.message;
        const msg = mapBusinessError(errorCode);
        this.notification.error(msg);
      }
    });
  }
}
