import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { BaseService } from './api';
import { User, Role } from '../models/user.model';
import { PaginatedResult, PaginatedResponse } from '../models/common.model';
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
    
    return this.post<ApiResponse<PaginatedResponse<User>>>('AppUser/FindAppUserList', body).pipe(
      map(response => this.transformPaginatedResponse(response))
    );
  }

  approveUser(userId: string): Observable<ApiResponse<any>> {
    return this.post('AppUser/UpdateAppUserApproval', `"${userId}"`);
  }

  updatePassword(data: any): Observable<ApiResponse<any>> {
    return this.post('AppUser/UpdateAppUserCredentials', data);
  }

  getRoles(): Observable<ApiResponse<Role[]>> {
    return this.get<ApiResponse<Role[]>>('AppUser/FindAppRole');
  }

  updateUserRole(userId: string, roleId: number): Observable<ApiResponse<any>> {
    return this.post('AppUser/UpdateUserRole', { userId, roleId });
  }

  registerUserByAdmin(data: any): Observable<ApiResponse<string>> {
    return this.post('AppUser/AppUserRegistration', data);
  }

  /**
   * Transform ApiResponse<PaginatedResponse<T>> to legacy PaginatedResult<T>
   */
  private transformPaginatedResponse<T>(response: ApiResponse<PaginatedResponse<T>>): PaginatedResult<T> {
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
      hasPreviousPage: !!paginatedData?.previousPage,
      hasNextPage: !!paginatedData?.nextPage
    };
  }
}
