import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from './api';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService extends BaseService {
  getUserProfile(id: string): Observable<ApiResponse<any>> {
    return this.get(`User/FindAppUser/${id}`);
  }

  getAdminDashboardData(userId?: string): Observable<ApiResponse<any>> {
    return this.post('Application/FindAdminDashboardData', { userId: userId ?? null });
  }

  getTop10Apps(): Observable<ApiResponse<any>> {
    return this.get('Application/FindTop10Apps');
  }

  getTop5AppsUtilisation(): Observable<ApiResponse<any>> {
    return this.get('Application/FindTop5AppsUtilisation');
  }

  getRowsCount(table: string, condition: string = ''): Observable<ApiResponse<number>> {
    return this.post('Application/FindRowsCount', { Table: table, Condition: condition });
  }
}
