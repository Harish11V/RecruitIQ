import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { DepartmentResponseDto, CreateDepartmentRequestDto, UpdateDepartmentRequestDto } from '../models/department.models';

@Injectable({
  providedIn: 'root'
})
export class DepartmentApiService {
  private readonly api = inject(ApiService);

  getDepartments(search?: string): Observable<ApiResponse<DepartmentResponseDto[]>> {
    let params = new HttpParams();
    if (search) {
      params = params.set('search', search);
    }
    return this.api.get<ApiResponse<DepartmentResponseDto[]>>('/departments', params);
  }

  createDepartment(dto: CreateDepartmentRequestDto): Observable<ApiResponse<string>> {
    return this.api.post<ApiResponse<string>>('/departments', dto);
  }

  updateDepartment(id: string, dto: UpdateDepartmentRequestDto): Observable<ApiResponse<unknown>> {
    return this.api.put<ApiResponse<unknown>>(`/departments/${id}`, dto);
  }

  deleteDepartment(id: string, rowVersion: string): Observable<ApiResponse<unknown>> {
    const encodedRowVersion = encodeURIComponent(rowVersion);
    return this.api.delete<ApiResponse<unknown>>(`/departments/${id}?rowVersion=${encodedRowVersion}`);
  }
}
