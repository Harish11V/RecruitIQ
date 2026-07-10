import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';

// Shared UI Foundation
import { PageContainerComponent } from '../../../../shared/ui/page-container/page-container.component';
import { AppCardComponent } from '../../../../shared/ui/app-card/app-card.component';
import { DetailRowComponent } from '../../../../shared/ui/detail-row/detail-row.component';
import { EntityHeaderComponent } from '../../../../shared/components/entity-header/entity-header.component';
import { CandidateStatusChipComponent } from '../../components/candidate-status-chip/candidate-status-chip.component';
import { ResumeUploadComponent } from '../../components/resume-upload/resume-upload.component';
import { CandidateTimelineComponent } from '../../components/candidate-timeline/candidate-timeline.component';
import { ChangeCandidateStatusDialogComponent } from '../../components/change-candidate-status-dialog/change-candidate-status-dialog.component';

// Services & Models
import { CandidateApiService } from '../../services/candidate-api.service';
import { ResumeApiService } from '../../services/resume-api.service';
import { ResumeStore } from '../../state/resume.store';
import { CandidateDetailsResponse, CandidateResumeSummary, CandidateStatus, CandidateStatusNames } from '../../models/candidate.models';
import { NotificationService } from '../../../../core/services/notification.service';
import { BreadcrumbService } from '../../../../core/services/breadcrumb.service';

// Confirmation Dialog
import { ConfirmationDialogComponent } from '../../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'app-candidate-details-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatTooltipModule,
    MatDividerModule,
    MatDialogModule,
    PageContainerComponent,
    AppCardComponent,
    DetailRowComponent,
    EntityHeaderComponent,
    CandidateStatusChipComponent,
    ResumeUploadComponent,
    CandidateTimelineComponent
  ],
  templateUrl: './candidate-details-page.component.html',
  styleUrl: './candidate-details-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CandidateDetailsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(CandidateApiService);
  private readonly notification = inject(NotificationService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly dialog = inject(MatDialog);
  
  protected readonly resumeStore = inject(ResumeStore);
  private readonly resumeApi = inject(ResumeApiService);

  protected readonly candidate = signal<CandidateDetailsResponse | null>(null);
  protected readonly isLoading = signal<boolean>(false);
  protected readonly error = signal<string | null>(null);
  protected readonly isUploadingNew = signal<boolean>(false);

  protected isCreatedToday(dateStr: string): boolean {
    if (!dateStr) return false;
    const created = new Date(dateStr);
    const today = new Date();
    return created.toDateString() === today.toDateString();
  }

  ngOnInit(): void {
    const candidateId = this.route.snapshot.paramMap.get('id');
    if (candidateId) {
      this.loadCandidate(candidateId);
    } else {
      this.error.set('No Candidate ID provided.');
      this.notification.error('No Candidate ID provided.');
    }
  }

  protected loadCandidate(id: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.api.getCandidateById(id)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            const data = response.data;
            this.candidate.set(data);
            
            // Dynamically update breadcrumb label to candidate's name
            const fullName = `${data.firstName} ${data.lastName}`;
            this.breadcrumbService.updateBreadcrumbLabel(`/admin/candidates/${id}`, fullName);
          } else {
            const msg = response.message || 'Failed to retrieve candidate profile.';
            this.error.set(msg);
            this.notification.error(msg);
          }
        },
        error: (err) => {
          const msg = err.error?.message || 'A network error occurred while loading profile.';
          this.error.set(msg);
          this.notification.error(msg);
        }
      });
  }

  protected onBackToList(): void {
    this.router.navigate(['/admin/candidates']);
  }

  protected onEdit(): void {
    this.notification.info('Editing candidates will be enabled in Sprint 9.4');
  }

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

  protected onUploadResumeFile(file: File): void {
    const candidateId = this.candidate()?.candidateId;
    if (!candidateId) return;

    this.resumeStore.uploadResume(candidateId, file, () => {
      this.isUploadingNew.set(false);
      this.loadCandidate(candidateId);
    });
  }

  protected onDeleteResume(resume: CandidateResumeSummary, event: MouseEvent): void {
    event.stopPropagation();
    const candidateId = this.candidate()?.candidateId;
    if (!candidateId) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Resume',
        message: `Are you sure you want to delete the resume "${resume.originalFileName}"?`,
        confirmButtonText: 'Delete',
        confirmButtonColor: 'warn',
        icon: 'delete_forever',
        warningText: 'This file will be permanently removed from storage.'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed === true) {
        this.resumeStore.deleteResume(candidateId, resume.id, () => {
          this.loadCandidate(candidateId);
        });
      }
    });
  }

  protected onSetPrimaryResume(resume: CandidateResumeSummary, event: MouseEvent): void {
    event.stopPropagation();
    const candidateId = this.candidate()?.candidateId;
    if (!candidateId) return;

    this.resumeStore.setPrimaryResume(candidateId, resume.id, () => {
      this.loadCandidate(candidateId);
    });
  }

  protected onDownloadResume(resume: CandidateResumeSummary, event: MouseEvent): void {
    event.stopPropagation();
    const candidateId = this.candidate()?.candidateId;
    if (!candidateId) return;

    const downloadUrl = this.resumeApi.getDownloadUrl(candidateId, resume.id);
    window.open(downloadUrl, '_blank');
  }

  protected readonly statusNames = CandidateStatusNames;

  protected getLifecycleExplanation(status: number): string {
    switch (status) {
      case CandidateStatus.New:
        return 'Candidate has been entered into the system and is awaiting screening.';
      case CandidateStatus.Available:
        return 'Candidate has cleared initial screening and is available for shortlisting.';
      case CandidateStatus.Shortlisted:
        return 'Candidate is shortlisted for active jobs and ready for interview scheduling.';
      case CandidateStatus.Interviewing:
        return 'Candidate is currently undergoing active interview loops.';
      case CandidateStatus.Offered:
        return 'An official job offer has been extended and is pending candidate response.';
      case CandidateStatus.Hired:
        return 'Candidate has accepted the offer and is successfully hired.';
      case CandidateStatus.Rejected:
        return 'Candidate was rejected during screening or interview stages.';
      case CandidateStatus.Inactive:
        return 'Candidate profile is currently archived or marked inactive.';
      default:
        return 'Unknown candidate status stage.';
    }
  }

  protected getAllowedStages(status: number): string[] {
    switch (status) {
      case CandidateStatus.New:
        return ['Available', 'Inactive'];
      case CandidateStatus.Available:
        return ['Shortlisted', 'Inactive'];
      case CandidateStatus.Shortlisted:
        return ['Interviewing', 'Rejected'];
      case CandidateStatus.Interviewing:
        return ['Offered', 'Rejected'];
      case CandidateStatus.Offered:
        return ['Hired', 'Rejected'];
      case CandidateStatus.Hired:
        return ['Inactive'];
      case CandidateStatus.Rejected:
        return ['Available'];
      case CandidateStatus.Inactive:
        return ['Available'];
      default:
        return [];
    }
  }

  protected onChangeStatusFlow(): void {
    const profile = this.candidate();
    if (!profile) return;

    const dialogRef = this.dialog.open(ChangeCandidateStatusDialogComponent, {
      width: '440px',
      data: {
        candidateId: profile.candidateId,
        currentStatus: profile.status,
        rowVersion: profile.rowVersion
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // Optimistically update candidate local signal and reload from api
        this.candidate.update(c => c ? { ...c, status: result.newStatus, rowVersion: result.newRowVersion } : null);
        this.loadCandidate(profile.candidateId);
      }
    });
  }
}
