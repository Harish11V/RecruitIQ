import { HttpClient, HttpEvent, HttpParams, HttpRequest } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class ResumeApiService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);
  private readonly baseUrl = environment.apiUrl;

  uploadResume(candidateId: string, file: File): Observable<HttpEvent<ApiResponse<string>>> {
    const formData = new FormData();
    formData.append('file', file);

    const req = new HttpRequest('POST', `${this.baseUrl}/candidates/${candidateId}/resume`, formData, {
      reportProgress: true,
      responseType: 'json'
    });

    return this.http.request<ApiResponse<string>>(req);
  }

  deleteResume(candidateId: string, resumeId: string): Observable<ApiResponse<boolean>> {
    return this.api.delete<ApiResponse<boolean>>(`/candidates/${candidateId}/resume/${resumeId}`);
  }

  setPrimaryResume(candidateId: string, resumeId: string): Observable<ApiResponse<boolean>> {
    return this.api.put<ApiResponse<boolean>>(`/candidates/${candidateId}/resume/${resumeId}/primary`);
  }

  getDownloadUrl(candidateId: string, resumeId: string): string {
    return `${this.baseUrl}/candidates/${candidateId}/resume/${resumeId}/download`;
  }
}
