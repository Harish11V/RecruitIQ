import { ChangeDetectionStrategy, Component, computed, Input, signal } from '@angular/core';
import { JobStatus } from '../../models/job.models';

@Component({
  selector: 'app-job-status-chip',
  standalone: true,
  templateUrl: './job-status-chip.component.html',
  styleUrl: './job-status-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JobStatusChipComponent {
  private readonly _status = signal<JobStatus | null>(null);

  @Input({ required: true }) set status(value: JobStatus | null | undefined) {
    this._status.set(value ?? null);
  }

  protected readonly statusText = computed(() => {
    const val = this._status();
    if (val === null) return 'Unknown';
    switch (val) {
      case JobStatus.Draft: return 'Draft';
      case JobStatus.Published: return 'Published';
      case JobStatus.Closed: return 'Closed';
      case JobStatus.Archived: return 'Archived';
      default: return 'Unknown';
    }
  });

  protected readonly statusClass = computed(() => {
    const val = this._status();
    if (val === null) return '';
    switch (val) {
      case JobStatus.Draft: return 'draft';
      case JobStatus.Published: return 'published';
      case JobStatus.Closed: return 'closed';
      case JobStatus.Archived: return 'archived';
      default: return '';
    }
  });
}
