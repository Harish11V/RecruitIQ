import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { AppCardComponent } from '../../../../shared/ui/app-card/app-card.component';

@Component({
  selector: 'app-candidate-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatDividerModule,
    AppCardComponent
  ],
  templateUrl: './candidate-form.component.html',
  styleUrl: './candidate-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CandidateFormComponent {
  readonly formGroup = input.required<FormGroup>();
  readonly isSaving = input<boolean>(false);
  readonly isEditMode = input<boolean>(false);

  readonly save = output<void>();
  readonly cancel = output<void>();

  protected onSubmit(): void {
    if (this.formGroup().valid && !this.isSaving()) {
      this.save.emit();
    }
  }

  protected onCancel(): void {
    this.cancel.emit();
  }
}
