import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-detail-row',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './detail-row.component.html',
  styleUrl: './detail-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DetailRowComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value: string | number | null | undefined;
  @Input() icon?: string;
  @Input() isMultiline = false;
}
