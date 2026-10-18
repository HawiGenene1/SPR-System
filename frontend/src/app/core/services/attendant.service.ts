import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  ApiResponse, 
  AttendantDashboardDto, 
  ScannedQRResult, 
  ParkingAreaDto, 
  ParkingRecordDto,
  PagedResultDto
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class AttendantService {
  private baseUrl = `${environment.apiUrl}/attendant`;

  constructor(private http: HttpClient) {}

  getOccupancy(): Observable<AttendantDashboardDto> {
    return this.http.get<AttendantDashboardDto>(`${this.baseUrl}/occupancy`);
  }

  scanQr(qrCode: string): Observable<ScannedQRResult> {
    return this.http.post<ScannedQRResult>(`${this.baseUrl}/scan-qr`, { qrCode });
  }

  checkIn(qrCode: string, notes?: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/check-in`, { qrCode, notes });
  }

  checkOut(qrCode: string, notes?: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/check-out`, { qrCode, notes });
  }

  getParkingAreas(city?: string): Observable<ParkingAreaDto[]> {
    let params = new HttpParams();
    if (city) params = params.set('city', city);
    return this.http.get<ParkingAreaDto[]>(`${this.baseUrl}/parking-areas`, { params });
  }

  getParkingHistory(page: number = 1, pageSize: number = 20, status?: string): Observable<PagedResultDto<ParkingRecordDto>> {
    let params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', pageSize.toString());
    if (status) params = params.set('status', status);

    return this.http.get<PagedResultDto<ParkingRecordDto>>(`${environment.apiUrl}/parking-history`, { params });
  }
}
