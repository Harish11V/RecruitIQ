import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly appVersion = environment.version;
  protected readonly loginForm: FormGroup;

  // UI state signals
  protected readonly isLoading = signal<boolean>(false);
  protected hidePassword = true;
  protected isCapsLockOn = false;

  // Background glow properties
  protected mouseGlowTransform = 'translate3d(0px, 0px, 0)';
  private lastMouseX = 0;
  private lastMouseY = 0;
  private frameId: number | null = null;

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });
  }

  protected onMouseMove(event: MouseEvent): void {
    this.lastMouseX = event.clientX;
    this.lastMouseY = event.clientY;

    if (this.frameId === null) {
      this.frameId = requestAnimationFrame(() => this.updateMouseGlow());
    }
  }

  private updateMouseGlow(): void {
    this.mouseGlowTransform = `translate3d(${this.lastMouseX}px, ${this.lastMouseY}px, 0)`;
    this.frameId = null;
  }

  protected checkCapsLock(event: KeyboardEvent): void {
    const isCapsOn = event.getModifierState && event.getModifierState('CapsLock');
    this.isCapsLockOn = isCapsOn;
  }

  protected onSubmit(): void {
    if (this.loginForm.invalid || this.isLoading()) {
      return;
    }

    this.isLoading.set(true);

    const email = this.loginForm.value.email;
    const password = this.loginForm.value.password;
    const rememberMe = this.loginForm.value.rememberMe;

    this.authService.login({ email, password }, rememberMe).subscribe({
      next: (success) => {
        this.isLoading.set(false);
        if (success) {
          this.notificationService.success('Login succeeded. Welcome back!');
          this.router.navigate(['/admin/dashboard']);
        } else {
          this.notificationService.error('Invalid email or password.');
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.notificationService.error(error.message || 'Authentication failed. Please try again.');
      }
    });
  }
}
