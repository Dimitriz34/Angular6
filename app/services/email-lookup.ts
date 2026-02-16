import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BaseService } from './api';
import { EmailServiceLookup } from '../models/email-lookup.model';
import { PaginatedResult } from '../models/common.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class EmailLookupService extends BaseService {
  getEmailServices(pageNumber: number, pageSize: number): Observable<PaginatedResult<EmailServiceLookup>> {
    const params = new HttpParams()
      .set('PageNumber', pageNumber.toString())
      .set('PageSize', pageSize.toString());
    return this.get<PaginatedResult<EmailServiceLookup>>('Application/GetEmailServicesList', params);
  }

  getEmailServiceById(id: number): Observable<ApiResponse<EmailServiceLookup>> {
    return this.get<ApiResponse<EmailServiceLookup>>(`Application/GetEmailServiceById/${id}`);
  }

  addEmailService(data: any): Observable<ApiResponse<EmailServiceLookup>> {
    return this.post('Application/AddEmailService', data);
  }

  updateEmailService(data: any): Observable<ApiResponse<EmailServiceLookup>> {
    return this.post('Application/UpdateEmailService', data);
  }

  getRowsCount(table: string): Observable<ApiResponse<number>> {
    return this.post<ApiResponse<number>>('Application/FindRowsCount', { Table: table });
  }
}
