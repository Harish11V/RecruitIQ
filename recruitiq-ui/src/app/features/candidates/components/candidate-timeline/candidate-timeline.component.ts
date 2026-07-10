import { ChangeDetectionStrategy, Component, inject, input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CandidateTimelineStore } from '../../state/candidate-timeline.store';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';

@Component({
  selector: 'app-candidate-timeline',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    UserAvatarComponent
  ],
  providers: [CandidateTimelineStore],
  templateUrl: './candidate-timeline.component.html',
  styleUrl: './candidate-timeline.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CandidateTimelineComponent implements OnInit {
  protected readonly store = inject(CandidateTimelineStore);
  readonly candidateId = input.required<string>();

  ngOnInit(): void {
    this.loadTimeline();
  }

  protected loadTimeline(): void {
    this.store.loadTimeline(this.candidateId());
  }

  protected getBgColorClass(color: string): string {
    switch (color) {
      case 'blue': return 'bg-blue';
      case 'green': return 'bg-green';
      case 'red': return 'bg-red';
      case 'amber': return 'bg-amber';
      case 'purple': return 'bg-purple';
      case 'indigo': return 'bg-indigo';
      default: return 'bg-gray';
    }
  }

  protected getTextColorClass(color: string): string {
    switch (color) {
      case 'blue': return 'text-blue';
      case 'green': return 'text-green';
      case 'red': return 'text-red';
      case 'amber': return 'text-amber';
      case 'purple': return 'text-purple';
      case 'indigo': return 'text-indigo';
      default: return 'text-gray';
    }
  }

  protected getRelativeTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSecs = Math.floor(diffMs / 1000);
    const diffMins = Math.floor(diffSecs / 60);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffSecs < 60) {
      return 'Just now';
    } else if (diffMins < 60) {
      return diffMins === 1 ? '1 minute ago' : `${diffMins} minutes ago`;
    } else if (diffHours < 24) {
      return diffHours === 1 ? '1 hour ago' : `${diffHours} hours ago`;
    } else if (diffDays === 1) {
      return 'Yesterday';
    } else if (diffDays < 7) {
      return `${diffDays} days ago`;
    } else {
      const diffWeeks = Math.floor(diffDays / 7);
      if (diffWeeks === 1) {
        return 'Last week';
      } else if (diffWeeks < 4) {
        return `${diffWeeks} weeks ago`;
      } else {
        const diffMonths = Math.floor(diffDays / 30);
        if (diffMonths === 1) {
          return '1 month ago';
        } else if (diffMonths < 12) {
          return `${diffMonths} months ago`;
        } else {
          return 'Over a year ago';
        }
      }
    }
  }
}
