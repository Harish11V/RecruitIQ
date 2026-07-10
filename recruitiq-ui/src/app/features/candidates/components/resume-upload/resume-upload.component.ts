import { ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Input, Output, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'app-resume-upload',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatProgressBarModule
  ],
  templateUrl: './resume-upload.component.html',
  styleUrl: './resume-upload.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResumeUploadComponent {
  @Input() isUploading = false;
  @Input() uploadProgress = 0;
  @Input() maxSizeMb = 10;
  @Input() placeholderText = 'Upload candidate resume';
  @Input() supportedFormats = 'Supported: PDF, DOC, DOCX (Max 10 MB)';

  @Output() readonly upload = new EventEmitter<File>();
  @Output() readonly cancel = new EventEmitter<void>();

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  protected readonly isDragOver = signal<boolean>(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (!this.isUploading) {
      this.isDragOver.set(true);
    }
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
    
    if (this.isUploading) return;
    this.errorMessage.set(null);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.validateAndEmit(files[0]);
    }
  }

  protected triggerFileBrowse(): void {
    if (!this.isUploading) {
      this.fileInput.nativeElement.click();
    }
  }

  protected onFileSelected(event: Event): void {
    this.errorMessage.set(null);
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (files && files.length > 0) {
      this.validateAndEmit(files[0]);
      input.value = ''; // Reset value to allow selecting same file again
    }
  }

  private validateAndEmit(file: File): void {
    // 1. Validate extension
    const allowedExtensions = ['.pdf', '.doc', '.docx'];
    const fileName = file.name.toLowerCase();
    const hasValidExt = allowedExtensions.some(ext => fileName.endsWith(ext));

    if (!hasValidExt) {
      this.errorMessage.set('Invalid format. Only PDF, DOC, and DOCX files are allowed.');
      return;
    }

    // 2. Validate size
    const maxSizeBytes = this.maxSizeMb * 1024 * 1024;
    if (file.size > maxSizeBytes) {
      this.errorMessage.set(`File size exceeds the limit of ${this.maxSizeMb} MB.`);
      return;
    }

    this.upload.emit(file);
  }
}
