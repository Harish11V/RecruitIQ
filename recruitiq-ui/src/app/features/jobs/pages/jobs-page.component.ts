import { ChangeDetectionStrategy, Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';

// Material Imports
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';

// UI Foundation Imports
import { PageContainerComponent } from '../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../shared/ui/section-header/section-header.component';
import { StatCardComponent } from '../../../shared/ui/stat-card/stat-card.component';
import { AppCardComponent } from '../../../shared/ui/app-card/app-card.component';

// Feature State, Service & Model Imports
import { JobStore } from '../state/job.store';
import { JobStatus, EmploymentType, DepartmentSummary, JobSummaryResponse } from '../models/job.models';
import { DepartmentApiService } from '../services/department-api.service';
import { JobActionService } from '../services/job-action.service';

@Component({
  selector: 'app-jobs-page',
  standalone: true,
  imports: [
    DatePipe,
    RouterModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule,
    MatChipsModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatMenuModule,
    MatTooltipModule,
    PageContainerComponent,
    SectionHeaderComponent,
    StatCardComponent,
    AppCardComponent
  ],
  providers: [JobStore],
  templateUrl: './jobs-page.component.html',
  styleUrl: './jobs-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JobsPageComponent implements OnInit, OnDestroy {
  protected readonly store = inject(JobStore);
  private readonly departmentApi = inject(DepartmentApiService);
  protected readonly jobAction = inject(JobActionService);
  private readonly destroy$ = new Subject<void>();

  // Filter Form Controls
  protected readonly searchControl = new FormControl<string>('');
  protected readonly departmentControl = new FormControl<string>('');
  protected readonly statusControl = new FormControl<JobStatus | null>(null);
  protected readonly employmentTypeControl = new FormControl<string>('');

  protected readonly departments = signal<DepartmentSummary[]>([]);

  // Table Columns
  protected readonly displayedColumns = [
    'jobCode',
    'title',
    'department',
    'employmentType',
    'status',
    'createdAt',
    'actions'
  ];

  ngOnInit(): void {
    // Initial data fetch
    this.store.loadStats();
    this.store.loadJobs();
    this.loadDepartments();

    // Subscribe to search input changes with 300ms debounce
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe((val) => {
      this.store.updateFilters({ search: val || '' });
    });

    // Subscribe to filter dropdown triggers
    this.departmentControl.valueChanges.pipe(
      takeUntil(this.destroy$)
    ).subscribe((val) => {
      this.store.updateFilters({ departmentId: val || '' });
    });

    this.statusControl.valueChanges.pipe(
      takeUntil(this.destroy$)
    ).subscribe((val) => {
      this.store.updateFilters({ status: val });
    });

    this.employmentTypeControl.valueChanges.pipe(
      takeUntil(this.destroy$)
    ).subscribe((val) => {
      this.store.updateFilters({ employmentType: val || '' });
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Formatting Helpers
  protected getStatusName(status: JobStatus): string {
    switch (status) {
      case JobStatus.Draft: return 'Draft';
      case JobStatus.Published: return 'Published';
      case JobStatus.Closed: return 'Closed';
      case JobStatus.Archived: return 'Archived';
      default: return 'Unknown';
    }
  }

  protected getStatusClass(status: JobStatus): string {
    switch (status) {
      case JobStatus.Draft: return 'draft';
      case JobStatus.Published: return 'published';
      case JobStatus.Closed: return 'closed';
      case JobStatus.Archived: return 'archived';
      default: return '';
    }
  }

  protected getEmploymentTypeName(type: EmploymentType): string {
    switch (type) {
      case EmploymentType.FullTime: return 'Full-Time';
      case EmploymentType.PartTime: return 'Part-Time';
      case EmploymentType.Contract: return 'Contract';
      case EmploymentType.Internship: return 'Internship';
      default: return 'Other';
    }
  }

  // Interactive Operations
  protected onPageChange(event: PageEvent): void {
    this.store.updatePage(event.pageIndex + 1, event.pageSize);
  }

  protected onSort(column: string): void {
    this.store.updateSort(column);
  }

  protected onRefresh(): void {
    this.store.loadStats();
    this.store.loadJobs();
  }

  protected onResetFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.departmentControl.setValue('', { emitEvent: false });
    this.statusControl.setValue(null, { emitEvent: false });
    this.employmentTypeControl.setValue('', { emitEvent: false });
    
    this.store.resetFilters();
  }

  private loadDepartments(): void {
    this.departmentApi.getDepartments().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.departments.set(response.data);
        }
      }
    });
  }

  protected onPublish(job: JobSummaryResponse): void {
    this.jobAction.publish(job.id, job.rowVersion, job.title, () => this.onRefresh());
  }

  protected onArchive(job: JobSummaryResponse): void {
    this.jobAction.archive(job.id, job.rowVersion, job.title, () => this.onRefresh());
  }

  protected onDelete(job: JobSummaryResponse): void {
    this.jobAction.delete(job.id, job.rowVersion, job.title, () => this.onRefresh());
  }
}
