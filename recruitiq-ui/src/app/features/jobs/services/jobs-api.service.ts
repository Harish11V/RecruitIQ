import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { GetJobsRequest, JobSummaryResponse, PagedResponse, CreateJobRequestDto, JobDetailsResponseDto, UpdateJobRequestDto, PublishJobRequestDto, ArchiveJobRequestDto, DeleteJobRequestDto } from '../models/job.models';

@Injectable({
  providedIn: 'root'
})
export class JobsApiService {
  private readonly apiService = inject(ApiService);

  getJobs(request: GetJobsRequest): Observable<ApiResponse<PagedResponse<JobSummaryResponse>>> {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.search) {
      params = params.set('search', request.search);
    }
    if (request.sortBy) {
      params = params.set('sortBy', request.sortBy);
    }
    if (request.status !== undefined && request.status !== null) {
      params = params.set('status', request.status.toString());
    }
    if (request.departmentId) {
      params = params.set('departmentId', request.departmentId);
    }
    if (request.employmentType !== undefined && request.employmentType !== null && request.employmentType !== '') {
      params = params.set('employmentType', request.employmentType.toString());
    }
    if (request.hiringManagerId) {
      params = params.set('hiringManagerId', request.hiringManagerId);
    }
    if (request.location) {
      params = params.set('location', request.location);
    }

    return this.apiService.get<ApiResponse<PagedResponse<JobSummaryResponse>>>('/jobs', params);
  }

  createJob(request: CreateJobRequestDto): Observable<ApiResponse<string>> {
    return this.apiService.post<ApiResponse<string>>('/jobs', request);
  }

  getJobById(id: string): Observable<ApiResponse<JobDetailsResponseDto>> {
    return this.apiService.get<ApiResponse<JobDetailsResponseDto>>(`/jobs/${id}`);
  }

  updateJob(id: string, request: UpdateJobRequestDto): Observable<ApiResponse<string>> {
    return this.apiService.put<ApiResponse<string>>(`/jobs/${id}`, request);
  }

  publishJob(id: string, request: PublishJobRequestDto): Observable<ApiResponse<string>> {
    return this.apiService.post<ApiResponse<string>>(`/jobs/${id}/publish`, request);
  }

  archiveJob(id: string, request: ArchiveJobRequestDto): Observable<ApiResponse<string>> {
    return this.apiService.post<ApiResponse<string>>(`/jobs/${id}/archive`, request);
  }

  deleteJob(id: string, request: DeleteJobRequestDto): Observable<ApiResponse<string>> {
    return this.apiService.delete<ApiResponse<string>>(`/jobs/${id}`, request);
  }
}
