import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';

// UI Foundation & Components
import { PageContainerComponent } from '../../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header/section-header.component';
import { CandidateFormComponent } from '../../components/candidate-form/candidate-form.component';

// Interfaces & Services
import { HasUnsavedChanges } from '../../../../core/guards/unsaved-changes.guard';
import { CandidateApiService } from '../../services/candidate-api.service';
import { CreateCandidateRequestDto } from '../../models/candidate.models';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'app-create-candidate-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    PageContainerComponent,
    SectionHeaderComponent,
    CandidateFormComponent
  ],
  templateUrl: './create-candidate-page.component.html',
  styleUrl: './create-candidate-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateCandidatePageComponent implements HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly api = inject(CandidateApiService);
  private readonly notification = inject(NotificationService);

  protected readonly form: FormGroup;
  protected readonly isSaving = signal<boolean>(false);
  protected isSubmitted = false;

  constructor() {
    this.form = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      linkedInUrl: ['', [Validators.pattern(/linkedin\.com/i)]],
      title: [''],
      yearsOfExperience: [null, [Validators.min(0), Validators.max(50)]]
    });
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.isSubmitted;
  }

  protected onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notification.error('Please correct the validation errors before saving.');
      setTimeout(() => this.scrollToFirstInvalid(), 100);
      return;
    }

    this.isSaving.set(true);
    const formValue = this.form.value;
    
    const requestDto: CreateCandidateRequestDto = {
      firstName: formValue.firstName.trim(),
      lastName: formValue.lastName.trim(),
      email: formValue.email.trim(),
      phoneNumber: formValue.phoneNumber ? formValue.phoneNumber.trim() : null,
      linkedInUrl: formValue.linkedInUrl ? formValue.linkedInUrl.trim() : null,
      title: formValue.title ? formValue.title.trim() : null,
      yearsOfExperience: formValue.yearsOfExperience
    };

    this.api.createCandidate(requestDto)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.isSubmitted = true;
            this.notification.success('Candidate created successfully.');
            this.router.navigate(['/admin/candidates', response.data]);
          } else {
            const errorMsg = response.message || 'Failed to create candidate profile.';
            this.notification.error(errorMsg);
            this.handleBackendErrors(errorMsg);
          }
        },
        error: (err) => {
          const errorMsg = err.error?.message || 'An error occurred while creating the profile.';
          this.notification.error(errorMsg);
          this.handleBackendErrors(errorMsg);
        }
      });
  }

  protected onCancel(): void {
    this.router.navigate(['/admin/candidates']);
  }

  private scrollToFirstInvalid(): void {
    const firstInvalid = document.querySelector('.mat-form-field-invalid, input:invalid');
    if (firstInvalid) {
      firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  private handleBackendErrors(errorMsg: string): void {
    if (errorMsg.toLowerCase().includes('email') && errorMsg.toLowerCase().includes('exists')) {
      const emailCtrl = this.form.get('email');
      if (emailCtrl) {
        emailCtrl.setErrors({ uniqueEmail: true });
        setTimeout(() => this.scrollToFirstInvalid(), 100);
      }
    }
  }
}
