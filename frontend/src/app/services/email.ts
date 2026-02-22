import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { BaseService } from './api';
import { Email, EmailPost, TPAssistRequest, TPAssistResult, EmailDetail } from '../models/email.model';
import { PaginatedResult, DataPageResult } from '../models/common.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class EmailService extends BaseService {
  getEmails(pageNumber: number, pageSize: number, searchTerm?: string, appName?: string, startDate?: string, endDate?: string): Observable<PaginatedResult<Email>> {
    const body: any = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm ?? null,
      appName: appName ?? null
    };
    
    // Add date filters if provided
    if (startDate) body.startDate = startDate;
    if (endDate) body.endDate = endDate;

    return this.post<ApiResponse<DataPageResult<Email>>>('EmailService/GetAll', body).pipe(
      map(response => this.transformPaginatedResponse(response))
    );
  }

  sendEmail(email: EmailPost, attachments: File[]): Observable<ApiResponse<Email>> {
    const formData = new FormData();
    formData.append('Subject', email.subject);
    formData.append('Body', email.body || '');
    formData.append('IsHtml', email.isHtml.toString());
    formData.append('ToRecipients', email.toRecipients);
    if (email.ccRecipients) {
      formData.append('CcRecipients', email.ccRecipients);
    }
    // Always append AppId (required for sending email via web app)
    formData.append('AppId', email.appId ? email.appId.toString() : '0');
    // Always append AppPassword (even if empty for TP Services)
    formData.append('AppPassword', email.appPassword || '');
    // Always append SmtpUserEmail (even if empty)
    formData.append('SmtpUserEmail', email.smtpUserEmail || '');
    // TP Data Assist: AI enhancement flag (defaults to false if not provided)
    formData.append('UseTPAssist', (email.useTPAssist ?? false).toString());

    attachments.forEach((file) => {
      formData.append('attachment', file, file.name);
    });

    return this.post<ApiResponse<Email>>('EmailService/SendEmail', formData);
  }

  /**
  * GetTPAssist - TP Data Assist email enhancement endpoint
   * Enhances email body using AI based on isHtml flag
   * API key is configured on the backend
   */
  getTPAssist(request: TPAssistRequest): Observable<ApiResponse<TPAssistResult>> {
    return this.post<ApiResponse<TPAssistResult>>('EmailService/GetTPAssist', request);
  }

  /**
   * Get detailed email information by emailId
   * Includes body, recipients, attachments and full metadata
   */
  getEmailDetail(emailId: string): Observable<ApiResponse<EmailDetail>> {
    return this.get<ApiResponse<EmailDetail>>(`EmailService/GetEmailDetail/${emailId}`);
  }

  sendMailTest2(data: any): Observable<ApiResponse<any>> {
    const formData = new FormData();
    formData.append('host', data.host);
    formData.append('port', data.port.toString());
    formData.append('username', data.username);
    formData.append('password', data.password);
    formData.append('fromEmail', data.fromEmail);
    formData.append('toEmail', data.toEmail);
    return this.post<ApiResponse<any>>('EmailService/SendMailTest2', formData);
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
