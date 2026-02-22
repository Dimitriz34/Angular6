/**
 * Standard Paginated Response from backend pagination helper (legacy format)
 * Used for list endpoints that return DataPageResult<T>
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
 * Backend DataPageResult<T> structure (wrapped in data property)
 * This is what the backend's DataPageResult.Create returns
 */
export interface DataPageResult<T> {
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  data: T[];
}
