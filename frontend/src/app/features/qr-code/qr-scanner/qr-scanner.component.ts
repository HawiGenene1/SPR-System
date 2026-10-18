import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ParkingService } from '../../../core/services/parking.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CheckInResultDto, CheckOutResultDto } from '../../../core/models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-qr-scanner',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, StatusBadgeComponent],
  template: `
    <div class="scanner-page-container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <h2>FastPass Gate Scanner Station</h2>
          <p>Optical scanner station for contactless vehicle entry check-in and barrier arm release.</p>
        </div>
        <div class="station-status">
          <span class="status-dot"></span>
          <span>Optical Scanner Online</span>
        </div>
      </div>

      <div class="scanner-layout">
        <!-- Viewfinder Left Column -->
        <div class="viewfinder-card">
          <div class="camera-viewport">
            <!-- Simulated Camera Grid -->
            <div class="viewport-overlay">
              <div class="target-bracket corner-tl"></div>
              <div class="target-bracket corner-tr"></div>
              <div class="target-bracket corner-bl"></div>
              <div class="target-bracket corner-br"></div>
              <div class="laser-scanner-line" [class.scanning]="isScanning"></div>
            </div>

            <div class="viewport-status-text">
              <span class="material-symbols-outlined">sensors</span>
              <span>Position customer FastPass QR code inside viewfinder</span>
            </div>
          </div>

          <!-- Token Input Controls -->
          <div class="scanner-controls">
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>FastPass Token / QR Code</mat-label>
              <input 
                matInput 
                [(ngModel)]="qrInput" 
                placeholder="Paste code or scan pass..." 
                (keyup.enter)="triggerCheckIn()">
              <button 
                *ngIf="qrInput" 
                matSuffix 
                mat-icon-button 
                (click)="qrInput = ''">
                <span class="material-symbols-outlined">close</span>
              </button>
            </mat-form-field>

            <div class="action-buttons-row">
              <button 
                mat-flat-button 
                color="primary" 
                class="gate-action-btn check-in-btn"
                [disabled]="!qrInput || processing"
                (click)="triggerCheckIn()">
                <span class="material-symbols-outlined">login</span>
                <span>Gate Check-In</span>
              </button>

              <button 
                mat-flat-button 
                color="accent" 
                class="gate-action-btn check-out-btn"
                [disabled]="!qrInput || processing"
                (click)="triggerCheckOut()">
                <span class="material-symbols-outlined">logout</span>
                <span>Gate Check-Out</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Telemetry & Feedback Right Column -->
        <div class="feedback-col">
          <!-- Gate Barrier Status Card -->
          <div class="gate-status-card" [class.open]="barrierOpen">
            <div class="gate-icon-circle">
              <span class="material-symbols-outlined">
                {{ barrierOpen ? 'lock_open' : 'lock' }}
              </span>
            </div>
            <div class="gate-text">
              <span class="gate-state-title">{{ barrierOpen ? 'BARRIER ARM OPEN' : 'BARRIER ARM CLOSED' }}</span>
              <span class="gate-state-desc">{{ barrierOpen ? 'Vehicle cleared to proceed through gate.' : 'Scan a valid QR code to open barrier.' }}</span>
            </div>
          </div>

          <!-- Check-In Result Result Card -->
          <div class="result-receipt-card" *ngIf="checkInResult">
            <div class="card-title-row">
              <span class="material-symbols-outlined text-green">check_circle</span>
              <h4>Vehicle Checked In</h4>
            </div>

            <div class="receipt-items">
              <div class="r-item">
                <span>Allocated Bay:</span>
                <strong class="bay-highlight">Bay {{ checkInResult.slotNumber }}</strong>
              </div>
              <div class="r-item">
                <span>License Plate:</span>
                <strong>{{ checkInResult.licensePlate }}</strong>
              </div>
              <div class="r-item">
                <span>Timestamp:</span>
                <span>{{ checkInResult.checkInTime | date:'shortTime' }}</span>
              </div>
            </div>
          </div>

          <!-- Check-Out Result Card -->
          <div class="result-receipt-card" *ngIf="checkOutResult">
            <div class="card-title-row">
              <span class="material-symbols-outlined text-green">verified</span>
              <h4>Vehicle Checked Out</h4>
            </div>

            <div class="receipt-items">
              <div class="r-item">
                <span>Duration:</span>
                <strong>{{ checkOutResult.durationMinutes }} minutes</strong>
              </div>
              <div class="r-item">
                <span>Overstay Minutes:</span>
                <strong [class.text-red]="checkOutResult.overstayMinutes > 0">{{ checkOutResult.overstayMinutes }} min</strong>
              </div>
              <div class="r-item" *ngIf="checkOutResult.overstayFee > 0">
                <span>Overstay Fee:</span>
                <strong class="text-red">\${{ checkOutResult.overstayFee | number:'1.2-2' }}</strong>
              </div>
              <div class="r-item">
                <span>Exit Time:</span>
                <span>{{ checkOutResult.checkOutTime | date:'shortTime' }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .scanner-page-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;

      .header-titles {
        h2 {
          margin: 0 0 4px 0;
          font-size: 1.5rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.02em;
        }

        p {
          margin: 0;
          color: #64748b;
          font-size: 0.875rem;
        }
      }

      .station-status {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        background: #ecfdf5;
        border: 1px solid #a7f3d0;
        padding: 6px 14px;
        border-radius: 9999px;
        font-size: 0.8125rem;
        font-weight: 700;
        color: #059669;

        .status-dot {
          width: 8px;
          height: 8px;
          border-radius: 50%;
          background: #10b981;
          box-shadow: 0 0 6px #10b981;
        }
      }
    }

    .scanner-layout {
      display: grid;
      grid-template-columns: 1fr 380px;
      gap: 24px;
      align-items: start;

      @media (max-width: 1024px) {
        grid-template-columns: 1fr;
      }
    }

    .viewfinder-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      padding: 24px;
      box-shadow: 0 1px 3px rgba(15, 23, 42, 0.04);
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .camera-viewport {
      position: relative;
      height: 320px;
      background: #0f172a;
      border-radius: 16px;
      overflow: hidden;
      display: flex;
      align-items: flex-end;
      justify-content: center;
      padding-bottom: 20px;

      .viewport-overlay {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        width: 220px;
        height: 220px;

        .target-bracket {
          position: absolute;
          width: 32px;
          height: 32px;
          border-color: #3b82f6;
          border-style: solid;

          &.corner-tl { top: 0; left: 0; border-width: 4px 0 0 4px; }
          &.corner-tr { top: 0; right: 0; border-width: 4px 4px 0 0; }
          &.corner-bl { bottom: 0; left: 0; border-width: 0 0 4px 4px; }
          &.corner-br { bottom: 0; right: 0; border-width: 0 4px 4px 0; }
        }

        .laser-scanner-line {
          position: absolute;
          width: 100%;
          height: 3px;
          background: linear-gradient(90deg, transparent, #ef4444, transparent);
          box-shadow: 0 0 8px #ef4444;
          top: 0;
          animation: laser-sweep 2.2s ease-in-out infinite alternate;
        }
      }

      .viewport-status-text {
        position: relative;
        z-index: 2;
        display: inline-flex;
        align-items: center;
        gap: 8px;
        color: #94a3b8;
        font-size: 0.8125rem;
        background: rgba(15, 23, 42, 0.7);
        padding: 6px 14px;
        border-radius: 9999px;
      }
    }

    @keyframes laser-sweep {
      0% { top: 10%; }
      100% { top: 90%; }
    }

    .scanner-controls {
      display: flex;
      flex-direction: column;
      gap: 12px;

      .w-full { width: 100%; }

      .action-buttons-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 14px;

        .gate-action-btn {
          height: 46px;
          font-weight: 700;
          font-size: 0.9375rem;
          border-radius: 10px;
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 8px;

          &.check-in-btn { background: #2563eb; color: #ffffff; }
          &.check-out-btn { background: #0d9488; color: #ffffff; }
        }
      }
    }

    .feedback-col {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .gate-status-card {
      background: #ffffff;
      border: 2px solid var(--border-light, #e2e8f0);
      border-radius: 16px;
      padding: 20px;
      display: flex;
      align-items: center;
      gap: 16px;
      transition: all 0.3s ease;

      .gate-icon-circle {
        width: 50px;
        height: 50px;
        border-radius: 12px;
        background: #f1f5f9;
        color: #64748b;
        display: flex;
        align-items: center;
        justify-content: center;
        .material-symbols-outlined { font-size: 28px; }
      }

      .gate-text {
        display: flex;
        flex-direction: column;

        .gate-state-title {
          font-size: 0.9375rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.01em;
        }

        .gate-state-desc {
          font-size: 0.75rem;
          color: #64748b;
        }
      }

      &.open {
        background: #ecfdf5;
        border-color: #10b981;
        box-shadow: 0 4px 14px rgba(16, 185, 129, 0.2);

        .gate-icon-circle {
          background: #10b981;
          color: #ffffff;
        }

        .gate-state-title {
          color: #065f46;
        }
      }
    }

    .result-receipt-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: 16px;
      padding: 20px;
      box-shadow: 0 2px 6px rgba(15, 23, 42, 0.04);

      .card-title-row {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 14px;

        h4 { margin: 0; font-size: 1.05rem; font-weight: 700; color: #0f172a; }
        .text-green { color: #10b981; }
      }

      .receipt-items {
        display: flex;
        flex-direction: column;
        gap: 8px;
        font-size: 0.875rem;

        .r-item {
          display: flex;
          justify-content: space-between;
          color: #64748b;

          strong { color: #0f172a; }
          .bay-highlight { color: #2563eb; font-weight: 800; }
          .text-red { color: #dc2626; font-weight: 700; }
        }
      }
    }
  `]
})
export class QrScannerComponent {
  private parkingService = inject(ParkingService);
  private notification = inject(NotificationService);

