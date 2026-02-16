import { inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { PaginatedResult } from '../models/common.model';

export abstract class BaseService {
  protected http = inject(HttpClient);
  protected apiUrl = environment.apiUrl;

  /**
   * GET request - Returns standard ApiResponse<T>
   * Use for endpoints that return ApiResponse<T>
   */
  protected get<T>(url: string, params?: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.apiUrl}${url}`, { params });
  }

  /**
   * POST request - Returns standard ApiResponse<T>
   * Use for endpoints that return ApiResponse<T>
   */
  protected post<T>(url: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.apiUrl}${url}`, body);
  }

  /**
   * PUT request - Returns standard ApiResponse<T>
   * Use for endpoints that return ApiResponse<T>
   */
  protected put<T>(url: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.apiUrl}${url}`, body);
  }

  /**
   * DELETE request - Returns standard ApiResponse<T>
   * Use for endpoints that return ApiResponse<T>
   */
  protected delete<T>(url: string): Observable<T> {
    return this.http.delete<T>(`${this.apiUrl}${url}`);
  }

  /**
   * Helper method to extract data from ApiResponse
   * @param response Standard ApiResponse<T> from backend
   * @returns The data array from resultData
   */
  protected extractApiData<T>(response: ApiResponse<T>): T[] {
    return response?.resultData || [];
  }

  /**
   * Helper method to extract single item from ApiResponse
   * @param response Standard ApiResponse<T> from backend
   * @returns The first item from resultData array
   */
  protected extractApiSingleData<T>(response: ApiResponse<T>): T | null {
    const data = response?.resultData;
    return data && data.length > 0 ? data[0] : null;
  }

  /**
   * Helper method to check if API response was successful
   * @param response Standard ApiResponse<T> from backend
   * @returns true if resultCode === 1 (SUCCESS)
   */
  protected isApiSuccess<T>(response: ApiResponse<T>): boolean {
    return response?.resultCode === 1;
  }

  /**
   * Helper method to get error messages from ApiResponse
   * @param response Standard ApiResponse<T> from backend
   * @returns Array of error messages
   */
  protected getApiMessages<T>(response: ApiResponse<T>): string[] {
    return response?.resultMessages || [];
  }
}
