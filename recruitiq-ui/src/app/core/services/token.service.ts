import { inject, Injectable } from '@angular/core';
import { StorageService } from './storage.service';
import { AuthenticatedUser } from '../models/authenticated-user.model';

@Injectable({
  providedIn: 'root'
})
export class TokenService {
  private readonly storage = inject(StorageService);
  private readonly tokenKey = 'recruitiq_access_token';
  private readonly refreshTokenKey = 'recruitiq_refresh_token';
  private readonly rememberMeKey = 'recruitiq_remember_me';

  getToken(): string | null {
    return this.storage.getItem(this.tokenKey);
  }

  getRefreshToken(): string | null {
    return this.storage.getItem(this.refreshTokenKey);
  }

  getRememberMe(): boolean {
    return this.storage.getItem(this.rememberMeKey) === 'true';
  }

  saveTokens(accessToken: string, refreshToken: string, rememberMe: boolean): void {
    this.storage.setItem(this.rememberMeKey, rememberMe ? 'true' : 'false', true);
    this.storage.setItem(this.tokenKey, accessToken, rememberMe);
    this.storage.setItem(this.refreshTokenKey, refreshToken, rememberMe);
  }

  clearTokens(): void {
    this.storage.removeItem(this.tokenKey);
    this.storage.removeItem(this.refreshTokenKey);
    this.storage.removeItem(this.rememberMeKey);
  }

  getUserFromToken(): AuthenticatedUser | null {
    const token = this.getToken();
    if (!token) return null;

    const payload = this.decodeToken(token);
    if (!payload) return null;

    // Extract claims from standard claims URI or standard fields
    const id = payload.sub || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || '';
    const email = payload.email || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '';
    const role = payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || '';
    const tenantId = payload.CompanyId || payload['CompanyId'] || '';
    const givenName = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] || '';
    const surname = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] || '';
    const fullName = payload.name || (givenName && surname ? `${givenName} ${surname}` : email.split('@')[0]);

    return { id, fullName, email, role, tenantId };
  }

  isTokenExpired(): boolean {
    const token = this.getToken();
    if (!token) return true;

    const payload = this.decodeToken(token);
    if (!payload || !payload.exp) return true;

    const expiryTime = payload.exp * 1000;
    return Date.now() >= expiryTime;
  }

  private decodeToken(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;

      let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      while (base64.length % 4) {
        base64 += '=';
      }

      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );

      return JSON.parse(jsonPayload);
    } catch {
      return null;
    }
  }
}
