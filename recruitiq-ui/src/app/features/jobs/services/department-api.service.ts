import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { DepartmentSummary } from '../models/job.models';

@Injectable({
  providedIn: 'root'
})
export class DepartmentApiService {
  private readonly apiService = inject(ApiService);

  getDepartments(): Observable<ApiResponse<DepartmentSummary[]>> {
    return this.apiService.get<ApiResponse<DepartmentSummary[]>>('/departments');
  }
}
