import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { BaseService } from './api';
import { User, Role } from '../models/user.model';
import { PaginatedResult, DataPageResult } from '../models/common.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class UserService extends BaseService {
  getUsers(
    pageNumber: number, 
    pageSize: number, 
    searchTerm?: string,
    roleId?: number,
    active?: number,
    sortBy?: string
  ): Observable<PaginatedResult<User>> {
    const body: any = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm || null
    };
    
    // Add optional filters
    if (roleId !== undefined) {
      body.roleId = roleId;
    }
    if (active !== undefined) {
      body.active = active;
    }
    if (sortBy) {
      body.sortBy = sortBy;
    }
    
    return this.post<ApiResponse<DataPageResult<User>>>('User/FindAppUserList', body).pipe(
      map(response => this.transformPaginatedResponse(response))
    );
  }

  approveUser(userId: string, active: number = 1): Observable<ApiResponse<any>> {
    return this.post('User/UpdateAppUserApproval', { userId, active });
  }

  updatePassword(data: any): Observable<ApiResponse<any>> {
    return this.post('User/UpdateAppUserCredentials', data);
  }

  getRoles(): Observable<ApiResponse<Role[]>> {
    return this.get<ApiResponse<Role[]>>('User/FindAppRole');
  }

  updateUserRole(userId: string, roleId: number): Observable<ApiResponse<any>> {
    return this.post('User/UpdateUserRole', { userId, roleId });
  }

  registerUserByAdmin(data: any): Observable<ApiResponse<string>> {
    return this.post('Auth/Register', data);
  }

  /**
   * Transform ApiResponse<DataPageResult<T>> to legacy PaginatedResult<T>
   */
  private transformPaginatedResponse<T>(response: ApiResponse<DataPageResult<T>>): PaginatedResult<T> {
    const paginatedData = response.resultData?.[0];
    return {
      status: response.resultCode,
      message: response.resultMessages?.[0] || '',
      statusCode: response.resultCode === 1 ? 200 : 400,
      data: paginatedData?.data || [],
      totalRecords: paginatedData?.totalRecords || 0,
      pageSize: paginatedData?.pageSize || 10,
      pageNumber: paginatedData?.pageNumber || 1,
      totalPages: paginatedData?.totalPages || 0,
      hasPreviousPage: paginatedData?.hasPreviousPage || false,
      hasNextPage: paginatedData?.hasNextPage || false
    };
  }
}
