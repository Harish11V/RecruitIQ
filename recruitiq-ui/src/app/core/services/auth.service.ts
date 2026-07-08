import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { AuthenticatedUser } from '../models/authenticated-user.model';
import { TokenService } from './token.service';
import { AuthApiService } from '../../features/auth/services/auth-api.service';
import { LoginRequestDto } from '../../features/auth/models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly tokenService = inject(TokenService);
  private readonly authApiService = inject(AuthApiService);
  private readonly router = inject(Router);

  // Core Authentication Signals
  readonly isAuthenticated = signal<boolean>(false);
  readonly currentUser = signal<AuthenticatedUser | null>(null);

  constructor() {
    this.initializeAuthState();
  }

  private initializeAuthState(): void {
    const token = this.tokenService.getToken();
    const user = this.tokenService.getUserFromToken();
    
    if (token && user && !this.tokenService.isTokenExpired()) {
      this.currentUser.set(user);
      this.isAuthenticated.set(true);
    } else {
      this.clearLocalSessionState();
    }
  }

  login(credentials: LoginRequestDto, rememberMe: boolean): Observable<boolean> {
    return this.authApiService.login(credentials).pipe(
      tap((response) => {
        if (response.success && response.data) {
          this.tokenService.saveTokens(
            response.data.accessToken,
            response.data.refreshToken,
            rememberMe
          );
          
          const user = this.tokenService.getUserFromToken();
          this.currentUser.set(user);
          this.isAuthenticated.set(true);
        }
      }),
      map(response => response.success),
      catchError(() => of(false))
    );
  }

  logout(): void {
    const refreshToken = this.tokenService.getRefreshToken();
    if (refreshToken) {
      // Fire-and-forget backend logout notification
      this.authApiService.logout({ refreshToken }).pipe(
        catchError(() => of(null))
      ).subscribe();
    }

    this.clearLocalSessionState();
    this.router.navigate(['/auth/login']);
  }

  private clearLocalSessionState(): void {
    this.tokenService.clearTokens();
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
  }
}
