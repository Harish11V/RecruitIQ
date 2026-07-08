import { inject, Injectable, signal } from '@angular/core';
import { forkJoin, finalize } from 'rxjs';
import { JobsApiService } from '../services/jobs-api.service';
import { JobSummaryResponse, JobStatus, EmploymentType } from '../models/job.models';

@Injectable()
export class JobStore {
  private readonly api = inject(JobsApiService);

  // Listing State Signals
  readonly jobs = signal<JobSummaryResponse[]>([]);
  readonly totalRecords = signal<number>(0);
  readonly page = signal<number>(1);
  readonly pageSize = signal<number>(10);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  // Active Query Parameters Signals
  readonly search = signal<string>('');
  readonly sortBy = signal<string>('');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');
  readonly status = signal<JobStatus | null>(null);
  readonly departmentId = signal<string>('');
  readonly employmentType = signal<string>('');

  // Statistics State Signals
  readonly totalJobs = signal<number>(0);
  readonly publishedCount = signal<number>(0);
  readonly draftCount = signal<number>(0);
  readonly archivedCount = signal<number>(0);
  readonly isStatsLoading = signal<boolean>(false);

  loadJobs(): void {
    this.isLoading.set(true);
    this.error.set(null);

    // Compute sort expression (e.g. "Title ASC" or "Title DESC")
    let sortByExpr = '';
    if (this.sortBy()) {
      sortByExpr = `${this.sortBy()} ${this.sortDirection().toUpperCase()}`;
    }

    this.api.getJobs({
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.search(),
      sortBy: sortByExpr,
      status: this.status() !== null ? this.status()! : undefined,
      departmentId: this.departmentId() || undefined,
      employmentType: this.employmentType() || undefined
    }).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.jobs.set(response.data.items);
          this.totalRecords.set(response.data.totalRecords);
        } else {
          this.error.set(response.message || 'Failed to retrieve jobs.');
        }
      },
      error: (err) => {
        this.error.set(err.message || 'A network error occurred. Please try again.');
      }
    });
  }

  loadStats(): void {
    this.isStatsLoading.set(true);

    forkJoin({
      total: this.api.getJobs({ page: 1, pageSize: 1 }),
      published: this.api.getJobs({ page: 1, pageSize: 1, status: JobStatus.Published }),
      draft: this.api.getJobs({ page: 1, pageSize: 1, status: JobStatus.Draft }),
      archived: this.api.getJobs({ page: 1, pageSize: 1, status: JobStatus.Archived })
    }).pipe(
      finalize(() => this.isStatsLoading.set(false))
    ).subscribe({
      next: (res) => {
        this.totalJobs.set(res.total.data?.totalRecords || 0);
        this.publishedCount.set(res.published.data?.totalRecords || 0);
        this.draftCount.set(res.draft.data?.totalRecords || 0);
        this.archivedCount.set(res.archived.data?.totalRecords || 0);
      },
      error: () => {
        this.totalJobs.set(0);
        this.publishedCount.set(0);
        this.draftCount.set(0);
        this.archivedCount.set(0);
      }
    });
  }

  updateFilters(filters: { search?: string; status?: JobStatus | null; departmentId?: string; employmentType?: string }): void {
    if (filters.search !== undefined) this.search.set(filters.search);
    if (filters.status !== undefined) this.status.set(filters.status);
    if (filters.departmentId !== undefined) this.departmentId.set(filters.departmentId);
    if (filters.employmentType !== undefined) this.employmentType.set(filters.employmentType);
    
    this.page.set(1); // Reset to first page
    this.loadJobs();
  }

  updateSort(sortBy: string): void {
    if (this.sortBy() === sortBy) {
      // Toggle direction
      this.sortDirection.update(dir => dir === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(sortBy);
      this.sortDirection.set('asc');
    }
    this.page.set(1);
    this.loadJobs();
  }

  updatePage(page: number, pageSize: number): void {
    this.page.set(page);
    this.pageSize.set(pageSize);
    this.loadJobs();
  }

  resetFilters(): void {
    this.search.set('');
    this.status.set(null);
    this.departmentId.set('');
    this.employmentType.set('');
    this.sortBy.set('');
    this.sortDirection.set('asc');
    this.page.set(1);
    this.loadJobs();
  }
}
