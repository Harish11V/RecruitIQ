import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserAvatarComponent } from '../user-avatar/user-avatar.component';

@Component({
  selector: 'app-entity-header',
  standalone: true,
  imports: [CommonModule, UserAvatarComponent],
  templateUrl: './entity-header.component.html',
  styleUrl: './entity-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EntityHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly avatarName = input<string | null>(null);
  readonly avatarImageUrl = input<string | null>(null);
  readonly avatarSize = input<number>(56);
}
