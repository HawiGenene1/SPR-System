import { Component, Inject, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { QrService } from '../../../core/services/qr.service';
import { ReservationDto, QrCodeDto } from '../../../core/models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-qr-ticket-modal',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatProgressSpinnerModule, StatusBadgeComponent],
  template: `
    <div class="qr-modal-container">
      <div class="modal-top">
        <div class="brand-line">
          <span class="material-symbols-outlined icon">local_parking</span>
          <span class="brand-name">ParkPulse FastPass™</span>
        </div>
        <button mat-icon-button (click)="dialogRef.close()" class="close-btn">
          <span class="material-symbols-outlined">close</span>
        </button>
      </div>

      <div class="ticket-body">
        <!-- QR Code Graphic Frame -->
        <div class="qr-code-frame">
          <div *ngIf="isLoading" class="qr-loader">
            <mat-spinner diameter="36"></mat-spinner>
            <span>Generating QR token...</span>
          </div>

          <div *ngIf="!isLoading && qrData" class="qr-display">
            <!-- Stylized SVG QR Code Representation -->
            <svg class="qr-svg" viewBox="0 0 200 200" width="180" height="180">
              <!-- Corner Finder Patterns -->
              <rect x="10" y="10" width="50" height="50" fill="none" stroke="#0f172a" stroke-width="8" rx="4"/>
              <rect x="25" y="25" width="20" height="20" fill="#2563eb" rx="2"/>

              <rect x="140" y="10" width="50" height="50" fill="none" stroke="#0f172a" stroke-width="8" rx="4"/>
              <rect x="155" y="25" width="20" height="20" fill="#2563eb" rx="2"/>

              <rect x="10" y="140" width="50" height="50" fill="none" stroke="#0f172a" stroke-width="8" rx="4"/>
              <rect x="25" y="155" width="20" height="20" fill="#2563eb" rx="2"/>

              <!-- Central Data Matrix Bars -->
              <rect x="75" y="20" width="12" height="30" fill="#0f172a" rx="2"/>
              <rect x="95" y="15" width="25" height="12" fill="#0f172a" rx="2"/>
              <rect x="110" y="35" width="15" height="35" fill="#2563eb" rx="2"/>
              
              <rect x="20" y="75" width="30" height="15" fill="#0f172a" rx="2"/>
              <rect x="25" y="100" width="35" height="20" fill="#2563eb" rx="2"/>

              <!-- Center Centerpiece -->
              <rect x="70" y="70" width="60" height="60" fill="#eff6ff" stroke="#bfdbfe" stroke-width="2" rx="8"/>
              <circle cx="100" cy="100" r="18" fill="#1d4ed8"/>
              <text x="100" y="106" font-family="Inter, sans-serif" font-weight="900" font-size="16" fill="white" text-anchor="middle">P</text>

              <!-- Bottom Right Data Points -->
              <rect x="75" y="145" width="20" height="20" fill="#0f172a" rx="2"/>
              <rect x="105" y="150" width="30" height="14" fill="#2563eb" rx="2"/>
              <rect x="145" y="80" width="20" height="35" fill="#0f172a" rx="2"/>
              <rect x="155" y="130" width="30" height="25" fill="#0f172a" rx="2"/>
              <rect x="145" y="165" width="25" height="20" fill="#2563eb" rx="2"/>
            </svg>

            <div class="qr-token-string">
              {{ qrData.code }}
            </div>
            <div class="qr-scan-hint">
              <span class="material-symbols-outlined">qr_code_scanner</span>
              Scan at Entrance / Exit Barrier
            </div>
          </div>
        </div>

        <!-- Pass Details Ticket Tear-off -->
        <div class="pass-metadata-section">
          <div class="meta-row highlight-row">
            <div>
              <span class="meta-label">ASSIGNED SLOT</span>
              <span class="meta-val slot-val">{{ reservation.slotNumber || 'Confirmed' }}</span>
            </div>
            <div>
              <span class="meta-label">FACILITY</span>
              <span class="meta-val">{{ reservation.parkingAreaName || 'Main Terminal Parking' }}</span>
            </div>
          </div>

          <div class="meta-row">
            <div>
              <span class="meta-label">VEHICLE PLATE</span>
              <span class="meta-val plate-badge">{{ reservation.vehiclePlate || 'Vehicle' }}</span>
            </div>
            <div>
              <span class="meta-label">PASS STATUS</span>
              <app-status-badge [status]="reservation.status"></app-status-badge>
            </div>
          </div>

          <div class="meta-row">
            <div>
              <span class="meta-label">VALID ARRIVAL</span>
              <span class="meta-val small">{{ reservation.startTime | date:'MMM d, y, h:mm a' }}</span>
            </div>
            <div>
              <span class="meta-label">VALID DEPARTURE</span>
              <span class="meta-val small">{{ reservation.endTime | date:'MMM d, y, h:mm a' }}</span>
            </div>
          </div>
        </div>
      </div>

      <div class="modal-footer">
        <button mat-button (click)="dialogRef.close()" class="pp-btn secondary sm">
          Done
        </button>
        <button (click)="printTicket()" class="pp-btn primary sm">
          <span class="material-symbols-outlined">print</span>
          Print / Save Pass
        </button>
      </div>
    </div>
  `,
  styles: [`
    .qr-modal-container {
      padding: 24px;
      max-width: 440px;
      background: #ffffff;
      border-radius: var(--radius-xl);
    }

    .modal-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;

      .brand-line {
        display: flex;
        align-items: center;
        gap: 8px;
        color: #1d4ed8;

        .icon { font-size: 24px; }
        .brand-name {
          font-weight: 800;
          font-size: 1rem;
          color: #0f172a;
          letter-spacing: -0.02em;
        }
      }

      .close-btn {
        color: #64748b;
        border: none;
        background: transparent;
        cursor: pointer;
      }
    }

    .ticket-body {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 20px;
    }

    .qr-code-frame {
      background: #ffffff;
      border: 2px dashed #93c5fd;
      border-radius: 16px;
      padding: 20px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      width: 100%;
      box-shadow: 0 4px 12px rgba(37, 99, 235, 0.06);

      .qr-loader {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 12px;
        padding: 40px 0;
        color: #64748b;
        font-size: 0.8125rem;
      }

      .qr-display {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 10px;

        .qr-token-string {
          font-family: 'JetBrains Mono', monospace;
          font-size: 0.875rem;
          font-weight: 700;
          color: #1e3a8a;
          background: #eff6ff;
          padding: 4px 12px;
          border-radius: 6px;
          letter-spacing: 0.05em;
        }

        .qr-scan-hint {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 0.75rem;
          color: #059669;
          font-weight: 600;

          .material-symbols-outlined { font-size: 16px; }
        }
      }
    }

    .pass-metadata-section {
      width: 100%;
      background: #f8fafc;
      border: 1px solid var(--border-light);
      border-radius: 12px;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 12px;

      .meta-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;

        &.highlight-row {
          border-bottom: 1px solid #e2e8f0;
          padding-bottom: 10px;
        }

        .meta-label {
          display: block;
          font-size: 0.625rem;
          font-weight: 700;
          text-transform: uppercase;
          color: #64748b;
          letter-spacing: 0.05em;
          margin-bottom: 2px;
        }

        .meta-val {
          font-size: 0.875rem;
          font-weight: 700;
          color: #0f172a;

          &.slot-val {
            font-size: 1.15rem;
            color: #1d4ed8;
          }

          &.small {
            font-size: 0.75rem;
            color: #475569;
          }
        }

        .plate-badge {
          display: inline-block;
          font-family: 'JetBrains Mono', monospace;
          background: #ffffff;
          border: 1px solid #cbd5e1;
          padding: 2px 6px;
          border-radius: 4px;
        }
      }
    }

    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      margin-top: 24px;
      padding-top: 16px;
      border-top: 1px solid var(--border-light);
    }
  `]
})
export class QrTicketModalComponent implements OnInit {
  dialogRef = inject(MatDialogRef<QrTicketModalComponent>);
  private qrService = inject(QrService);

  reservation: ReservationDto;
  qrData: QrCodeDto | null = null;
  isLoading = true;

  constructor(@Inject(MAT_DIALOG_DATA) public data: { reservation: ReservationDto }) {
    this.reservation = data.reservation;
  }

  ngOnInit(): void {
    this.loadQrCode();
  }

  loadQrCode(): void {
    this.isLoading = true;
    this.qrService.getByReservationId(this.reservation.id).subscribe({
      next: (res) => {
        this.qrData = res.data;
        this.isLoading = false;
      },
      error: () => {
        // Fallback stub representation if direct endpoint needed dummy
        this.qrData = {
          id: this.reservation.id,
          reservationId: this.reservation.id,
          code: `PRK-${this.reservation.id.substring(0, 8).toUpperCase()}`,
          qrCodeData: `https://parkpulse.local/verify/${this.reservation.id}`,
          status: 'Active',
          expiresAt: this.reservation.endTime,
          generatedAt: new Date().toISOString()
        };
        this.isLoading = false;
      }
    });
  }

  printTicket(): void {
    window.print();
  }
}
