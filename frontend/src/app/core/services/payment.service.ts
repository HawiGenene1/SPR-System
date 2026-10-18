import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PaymentDto, ProcessPaymentRequest } from '../models';

export interface PaymentFilterParams {
  status?: string;
  method?: string;
  fromDate?: string;
  toDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private readonly baseUrl = `${environment.apiUrl}/payments`;

  constructor(private http: HttpClient) {}

  processPayment(request: ProcessPaymentRequest | any): Observable<ApiResponse<PaymentDto> & PaymentDto> {
    return this.http.post<ApiResponse<PaymentDto> & PaymentDto>(this.baseUrl, request);
  }

  confirmPayment(id: string, request?: any): Observable<ApiResponse<PaymentDto> & PaymentDto> {
    return this.http.post<ApiResponse<PaymentDto> & PaymentDto>(`${this.baseUrl}/${id}/confirm`, request || {});
  }

  getMyPayments(): Observable<ApiResponse<PaymentDto[]> & PaymentDto[]> {
    return this.http.get<ApiResponse<PaymentDto[]> & PaymentDto[]>(`${this.baseUrl}/my`);
  }

  getPaymentById(id: string): Observable<ApiResponse<PaymentDto> & PaymentDto> {
    return this.http.get<ApiResponse<PaymentDto> & PaymentDto>(`${this.baseUrl}/${id}`);
  }

  getPaymentStatus(id: string): Observable<ApiResponse<any> & any> {
    return this.http.get<ApiResponse<any> & any>(`${this.baseUrl}/${id}/status`);
  }

  getAllPayments(
    filterOrStatus?: string | PaymentFilterParams,
    method?: string,
    fromDate?: string,
    toDate?: string
  ): Observable<ApiResponse<PaymentDto[]> & PaymentDto[]> {
    let params = new HttpParams();

    if (filterOrStatus && typeof filterOrStatus === 'object') {
      const f = filterOrStatus as PaymentFilterParams;
      if (f.status) params = params.set('status', f.status);
      if (f.method) params = params.set('method', f.method);
      if (f.fromDate) params = params.set('fromDate', f.fromDate);
      if (f.toDate) params = params.set('toDate', f.toDate);
    } else {
      if (filterOrStatus) params = params.set('status', filterOrStatus.toString());
      if (method) params = params.set('method', method);
      if (fromDate) params = params.set('fromDate', fromDate);
      if (toDate) params = params.set('toDate', toDate);
    }

    return this.http.get<ApiResponse<PaymentDto[]> & PaymentDto[]>(this.baseUrl, { params });
  }

  getByReservationId(reservationId: string): Observable<ApiResponse<PaymentDto> & PaymentDto> {
    return this.http.get<ApiResponse<PaymentDto> & PaymentDto>(`${this.baseUrl}/reservation/${reservationId}`);
  }
}
