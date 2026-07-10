import { ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Input, Output, ViewChild, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-image-upload',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './image-upload.component.html',
  styleUrl: './image-upload.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ImageUploadComponent {
  @Input() imageUrl: string | null = null;
  @Input() isUploading = false;
  @Input() maxSizeMb = 5;
  @Input() placeholderText = 'Upload your logo';
  @Input() supportedFormats = 'Supported: PNG, JPG, JPEG';

  @Output() readonly upload = new EventEmitter<File>();
  @Output() readonly remove = new EventEmitter<void>();

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  protected readonly isDragOver = signal<boolean>(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
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
    this.errorMessage.set(null);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.validateAndEmit(files[0]);
    }
  }

  protected triggerFileBrowse(): void {
    this.fileInput.nativeElement.click();
  }

  protected onFileSelected(event: Event): void {
    this.errorMessage.set(null);
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (files && files.length > 0) {
      this.validateAndEmit(files[0]);
      // Reset input value to allow selecting same file again
      input.value = '';
    }
  }

  protected onRemove(event: MouseEvent): void {
    event.stopPropagation();
    this.remove.emit();
  }

  private validateAndEmit(file: File): void {
    // 1. Validate extension
    const allowedExtensions = ['.png', '.jpg', '.jpeg', '.webp'];
    const fileName = file.name.toLowerCase();
    const hasValidExt = allowedExtensions.some(ext => fileName.endsWith(ext));

    if (!hasValidExt) {
      this.errorMessage.set('Invalid format. Please upload PNG, JPG, JPEG, or WEBP.');
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
