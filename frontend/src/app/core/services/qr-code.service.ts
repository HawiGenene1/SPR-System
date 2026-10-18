import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, QrCodeDto, ScannedQRResult } from '../models';

export interface ScanQRRequest {
  qrCodeData: string;
  gateId?: string;
  attendantNotes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class QRCodeService {
  private readonly baseUrl = `${environment.apiUrl}/qr-codes`;

  constructor(private http: HttpClient) {}

  getQRCodeByReservationId(reservationId: string): Observable<ApiResponse<QrCodeDto> & QrCodeDto> {
    return this.http.get<ApiResponse<QrCodeDto> & QrCodeDto>(`${this.baseUrl}/${reservationId}`);
  }

  getByReservationId(reservationId: string): Observable<ApiResponse<QrCodeDto> & QrCodeDto> {
    return this.getQRCodeByReservationId(reservationId);
  }

  scanQRCode(request: ScanQRRequest | any): Observable<ApiResponse<ScannedQRResult> & ScannedQRResult> {
    // Standardize request format for backend
    const payload = typeof request === 'string'
      ? { qrCodeData: request }
      : {
          qrCodeData: request.qrCodeData || request.qrCode || request.code,
          gateId: request.gateId,
          attendantNotes: request.attendantNotes
        };

    return this.http.post<ApiResponse<ScannedQRResult> & ScannedQRResult>(`${this.baseUrl}/scan`, payload);
  }

  scan(request: any): Observable<ApiResponse<any> & any> {
    return this.scanQRCode(request);
  }

  validateCode(code: string): Observable<ApiResponse<any> & any> {
    return this.scanQRCode({ qrCodeData: code });
  }
}

// Backwards-compatible alias export
export const QrService = QRCodeService;
export type QrService = QRCodeService;
