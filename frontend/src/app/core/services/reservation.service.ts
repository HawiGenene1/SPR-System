import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  ReservationDto,
  CreateReservationRequest,
  CheckAvailabilityRequest,
  AvailabilityResultDto
} from '../models';

export interface ReservationFilterParams {
  parkingAreaId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReservationService {
  private readonly baseUrl = `${environment.apiUrl}/reservations`;

  constructor(private http: HttpClient) {}

  getMyReservations(): Observable<ApiResponse<ReservationDto[]> & ReservationDto[]> {
    return this.http.get<ApiResponse<ReservationDto[]> & ReservationDto[]>(`${this.baseUrl}/my`);
  }

  getAllReservations(
    filterOrAreaId?: string | ReservationFilterParams,
    status?: string,
    fromDate?: string,
    toDate?: string
  ): Observable<ApiResponse<ReservationDto[]> & ReservationDto[]> {
    let params = new HttpParams();

    if (filterOrAreaId && typeof filterOrAreaId === 'object') {
      const f = filterOrAreaId as ReservationFilterParams;
      if (f.parkingAreaId) params = params.set('parkingAreaId', f.parkingAreaId);
      if (f.status) params = params.set('status', f.status);
      if (f.fromDate) params = params.set('fromDate', f.fromDate);
      if (f.toDate) params = params.set('toDate', f.toDate);
    } else {
      if (filterOrAreaId) params = params.set('parkingAreaId', filterOrAreaId.toString());
      if (status) params = params.set('status', status);
      if (fromDate) params = params.set('fromDate', fromDate);
      if (toDate) params = params.set('toDate', toDate);
    }

    return this.http.get<ApiResponse<ReservationDto[]> & ReservationDto[]>(this.baseUrl, { params });
  }

  getById(id: string): Observable<ApiResponse<ReservationDto> & ReservationDto> {
    return this.http.get<ApiResponse<ReservationDto> & ReservationDto>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateReservationRequest): Observable<ApiResponse<ReservationDto> & ReservationDto> {
    return this.http.post<ApiResponse<ReservationDto> & ReservationDto>(this.baseUrl, request);
  }

  cancel(id: string, reason?: string): Observable<ApiResponse<ReservationDto> & ReservationDto> {
    return this.http.put<ApiResponse<ReservationDto> & ReservationDto>(
      `${this.baseUrl}/${id}/cancel`,
      { reason: reason || 'Customer requested cancellation' }
    );
  }

  updateStatus(id: string, status: string, note?: string): Observable<ApiResponse<ReservationDto> & ReservationDto> {
    return this.http.put<ApiResponse<ReservationDto> & ReservationDto>(
      `${this.baseUrl}/${id}/status`,
      { status, note }
    );
  }

  checkAvailability(request: CheckAvailabilityRequest): Observable<ApiResponse<AvailabilityResultDto> & AvailabilityResultDto> {
    return this.http.post<ApiResponse<AvailabilityResultDto> & AvailabilityResultDto>(
      `${this.baseUrl}/availability`,
      request
    );
  }
}