  qrInput: string = '';
  isScanning: boolean = true;
  processing: boolean = false;
  barrierOpen: boolean = false;

  checkInResult?: CheckInResultDto;
  checkOutResult?: CheckOutResultDto;

  triggerCheckIn(): void {
    if (!this.qrInput.trim()) return;

    this.processing = true;
    this.parkingService.checkIn({ qrCode: this.qrInput.trim() }).subscribe({
      next: (res) => {
        this.processing = false;
        this.checkInResult = res.data;
        this.checkOutResult = undefined;
        this.openBarrier();
        this.notification.success('Check-in verified! Gate barrier opened.');
      },
      error: (err) => {
        this.processing = false;
        this.notification.error('Check-in failed: ' + (err.error?.message || err.message));
      }
    });
  }

  triggerCheckOut(): void {
    if (!this.qrInput.trim()) return;

    this.processing = true;
    this.parkingService.checkOut({ qrCode: this.qrInput.trim() }).subscribe({
      next: (res) => {
        this.processing = false;
        this.checkOutResult = res.data;
        this.checkInResult = undefined;
        this.openBarrier();
        this.notification.success('Check-out verified! Gate barrier opened.');
      },
      error: (err) => {
        this.processing = false;
        this.notification.error('Check-out failed: ' + (err.error?.message || err.message));
      }
    });
  }

  openBarrier(): void {
    this.barrierOpen = true;
    setTimeout(() => {
      this.barrierOpen = false;
    }, 6000);
  }
}
