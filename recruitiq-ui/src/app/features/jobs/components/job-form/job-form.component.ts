import { ChangeDetectionStrategy, Component, EventEmitter, inject, Input, OnInit, Output, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { MatChipInputEvent, MatChipsModule } from '@angular/material/chips';

// Material Imports
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

// UI Foundation Imports
import { AppCardComponent } from '../../../../shared/ui/app-card/app-card.component';

// Services, Models & Guards
import { LayoutService } from '../../../../layout/services/layout.service';
import { DepartmentApiService } from '../../services/department-api.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { DepartmentSummary, JobDetailsResponseDto, JobStatus } from '../../models/job.models';

// Custom Range Validator
function salaryRangeValidator(group: AbstractControl): ValidationErrors | null {
  const min = group.get('salaryMin')?.value;
  const max = group.get('salaryMax')?.value;

  if (min !== null && min !== '' && max !== null && max !== '' && Number(max) < Number(min)) {
    return { salaryRangeInvalid: true };
  }
  return null;
}

import { JobStatusChipComponent } from '../job-status-chip/job-status-chip.component';

@Component({
  selector: 'app-job-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    AppCardComponent,
    JobStatusChipComponent
  ],
  templateUrl: './job-form.component.html',
  styleUrl: './job-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JobFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  protected readonly layoutService = inject(LayoutService);
  private readonly departmentApi = inject(DepartmentApiService);
  private readonly notification = inject(NotificationService);

  @Input() mode: 'create' | 'edit' = 'create';
  @Input() isLoading = false;
  
  @Input() set job(value: JobDetailsResponseDto | null | undefined) {
    if (value) {
      this.populateForm(value);
    }
  }

  @Output() readonly save = new EventEmitter<any>();
  @Output() readonly cancel = new EventEmitter<void>();

  protected readonly jobForm: FormGroup;
  protected readonly minDate = new Date();
  protected readonly separatorKeysCodes: number[] = [ENTER, COMMA];

  // State Signals
  protected readonly departments = signal<DepartmentSummary[]>([]);
  protected readonly isDeptLoading = signal<boolean>(false);
  protected readonly skills = signal<string[]>([]);
  protected readonly currentStatus = signal<JobStatus | null>(null);

  constructor() {
    this.jobForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      departmentId: ['', [Validators.required]],
      employmentType: [0, [Validators.required]],
      location: ['', [Validators.required]],
      closingDate: [null],
      hiringManagerPlaceholder: [''],
      salaryMin: [null, [Validators.min(0), Validators.max(10000000)]],
      salaryMax: [null, [Validators.min(0), Validators.max(10000000)]],
      description: ['', [Validators.required]],
      requirements: ['', [Validators.required]]
    }, {
      validators: [salaryRangeValidator, this.salaryPresenceValidator]
    });
  }

  ngOnInit(): void {
    this.loadDepartments();
  }

  get formDirty(): boolean {
    return this.jobForm.dirty;
  }

  markAsPristine(): void {
    this.jobForm.markAsPristine();
  }

  disableForm(): void {
    this.jobForm.disable();
  }

  enableForm(): void {
    this.jobForm.enable();
  }

  setErrors(errors: { [key: string]: string }): void {
    Object.keys(errors).forEach(key => {
      this.jobForm.get(key)?.setErrors({ backendError: errors[key] });
    });
    this.scrollToFirstInvalid();
  }

  private loadDepartments(): void {
    this.isDeptLoading.set(true);
    this.departmentApi.getDepartments().subscribe({
      next: (response) => {
        this.isDeptLoading.set(false);
        if (response.success && response.data) {
          this.departments.set(response.data);
        } else {
          this.notification.error('Failed to load departments list.');
        }
      },
      error: () => {
        this.isDeptLoading.set(false);
        this.notification.error('A network error occurred while loading departments.');
      }
    });
  }

  private populateForm(data: JobDetailsResponseDto): void {
    this.currentStatus.set(data.status);
    
    this.jobForm.patchValue({
      title: data.title,
      departmentId: data.department.id,
      employmentType: data.employmentType,
      location: data.location,
      closingDate: data.closingDate ? new Date(data.closingDate) : null,
      hiringManagerPlaceholder: data.hiringManager ? data.hiringManager.fullName : '',
      salaryMin: data.salaryMin,
      salaryMax: data.salaryMax,
      description: data.description,
      requirements: data.requirements
    });

    if (data.requiredSkills) {
      this.skills.set(data.requiredSkills.map(s => s.name));
    }
  }

  // Cross-Field Presence Validator
  private salaryPresenceValidator(group: AbstractControl): ValidationErrors | null {
    const minCtrl = group.get('salaryMin');
    const maxCtrl = group.get('salaryMax');
    if (!minCtrl || !maxCtrl) return null;

    const min = minCtrl.value;
    const max = maxCtrl.value;

    const minHasValue = min !== null && min !== '';
    const maxHasValue = max !== null && max !== '';

    // Clear previous errors
    if (minCtrl.hasError('requiredIfOtherSet')) {
      const { requiredIfOtherSet, ...remaining } = minCtrl.errors || {};
      minCtrl.setErrors(Object.keys(remaining).length ? remaining : null);
    }
    if (maxCtrl.hasError('requiredIfOtherSet')) {
      const { requiredIfOtherSet, ...remaining } = maxCtrl.errors || {};
      maxCtrl.setErrors(Object.keys(remaining).length ? remaining : null);
    }

    if (minHasValue && !maxHasValue) {
      maxCtrl.setErrors({ ...maxCtrl.errors, requiredIfOtherSet: true });
    } else if (maxHasValue && !minHasValue) {
      minCtrl.setErrors({ ...minCtrl.errors, requiredIfOtherSet: true });
    }

    return null;
  }

  // Dynamic Chip Methods
  protected addSkill(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value) {
      this.skills.update(current => [...current, value]);
      this.jobForm.markAsDirty();
    }
    event.chipInput!.clear();
  }

  protected removeSkill(skill: string): void {
    this.skills.update(current => current.filter(s => s !== skill));
    this.jobForm.markAsDirty();
  }

  // Error Counter
  protected getValidationErrorCount(): number {
    let count = 0;
    Object.keys(this.jobForm.controls).forEach(key => {
      const control = this.jobForm.get(key);
      if (control && control.invalid) {
        count++;
      }
    });
    if (this.jobForm.hasError('salaryRangeInvalid')) {
      count++;
    }
    return count;
  }


  protected onSubmit(): void {
    if (this.jobForm.invalid) {
      this.jobForm.markAllAsTouched();
      this.scrollToFirstInvalid();
      return;
    }

    const formValues = {
      ...this.jobForm.value,
      requiredSkills: this.skills()
    };

    this.save.emit(formValues);
  }

  protected onCancel(): void {
    this.cancel.emit();
  }

  private scrollToFirstInvalid(): void {
    setTimeout(() => {
      const firstInvalid = document.querySelector('.ng-invalid[formControlName], .ng-invalid[formGroup], .mat-form-field-invalid');
      if (firstInvalid) {
        firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
        const inputEl = firstInvalid.querySelector('input, select, textarea') as HTMLElement;
        if (inputEl) inputEl.focus();
      }
    }, 100);
  }
}
