import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, VehicleDto, CreateVehicleRequest } from '../models';

@Injectable({
  providedIn: 'root'
})
export class VehicleService {
  private readonly baseUrl = `${environment.apiUrl}/vehicles`;

  constructor(private http: HttpClient) {}

  getMyVehicles(): Observable<ApiResponse<VehicleDto[]> & VehicleDto[]> {
    return this.http.get<ApiResponse<VehicleDto[]> & VehicleDto[]>(`${this.baseUrl}/my`);
  }

  getVehicles(): Observable<ApiResponse<VehicleDto[]> & VehicleDto[]> {
    return this.getMyVehicles();
  }

  getVehicleById(id: string): Observable<ApiResponse<VehicleDto> & VehicleDto> {
    return this.http.get<ApiResponse<VehicleDto> & VehicleDto>(`${this.baseUrl}/${id}`);
  }

  createVehicle(request: CreateVehicleRequest): Observable<ApiResponse<VehicleDto> & VehicleDto> {
    return this.http.post<ApiResponse<VehicleDto> & VehicleDto>(this.baseUrl, request);
  }

  updateVehicle(id: string, request: CreateVehicleRequest): Observable<ApiResponse<VehicleDto> & VehicleDto> {
    return this.http.put<ApiResponse<VehicleDto> & VehicleDto>(`${this.baseUrl}/${id}`, request);
  }

  deleteVehicle(id: string): Observable<ApiResponse<any> & { message: string }> {
    return this.http.delete<ApiResponse<any> & { message: string }>(`${this.baseUrl}/${id}`);
  }
}
