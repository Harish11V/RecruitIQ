import { ChangeDetectionStrategy, Component, inject, signal, ViewChild } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

// UI Foundation Imports
import { PageContainerComponent } from '../../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header/section-header.component';
import { JobFormComponent } from '../../components/job-form/job-form.component';

// Services, Models & Guards
import { JobsApiService } from '../../services/jobs-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { CreateJobRequestDto } from '../../models/job.models';
import { HasUnsavedChanges } from '../../../../core/guards/unsaved-changes.guard';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-create-job-page',
  standalone: true,
  imports: [
    RouterModule,
    MatButtonModule,
    MatIconModule,
    PageContainerComponent,
    SectionHeaderComponent,
    JobFormComponent
  ],
  templateUrl: './create-job-page.component.html',
  styleUrl: './create-job-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateJobPageComponent implements HasUnsavedChanges {
  private readonly router = inject(Router);
  private readonly jobsApi = inject(JobsApiService);
  private readonly notification = inject(NotificationService);

  @ViewChild(JobFormComponent) private readonly formComponent!: JobFormComponent;

  protected readonly isLoading = signal<boolean>(false);
  private isSubmitted = false;

  hasUnsavedChanges(): boolean {
    return this.formComponent?.formDirty && !this.isSubmitted;
  }

  protected onCancel(): void {
    this.router.navigate(['/admin/jobs']);
  }

  protected onSave(formValue: any): void {
    this.isLoading.set(true);
    this.formComponent.disableForm();

    // Map form values to API DTO contract
    const payload: CreateJobRequestDto = {
      title: formValue.title,
      description: formValue.description,
      requirements: formValue.requirements,
      location: formValue.location,
      employmentType: Number(formValue.employmentType),
      salaryMin: formValue.salaryMin !== null && formValue.salaryMin !== '' ? Number(formValue.salaryMin) : null,
      salaryMax: formValue.salaryMax !== null && formValue.salaryMax !== '' ? Number(formValue.salaryMax) : null,
      departmentId: formValue.departmentId,
      hiringManagerId: null, // Scaffolding placeholder
      closingDate: formValue.closingDate ? new Date(formValue.closingDate).toISOString() : null,
      requiredSkills: formValue.requiredSkills.length > 0 ? formValue.requiredSkills : null
    };

    this.jobsApi.createJob(payload).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.formComponent.enableForm();

        if (response.success) {
          this.isSubmitted = true;
          this.formComponent.markAsPristine();
          this.notification.success('Job draft created successfully.');
          this.router.navigate(['/admin/jobs']);
        } else {
          this.notification.error(response.message || 'Failed to create job.');
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.formComponent.enableForm();

        if (err.error && err.error.errors && err.error.errors.length > 0) {
          this.mapBackendErrors(err.error.errors);
          this.notification.error('Please correct the highlighted validation errors.');
        } else {
          this.notification.error(err.message || 'A network error occurred. Please try again.');
        }
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
