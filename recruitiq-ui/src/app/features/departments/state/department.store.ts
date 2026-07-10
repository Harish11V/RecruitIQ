import { inject, Injectable, signal, effect } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { DepartmentApiService } from '../services/department-api.service';
import { DepartmentResponseDto, CreateDepartmentRequestDto, UpdateDepartmentRequestDto } from '../models/department.models';
import { NotificationService } from '../../../core/services/notification.service';
import { ConcurrencyConflictDialogComponent } from '../../../shared/dialogs/concurrency-conflict-dialog/concurrency-conflict-dialog.component';
import { mapBusinessError } from '../../../core/errors/business-error-mapper';

@Injectable()
export class DepartmentStore {
  private readonly api = inject(DepartmentApiService);
  private readonly dialog = inject(MatDialog);
  private readonly notification = inject(NotificationService);

  readonly departments = signal<DepartmentResponseDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  readonly searchTerm = signal<string>('');

  constructor() {
    // Automatically trigger department loading when the search term signal changes
    effect(() => {
      this.loadDepartments(this.searchTerm());
    });
  }

  loadDepartments(search?: string, silent = false): void {
    if (!silent) {
      this.isLoading.set(true);
    }
    this.error.set(null);

    this.api.getDepartments(search).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.departments.set(response.data);
        } else {
          this.error.set(response.message || 'Failed to retrieve departments.');
          this.notification.error(response.message || 'Failed to retrieve departments.');
        }
      },
      error: (err: HttpErrorResponse) => {
        const msg = err.error?.message || 'A network error occurred. Please try again.';
        this.error.set(msg);
        this.notification.error(msg);
      }
    });
  }

  createDepartment(dto: CreateDepartmentRequestDto, onSuccess?: () => void): void {
    this.isSaving.set(true);
    this.api.createDepartment(dto).pipe(
      finalize(() => this.isSaving.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.notification.success('Department created successfully.');
          this.loadDepartments(this.searchTerm(), true);
          if (onSuccess) onSuccess();
        } else {
          this.notification.error(response.message || 'Failed to create department.');
        }
      },
      error: (err: HttpErrorResponse) => {
        this.handleApiError(err);
      }
    });
  }

  updateDepartment(id: string, dto: UpdateDepartmentRequestDto, onSuccess?: () => void): void {
    this.isSaving.set(true);
    this.api.updateDepartment(id, dto).pipe(
      finalize(() => this.isSaving.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.notification.success('Department updated successfully.');
          this.loadDepartments(this.searchTerm(), true);
          if (onSuccess) onSuccess();
        } else {
          this.notification.error(response.message || 'Failed to update department.');
        }
      },
      error: (err: HttpErrorResponse) => {
        this.handleApiError(err, () => {
          this.loadDepartments(this.searchTerm());
          if (onSuccess) onSuccess();
        });
      }
    });
  }

  deleteDepartment(id: string, rowVersion: string, onSuccess?: () => void): void {
    this.api.deleteDepartment(id, rowVersion).subscribe({
      next: (response) => {
        if (response.success) {
          this.notification.success('Department deleted successfully.');
          this.loadDepartments(this.searchTerm(), true);
          if (onSuccess) onSuccess();
        } else {
          this.notification.error(response.message || 'Failed to delete department.');
        }
      },
      error: (err: HttpErrorResponse) => {
        this.handleApiError(err, () => {
          this.loadDepartments(this.searchTerm());
          if (onSuccess) onSuccess();
        });
      }
    });
  }

  private handleApiError(err: HttpErrorResponse, reloadCallback?: () => void): void {
    if (err.status === 409) {
      const dialogRef = this.dialog.open(ConcurrencyConflictDialogComponent, {
        width: '460px',
        disableClose: true,
        data: {
          title: 'Department Modified',
          message: 'This department was updated by another user while you were making changes. Please reload to see the latest version.',
          reloadButtonText: 'Reload Department',
          cancelButtonText: 'Cancel'
        }
      });

      dialogRef.afterClosed().subscribe((result: unknown) => {
        if (result === 'reload' && reloadCallback) {
          reloadCallback();
        }
      });
    } else {
      let userFriendlyMsg = 'A network error occurred. Please try again.';
      if (err.error && err.error.message) {
        userFriendlyMsg = mapBusinessError(err.error.message);
      } else if (err.error && err.error.errors && err.error.errors.length > 0) {
        userFriendlyMsg = mapBusinessError(err.error.errors[0]);
      } else if (err.message) {
        userFriendlyMsg = err.message;
      }
      this.notification.error(userFriendlyMsg);
    }
  }
}
