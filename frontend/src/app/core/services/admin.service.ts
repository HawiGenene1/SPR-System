import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  UserDto,
  ParkingAreaDto,
  CreateParkingAreaRequest,
  ParkingSlotDto,
  CreateParkingSlotRequest,
  ReservationDto,
  PaymentDto,
  RevenueReportDto,
  OccupancyReportDto,
  SystemSettingsDto
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private baseUrl = `${environment.apiUrl}/admin`;
  private paymentsUrl = `${environment.apiUrl}/payments`;
  private reservationsUrl = `${environment.apiUrl}/reservations`;

  constructor(private http: HttpClient) {}

  // ================= USERS =================
  getUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${this.baseUrl}/users`);
  }

  setUserStatus(id: string, isActive: boolean): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.baseUrl}/users/${id}/status`, isActive);
  }

  // ================= PARKING AREAS =================
  getParkingAreas(): Observable<ParkingAreaDto[]> {
    return this.http.get<ParkingAreaDto[]>(`${this.baseUrl}/parking-areas`);
  }

  createParkingArea(request: CreateParkingAreaRequest): Observable<ParkingAreaDto> {
    return this.http.post<ParkingAreaDto>(`${this.baseUrl}/parking-areas`, request);
  }

  updateParkingArea(id: string, request: any): Observable<ParkingAreaDto> {
    return this.http.put<ParkingAreaDto>(`${this.baseUrl}/parking-areas/${id}`, request);
  }

  deleteParkingArea(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/parking-areas/${id}`);
  }

  // ================= PARKING SLOTS =================
  getParkingSlots(parkingAreaId?: string): Observable<ParkingSlotDto[]> {
    let params = new HttpParams();
    if (parkingAreaId) params = params.set('parkingAreaId', parkingAreaId);
    return this.http.get<ParkingSlotDto[]>(`${this.baseUrl}/parking-slots`, { params });
  }

  createParkingSlot(request: CreateParkingSlotRequest & { parkingAreaId: string; floorNumber?: number; pricePerHour?: number }): Observable<ParkingSlotDto> {
    return this.http.post<ParkingSlotDto>(`${this.baseUrl}/parking-slots`, request);
  }

  updateParkingSlot(id: string, request: any): Observable<ParkingSlotDto> {
    return this.http.put<ParkingSlotDto>(`${this.baseUrl}/parking-slots/${id}`, request);
  }

  deleteParkingSlot(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/parking-slots/${id}`);
  }

  // ================= RESERVATIONS =================
  getAllReservations(filters?: { parkingAreaId?: string; status?: string; fromDate?: string; toDate?: string }): Observable<ReservationDto[]> {
    let params = new HttpParams();
    if (filters?.parkingAreaId) params = params.set('parkingAreaId', filters.parkingAreaId);
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params = params.set('toDate', filters.toDate);

    return this.http.get<ReservationDto[]>(this.reservationsUrl, { params });
  }

  updateReservationStatus(id: string, status: string, note?: string): Observable<ReservationDto> {
    return this.http.put<ReservationDto>(`${this.reservationsUrl}/${id}/status`, { status, note });
  }

  cancelReservation(id: string, reason?: string): Observable<ReservationDto> {
    return this.http.put<ReservationDto>(`${this.reservationsUrl}/${id}/cancel`, { reason });
  }

  // ================= PAYMENTS =================
  getAllPayments(filters?: { status?: string; method?: string; fromDate?: string; toDate?: string }): Observable<PaymentDto[]> {
    let params = new HttpParams();
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.method) params = params.set('method', filters.method);
    if (filters?.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params = params.set('toDate', filters.toDate);

    return this.http.get<PaymentDto[]>(this.paymentsUrl, { params });
  }

  confirmPayment(id: string, transactionId?: string): Observable<PaymentDto> {
    return this.http.post<PaymentDto>(`${this.paymentsUrl}/${id}/confirm`, { transactionId });
  }

  // ================= REPORTS =================
  getRevenueReport(filters?: any): Observable<RevenueReportDto> {
    let params = new HttpParams();
    if (filters?.startDate) params = params.set('startDate', filters.startDate);
    if (filters?.endDate) params = params.set('endDate', filters.endDate);
    if (filters?.parkingAreaId) params = params.set('parkingAreaId', filters.parkingAreaId);
    return this.http.get<RevenueReportDto>(`${this.baseUrl}/reports/revenue`, { params });
  }

  getOccupancyReport(filters?: any): Observable<OccupancyReportDto> {
    let params = new HttpParams();
    if (filters?.startDate) params = params.set('startDate', filters.startDate);
    if (filters?.endDate) params = params.set('endDate', filters.endDate);
    if (filters?.parkingAreaId) params = params.set('parkingAreaId', filters.parkingAreaId);
    return this.http.get<OccupancyReportDto>(`${this.baseUrl}/reports/occupancy`, { params });
  }

  getReservationReport(filters?: any): Observable<any> {
    let params = new HttpParams();
    if (filters?.startDate) params = params.set('startDate', filters.startDate);
    if (filters?.endDate) params = params.set('endDate', filters.endDate);
    return this.http.get<any>(`${this.baseUrl}/reports/reservations`, { params });
  }

  getCustomerReport(filters?: any): Observable<any> {
    let params = new HttpParams();
    if (filters?.startDate) params = params.set('startDate', filters.startDate);
    if (filters?.endDate) params = params.set('endDate', filters.endDate);
    return this.http.get<any>(`${this.baseUrl}/reports/customers`, { params });
  }

  // ================= SETTINGS =================
  getSettings(): Observable<SystemSettingsDto> {
    // Local storage persistence or server fallback
    const saved = localStorage.getItem('parkpulse_system_settings');
    if (saved) {
      try {
        return of(JSON.parse(saved));
      } catch (e) {
        // fallback
      }
    }
    const defaultSettings: SystemSettingsDto = {
      systemName: 'ParkPulse Enterprise',
      supportEmail: 'ops@parkpulse.io',
      contactPhone: '+1 (800) 555-PARK',
      currency: 'USD',
      currencySymbol: '$',
      taxRatePercentage: 8.5,
      gracePeriodMinutes: 15,
      hourlyOverstayFee: 5.0,
      maxAdvanceBookingDays: 30,
      minBookingDurationMinutes: 15,
      autoCancelUnattendedMinutes: 45,
      requirePrepayment: true,
      maintenanceMode: false,
      enableFastPassBarriers: true,
      enableEmailReceipts: true
    };
    return of(defaultSettings);
  }

  saveSettings(settings: SystemSettingsDto): Observable<SystemSettingsDto> {
    localStorage.setItem('parkpulse_system_settings', JSON.stringify(settings));
    return of(settings);
  }
}
