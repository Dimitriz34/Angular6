/**
 * Standard Paginated Response from backend pagination helper (legacy format)
 * Used for list endpoints that return PaginatedResponse<T>
 */
export interface PaginatedResult<T> {
  status: number;
  message: string;
  statusCode: number;
  data: T[];
  totalRecords: number;
  pageSize: number;
  pageNumber: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/**
 * Backend PaginatedResponse<T> structure (wrapped in data property)
 * This is what the backend's PaginationHelper returns
 */
export interface PaginatedResponse<T> {
  pageNumber: number;
  pageSize: number;
  firstPage: string;
  lastPage: string;
  totalPages: number;
  totalRecords: number;
  nextPage: string | null;
  previousPage: string | null;
  data: T[];
  status: number;
  message: string;
  errors: any;
}
