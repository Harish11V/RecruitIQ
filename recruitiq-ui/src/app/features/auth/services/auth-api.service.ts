import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import { ApiService } from '../../../core/services/api.service';
import { AuthResponseDto, LoginRequestDto, RefreshTokenRequestDto, LogoutRequestDto } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthApiService {
  private readonly apiService = inject(ApiService);

  login(request: LoginRequestDto): Observable<ApiResponse<AuthResponseDto>> {
    return this.apiService.post<ApiResponse<AuthResponseDto>>('/auth/login', request);
  }

  refresh(request: RefreshTokenRequestDto): Observable<ApiResponse<AuthResponseDto>> {
    return this.apiService.post<ApiResponse<AuthResponseDto>>('/auth/refresh', request);
  }

  logout(request: LogoutRequestDto): Observable<ApiResponse<unknown>> {
    return this.apiService.post<ApiResponse<unknown>>('/auth/logout', request);
  }
}
