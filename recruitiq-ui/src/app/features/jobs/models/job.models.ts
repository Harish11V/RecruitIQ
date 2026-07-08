export enum EmploymentType {
  FullTime = 0,
  PartTime = 1,
  Contract = 2,
  Internship = 3
}

export enum JobStatus {
  Draft = 0,
  Published = 1,
  Closed = 2,
  Archived = 3
}

export interface JobSummaryResponse {
  id: string;
  jobCode: string;
  title: string;
  departmentName: string;
  employmentType: EmploymentType;
  status: JobStatus;
  location: string;
  createdAt: string;
  closingDate?: string;
  applicantCount: number;
  rowVersion: string; // Concurrency base64 string
}

export interface PagedResponse<T> {
  page: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  items: T[];
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface GetJobsRequest {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  status?: JobStatus;
  departmentId?: string;
  employmentType?: string;
  hiringManagerId?: string;
  location?: string;
}

export interface DepartmentSummary {
  id: string;
  name: string;
}

export interface CreateJobRequestDto {
  title: string;
  description: string;
  requirements: string;
  location: string;
  employmentType: number;
  salaryMin?: number | null;
  salaryMax?: number | null;
  departmentId: string;
  hiringManagerId?: string | null;
  closingDate?: string | null;
  requiredSkills?: string[] | null;
}

export interface SkillSummaryResponse {
  id: string;
  name: string;
}

export interface UserSummaryResponse {
  id: string;
  fullName: string;
  email: string;
}

export interface JobDetailsResponseDto {
  jobId: string;
  jobCode: string;
  title: string;
  slug: string;
  description: string;
  requirements: string;
  responsibilities: string;
  location: string;
  department: DepartmentSummary;
  hiringManager?: UserSummaryResponse | null;
  employmentType: EmploymentType;
  status: JobStatus;
  salaryMin?: number | null;
  salaryMax?: number | null;
  publishedAt?: string | null;
  closingDate?: string | null;
  createdAt: string;
  requiredSkills: SkillSummaryResponse[];
  applicantCount: number;
  interviewCount: number;
  rowVersion: string; // Concurrency base64 string
}

export interface UpdateJobRequestDto {
  title: string;
  description: string;
  requirements: string;
  responsibilities: string;
  departmentId: string;
  hiringManagerId?: string | null;
  employmentType: number;
  salaryMin?: number | null;
  salaryMax?: number | null;
  location: string;
  closingDate?: string | null;
  requiredSkills: string[];
  rowVersion: string; // Concurrency base64 string
}

export interface PublishJobRequestDto {
  rowVersion: string;
}

export interface ArchiveJobRequestDto {
  rowVersion: string;
}

export interface DeleteJobRequestDto {
  rowVersion: string;
}

