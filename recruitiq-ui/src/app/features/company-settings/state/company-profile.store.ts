import { inject, Injectable, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { CompanyProfileApiService } from '../services/company-profile-api.service';
import { CompanySettingsResponseDto, UpdateCompanySettingsRequestDto } from '../models/company-profile.models';
import { NotificationService } from '../../../core/services/notification.service';
import { ConcurrencyConflictDialogComponent } from '../../../shared/dialogs/concurrency-conflict-dialog/concurrency-conflict-dialog.component';
import { mapBusinessError } from '../../../core/errors/business-error-mapper';

@Injectable()
export class CompanyProfileStore {
  private readonly api = inject(CompanyProfileApiService);
  private readonly dialog = inject(MatDialog);
  private readonly notification = inject(NotificationService);

  readonly settings = signal<CompanySettingsResponseDto | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly isLogoUploading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly saveSuccessMessage = signal<string | null>(null);

  loadSettings(silent = false): void {
    if (!silent) {
      this.isLoading.set(true);
    }
    this.error.set(null);

    this.api.getSettings().pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.settings.set(response.data);
        } else {
          this.error.set(response.message || 'Failed to load company profile.');
          this.notification.error(response.message || 'Failed to load company profile.');
        }
      },
      error: (err: HttpErrorResponse) => {
        const msg = err.error?.message || 'A network error occurred. Please try again.';
        this.error.set(msg);
        this.notification.error(msg);
      }
    });
  }

  updateSettings(dto: UpdateCompanySettingsRequestDto, onFormReset?: () => void): void {
    this.isSaving.set(true);
    this.saveSuccessMessage.set(null);

    this.api.updateSettings(dto).pipe(
      finalize(() => this.isSaving.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.notification.success('Company settings updated successfully.');
          this.saveSuccessMessage.set('Last updated successfully');
          // Reload settings to get the fresh RowVersion
          this.loadSettings(true);
        } else {
          this.notification.error(response.message || 'Failed to save settings.');
        }
      },
      error: (err: HttpErrorResponse) => {
        this.handleApiError(err, () => {
          this.loadSettings();
          if (onFormReset) onFormReset();
        });
      }
    });
  }

  uploadLogo(file: File): void {
    this.isLogoUploading.set(true);
    this.api.uploadLogo(file).pipe(
      finalize(() => this.isLogoUploading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.notification.success('Company logo uploaded successfully.');
          // Reload settings to sync logo URL & RowVersion
          this.loadSettings(true);
        } else {
          this.notification.error(response.message || 'Failed to upload logo.');
        }
      },
      error: (err: HttpErrorResponse) => {
        const msg = err.error?.message || 'Failed to upload logo.';
        this.notification.error(mapBusinessError(msg));
      }
    });
  }

  deleteLogo(): void {
    this.isLogoUploading.set(true);
    this.api.deleteLogo().pipe(
      finalize(() => this.isLogoUploading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.notification.success('Company logo removed successfully.');
          this.loadSettings(true);
        } else {
          this.notification.error(response.message || 'Failed to remove logo.');
        }
      },
      error: (err: HttpErrorResponse) => {
        const msg = err.error?.message || 'Failed to remove logo.';
        this.notification.error(mapBusinessError(msg));
      }
    });
  }

  private handleApiError(err: HttpErrorResponse, reloadCallback: () => void): void {
    if (err.status === 409) {
      const dialogRef = this.dialog.open(ConcurrencyConflictDialogComponent, {
        width: '460px',
        disableClose: true,
        data: {
          title: 'Company Settings Modified',
          message: 'These settings were updated by another administrator while you were making changes. Please reload to see the latest version.',
          reloadButtonText: 'Reload Settings',
          cancelButtonText: 'Cancel'
        }
      });

      dialogRef.afterClosed().subscribe((result: unknown) => {
        if (result === 'reload') {
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
