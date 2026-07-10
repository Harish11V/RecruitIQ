export interface CompanySettingsResponseDto {
  companyId: string;
  theme: string;
  logoUrl: string | null;
  timezone: string;
  defaultInterviewDuration: number;
  allowedEmailDomain: string | null;
  rowVersion: string; // base64 string
}

export interface UpdateCompanySettingsRequestDto {
  theme: string;
  timezone: string;
  defaultInterviewDuration: number;
  allowedEmailDomain: string | null;
  rowVersion: string; // base64 string
}
