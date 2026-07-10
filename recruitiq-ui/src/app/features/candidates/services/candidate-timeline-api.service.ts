import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { CandidateTimelineItemResponse } from '../models/candidate.models';

@Injectable({
  providedIn: 'root'
})
export class CandidateTimelineApiService {
  private readonly api = inject(ApiService);

  getCandidateTimeline(candidateId: string): Observable<ApiResponse<CandidateTimelineItemResponse[]>> {
    return this.api.get<ApiResponse<CandidateTimelineItemResponse[]>>(`/candidates/${candidateId}/timeline`);
  }
}
