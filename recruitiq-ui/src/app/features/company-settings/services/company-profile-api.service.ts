import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { CompanySettingsResponseDto, UpdateCompanySettingsRequestDto } from '../models/company-profile.models';

@Injectable({
  providedIn: 'root'
})
export class CompanyProfileApiService {
  private readonly api = inject(ApiService);

  getSettings(): Observable<ApiResponse<CompanySettingsResponseDto>> {
    return this.api.get<ApiResponse<CompanySettingsResponseDto>>('/company-settings');
  }

  updateSettings(dto: UpdateCompanySettingsRequestDto): Observable<ApiResponse<unknown>> {
    return this.api.put<ApiResponse<unknown>>('/company-settings', dto);
  }

  uploadLogo(file: File): Observable<ApiResponse<string>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.api.post<ApiResponse<string>>('/company-settings/logo', formData);
  }

  deleteLogo(): Observable<ApiResponse<unknown>> {
    return this.api.delete<ApiResponse<unknown>>('/company-settings/logo');
  }
}
