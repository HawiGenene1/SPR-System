import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, ParkingSlotDto, CreateParkingSlotRequest } from '../models';

export interface ParkingSlotFilterParams {
  parkingAreaId?: string;
  floorNumber?: number;
  status?: string;
  vehicleType?: string;
  availableOnly?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ParkingSlotService {
  private readonly baseUrl = `${environment.apiUrl}/parking-slots`;

  constructor(private http: HttpClient) {}

  getAll(
    areaIdOrFilter?: string | ParkingSlotFilterParams,
    status?: string,
    type?: string,
    availableOnly?: boolean
  ): Observable<ApiResponse<ParkingSlotDto[]> & ParkingSlotDto[]> {
    let params = new HttpParams();

    if (areaIdOrFilter && typeof areaIdOrFilter === 'object') {
      const f = areaIdOrFilter as ParkingSlotFilterParams;
      if (f.parkingAreaId) params = params.set('parkingAreaId', f.parkingAreaId);
      if (f.floorNumber !== undefined) params = params.set('floorNumber', f.floorNumber.toString());
      if (f.status) params = params.set('status', f.status);
      if (f.vehicleType) params = params.set('vehicleType', f.vehicleType);
      if (f.availableOnly !== undefined) params = params.set('availableOnly', f.availableOnly.toString());
    } else {
      if (areaIdOrFilter) params = params.set('parkingAreaId', areaIdOrFilter.toString());
      if (status) params = params.set('status', status);
      if (type) params = params.set('vehicleType', type);
      if (availableOnly !== undefined) params = params.set('availableOnly', availableOnly.toString());
    }

    return this.http.get<ApiResponse<ParkingSlotDto[]> & ParkingSlotDto[]>(this.baseUrl, { params });
  }

  getSlots(
    parkingAreaId?: string,
    floorNumber?: number,
    status?: string,
    vehicleType?: string,
    availableOnly?: boolean
  ): Observable<ApiResponse<ParkingSlotDto[]> & ParkingSlotDto[]> {
    return this.getAll({ parkingAreaId, floorNumber, status, vehicleType, availableOnly });
  }

  getById(id: string): Observable<ApiResponse<ParkingSlotDto> & ParkingSlotDto> {
    return this.http.get<ApiResponse<ParkingSlotDto> & ParkingSlotDto>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateParkingSlotRequest | any): Observable<ApiResponse<ParkingSlotDto> & ParkingSlotDto> {
    return this.http.post<ApiResponse<ParkingSlotDto> & ParkingSlotDto>(this.baseUrl, request);
  }

  update(id: string, request: any): Observable<ApiResponse<ParkingSlotDto> & ParkingSlotDto> {
    return this.http.put<ApiResponse<ParkingSlotDto> & ParkingSlotDto>(`${this.baseUrl}/${id}`, request);
  }

  setMaintenanceStatus(id: string, isMaintenance: boolean = true): Observable<ApiResponse<ParkingSlotDto> & ParkingSlotDto> {
    return this.http.put<ApiResponse<ParkingSlotDto> & ParkingSlotDto>(
      `${this.baseUrl}/${id}/maintenance?isMaintenance=${isMaintenance}`,
      {}
    );
  }

  updateStatus(id: string, status: string): Observable<ApiResponse<ParkingSlotDto> & ParkingSlotDto> {
    if (status.toLowerCase() === 'maintenance') {
      return this.setMaintenanceStatus(id, true);
    } else if (status.toLowerCase() === 'available') {
      return this.setMaintenanceStatus(id, false);
    }
    return this.update(id, { status });
  }

  delete(id: string): Observable<ApiResponse<any> & { message: string }> {
    return this.http.delete<ApiResponse<any> & { message: string }>(`${this.baseUrl}/${id}`);
  }
}
