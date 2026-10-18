import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  AdminDashboardDto,
  AttendantDashboardDto,
  CustomerDashboardDto
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly baseUrl = `${environment.apiUrl}/dashboard`;

  constructor(private http: HttpClient) {}

  getAdminDashboard(): Observable<ApiResponse<AdminDashboardDto> & AdminDashboardDto> {
    return this.http.get<ApiResponse<AdminDashboardDto> & AdminDashboardDto>(`${this.baseUrl}/admin`);
  }

  getAttendantDashboard(parkingAreaId?: string): Observable<ApiResponse<AttendantDashboardDto> & AttendantDashboardDto> {
    let params = new HttpParams();
    if (parkingAreaId) {
      params = params.set('parkingAreaId', parkingAreaId);
    }
    return this.http.get<ApiResponse<AttendantDashboardDto> & AttendantDashboardDto>(`${this.baseUrl}/attendant`, { params });
  }

  getCustomerDashboard(customerId?: string): Observable<ApiResponse<CustomerDashboardDto> & CustomerDashboardDto> {
    let params = new HttpParams();
    if (customerId) {
      params = params.set('customerId', customerId);
    }
    return this.http.get<ApiResponse<CustomerDashboardDto> & CustomerDashboardDto>(`${this.baseUrl}/customer`, { params });
  }
}
