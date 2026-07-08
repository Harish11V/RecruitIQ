import { ChangeDetectionStrategy, Component, Inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ThemePalette } from '@angular/material/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable } from 'rxjs';

export interface ConfirmationDialogData {
  title: string;
  message: string;
  confirmButtonText: string;
  confirmButtonColor?: ThemePalette; // 'primary' | 'accent' | 'warn'
  icon?: string;
  warningText?: string;
  confirmAction?: () => Observable<unknown>;
}

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './confirmation-dialog.component.html',
  styleUrl: './confirmation-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmationDialogComponent {
  protected readonly isLoading = signal<boolean>(false);

  constructor(
    protected readonly dialogRef: MatDialogRef<ConfirmationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) protected readonly data: ConfirmationDialogData
  ) {}

  protected onConfirm(): void {
    if (this.data.confirmAction) {
      this.isLoading.set(true);
      this.data.confirmAction().subscribe({
        next: (result) => {
          this.isLoading.set(false);
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.isLoading.set(false);
          // Pass error back so caller can map it
          this.dialogRef.close({ error: err });
        }
      });
    } else {
      this.dialogRef.close(true);
    }
  }

  protected getIconColor(): string {
    switch (this.data.confirmButtonColor) {
      case 'warn': return '#ef4444'; // red
      case 'accent': return '#f59e0b'; // amber
      default: return '#3f51b5'; // primary indigo
    }
  }
}
