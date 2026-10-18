import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  CheckInRequest,
  CheckOutRequest,
  CheckInResultDto,
  CheckOutResultDto,
  ParkingRecordDto
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class ParkingService {
  private baseUrl = `${environment.apiUrl}/parking`;

  constructor(private http: HttpClient) {}

  checkIn(request: CheckInRequest): Observable<ApiResponse<CheckInResultDto>> {
    return this.http.post<ApiResponse<CheckInResultDto>>(`${this.baseUrl}/check-in`, request);
  }

  checkOut(request: CheckOutRequest): Observable<ApiResponse<CheckOutResultDto>> {
    return this.http.post<ApiResponse<CheckOutResultDto>>(`${this.baseUrl}/check-out`, request);
  }

  getHistory(reservationId?: string): Observable<ApiResponse<ParkingRecordDto[]>> {
    let params = new HttpParams();
    if (reservationId) params = params.set('reservationId', reservationId);

    return this.http.get<ApiResponse<ParkingRecordDto[]>>(`${this.baseUrl}/history`, { params });
  }
}
