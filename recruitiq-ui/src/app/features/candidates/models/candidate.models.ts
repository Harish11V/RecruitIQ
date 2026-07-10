export interface PersonSummary {
  firstName: string;
  lastName: string;
  title: string | null;
}

export interface ContactSummary {
  email: string;
  phoneNumber: string | null;
  linkedInUrl: string | null;
}

export interface StatusSummary {
  code: string;
  label: string;
  color: string; // success, primary, warning, accent, warn, info
}

export interface ResumeSummary {
  hasResume: boolean;
  fileName: string | null;
  uploadedAt: string | null;
}

export interface CandidateSummaryResponse {
  id: string;
  candidateNumber: string;
  person: PersonSummary;
  contact: ContactSummary;
  status: StatusSummary;
  resume: ResumeSummary;
  skills: string[];
  yearsOfExperience: number | null;
}

export interface CandidateExperienceSummary {
  company: string;
  role: string;
  startDate: string;
  endDate: string | null;
  description: string | null;
}

export interface CandidateEducationSummary {
  institution: string;
  degree: string;
  gpa: number | null;
  startYear: number;
  endYear: number | null;
}

export interface CandidateCertificationSummary {
  certificate: string;
  provider: string;
  year: number;
}

export interface CandidateResumeSummary {
  id: string;
  fileName: string;
  originalFileName: string;
  fileSize: number;
  mimeType: string;
  uploadedDate: string;
  uploadedBy: string | null;
  parserVersion: string;
  isPrimary: boolean;
  storagePath: string;
  version: number;
}

export interface CandidateDetailsResponse {
  candidateId: string;
  candidateNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  linkedInUrl: string | null;
  title: string | null;
  status: number; // CandidateStatus enum value
  yearsOfExperience: number | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
  
  // Resumes list
  resumes: CandidateResumeSummary[];
  
  // Skills
  skills: string[];
  
  // Overview Collections
  experiences: CandidateExperienceSummary[];
  educations: CandidateEducationSummary[];
  certifications: CandidateCertificationSummary[];
  
  // Activity Summary
  applicationsCount: number;
  interviewsCount: number;
}

export interface CreateCandidateRequestDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  linkedInUrl: string | null;
  title: string | null;
  yearsOfExperience: number | null;
}

export interface UpdateCandidateRequestDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  linkedInUrl: string | null;
  title: string | null;
  status: number; // CandidateStatus enum value
  yearsOfExperience: number | null;
  rowVersion: string;
}

export interface CandidateTimelineItemResponse {
  activityId: string;
  timestamp: string;
  action: string;
  description: string | null;
  performedBy: string;
  icon: string;
  color: string;
  metadata: string | null;
}

export enum CandidateStatus {
  New = 0,
  Available = 1,
  Shortlisted = 2,
  Interviewing = 3,
  Offered = 4,
  Hired = 5,
  Rejected = 6,
  Inactive = 7
}

export const CandidateStatusNames: Record<number, string> = {
  [CandidateStatus.New]: 'New',
  [CandidateStatus.Available]: 'Available',
  [CandidateStatus.Shortlisted]: 'Shortlisted',
  [CandidateStatus.Interviewing]: 'Interviewing',
  [CandidateStatus.Offered]: 'Offered',
  [CandidateStatus.Hired]: 'Hired',
  [CandidateStatus.Rejected]: 'Rejected',
  [CandidateStatus.Inactive]: 'Inactive'
};




