import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  RevenueReportDto,
  OccupancyReportDto,
  ReservationReportDto,
  CustomerReportDto,
  ReportFilterRequest
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private readonly baseUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  private buildParams(filterOrStartDate?: string | ReportFilterRequest, endDate?: string): HttpParams {
    let params = new HttpParams();

    if (filterOrStartDate && typeof filterOrStartDate === 'object') {
      const f = filterOrStartDate as ReportFilterRequest;
      if (f.period) params = params.set('period', f.period);
      if (f.startDate) params = params.set('startDate', f.startDate);
      if (f.endDate) params = params.set('endDate', f.endDate);
      if (f.parkingAreaId) params = params.set('parkingAreaId', f.parkingAreaId);
      if (f.search) params = params.set('search', f.search);
      if (f.pageNumber) params = params.set('pageNumber', f.pageNumber.toString());
      if (f.pageSize) params = params.set('pageSize', f.pageSize.toString());
    } else {
      if (filterOrStartDate) params = params.set('startDate', filterOrStartDate.toString());
      if (endDate) params = params.set('endDate', endDate);
    }

    return params;
  }

  getRevenueReport(
    filterOrStartDate?: string | ReportFilterRequest,
    endDate?: string
  ): Observable<ApiResponse<RevenueReportDto> & RevenueReportDto> {
    const params = this.buildParams(filterOrStartDate, endDate);
    return this.http.get<ApiResponse<RevenueReportDto> & RevenueReportDto>(`${this.baseUrl}/revenue`, { params });
  }

  getOccupancyReport(
    filterOrStartDate?: string | ReportFilterRequest,
    endDate?: string
  ): Observable<ApiResponse<OccupancyReportDto> & OccupancyReportDto> {
    const params = this.buildParams(filterOrStartDate, endDate);
    return this.http.get<ApiResponse<OccupancyReportDto> & OccupancyReportDto>(`${this.baseUrl}/occupancy`, { params });
  }

  getReservationReport(
    filterOrStartDate?: string | ReportFilterRequest,
    endDate?: string
  ): Observable<ApiResponse<ReservationReportDto> & ReservationReportDto> {
    const params = this.buildParams(filterOrStartDate, endDate);
    return this.http.get<ApiResponse<ReservationReportDto> & ReservationReportDto>(`${this.baseUrl}/reservations`, { params });
  }

  getCustomerReport(
    filter?: ReportFilterRequest
  ): Observable<ApiResponse<CustomerReportDto> & CustomerReportDto> {
    const params = this.buildParams(filter);
    return this.http.get<ApiResponse<CustomerReportDto> & CustomerReportDto>(`${this.baseUrl}/customers`, { params });
  }
}
