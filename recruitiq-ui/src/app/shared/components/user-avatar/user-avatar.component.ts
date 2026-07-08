import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-user-avatar',
  standalone: true,
  templateUrl: './user-avatar.component.html',
  styleUrl: './user-avatar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserAvatarComponent {
  readonly name = input.required<string>();
  readonly imageUrl = input<string | null>(null);
  readonly size = input<number>(40);
  readonly status = input<'online' | 'offline' | 'away' | null>(null);

  protected readonly initials = computed(() => {
    const fullName = this.name().trim();
    if (!fullName) return '?';
    const parts = fullName.split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return fullName[0].toUpperCase();
  });

  protected readonly statusSize = computed(() => {
    const currentSize = this.size();
    return Math.max(8, Math.floor(currentSize * 0.28));
  });

  protected readonly backgroundColor = computed(() => {
    const text = this.name();
    let hash = 0;
    for (let i = 0; i < text.length; i++) {
      hash = text.charCodeAt(i) + ((hash << 5) - hash);
    }
    const colors = [
      '#3f51b5', // Indigo
      '#009688', // Teal
      '#3b82f6', // Blue
      '#6366f1', // Indigo light
      '#8b5cf6', // Violet
      '#ec4899', // Pink
      '#f97316', // Orange
      '#14b8a6'  // Teal light
    ];
    const index = Math.abs(hash) % colors.length;
    return colors[index];
  });
}
