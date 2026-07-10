import { inject, Injectable, signal } from '@angular/core';
import { HttpEvent, HttpEventType, HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { ResumeApiService } from '../services/resume-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { mapBusinessError } from '../../../core/errors/business-error-mapper';

@Injectable({
  providedIn: 'root'
})
export class ResumeStore {
  private readonly api = inject(ResumeApiService);
  private readonly notification = inject(NotificationService);

  readonly isUploading = signal<boolean>(false);
  readonly uploadProgress = signal<number>(0);
  readonly isDeleting = signal<boolean>(false);
  readonly isSettingPrimary = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  uploadResume(candidateId: string, file: File, onSuccess: () => void): void {
    this.isUploading.set(true);
    this.uploadProgress.set(0);
    this.error.set(null);

    this.api.uploadResume(candidateId, file)
      .subscribe({
        next: (event: HttpEvent<any>) => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            const progress = Math.round((100 * event.loaded) / event.total);
            this.uploadProgress.set(progress);
          } else if (event.type === HttpEventType.Response) {
            const body = event.body;
            this.isUploading.set(false);
            if (body && body.success) {
              this.notification.success('Resume uploaded successfully.');
              onSuccess();
            } else {
              const msg = body?.message || 'Failed to upload resume.';
              this.error.set(msg);
              this.notification.error(msg);
            }
          }
        },
        error: (err: HttpErrorResponse) => {
          this.isUploading.set(false);
          const errorMsg = err.error?.message || 'An error occurred during resume upload.';
          this.error.set(errorMsg);
          this.notification.error(errorMsg);
        }
      });
  }

  deleteResume(candidateId: string, resumeId: string, onSuccess: () => void): void {
    this.isDeleting.set(true);
    this.error.set(null);

    this.api.deleteResume(candidateId, resumeId)
      .pipe(finalize(() => this.isDeleting.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notification.success('Resume deleted successfully.');
            onSuccess();
          } else {
            const msg = response.message || 'Failed to delete resume.';
            this.error.set(msg);
            this.notification.error(msg);
          }
        },
        error: (err: HttpErrorResponse) => {
          const errorMsg = err.error?.message || 'An error occurred while deleting resume.';
          this.error.set(errorMsg);
          this.notification.error(errorMsg);
        }
      });
  }

  setPrimaryResume(candidateId: string, resumeId: string, onSuccess: () => void): void {
    this.isSettingPrimary.set(true);
    this.error.set(null);

    this.api.setPrimaryResume(candidateId, resumeId)
      .pipe(finalize(() => this.isSettingPrimary.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notification.success('Primary resume updated successfully.');
            onSuccess();
          } else {
            const msg = response.message || 'Failed to set primary resume.';
            this.error.set(msg);
            this.notification.error(msg);
          }
        },
        error: (err: HttpErrorResponse) => {
          const errorMsg = err.error?.message || 'An error occurred while setting primary resume.';
          this.error.set(errorMsg);
          this.notification.error(errorMsg);
        }
      });
  }
}
