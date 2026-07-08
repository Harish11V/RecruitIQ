import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

export interface ConcurrencyConflictDialogData {
  title: string;
  message: string;
  reloadButtonText: string;
  cancelButtonText: string;
}

@Component({
  selector: 'app-concurrency-conflict-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  templateUrl: './concurrency-conflict-dialog.component.html',
  styleUrl: './concurrency-conflict-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConcurrencyConflictDialogComponent {
  constructor(
    protected readonly dialogRef: MatDialogRef<ConcurrencyConflictDialogComponent>,
    @Inject(MAT_DIALOG_DATA) protected readonly data: ConcurrencyConflictDialogData
  ) {}
}
