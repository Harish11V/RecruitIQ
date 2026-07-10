import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { PagedResponse } from '../../../core/models/api-response.model'; // Let's check if PagedResponse is in api-response.model or needs to be imported from core/models
import { CandidateSummaryResponse, CandidateDetailsResponse, CreateCandidateRequestDto, UpdateCandidateRequestDto } from '../models/candidate.models';

@Injectable({
  providedIn: 'root'
})
export class CandidateApiService {
  private readonly api = inject(ApiService);

  getCandidates(params: {
    search?: string;
    status?: string;
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortOrder?: string;
  }): Observable<ApiResponse<PagedResponse<CandidateSummaryResponse>>> {
    let httpParams = new HttpParams();
    
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortOrder) httpParams = httpParams.set('sortOrder', params.sortOrder);

    return this.api.get<ApiResponse<PagedResponse<CandidateSummaryResponse>>>('/candidates', httpParams);
  }

  getCandidateById(id: string): Observable<ApiResponse<CandidateDetailsResponse>> {
    return this.api.get<ApiResponse<CandidateDetailsResponse>>(`/candidates/${id}`);
  }

  createCandidate(dto: CreateCandidateRequestDto): Observable<ApiResponse<string>> {
    return this.api.post<ApiResponse<string>>('/candidates', dto);
  }

  updateCandidate(id: string, dto: UpdateCandidateRequestDto): Observable<ApiResponse<string>> {
    return this.api.put<ApiResponse<string>>(`/candidates/${id}`, dto);
  }

  changeStatus(id: string, status: number, rowVersion: string): Observable<ApiResponse<string>> {
    return this.api.patch<ApiResponse<string>>(`/candidates/${id}/status`, { status, rowVersion });
  }
}
