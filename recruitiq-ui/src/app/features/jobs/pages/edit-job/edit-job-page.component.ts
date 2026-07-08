import { ChangeDetectionStrategy, Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { HttpErrorResponse } from '@angular/common/http';

// UI Foundation Imports
import { PageContainerComponent } from '../../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header/section-header.component';
import { JobFormComponent } from '../../components/job-form/job-form.component';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

// Dialogs, Guards & Services
import { ConcurrencyConflictDialogComponent } from '../../../../shared/dialogs/concurrency-conflict-dialog/concurrency-conflict-dialog.component';
import { JobsApiService } from '../../services/jobs-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { JobDetailsResponseDto, UpdateJobRequestDto } from '../../models/job.models';
import { HasUnsavedChanges } from '../../../../core/guards/unsaved-changes.guard';
import { ApiResponse } from '../../../../core/models/api-response.model';

@Component({
  selector: 'app-edit-job-page',
  standalone: true,
  imports: [
    RouterModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    PageContainerComponent,
    SectionHeaderComponent,
    JobFormComponent
  ],
  templateUrl: './edit-job-page.component.html',
  styleUrl: './edit-job-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditJobPageComponent implements OnInit, HasUnsavedChanges {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly jobsApi = inject(JobsApiService);
  private readonly notification = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  @ViewChild(JobFormComponent) private readonly formComponent!: JobFormComponent;

  // Page States
  protected readonly job = signal<JobDetailsResponseDto | null>(null);
  protected readonly isPageLoading = signal<boolean>(false);
  protected readonly isSaving = signal<boolean>(false);
  protected readonly error = signal<string | null>(null);

  private jobId: string | null = null;
  private isSubmitted = false;

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

  hasUnsavedChanges(): boolean {
    return this.formComponent?.formDirty && !this.isSubmitted;
  }

  protected onCancel(): void {
    this.router.navigate(['/admin/jobs', this.jobId]);
  }

  protected onRetry(): void {
    if (this.jobId) {
      this.loadJobDetails(this.jobId);
    }
  }

  private loadJobDetails(id: string): void {
    this.isPageLoading.set(true);
    this.error.set(null);

    this.jobsApi.getJobById(id).subscribe({
      next: (response) => {
        this.isPageLoading.set(false);
        if (response.success && response.data) {
          this.job.set(response.data);
        } else {
          this.error.set(response.message || 'Job details could not be retrieved.');
          this.notification.error('Failed to load job details.');
        }
      },
      error: (err) => {
        this.isPageLoading.set(false);
        this.error.set(err.message || 'A network error occurred while retrieving job data.');
        this.notification.error('Network failure loading job.');
      }
    });
  }

  protected onSave(formValue: any): void {
    const currentJob = this.job();
    if (!this.jobId || !currentJob) return;

    this.isSaving.set(true);
    this.formComponent.disableForm();

    // Map form values to Update API contract, preserving RowVersion
    const payload: UpdateJobRequestDto = {
      title: formValue.title,
      description: formValue.description,
      requirements: formValue.requirements,
      responsibilities: '', // default to empty matching C# handler expectations
      location: formValue.location,
      employmentType: Number(formValue.employmentType),
      salaryMin: formValue.salaryMin !== null && formValue.salaryMin !== '' ? Number(formValue.salaryMin) : null,
      salaryMax: formValue.salaryMax !== null && formValue.salaryMax !== '' ? Number(formValue.salaryMax) : null,
      departmentId: formValue.departmentId,
      hiringManagerId: null,
      closingDate: formValue.closingDate ? new Date(formValue.closingDate).toISOString() : null,
      requiredSkills: formValue.requiredSkills,
      rowVersion: currentJob.rowVersion // Pass raw token unchanged
    };

    this.jobsApi.updateJob(this.jobId, payload).subscribe({
      next: (response: ApiResponse<string>) => {
        this.isSaving.set(false);
        this.formComponent.enableForm();

        if (response.success) {
          this.notification.success('Job updated successfully.');
          this.formComponent.markAsPristine();
          
          // Re-fetch to obtain the newly rotated rowVersion token from EF Core
          this.loadJobDetails(this.jobId!);
        } else {
          this.notification.error(response.message || 'Failed to update job.');
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving.set(false);
        this.formComponent.enableForm();

        // 1. Concurrency Conflict (409)
        if (err.status === 409) {
          this.handleConcurrencyConflict();
        }
        // 2. Bad Request Validation (400)
        else if (err.error && err.error.errors && err.error.errors.length > 0) {
          this.mapBackendErrors(err.error.errors);
          this.notification.error('Please correct the highlighted validation errors.');
        }
        // 3. General Server Failure
        else {
          this.notification.error(err.message || 'Failed to update job details.');
        }
      }
    });
  }

  private handleConcurrencyConflict(): void {
    const dialogRef = this.dialog.open(ConcurrencyConflictDialogComponent, {
      width: '460px',
      disableClose: true,
      data: {
        title: 'This job has been modified',
        message: 'This job was updated by another user while you were editing it. Reload the latest version before making further changes.',
        reloadButtonText: 'Reload Latest',
        cancelButtonText: 'Cancel'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'reload') {
        this.loadJobDetails(this.jobId!);
      }
    });
  }

  private mapBackendErrors(errors: string[]): void {
    const errorMap: { [key: string]: string } = {};
    errors.forEach(err => {
      const errLower = err.toLowerCase();
      if (errLower.includes('title')) errorMap['title'] = err;
      else if (errLower.includes('description')) errorMap['description'] = err;
      else if (errLower.includes('requirements')) errorMap['requirements'] = err;
      else if (errLower.includes('location')) errorMap['location'] = err;
      else if (errLower.includes('salarymin')) errorMap['salaryMin'] = err;
      else if (errLower.includes('salarymax')) errorMap['salaryMax'] = err;
      else if (errLower.includes('closingdate')) errorMap['closingDate'] = err;
    });

    this.formComponent.setErrors(errorMap);
  }
}
