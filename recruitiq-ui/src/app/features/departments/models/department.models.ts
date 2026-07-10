export interface DepartmentResponseDto {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string; // base64 string
}

export interface CreateDepartmentRequestDto {
  name: string;
  description: string | null;
}

export interface UpdateDepartmentRequestDto {
  name: string;
  description: string | null;
  rowVersion: string; // base64 string
}
