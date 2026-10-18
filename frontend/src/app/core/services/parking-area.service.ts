import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, ParkingAreaDto, CreateParkingAreaRequest } from '../models';

@Injectable({
  providedIn: 'root'
})
export class ParkingAreaService {
  private readonly baseUrl = `${environment.apiUrl}/parking-areas`;

  constructor(private http: HttpClient) {}

  getAll(city?: string, includeInactive?: boolean): Observable<ApiResponse<ParkingAreaDto[]> & ParkingAreaDto[]> {
    let params = new HttpParams();
    if (city) params = params.set('city', city);
    if (includeInactive !== undefined) params = params.set('includeInactive', includeInactive.toString());

    return this.http.get<ApiResponse<ParkingAreaDto[]> & ParkingAreaDto[]>(this.baseUrl, { params });
  }

  getById(id: string): Observable<ApiResponse<ParkingAreaDto> & ParkingAreaDto> {
    return this.http.get<ApiResponse<ParkingAreaDto> & ParkingAreaDto>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateParkingAreaRequest): Observable<ApiResponse<ParkingAreaDto> & ParkingAreaDto> {
    return this.http.post<ApiResponse<ParkingAreaDto> & ParkingAreaDto>(this.baseUrl, request);
  }

  update(id: string, request: any): Observable<ApiResponse<ParkingAreaDto> & ParkingAreaDto> {
    return this.http.put<ApiResponse<ParkingAreaDto> & ParkingAreaDto>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<ApiResponse<any> & { message: string }> {
    return this.http.delete<ApiResponse<any> & { message: string }>(`${this.baseUrl}/${id}`);
  }
}
