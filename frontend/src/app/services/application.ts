import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { BaseService } from './api';
import { Application, ApplicationCreateDto } from '../models/application.model';
import { User } from '../models/user.model';
import { EmailServiceLookup } from '../models/email-lookup.model';
import { PaginatedResult, DataPageResult } from '../models/common.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class ApplicationService extends BaseService {
  getApplications(pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedResult<Application>> {
    const body = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm ?? null
    };
    
    return this.post<ApiResponse<DataPageResult<Application>>>('Application/GetApplicationList', body).pipe(
      map(response => this.transformPaginatedResponse(response))
    );
  }

  getUserApplications(userId: string, pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedResult<Application>> {
    const body = {
      userId,
      pageNumber,
      pageSize,
      searchTerm: searchTerm ?? null
    };
    
    return this.post<ApiResponse<DataPageResult<Application>>>('Application/GetUserApplicationList', body).pipe(
      map(response => this.transformPaginatedResponse(response))
    );
  }

  approveApplication(appId: number): Observable<ApiResponse<number>> {
    return this.post('Application/UpdateApplicationApproval', { appId });
  }

  addApplication(data: ApplicationCreateDto): Observable<ApiResponse<any>> {
    return this.post('Application/SaveApplication', data);
  }

  updateApplication(data: any): Observable<ApiResponse<string>> {
    return this.post('Application/UpdateApplication', data);
  }

  getApplicationById(id: number): Observable<ApiResponse<Application>> {
    return this.get<ApiResponse<Application>>(`Application/FindApplicationById/${id}`);
  }

  getUsersDDL(): Observable<ApiResponse<User[]>> {
    return this.get<ApiResponse<User[]>>('User/FindAppUserListData');
  }

  getEmailServiceLookups(): Observable<ApiResponse<EmailServiceLookup>> {
    return this.get<ApiResponse<EmailServiceLookup>>('Application/GetEmailServicesList');
  }

  searchADUsers(term: string): Observable<any> {
    return this.post<ApiResponse<any>>('User/SearchADUsers', { term }).pipe(
      map(response => response.resultData?.[0] || { users: [], count: 0 })
    );
  }

  getADUserPhoto(upn: string): Observable<any> {
    return this.post<any>('User/GetADUserPhoto', { upn }).pipe(
      map(response => response || null)
    );
  }

  sendGuidanceEmail(appId: number, ownerEmail: string, appPassword: string, appSecret: string, baseApiUrl: string, coOwnerEmail?: string): Observable<ApiResponse<string>> {
    const formData = new FormData();
    formData.append('ownerEmail', ownerEmail);
    formData.append('appPassword', appPassword);
    formData.append('appSecret', appSecret);
    formData.append('baseApiUrl', baseApiUrl);
    if (coOwnerEmail) formData.append('coOwnerEmail', coOwnerEmail);
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}Application/SendGuidanceEmail/${appId}`, formData);
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
