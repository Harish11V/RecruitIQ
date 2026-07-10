import { ChangeDetectionStrategy, Component, inject, OnInit, effect, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

// UI Foundation & Shared Imports
import { PageContainerComponent } from '../../../shared/ui/page-container/page-container.component';
import { SectionHeaderComponent } from '../../../shared/ui/section-header/section-header.component';
import { AppCardComponent } from '../../../shared/ui/app-card/app-card.component';
import { DetailRowComponent } from '../../../shared/ui/detail-row/detail-row.component';
import { ImageUploadComponent } from '../../../shared/components/image-upload/image-upload.component';

// Service, Guard & Store Imports
import { HasUnsavedChanges } from '../../../core/guards/unsaved-changes.guard';
import { CompanyProfileStore } from '../state/company-profile.store';
import { LayoutService } from '../../../layout/services/layout.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-company-settings-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    PageContainerComponent,
    SectionHeaderComponent,
    AppCardComponent,
    DetailRowComponent,
    ImageUploadComponent
  ],
  providers: [CompanyProfileStore],
  templateUrl: './company-settings-page.component.html',
  styleUrl: './company-settings-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompanySettingsPageComponent implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  protected readonly store = inject(CompanyProfileStore);
  protected readonly layoutService = inject(LayoutService);
  protected readonly authService = inject(AuthService);

  protected settingsForm!: FormGroup;

  protected readonly themes = ['Light', 'Dark'];
  protected readonly timezones = [
    'UTC', 'US/Eastern', 'US/Central', 'US/Mountain', 'US/Pacific', 
    'Europe/London', 'Europe/Paris', 'Asia/Kolkata', 'Asia/Tokyo', 'Australia/Sydney'
  ];

  // Dummy read-only profile data that doesn't exist on the backend settings endpoints
  protected readonly companyName = signal<string>('RecruitIQ Technologies');
  protected readonly subdomain = signal<string>('recruitiq');

  constructor() {
    this.settingsForm = this.fb.group({
      theme: ['Light', Validators.required],
      timezone: ['UTC', Validators.required],
      defaultInterviewDuration: [30, [Validators.required, Validators.min(15), Validators.max(180)]],
      allowedEmailDomain: ['', [Validators.pattern(/^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$/)]]
    });

    // Sync form values when settings signal updates in store
    effect(() => {
      const data = this.store.settings();
      if (data) {
        this.settingsForm.patchValue({
          theme: data.theme,
          timezone: data.timezone,
          defaultInterviewDuration: data.defaultInterviewDuration,
          allowedEmailDomain: data.allowedEmailDomain || ''
        });
        this.settingsForm.markAsPristine();

        // Update read-only metadata if company name or subdomain is available in auth context
        const user = this.authService.currentUser();
        if (user && user.tenantId) {
          // Keep subdomain aligned with claims or fallbacks
          this.subdomain.set(user.tenantId.split('-')[0] || 'recruitiq');
        }
      }
    });
  }

  ngOnInit(): void {
    this.store.loadSettings();
  }

  hasUnsavedChanges(): boolean {
    return this.settingsForm.dirty;
  }

  protected onUploadLogo(file: File): void {
    this.store.uploadLogo(file);
  }

  protected onRemoveLogo(): void {
    this.store.deleteLogo();
  }

  protected onReset(): void {
    const data = this.store.settings();
    if (data) {
      this.settingsForm.patchValue({
        theme: data.theme,
        timezone: data.timezone,
        defaultInterviewDuration: data.defaultInterviewDuration,
        allowedEmailDomain: data.allowedEmailDomain || ''
      });
      this.settingsForm.markAsPristine();
    }
  }

  protected onSubmit(): void {
    if (this.settingsForm.invalid || this.store.isSaving()) {
      this.settingsForm.markAllAsTouched();
      this.scrollToFirstInvalid();
      return;
    }

    const currentData = this.store.settings();
    if (!currentData) return;

    const formValue = this.settingsForm.value;
    const request = {
      theme: formValue.theme,
      timezone: formValue.timezone,
      defaultInterviewDuration: formValue.defaultInterviewDuration,
      allowedEmailDomain: formValue.allowedEmailDomain || null,
      rowVersion: currentData.rowVersion
    };

    this.store.updateSettings(request, () => this.onReset());
  }

  private scrollToFirstInvalid(): void {
    const firstInvalid = document.querySelector('mat-form-field.ng-invalid, input.ng-invalid, mat-select.ng-invalid');
    if (firstInvalid) {
      firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }
}
