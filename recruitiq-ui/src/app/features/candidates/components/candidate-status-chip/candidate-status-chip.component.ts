import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-candidate-status-chip',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './candidate-status-chip.component.html',
  styleUrl: './candidate-status-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CandidateStatusChipComponent {
  @Input({ required: true }) status!: string;

  protected getStatusClass(): string {
    const s = this.status ? this.status.toLowerCase() : '';
    switch (s) {
      case 'new':
        return 'status-new';
      case 'available':
        return 'status-available';
      case 'shortlisted':
        return 'status-shortlisted';
      case 'interviewing':
        return 'status-interviewing';
      case 'offered':
        return 'status-offered';
      case 'hired':
        return 'status-hired';
      case 'rejected':
        return 'status-rejected';
      case 'inactive':
      default:
        return 'status-inactive';
    }
  }

  protected getStatusLabel(): string {
    return this.status || 'Unknown';
  }
}
