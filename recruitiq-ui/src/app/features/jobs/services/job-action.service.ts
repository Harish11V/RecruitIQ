import { inject, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

// Core Services & Mappers
import { NotificationService } from '../../../core/services/notification.service';
import { mapBusinessError } from '../../../core/errors/business-error-mapper';
import { JobsApiService } from './jobs-api.service';

// Shared Dialogs
import { ConfirmationDialogComponent } from '../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';
import { ConcurrencyConflictDialogComponent } from '../../../shared/dialogs/concurrency-conflict-dialog/concurrency-conflict-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class JobActionService {
  private readonly dialog = inject(MatDialog);
  private readonly jobsApi = inject(JobsApiService);
  private readonly notification = inject(NotificationService);

  publish(id: string, rowVersion: string, title: string, onSuccess?: () => void): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '420px',
      disableClose: true,
      data: {
        title: 'Publish Job',
        message: `This will make the job "${title}" available for candidates.`,
        confirmButtonText: 'Publish',
        confirmButtonColor: 'primary',
        icon: 'rocket_launch',
        confirmAction: () => this.jobsApi.publishJob(id, { rowVersion })
      }
    });

    dialogRef.afterClosed().subscribe((result: unknown) => {
      const r = result as { error?: HttpErrorResponse } | boolean | null;
      if (r === true) {
        this.notification.success('Job published successfully.');
        if (onSuccess) onSuccess();
      } else if (r && typeof r === 'object' && 'error' in r && r.error) {
        this.handleApiError(r.error, onSuccess);
      }
    });
  }

  archive(id: string, rowVersion: string, title: string, onSuccess?: () => void): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '420px',
      disableClose: true,
      data: {
        title: 'Archive Job',
        message: `Archived jobs can no longer receive new applications. Are you sure you want to archive "${title}"?`,
        confirmButtonText: 'Archive',
        confirmButtonColor: 'accent',
        icon: 'archive',
        confirmAction: () => this.jobsApi.archiveJob(id, { rowVersion })
      }
    });

    dialogRef.afterClosed().subscribe((result: unknown) => {
      const r = result as { error?: HttpErrorResponse } | boolean | null;
      if (r === true) {
        this.notification.success('Job archived successfully.');
        if (onSuccess) onSuccess();
      } else if (r && typeof r === 'object' && 'error' in r && r.error) {
        this.handleApiError(r.error, onSuccess);
      }
    });
  }

  delete(id: string, rowVersion: string, title: string, onSuccess?: () => void): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '420px',
      disableClose: true,
      data: {
        title: 'Delete Job',
        message: `Are you sure you want to delete the job "${title}"?`,
        warningText: 'This performs a soft delete. Only archived jobs can be deleted.',
        confirmButtonText: 'Delete',
        confirmButtonColor: 'warn',
        icon: 'delete_forever',
        confirmAction: () => this.jobsApi.deleteJob(id, { rowVersion })
      }
    });

    dialogRef.afterClosed().subscribe((result: unknown) => {
      const r = result as { error?: HttpErrorResponse } | boolean | null;
      if (r === true) {
        this.notification.success('Job deleted successfully.');
        if (onSuccess) onSuccess();
      } else if (r && typeof r === 'object' && 'error' in r && r.error) {
        this.handleApiError(r.error, onSuccess);
      }
    });
  }

  private handleApiError(err: HttpErrorResponse, reloadCallback?: () => void): void {
    if (err.status === 409) {
      const dialogRef = this.dialog.open(ConcurrencyConflictDialogComponent, {
        width: '460px',
        disableClose: true,
        data: {
          title: 'This job has been modified',
          message: 'This job was updated by another user while you were performing this action. Please reload the latest version.',
          reloadButtonText: 'Reload Latest',
          cancelButtonText: 'Cancel'
        }
      });

      dialogRef.afterClosed().subscribe((reloadResult: unknown) => {
        if (reloadResult === 'reload' && reloadCallback) {
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
