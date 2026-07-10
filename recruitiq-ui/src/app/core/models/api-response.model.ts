export interface ApiResponse<T> {
  success: boolean;
  message: string;
  errors: string[];
  data: T | null;
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
