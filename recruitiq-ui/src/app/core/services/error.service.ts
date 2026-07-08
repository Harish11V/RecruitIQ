import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { NotificationService } from './notification.service';

@Injectable({
  providedIn: 'root'
})
export class ErrorService {
  private readonly notifier = inject(NotificationService);

  handleError(error: unknown): void {
    let message = 'An unexpected error occurred.';

    if (error instanceof HttpErrorResponse) {
      message = error.error?.message || error.message || message;
    } else if (error instanceof Error) {
      message = error.message;
    }

    this.notifier.error(message);
    console.error('[ErrorService] Caught exception:', error);
  }
}
