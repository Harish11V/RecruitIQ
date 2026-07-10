import { ChangeDetectionStrategy, Component, Inject, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DepartmentResponseDto } from '../../models/department.models';

export interface DepartmentFormDialogData {
  mode: 'create' | 'edit';
  department?: DepartmentResponseDto;
}

@Component({
  selector: 'app-department-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './department-form-dialog.component.html',
  styleUrl: './department-form-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DepartmentFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  protected readonly dialogRef = inject(MatDialogRef<DepartmentFormDialogComponent>);

  protected departmentForm!: FormGroup;
  protected isSaving = false;

  constructor(
    @Inject(MAT_DIALOG_DATA) protected readonly data: DepartmentFormDialogData
  ) {
    this.departmentForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]]
    });
  }

  ngOnInit(): void {
    if (this.data.mode === 'edit' && this.data.department) {
      this.departmentForm.patchValue({
        name: this.data.department.name,
        description: this.data.department.description || ''
      });
    }
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }

  protected onSubmit(): void {
    if (this.departmentForm.invalid) {
      this.departmentForm.markAllAsTouched();
      return;
    }

    const value = this.departmentForm.value;
    this.dialogRef.close(value);
  }
}
