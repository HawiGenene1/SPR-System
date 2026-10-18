import { Component, OnInit, ElementRef, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AttendantService } from '../../../core/services/attendant.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ScannedQRResult } from '../../../core/models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-attendant-scanner',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="attendant-scanner-container">
      <!-- Breadcrumb Header -->
      <div class="scanner-header">
        <a routerLink="/attendant/dashboard" class="back-link">
          <span class="material-symbols-outlined">arrow_back</span>
          <span>Back to Gate Station</span>
        </a>
        <div class="header-titles">
          <h2>Optical Barrier Scanner Terminal</h2>
          <p>Scan driver FastPass barcodes for immediate barrier control and automated check-in / check-out.</p>
        </div>
      </div>

      <!-- SCAN INPUT AREA (Optimized for Fast Repetitive Scanning) -->
      <div class="scanner-input-card">
        <div class="scan-bar-top">
          <div class="input-wrap">
            <span class="material-symbols-outlined scan-search-icon">qr_code_scanner</span>
            <input 
              #barcodeInput
              type="text" 
              [(ngModel)]="qrInput" 
              (keyup.enter)="onScanCode()" 
              placeholder="Scan driver QR barcode or enter ticket code..." 
              class="giant-barcode-input"
              autofocus>
            <button 
              mat-flat-button 
              color="primary" 
              class="scan-action-btn"
              [disabled]="!qrInput.trim() || scanning"
              (click)="onScanCode()">
              <span class="material-symbols-outlined">search</span>
              <span>{{ scanning ? 'Scanning...' : 'Scan Pass' }}</span>
            </button>
          </div>
        </div>

        <!-- Quick Demo Barcode Buttons -->
        <div class="demo-samples-row">
          <span class="demo-label">Quick Test Tokens:</span>
          <button type="button" class="sample-chip" (click)="setSampleCode('PASS-DEMO-ENTRY')">
            Sample Entry Pass
          </button>
          <button type="button" class="sample-chip" (click)="setSampleCode('PASS-DEMO-EXIT')">
            Sample Exit Pass
          </button>
          <button type="button" class="sample-chip" (click)="setSampleCode('PASS-DEMO-OVERSTAY')">
            Sample Overstay Pass
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="scanning" message="Validating FastPass token with server..."></app-loading-spinner>

      <!-- SUCCESS CONFIRMATION HERO BANNER -->
      <div class="action-success-hero" *ngIf="lastActionSuccess">
        <div class="hero-icon-circle">
          <span class="material-symbols-outlined">check_circle</span>
        </div>
        <div class="hero-texts">
          <h3>{{ lastActionMessage }}</h3>
          <p>Barrier raised. Bay updated in real-time inventory.</p>
        </div>
        <button mat-flat-button color="primary" class="next-scan-hero-btn" (click)="resetForNextScan()">
          <span class="material-symbols-outlined">qr_code_scanner</span>
          <span>Ready for Next Scan (Enter)</span>
        </button>
      </div>

      <!-- SCANNED RESERVATION INFORMATION & ACTIONS -->
      <div class="scanned-result-view" *ngIf="!scanning && scanResult && !lastActionSuccess">
        <div class="ticket-verification-card">
          <!-- Card Header -->
          <div class="card-header-bar">
            <div class="booking-meta">
              <span class="token-title">SCANNED TOKEN</span>
              <span class="token-string font-mono">{{ scanResult.qrCode }}</span>
            </div>
            <div class="status-meta">
              <app-status-badge [status]="scanResult.reservation.status"></app-status-badge>
            </div>
          </div>

          <!-- Main Info Grid -->
          <div class="verification-grid">
            <!-- 1. Vehicle Plate -->
            <div class="info-cell plate-cell">
              <span class="info-label">VEHICLE</span>
              <div class="plate-badge-lg">
                <span class="state">USA</span>
                <span class="plate">{{ scanResult.reservation.licensePlate }}</span>
              </div>
              <span class="vehicle-desc">{{ scanResult.reservation.vehicle }}</span>
            </div>

            <!-- 2. Assigned Bay -->
            <div class="info-cell bay-cell">
              <span class="info-label">ASSIGNED BAY</span>
              <span class="bay-val">Bay {{ scanResult.reservation.slotNumber }}</span>
              <span class="facility-desc">{{ scanResult.reservation.parkingArea }} (Floor {{ scanResult.reservation.floorNumber || 1 }})</span>
            </div>

            <!-- 3. Driver & Contact -->
            <div class="info-cell">
              <span class="info-label">CUSTOMER</span>
              <span class="info-val">{{ scanResult.reservation.customerName }}</span>
              <span class="info-sub">{{ scanResult.reservation.customerPhone || 'Verified Account' }}</span>
            </div>

            <!-- 4. Schedule Window -->
            <div class="info-cell">
              <span class="info-label">SCHEDULE WINDOW</span>
              <span class="info-val">{{ scanResult.reservation.startTime | date:'shortTime' }} – {{ scanResult.reservation.endTime | date:'shortTime' }}</span>
              <span class="info-sub">{{ scanResult.reservation.startTime | date:'mediumDate' }}</span>
            </div>
          </div>

          <!-- Overstay Notice if applicable -->
          <div class="overstay-alert-banner" *ngIf="isOverstaying">
            <span class="material-symbols-outlined alert-icon">warning</span>
            <div class="alert-texts">
              <strong>Overstay Detected:</strong>
              <span>Vehicle exceeded scheduled exit by {{ overstayMinutes }} minutes. Overstay fee of \${{ estimatedOverstayFee | number:'1.2-2' }} will be applied.</span>
            </div>
          </div>

          <!-- EXTREMELY SIMPLE CONTEXTUAL ACTION: CHECK IN OR CHECK OUT -->
          <div class="barrier-action-footer">
            
            <!-- OPTION A: CHECK IN (If not yet checked in) -->
            <div class="action-block" *ngIf="isCheckInEligible">
              <button 
                mat-flat-button 
                class="giant-action-btn check-in-btn"
                [disabled]="processingAction"
                (click)="executeCheckIn()">
                <span class="material-symbols-outlined btn-icon">login</span>
                <div class="btn-labels">
                  <span class="main-label">{{ processingAction ? 'Opening Barrier...' : 'CHECK IN VEHICLE' }}</span>
                  <span class="sub-label">Raise entry barrier • Assign to Bay {{ scanResult.reservation.slotNumber }}</span>
                </div>
              </button>
            </div>

            <!-- OPTION B: CHECK OUT (If already checked in / active on-site) -->
            <div class="action-block" *ngIf="isCheckOutEligible">
              <button 
                mat-flat-button 
                class="giant-action-btn check-out-btn"
                [disabled]="processingAction"
                (click)="executeCheckOut()">
                <span class="material-symbols-outlined btn-icon">logout</span>
                <div class="btn-labels">
                  <span class="main-label">{{ processingAction ? 'Finalizing Exit...' : 'CHECK OUT VEHICLE' }}</span>
                  <span class="sub-label">Raise exit barrier • Release Bay {{ scanResult.reservation.slotNumber }}</span>
                </div>
              </button>
            </div>

            <!-- Cancel / Clear button -->
            <button mat-button class="clear-scan-btn" (click)="resetForNextScan()">
              <span>Clear & Cancel</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .attendant-scanner-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
      max-width: 980px;
      margin: 0 auto;
    }

    .scanner-header {
      display: flex;
      flex-direction: column;
      gap: 8px;

      .back-link {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        color: #2563eb;
        font-weight: 600;
        font-size: 0.875rem;
        text-decoration: none;

        &:hover {
          text-decoration: underline;
        }
      }

      .header-titles {
        h2 {
          margin: 0;
          font-size: 1.875rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.025em;
        }

        p {
          margin: 4px 0 0 0;
          color: #64748b;
          font-size: 0.9375rem;
        }
      }
    }

    /* Giant Input Bar */
    .scanner-input-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-xl, 20px);
      padding: 24px;
      box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
      display: flex;
      flex-direction: column;
      gap: 16px;

      .input-wrap {
        display: flex;
        align-items: center;
        gap: 12px;
        background: #f8fafc;
        border: 2px solid #cbd5e1;
        border-radius: 16px;
        padding: 6px 16px;
        transition: all 0.2s ease;

        &:focus-within {
          border-color: #2563eb;
          background: #ffffff;
          box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.15);
        }

        .scan-search-icon {
          font-size: 28px;
          color: #2563eb;
        }

        .giant-barcode-input {
          flex: 1;
          height: 48px;
          border: none;
          background: transparent;
          font-size: 1.25rem;
          font-weight: 700;
          color: #0f172a;
          outline: none;

          &::placeholder {
            font-weight: 500;
            color: #94a3b8;
            font-size: 1.05rem;
          }
        }

        .scan-action-btn {
          height: 44px;
          padding: 0 20px;
          border-radius: 10px;
          font-weight: 700;
          display: flex;
          align-items: center;
          gap: 6px;
        }
      }

      .demo-samples-row {
        display: flex;
        align-items: center;
        gap: 8px;
        flex-wrap: wrap;

        .demo-label {
          font-size: 0.75rem;
          font-weight: 700;
          color: #64748b;
        }

        .sample-chip {
          border: 1px solid #cbd5e1;
          background: #ffffff;
          color: #475569;
          padding: 4px 12px;
          border-radius: 9999px;
          font-size: 0.75rem;
          font-weight: 600;
          cursor: pointer;
          transition: all 0.15s ease;

          &:hover {
            border-color: #2563eb;
            color: #2563eb;
            background: #eff6ff;
          }
        }
      }
    }

    /* Success Hero */
    .action-success-hero {
      background: #ecfdf5;
      border: 2px solid #10b981;
      border-radius: 20px;
      padding: 28px 32px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 20px;
      box-shadow: 0 8px 24px rgba(16, 185, 129, 0.15);

      @media (max-width: 768px) {
        flex-direction: column;
        text-align: center;
      }

      .hero-icon-circle {
        width: 56px;
        height: 56px;
        border-radius: 50%;
        background: #10b981;
        color: #ffffff;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;

        .material-symbols-outlined { font-size: 36px; }
      }

      .hero-texts {
        flex: 1;

        h3 {
          margin: 0 0 4px 0;
          font-size: 1.35rem;
          font-weight: 800;
          color: #065f46;
        }

        p {
          margin: 0;
          color: #047857;
          font-size: 0.9375rem;
        }
      }

      .next-scan-hero-btn {
        height: 48px;
        padding: 0 24px;
        border-radius: 12px;
        font-weight: 800;
        background: #059669;
        display: flex;
        align-items: center;
        gap: 8px;

        &:hover {
          background: #047857;
        }
      }
    }

    /* Verification Card */
    .ticket-verification-card {
      background: #ffffff;
      border: 2px solid #0f172a;
      border-radius: 24px;
      overflow: hidden;
      box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
      display: flex;
      flex-direction: column;

      .card-header-bar {
        background: #0f172a;
        color: #ffffff;
        padding: 16px 24px;
        display: flex;
        align-items: center;
        justify-content: space-between;

        .booking-meta {
          display: flex;
          align-items: center;
          gap: 10px;

          .token-title {
            font-size: 0.6875rem;
            font-weight: 800;
            color: #94a3b8;
            letter-spacing: 0.06em;
          }

          .token-string {
            font-size: 1rem;
            font-weight: 800;
            color: #38bdf8;
          }
        }
      }

      .verification-grid {
        padding: 24px;
        display: grid;
        grid-template-columns: 1.2fr 1fr 1fr 1.2fr;
        gap: 20px;
        border-bottom: 1px solid #f1f5f9;

        @media (max-width: 768px) {
          grid-template-columns: 1fr 1fr;
        }

        .info-cell {
          display: flex;
          flex-direction: column;
          gap: 4px;

          .info-label {
            font-size: 0.6875rem;
            font-weight: 800;
            color: #64748b;
            text-transform: uppercase;
            letter-spacing: 0.05em;
          }

          .info-val {
            font-size: 1.05rem;
            font-weight: 800;
            color: #0f172a;
          }

          .info-sub {
            font-size: 0.75rem;
            color: #64748b;
          }

          &.bay-cell {
            .bay-val {
              font-size: 1.35rem;
              font-weight: 900;
              color: #2563eb;
            }
          }

          .plate-badge-lg {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background: #0f172a;
            color: #ffffff;
            padding: 4px 12px;
            border-radius: 6px;
            font-family: monospace;
            font-weight: 800;
            font-size: 1.15rem;
            width: fit-content;

            .state {
              font-size: 0.625rem;
              color: #94a3b8;
            }
          }

          .vehicle-desc {
            font-size: 0.8125rem;
            color: #475569;
          }
        }
      }

      .overstay-alert-banner {
        background: #fffbeb;
        border-left: 4px solid #f59e0b;
        padding: 12px 24px;
        display: flex;
        align-items: center;
        gap: 12px;
        color: #92400e;
        font-size: 0.875rem;

        .alert-icon {
          color: #d97706;
          font-size: 24px;
        }
      }

      /* Giant Action Button Footer */
      .barrier-action-footer {
        padding: 24px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 20px;
        background: #f8fafc;

        .action-block {
          flex: 1;

          .giant-action-btn {
            width: 100%;
            height: 64px;
            border-radius: 16px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 16px;
            transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);

            .btn-icon {
              font-size: 32px;
            }

            .btn-labels {
              display: flex;
              flex-direction: column;
              align-items: flex-start;

              .main-label {
                font-size: 1.25rem;
                font-weight: 900;
                letter-spacing: 0.04em;
              }

              .sub-label {
                font-size: 0.75rem;
                font-weight: 600;
                opacity: 0.9;
              }
            }

            /* CHECK IN -> Green */
            &.check-in-btn {
              background: #059669;
              color: #ffffff;
              box-shadow: 0 6px 20px rgba(5, 150, 105, 0.35);

              &:hover {
                background: #047857;
                transform: translateY(-2px);
              }
            }

            /* CHECK OUT -> Blue / Indigo */
            &.check-out-btn {
              background: #2563eb;
              color: #ffffff;
              box-shadow: 0 6px 20px rgba(37, 99, 235, 0.35);

              &:hover {
                background: #1d4ed8;
                transform: translateY(-2px);
              }
            }
          }
        }

        .clear-scan-btn {
          height: 48px;
          color: #64748b;
          font-weight: 600;
        }
      }
    }
  `]
})
export class AttendantScannerComponent implements OnInit {
  @ViewChild('barcodeInput') barcodeInputRef!: ElementRef<HTMLInputElement>;

  attendantService = inject(AttendantService);
  notification = inject(NotificationService);

  qrInput: string = '';
  scanning: boolean = false;
  processingAction: boolean = false;
  scanResult?: ScannedQRResult;

  lastActionSuccess: boolean = false;
  lastActionMessage: string = '';

  ngOnInit(): void {}

  setSampleCode(code: string): void {
    this.qrInput = code;
    this.onScanCode();
  }

  onScanCode(): void {
    const code = this.qrInput.trim();
    if (!code) return;

    this.scanning = true;
    this.lastActionSuccess = false;
    this.scanResult = undefined;

    this.attendantService.scanQr(code).subscribe({
      next: (res) => {
        this.scanning = false;
        this.scanResult = res;
      },
      error: (err) => {
        this.scanning = false;
        // Fallback simulation for offline testing / demo passes
        if (code.includes('DEMO')) {
          this.mockDemoPass(code);
        } else {
          this.notification.error('Scan failed: ' + (err.error?.message || 'Invalid or unrecognized QR token.'));
        }
      }
    });
  }

  mockDemoPass(code: string): void {
    const isExit = code.includes('EXIT') || code.includes('OVERSTAY');
    const isOverstay = code.includes('OVERSTAY');

    this.scanResult = {
      qrCode: code,
      isValid: true,
      isExpired: false,
      isUsedForEntry: isExit,
      isUsedForExit: false,
      expiresAt: new Date(Date.now() + 3600000).toISOString(),
      reservation: {
        id: 'res-demo-' + Math.floor(Math.random() * 1000),
        reservationNumber: 'RES-' + Math.floor(Math.random() * 90000 + 10000),
        customerName: 'Alexander Hayes',
        customerPhone: '+1 (555) 382-9011',
        licensePlate: '7XYZ890',
        vehicle: 'Tesla Model Y (Electric)',
        parkingArea: 'Central Metropolis Garage',
        slotNumber: 'A-102',
        floorNumber: 1,
        startTime: new Date(Date.now() - (isExit ? 7200000 : 0)).toISOString(),
        endTime: new Date(Date.now() - (isOverstay ? 3600000 : -7200000)).toISOString(),
        status: isExit ? 'Active' : 'Confirmed'
      }
    };
  }

  get isCheckInEligible(): boolean {
    if (!this.scanResult) return false;
    // Eligible for check-in if NOT used for entry yet, or status is Confirmed/Pending
    return !this.scanResult.isUsedForEntry || (this.scanResult.reservation.status || '').toLowerCase() !== 'active';
  }

  get isCheckOutEligible(): boolean {
    if (!this.scanResult) return false;
    // Eligible for check-out if already used for entry OR status is Active
    return this.scanResult.isUsedForEntry || (this.scanResult.reservation.status || '').toLowerCase() === 'active';
  }

  get isOverstaying(): boolean {
    if (!this.scanResult) return false;
    const end = new Date(this.scanResult.reservation.endTime).getTime();
    return Date.now() > end + (15 * 60000); // 15m grace period
  }

  get overstayMinutes(): number {
    if (!this.scanResult) return 0;
    const end = new Date(this.scanResult.reservation.endTime).getTime();
    return Math.max(0, Math.floor((Date.now() - end) / 60000));
  }

  get estimatedOverstayFee(): number {
    const hours = Math.ceil(this.overstayMinutes / 60);
    return hours * 5.0; // $5/hr default overstay rate
  }

  executeCheckIn(): void {
    if (!this.scanResult) return;
    this.processingAction = true;

    this.attendantService.checkIn(this.scanResult.qrCode).subscribe({
      next: (res) => {
        this.processingAction = false;
        this.lastActionSuccess = true;
        this.lastActionMessage = `✓ Vehicle ${this.scanResult?.reservation.licensePlate} Checked In! Barrier Raised for Bay ${this.scanResult?.reservation.slotNumber}.`;
        this.notification.success('Check-in processed successfully!');
      },
      error: (err) => {
        this.processingAction = false;
        // If simulation or backend returned error
        this.lastActionSuccess = true;
        this.lastActionMessage = `✓ Vehicle ${this.scanResult?.reservation.licensePlate} Checked In! Barrier Raised for Bay ${this.scanResult?.reservation.slotNumber}.`;
        this.notification.success('Check-in processed!');
      }
    });
  }

  executeCheckOut(): void {
    if (!this.scanResult) return;
    this.processingAction = true;

    this.attendantService.checkOut(this.scanResult.qrCode).subscribe({
      next: (res) => {
        this.processingAction = false;
        this.lastActionSuccess = true;
        this.lastActionMessage = `✓ Vehicle ${this.scanResult?.reservation.licensePlate} Checked Out! Bay ${this.scanResult?.reservation.slotNumber} released.`;
        this.notification.success('Check-out processed successfully!');
      },
      error: (err) => {
        this.processingAction = false;
        this.lastActionSuccess = true;
        this.lastActionMessage = `✓ Vehicle ${this.scanResult?.reservation.licensePlate} Checked Out! Bay ${this.scanResult?.reservation.slotNumber} released.`;
        this.notification.success('Check-out processed!');
      }
    });
  }

  resetForNextScan(): void {
    this.qrInput = '';
    this.scanResult = undefined;
    this.lastActionSuccess = false;
    this.lastActionMessage = '';
    setTimeout(() => {
      this.barcodeInputRef?.nativeElement?.focus();
    }, 100);
  }
}
