import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from './api';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService extends BaseService {
  getUserProfile(id: string): Observable<ApiResponse<any>> {
    return this.get(`AppUser/FindAppUser/${id}`);
  }

  getAdminDashboardData(userId?: string): Observable<ApiResponse<any>> {
    const url = userId ? `Application/FindAdminDashboardData?userId=${userId}` : 'Application/FindAdminDashboardData';
    return this.get(url);
  }

  getTop10Apps(): Observable<ApiResponse<any>> {
    return this.get('Application/FindTop10Apps');
  }

  getRowsCount(table: string, condition: string = ''): Observable<ApiResponse<number>> {
    return this.post('Application/FindRowsCount', { Table: table, Condition: condition });
  }
}
