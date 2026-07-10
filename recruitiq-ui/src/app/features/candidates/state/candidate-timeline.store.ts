import { inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { CandidateTimelineApiService } from '../services/candidate-timeline-api.service';
import { CandidateTimelineItemResponse } from '../models/candidate.models';
import { NotificationService } from '../../../core/services/notification.service';

@Injectable()
export class CandidateTimelineStore {
  private readonly api = inject(CandidateTimelineApiService);
  private readonly notification = inject(NotificationService);

  readonly timelineItems = signal<CandidateTimelineItemResponse[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  loadTimeline(candidateId: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.api.getCandidateTimeline(candidateId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.timelineItems.set(response.data);
          } else {
            const msg = response.message || 'Failed to load activity timeline.';
            this.error.set(msg);
            this.notification.error(msg);
          }
        },
        error: (err) => {
          const msg = err.error?.message || 'A network error occurred while loading timeline.';
          this.error.set(msg);
          this.notification.error(msg);
        }
      });
  }
}
