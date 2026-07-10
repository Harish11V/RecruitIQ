import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { CandidateStatus, CandidateStatusNames } from '../../models/candidate.models';
import { CandidateStore } from '../../state/candidate.store';
import { ConfirmationDialogComponent } from '../../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';

export interface ChangeStatusDialogData {
  candidateId: string;
  currentStatus: number;
  rowVersion: string;
}

@Component({
  selector: 'app-change-candidate-status-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatIconModule
  ],
  providers: [CandidateStore],
  templateUrl: './change-candidate-status-dialog.component.html',
  styleUrl: './change-candidate-status-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChangeCandidateStatusDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ChangeCandidateStatusDialogComponent>);
  protected readonly data = inject<ChangeStatusDialogData>(MAT_DIALOG_DATA);
  protected readonly store = inject(CandidateStore);
  private readonly dialog = inject(MatDialog);

  protected readonly statusNames = CandidateStatusNames;

  // Form setup
  protected readonly statusForm = new FormGroup({
    newStatus: new FormControl<number | null>(null, [Validators.required]),
    reason: new FormControl<string>('')
  });

  // Allowed transitions map
  private readonly transitionMap: Record<number, number[]> = {
    [CandidateStatus.New]: [CandidateStatus.Available, CandidateStatus.Inactive],
    [CandidateStatus.Available]: [CandidateStatus.Shortlisted, CandidateStatus.Inactive],
    [CandidateStatus.Shortlisted]: [CandidateStatus.Interviewing, CandidateStatus.Rejected],
    [CandidateStatus.Interviewing]: [CandidateStatus.Offered, CandidateStatus.Rejected],
    [CandidateStatus.Offered]: [CandidateStatus.Hired, CandidateStatus.Rejected],
    [CandidateStatus.Hired]: [CandidateStatus.Inactive],
    [CandidateStatus.Rejected]: [CandidateStatus.Available],
    [CandidateStatus.Inactive]: [CandidateStatus.Available]
  };

  protected readonly allowedStatuses = computed(() => {
    const next = this.transitionMap[this.data.currentStatus] || [];
    return next.map(statusVal => ({
      value: statusVal,
      label: CandidateStatusNames[statusVal]
    }));
  });

  protected onCancel(): void {
    this.dialogRef.close(null);
  }

  protected onSubmit(): void {
    if (this.statusForm.invalid) return;

    const newStatus = this.statusForm.controls.newStatus.value!;

    // Check for high-stake transitions needing confirmation (Hired or Rejected)
    if (newStatus === CandidateStatus.Hired || newStatus === CandidateStatus.Rejected) {
      const isHired = newStatus === CandidateStatus.Hired;
      const confirmDialogRef = this.dialog.open(ConfirmationDialogComponent, {
        width: '400px',
        data: {
          title: isHired ? 'Confirm Candidate Hire' : 'Confirm Candidate Rejection',
          message: isHired 
            ? 'Are you sure you want to mark this candidate as Hired? This transitions them to the end of the recruitment cycle.' 
            : 'Are you sure you want to reject this candidate? This will end their active applications.',
          confirmButtonText: isHired ? 'Hire' : 'Reject',
          confirmButtonColor: isHired ? 'primary' : 'warn',
          icon: isHired ? 'check_circle' : 'cancel',
          warningText: isHired 
            ? 'This will set the candidate status to Hired.' 
            : 'This will set the candidate status to Rejected.'
        }
      });

      confirmDialogRef.afterClosed().subscribe(confirmed => {
        if (confirmed === true) {
          this.executeStatusChange(newStatus);
        }
      });
    } else {
      this.executeStatusChange(newStatus);
    }
  }

  private executeStatusChange(newStatus: number): void {
    this.store.changeStatus(
      this.data.candidateId,
      newStatus,
      this.data.rowVersion,
      (newRowVersion) => {
        this.dialogRef.close({ newStatus, newRowVersion });
      }
    );
  }
}
