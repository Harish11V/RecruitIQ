import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';

// UI Foundation & Components
import { PageContainerComponent } from '../../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header/section-header.component';
import { CandidateFormComponent } from '../../components/candidate-form/candidate-form.component';

// Concurrency Conflict Dialog
import { ConcurrencyConflictDialogComponent } from '../../../../shared/dialogs/concurrency-conflict-dialog/concurrency-conflict-dialog.component';

// Interfaces & Services
import { HasUnsavedChanges } from '../../../../core/guards/unsaved-changes.guard';
import { CandidateApiService } from '../../services/candidate-api.service';
import { UpdateCandidateRequestDto } from '../../models/candidate.models';
import { NotificationService } from '../../../../core/services/notification.service';
import { BreadcrumbService } from '../../../../core/services/breadcrumb.service';

@Component({
  selector: 'app-edit-candidate-page',
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
  templateUrl: './edit-candidate-page.component.html',
  styleUrl: './edit-candidate-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditCandidatePageComponent implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(CandidateApiService);
  private readonly notification = inject(NotificationService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly dialog = inject(MatDialog);

  protected readonly form: FormGroup;
  protected readonly isLoading = signal<boolean>(false);
  protected readonly isSaving = signal<boolean>(false);
  protected readonly error = signal<string | null>(null);
  
  private candidateId: string | null = null;
  private currentName = '';

  constructor() {
    this.form = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      linkedInUrl: ['', [Validators.pattern(/linkedin\.com/i)]],
      title: [''],
      status: [0, [Validators.required]],
      yearsOfExperience: [null, [Validators.min(0), Validators.max(50)]],
      rowVersion: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.candidateId = this.route.snapshot.paramMap.get('id');
    if (this.candidateId) {
      this.loadCandidate(this.candidateId);
    } else {
      this.error.set('No Candidate ID provided.');
      this.notification.error('No Candidate ID provided.');
    }
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.isSaving();
  }

  protected loadCandidate(id: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.api.getCandidateById(id)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            const data = response.data;
            this.currentName = `${data.firstName} ${data.lastName}`;
            
            // Populate Reactive Form Controls
            this.form.patchValue({
              firstName: data.firstName,
              lastName: data.lastName,
              email: data.email,
              phoneNumber: data.phoneNumber,
              linkedInUrl: data.linkedInUrl,
              title: data.title,
              status: data.status,
              yearsOfExperience: data.yearsOfExperience,
              rowVersion: data.rowVersion
            });
            this.form.markAsPristine();

            // Dynamic Breadcrumbs
            this.breadcrumbService.updateBreadcrumbLabel(`/admin/candidates/${id}`, this.currentName);
            this.breadcrumbService.updateBreadcrumbLabel(`/admin/candidates/${id}/edit`, 'Edit');
          } else {
            const msg = response.message || 'Failed to retrieve candidate profile.';
            this.error.set(msg);
            this.notification.error(msg);
          }
        },
        error: (err) => {
          const msg = err.error?.message || 'A network error occurred while loading profile.';
          this.error.set(msg);
          this.notification.error(msg);
        }
      });
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

    const requestDto: UpdateCandidateRequestDto = {
      firstName: formValue.firstName.trim(),
      lastName: formValue.lastName.trim(),
      email: formValue.email.trim(),
      phoneNumber: formValue.phoneNumber ? formValue.phoneNumber.trim() : null,
      linkedInUrl: formValue.linkedInUrl ? formValue.linkedInUrl.trim() : null,
      title: formValue.title ? formValue.title.trim() : null,
      status: formValue.status,
      yearsOfExperience: formValue.yearsOfExperience,
      rowVersion: formValue.rowVersion
    };

    this.api.updateCandidate(this.candidateId!, requestDto)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.notification.success('Candidate updated successfully.');
            
            // Refresh RowVersion after save
            this.form.get('rowVersion')?.setValue(response.data);
            this.form.markAsPristine();
          } else {
            this.notification.error(response.message || 'Failed to update candidate profile.');
          }
        },
        error: (err) => {
          if (err.status === 409) {
            this.handleConcurrencyConflict();
          } else {
            const errorMsg = err.error?.message || 'An error occurred while saving.';
            this.notification.error(errorMsg);
          }
        }
      });
  }

  protected onCancel(): void {
    this.router.navigate(['/admin/candidates', this.candidateId]);
  }

  private handleConcurrencyConflict(): void {
    const dialogRef = this.dialog.open(ConcurrencyConflictDialogComponent, {
      width: '400px',
      disableClose: true,
      data: {
        title: 'Concurrency Conflict',
        message: 'This candidate profile has been modified by another user. Would you like to discard your unsaved changes and reload the latest record?',
        reloadButtonText: 'Reload Latest',
        cancelButtonText: 'Keep My Changes'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadCandidate(this.candidateId!);
      }
    });
  }

  private scrollToFirstInvalid(): void {
    const firstInvalid = document.querySelector('.mat-form-field-invalid, input:invalid');
    if (firstInvalid) {
      firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }
}
