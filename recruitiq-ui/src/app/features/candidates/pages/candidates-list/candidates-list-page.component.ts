import { ChangeDetectionStrategy, Component, inject, OnInit, OnDestroy } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatButtonModule as MatBtnModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';

// UI Foundation Imports
import { PageContainerComponent } from '../../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../../shared/ui/section-header/section-header.component';
import { AppCardComponent } from '../../../../shared/ui/app-card/app-card.component';

// Store, models & notification
import { CandidateStore } from '../../state/candidate.store';
import { CandidateSummaryResponse } from '../../models/candidate.models';
import { NotificationService } from '../../../../core/services/notification.service';
import { LayoutService } from '../../../../layout/services/layout.service';

import { CandidateStatusChipComponent } from '../../components/candidate-status-chip/candidate-status-chip.component';

import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-candidates-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatBtnModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatMenuModule,
    MatTooltipModule,
    MatPaginatorModule,
    PageContainerComponent,
    SectionHeaderComponent,
    AppCardComponent,
    CandidateStatusChipComponent
  ],
  providers: [CandidateStore],
  templateUrl: './candidates-list-page.component.html',
  styleUrl: './candidates-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CandidatesListPageComponent implements OnInit, OnDestroy {
  protected readonly store = inject(CandidateStore);
  protected readonly layoutService = inject(LayoutService);
  private readonly notification = inject(NotificationService);

  protected readonly searchControl = new FormControl('');
  protected readonly statusControl = new FormControl('');
  private readonly destroy$ = new Subject<void>();

  // Supported ATS Status options
  protected readonly statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'New', label: 'New' },
    { value: 'Available', label: 'Available' },
    { value: 'Shortlisted', label: 'Shortlisted' },
    { value: 'Interviewing', label: 'Interviewing' },
    { value: 'Offered', label: 'Offered' },
    { value: 'Hired', label: 'Hired' },
    { value: 'Rejected', label: 'Rejected' },
    { value: 'Inactive', label: 'Inactive' }
  ];

  ngOnInit(): void {
    // Debounce search input changes and update store searchTerm signal
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(value => {
      this.store.setSearchTerm(value || '');
    });

    // Hook status select changes to store statusFilter signal
    this.statusControl.valueChanges.pipe(
      takeUntil(this.destroy$)
    ).subscribe(value => {
      this.store.setStatusFilter(value || '');
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  protected onRefresh(): void {
    this.store.loadCandidates();
  }

  protected onPageChange(event: PageEvent): void {
    this.store.setPage(event.pageIndex + 1);
    this.store.setPageSize(event.pageSize);
  }

  protected onEdit(candidate: CandidateSummaryResponse): void {
    this.notification.info(`Editing candidate ${candidate.person.firstName} will be implemented in Sprint 9.4`);
  }

  protected onDelete(candidate: CandidateSummaryResponse): void {
    this.notification.info(`Deleting candidate ${candidate.person.firstName} will be implemented in Sprint 9.4`);
  }

  protected onApplyToJob(candidate: CandidateSummaryResponse): void {
    this.notification.info(`Applying candidate ${candidate.person.firstName} to a job will be implemented in Sprint 9.8`);
  }

  // UX Actions: Copy Email and Open LinkedIn
  protected onCopyEmail(email: string, event: MouseEvent): void {
    event.stopPropagation();
    navigator.clipboard.writeText(email).then(() => {
      this.notification.success('Email copied to clipboard.');
    }).catch(() => {
      this.notification.error('Failed to copy email.');
    });
  }

  protected onOpenLinkedIn(url: string | null | undefined, event: MouseEvent): void {
    event.stopPropagation();
    if (url) {
      window.open(url, '_blank', 'noopener,noreferrer');
    } else {
      this.notification.error('LinkedIn URL not provided.');
    }
  }

  // Helper to extract initials (e.g. "John Smith" -> "JS")
  protected getInitials(firstName: string, lastName: string): string {
    const f = firstName ? firstName.charAt(0).toUpperCase() : '';
    const l = lastName ? lastName.charAt(0).toUpperCase() : '';
    return `${f}${l}`;
  }

  // Helper to get a random/hashed color based on candidate ID
  protected getAvatarBgColor(id: string): string {
    const colors = [
      '#e0f2fe', // sky
      '#dcfce7', // green
      '#fef9c3', // yellow
      '#f3e8ff', // purple
      '#fee2e2', // red
      '#ffedd5', // orange
      '#e0e7ff', // indigo
      '#ccfbf1', // teal
      '#fae8ff', // fuchsia
      '#f1f5f9'  // slate
    ];
    let hash = 0;
    if (id) {
      for (let i = 0; i < id.length; i++) {
        hash = id.charCodeAt(i) + ((hash << 5) - hash);
      }
    }
    const index = Math.abs(hash) % colors.length;
    return colors[index];
  }

  protected getAvatarTextColor(id: string): string {
    const colors = [
      '#0369a1', // sky
      '#15803d', // green
      '#a16207', // yellow
      '#6b21a8', // purple
      '#b91c1c', // red
      '#c2410c', // orange
      '#3730a3', // indigo
      '#0f766e', // teal
      '#86198f', // fuchsia
      '#475569'  // slate
    ];
    let hash = 0;
    if (id) {
      for (let i = 0; i < id.length; i++) {
        hash = id.charCodeAt(i) + ((hash << 5) - hash);
      }
    }
    const index = Math.abs(hash) % colors.length;
    return colors[index];
  }
}
