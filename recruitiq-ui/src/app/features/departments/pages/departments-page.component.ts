import { ChangeDetectionStrategy, Component, inject, OnInit, OnDestroy } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';

// UI Foundation Imports
import { PageContainerComponent } from '../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../shared/ui/section-header/section-header.component';
import { AppCardComponent } from '../../../shared/ui/app-card/app-card.component';
import { ConfirmationDialogComponent } from '../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';

// Custom Store, Dialog & Models
import { DepartmentStore } from '../state/department.store';
import { DepartmentResponseDto } from '../models/department.models';
import { DepartmentFormDialogComponent } from '../components/department-form-dialog/department-form-dialog.component';
import { LayoutService } from '../../../layout/services/layout.service';

@Component({
  selector: 'app-departments-page',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatMenuModule,
    MatTooltipModule,
    PageContainerComponent,
    SectionHeaderComponent,
    AppCardComponent
  ],
  providers: [DepartmentStore],
  templateUrl: './departments-page.component.html',
  styleUrl: './departments-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DepartmentsPageComponent implements OnInit, OnDestroy {
  private readonly dialog = inject(MatDialog);
  protected readonly store = inject(DepartmentStore);
  protected readonly layoutService = inject(LayoutService);

  protected readonly searchControl = new FormControl('');
  private readonly destroy$ = new Subject<void>();

  ngOnInit(): void {
    // Debounce search input changes and update the store's search signal
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(value => {
      this.store.searchTerm.set(value || '');
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  protected onRefresh(): void {
    this.store.loadDepartments(this.store.searchTerm());
  }

  protected openFormDialog(mode: 'create' | 'edit', department?: DepartmentResponseDto): void {
    const dialogRef = this.dialog.open(DepartmentFormDialogComponent, {
      width: '460px',
      disableClose: true,
      data: { mode, department }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        if (mode === 'create') {
          this.store.createDepartment({
            name: result.name,
            description: result.description || null
          });
        } else if (mode === 'edit' && department) {
          this.store.updateDepartment(department.id, {
            name: result.name,
            description: result.description || null,
            rowVersion: department.rowVersion
          });
        }
      }
    });
  }

  protected onDelete(department: DepartmentResponseDto): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '460px',
      disableClose: true,
      data: {
        title: 'Delete Department',
        message: `Are you sure you want to delete the department "${department.name}"?`,
        warningText: 'Deleting a department is only possible when no jobs are assigned to it.',
        confirmButtonText: 'Delete',
        confirmButtonColor: 'warn',
        icon: 'delete_forever',
        confirmAction: () => this.store.deleteDepartment(department.id, department.rowVersion)
      }
    });

    dialogRef.afterClosed().subscribe((result: unknown) => {
      // The store handles notifications and lists reloading internally
    });
  }
}
