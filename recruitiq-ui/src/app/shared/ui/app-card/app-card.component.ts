import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-card',
  standalone: true,
  templateUrl: './app-card.component.html',
  styleUrl: './app-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppCardComponent {
  readonly hoverable = input<boolean>(false);
  readonly hasHeader = input<boolean>(true);
  readonly hasFooter = input<boolean>(false);
}
// Keep selector name to match custom prefix "app-card"
