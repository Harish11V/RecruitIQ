import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';

// Material Imports
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

// UI Foundation Imports
import { PageContainerComponent } from '../../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header/section-header.component';
import { AppCardComponent } from '../../../../shared/ui/app-card/app-card.component';
import { DetailRowComponent } from '../../../../shared/ui/detail-row/detail-row.component';

// Services & Models
import { JobsApiService } from '../../services/jobs-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { JobDetailsResponseDto, JobStatus, EmploymentType } from '../../models/job.models';
import { JobActionService } from '../../services/job-action.service';

@Component({
  selector: 'app-job-details-page',
  standalone: true,
  imports: [
    DatePipe,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    PageContainerComponent,
    SectionHeaderComponent,
    AppCardComponent,
    DetailRowComponent
  ],
  templateUrl: './job-details-page.component.html',
  styleUrl: './job-details-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JobDetailsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly jobsApi = inject(JobsApiService);
  private readonly notification = inject(NotificationService);
  private readonly jobAction = inject(JobActionService);

  // State Signals
  protected readonly job = signal<JobDetailsResponseDto | null>(null);
  protected readonly isLoading = signal<boolean>(false);
  protected readonly error = signal<string | null>(null);

  private jobId: string | null = null;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.jobId = params.get('id');
      if (this.jobId) {
        this.loadJobDetails(this.jobId);
      } else {
        this.error.set('Invalid job identifier.');
      }
    });
  }

  protected onRetry(): void {
    if (this.jobId) {
      this.loadJobDetails(this.jobId);
    }
  }

  private loadJobDetails(id: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.jobsApi.getJobById(id).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success && response.data) {
          this.job.set(response.data);
        } else {
          this.error.set(response.message || 'Job details could not be retrieved.');
          this.notification.error('Failed to load job details.');
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.error.set(err.message || 'A network error occurred while retrieving job data.');
        this.notification.error('Network failure loading job details.');
      }
    });
  }

  // Formatting Helpers
  protected getStatusName(status: JobStatus | undefined): string {
    if (status === undefined) return '';
    switch (status) {
      case JobStatus.Draft: return 'Draft';
      case JobStatus.Published: return 'Published';
      case JobStatus.Closed: return 'Closed';
      case JobStatus.Archived: return 'Archived';
      default: return 'Unknown';
    }
  }

  protected getStatusClass(status: JobStatus | undefined): string {
    if (status === undefined) return '';
    switch (status) {
      case JobStatus.Draft: return 'draft';
      case JobStatus.Published: return 'published';
      case JobStatus.Closed: return 'closed';
      case JobStatus.Archived: return 'archived';
      default: return '';
    }
  }

  protected getEmploymentTypeName(type: EmploymentType | undefined): string {
    if (type === undefined) return '';
    switch (type) {
      case EmploymentType.FullTime: return 'Full-Time';
      case EmploymentType.PartTime: return 'Part-Time';
      case EmploymentType.Contract: return 'Contract';
      case EmploymentType.Internship: return 'Internship';
      default: return 'Other';
    }
  }

  protected formatSalaryRange(min: number | null | undefined, max: number | null | undefined): string {
    const currencyPipe = new CurrencyPipe('en-US');
    const minStr = min !== null && min !== undefined ? currencyPipe.transform(min, 'USD', 'symbol', '1.0-0') : null;
    const maxStr = max !== null && max !== undefined ? currencyPipe.transform(max, 'USD', 'symbol', '1.0-0') : null;

    if (minStr && maxStr) {
      return `${minStr} – ${maxStr}`;
    } else if (minStr) {
      return `${minStr} (Minimum)`;
    } else if (maxStr) {
      return `${maxStr} (Maximum)`;
    }
    return 'Not Specified';
  }

  protected onPublish(): void {
    const currentJob = this.job();
    if (!this.jobId || !currentJob) return;
    this.jobAction.publish(this.jobId, currentJob.rowVersion, currentJob.title, () => this.loadJobDetails(this.jobId!));
  }

  protected onArchive(): void {
    const currentJob = this.job();
    if (!this.jobId || !currentJob) return;
    this.jobAction.archive(this.jobId, currentJob.rowVersion, currentJob.title, () => this.loadJobDetails(this.jobId!));
  }

  protected onDelete(): void {
    const currentJob = this.job();
    if (!this.jobId || !currentJob) return;
    this.jobAction.delete(this.jobId, currentJob.rowVersion, currentJob.title, () => this.router.navigate(['/admin/jobs']));
  }
}
