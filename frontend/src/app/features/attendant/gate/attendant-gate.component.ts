import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ParkingService } from '../../../core/services/parking.service';
import { ReservationService } from '../../../core/services/reservation.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  CheckInResultDto,
  CheckOutResultDto,
  ReservationDto
} from '../../../core/models';

@Component({
  selector: 'app-attendant-gate',
  standalone: true,
  imports: [CommonModule, FormsModule, MatProgressSpinnerModule],
  template: `
    <div class="gate-page">
      <div class="page-header">
        <div>
          <h2>Gate Access Control Station</h2>
          <p>Scan driver QR FastPass or input reservation access code to lift barriers.</p>
        </div>
      </div>

      <!-- Mode Switcher -->
      <div class="gate-mode-selector pp-card">
        <button 
          class="mode-btn in" 
          [class.active]="activeMode === 'checkin'" 
          (click)="setMode('checkin')">
          <span class="material-symbols-outlined">login</span>
          <span>ENTRY BARRIER (CHECK-IN)</span>
        </button>
        <button 
          class="mode-btn out" 
          [class.active]="activeMode === 'checkout'" 
          (click)="setMode('checkout')">
          <span class="material-symbols-outlined">logout</span>
          <span>EXIT BARRIER (CHECK-OUT)</span>
        </button>
      </div>

      <div class="gate-grid">
        <!-- Scanner Terminal Box -->
        <div class="pp-card terminal-card">
          <div class="terminal-header">
            <span class="terminal-badge" [ngClass]="activeMode">
              {{ activeMode === 'checkin' ? 'ENTRY SCANNER' : 'EXIT SCANNER' }}
            </span>
            <span class="barrier-status">
              <span class="dot" [class.open]="lastResult"></span>
              {{ lastResult ? 'BARRIER OPEN' : 'BARRIER CLOSED' }}
            </span>
          </div>

          <form (ngSubmit)="processCode()" class="scanner-form">
            <div class="code-field">
              <label>Driver QR Code / Pass Token</label>
              <div class="input-scanner-wrap">
                <span class="material-symbols-outlined scanner-icon">qr_code_scanner</span>
                <input 
                  type="text" 
                  [(ngModel)]="qrInput" 
                  name="qrInput" 
                  placeholder="Scan or paste QR code (e.g. PRK-...)" 
                  class="code-input" 
                  autofocus />
              </div>
            </div>

            <div class="notes-field">
              <label>Attendant Notes (Optional)</label>
              <input 
                type="text" 
                [(ngModel)]="attendantNotes" 
                name="attendantNotes" 
                placeholder="e.g. Scratch on bumper, oversize trailer..." 
                class="notes-input" />
            </div>

            <button 
              type="submit" 
              class="pp-btn primary action-submit-btn" 
              [disabled]="!qrInput.trim() || isProcessing">
              <mat-spinner *ngIf="isProcessing" diameter="22" color="accent"></mat-spinner>
              <span *ngIf="!isProcessing">
                {{ activeMode === 'checkin' ? 'Validate Entry & Lift Barrier' : 'Process Exit & Clear Slot' }}
              </span>
            </button>
          </form>

          <!-- Quick Select Active Reservations Helper -->
          <div class="quick-tokens-section" *ngIf="availableReservations.length > 0">
            <span class="quick-title">Quick Select Active Today's Passes:</span>
            <div class="quick-tags">
              <button 
                *ngFor="let res of availableReservations" 
                type="button" 
                class="token-pill"
                (click)="fillFromReservation(res)">
                <span class="plate">{{ res.vehiclePlate || 'Plate' }}</span>
                <span class="slot">Slot {{ res.slotNumber }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Result / Telemetry Feedback Panel -->
        <div class="pp-card result-card">
          <h3>Gate Operation Result</h3>

          <!-- Idle Waiting -->
          <div *ngIf="!lastResult && !isProcessing" class="idle-state">
            <div class="barrier-graphic">
              <span class="material-symbols-outlined">toll</span>
            </div>
            <p>Ready to validate. Scan pass at terminal to verify reservation validity and grant gate access.</p>
          </div>

          <!-- Processing Spinner -->
          <div *ngIf="isProcessing" class="loading-state">
            <mat-spinner diameter="44"></mat-spinner>
            <span>Contacting gate barrier actuator...</span>
          </div>

          <!-- Check-In Success Result -->
          <div *ngIf="checkInResult" class="success-result-box entry">
            <div class="result-header">
              <span class="material-symbols-outlined icon">check_circle</span>
              <div>
                <h4>ENTRY AUTHORIZED</h4>
                <p>Gate barrier raised. Welcome customer.</p>
              </div>
            </div>

            <div class="result-breakdown">
              <div class="breakdown-row">
                <span>Assigned Slot:</span>
                <strong class="slot-text">{{ checkInResult.slotNumber }}</strong>
              </div>
              <div class="breakdown-row">
                <span>Vehicle Plate:</span>
                <strong class="plate-text">{{ checkInResult.licensePlate }}</strong>
              </div>
              <div class="breakdown-row">
                <span>Check-In Time:</span>
                <span>{{ checkInResult.checkInTime | date:'medium' }}</span>
              </div>
            </div>
          </div>

          <!-- Check-Out Success Result -->
          <div *ngIf="checkOutResult" class="success-result-box exit">
            <div class="result-header">
              <span class="material-symbols-outlined icon">directions_car</span>
              <div>
                <h4>EXIT PROCESSED</h4>
                <p>Slot released back to facility pool.</p>
              </div>
            </div>

            <div class="result-breakdown">
              <div class="breakdown-row">
                <span>Duration Parked:</span>
                <strong>{{ checkOutResult.durationMinutes }} Minutes</strong>
              </div>
              <div class="breakdown-row" *ngIf="checkOutResult.overstayMinutes > 0">
                <span class="danger-text">Overstay Detected:</span>
                <strong class="danger-text">{{ checkOutResult.overstayMinutes }} Minutes</strong>
              </div>
              <div class="breakdown-row" *ngIf="checkOutResult.overstayFee > 0">
                <span class="danger-text">Overstay Fee:</span>
                <strong class="danger-text">Birr {{ checkOutResult.overstayFee | number:'1.2-2' }}</strong>
              </div>
              <div class="breakdown-row highlight">
                <span>Total Due at Exit:</span>
                <strong class="price-val">Birr {{ checkOutResult.totalDue | number:'1.2-2' }}</strong>
              </div>
              <div class="breakdown-row">
                <span>Payment Settlement:</span>
                <span class="settled-tag">
                  {{ checkOutResult.requiresAdditionalPayment ? 'Pending Cash/Card Settlement' : 'Pre-paid Online' }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .gate-page {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      h2 { margin: 0 0 4px 0; font-size: 1.75rem; }
      p { margin: 0; color: #64748b; font-size: 0.9375rem; }
    }

    .gate-mode-selector {
      display: grid;
      grid-template-columns: 1fr 1fr;
      padding: 8px;
      gap: 12px;
      background: #f1f5f9;

      .mode-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 10px;
        padding: 16px;
        border: none;
        border-radius: var(--radius-md);
        font-size: 0.9375rem;
        font-weight: 800;
        cursor: pointer;
        background: transparent;
        color: #64748b;
        transition: all 0.2s ease;

        .material-symbols-outlined { font-size: 24px; }

        &.active {
          background: #ffffff;
          box-shadow: 0 4px 10px rgba(0, 0, 0, 0.08);

          &.in { color: #10b981; }
          &.out { color: #2563eb; }
        }
      }
    }

    .gate-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 24px;
    }

    .terminal-card {
      padding: 28px;

      .terminal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 24px;

        .terminal-badge {
          font-size: 0.75rem;
          font-weight: 800;
          letter-spacing: 0.05em;
          padding: 4px 10px;
          border-radius: 999px;

          &.checkin { background: #ecfdf5; color: #059669; border: 1px solid #a7f3d0; }
          &.checkout { background: #eff6ff; color: #1d4ed8; border: 1px solid #bfdbfe; }
        }

        .barrier-status {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 0.75rem;
          font-weight: 700;
          color: #64748b;

          .dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: #ef4444;

            &.open { background: #10b981; }
          }
        }
      }

      .scanner-form {
        display: flex;
        flex-direction: column;
        gap: 20px;

        .code-field, .notes-field {
          display: flex;
          flex-direction: column;
          gap: 8px;

          label {
            font-size: 0.8125rem;
            font-weight: 700;
            color: #334155;
          }

          .input-scanner-wrap {
            position: relative;

            .scanner-icon {
              position: absolute;
              left: 12px;
              top: 50%;
              transform: translateY(-50%);
              color: #2563eb;
              font-size: 24px;
            }

            .code-input {
              width: 100%;
              padding: 14px 14px 14px 44px;
              border: 2px solid #cbd5e1;
              border-radius: 10px;
              font-family: 'JetBrains Mono', monospace;
              font-size: 1.1rem;
              font-weight: 600;
              color: #0f172a;
              background: #f8fafc;
              transition: all 0.2s ease;

              &:focus {
                outline: none;
                border-color: #2563eb;
                background: #ffffff;
                box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.15);
              }
            }
          }

          .notes-input {
            padding: 10px 14px;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            font-family: inherit;
            font-size: 0.875rem;
            background: #ffffff;
            &:focus { outline: none; border-color: #2563eb; }
          }
        }

        .action-submit-btn {
          padding: 16px;
          font-size: 1rem;
          margin-top: 8px;
        }
      }

      .quick-tokens-section {
        margin-top: 28px;
        padding-top: 20px;
        border-top: 1px solid var(--border-light);

        .quick-title {
          display: block;
          font-size: 0.75rem;
          font-weight: 700;
          color: #64748b;
          text-transform: uppercase;
          margin-bottom: 10px;
        }

        .quick-tags {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;

          .token-pill {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 6px 10px;
            background: #f8fafc;
            border: 1px solid #cbd5e1;
            border-radius: 6px;
            cursor: pointer;
            font-size: 0.75rem;
            transition: all 0.15s ease;

            .plate { font-family: 'JetBrains Mono', monospace; font-weight: 700; color: #0f172a; }
            .slot { color: #2563eb; font-weight: 600; }

            &:hover {
              background: #eff6ff;
              border-color: #bfdbfe;
            }
          }
        }
      }
    }

    .result-card {
      padding: 28px;
      display: flex;
      flex-direction: column;

      h3 { margin: 0 0 20px 0; font-size: 1.15rem; color: #0f172a; }

      .idle-state {
        margin: auto;
        text-align: center;
        padding: 40px 20px;
        color: #64748b;

        .barrier-graphic {
          font-size: 48px;
          color: #94a3b8;
          margin-bottom: 16px;
          .material-symbols-outlined { font-size: 64px; }
        }

        p { margin: 0; max-width: 320px; font-size: 0.875rem; }
      }

      .loading-state {
        margin: auto;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 16px;
        color: #64748b;
      }

      .success-result-box {
        border-radius: 12px;
        padding: 20px;

        &.entry {
          background: #ecfdf5;
          border: 1px solid #a7f3d0;
          .result-header .icon { color: #059669; }
        }

        &.exit {
          background: #eff6ff;
          border: 1px solid #bfdbfe;
          .result-header .icon { color: #1d4ed8; }
        }

        .result-header {
          display: flex;
          align-items: center;
          gap: 14px;
          margin-bottom: 18px;

          .icon { font-size: 40px; }
          h4 { margin: 0; font-size: 1.1rem; color: #0f172a; }
          p { margin: 2px 0 0 0; font-size: 0.8125rem; color: #64748b; }
        }

        .result-breakdown {
          display: flex;
          flex-direction: column;
          gap: 10px;
          background: #ffffff;
          padding: 16px;
          border-radius: 8px;

          .breakdown-row {
            display: flex;
            justify-content: space-between;
            font-size: 0.875rem;
            color: #475569;

            .slot-text { font-size: 1.25rem; color: #1d4ed8; font-weight: 800; }
            .plate-text { font-family: 'JetBrains Mono', monospace; font-size: 1rem; color: #0f172a; }
            .danger-text { color: #dc2626; font-weight: 700; }
            .price-val { font-size: 1.2rem; color: #059669; font-weight: 800; }
            .settled-tag { font-weight: 600; color: #059669; }

            &.highlight {
              border-top: 1px solid #f1f5f9;
              padding-top: 8px;
              margin-top: 4px;
            }
          }
        }
      }
    }

    @media (max-width: 900px) {
      .gate-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class AttendantGateComponent implements OnInit {
  private parkingService = inject(ParkingService);
  private reservationService = inject(ReservationService);
  private notify = inject(NotificationService);

  activeMode: 'checkin' | 'checkout' = 'checkin';
  qrInput = '';
  attendantNotes = '';
  isProcessing = false;

  checkInResult: CheckInResultDto | null = null;
  checkOutResult: CheckOutResultDto | null = null;
  lastResult = false;

  availableReservations: ReservationDto[] = [];

  ngOnInit(): void {
    this.loadTodayReservations();
  }

  setMode(mode: 'checkin' | 'checkout'): void {
    this.activeMode = mode;
    this.clearResults();
  }

  clearResults(): void {
    this.checkInResult = null;
    this.checkOutResult = null;
    this.lastResult = false;
  }

  loadTodayReservations(): void {
    this.reservationService.getAllReservations().subscribe({
      next: (res) => {
        this.availableReservations = (res.data || []).slice(0, 6);
      }
    });
  }

  fillFromReservation(res: ReservationDto): void {
    // Fill in reservation code or dummy code
    this.qrInput = res.qrCode?.code || `PRK-${res.id.substring(0, 8).toUpperCase()}`;
  }

  processCode(): void {
    if (!this.qrInput.trim()) return;

    this.isProcessing = true;
    this.clearResults();

    const code = this.qrInput.trim();

    if (this.activeMode === 'checkin') {
      this.parkingService.checkIn({
        qrCode: code,
        attendantNotes: this.attendantNotes
      }).subscribe({
        next: (res) => {
          this.isProcessing = false;
          this.checkInResult = res.data;
          this.lastResult = true;
          this.notify.success(`Check-in successful! Assigned Slot: ${res.data.slotNumber}`);
          this.loadTodayReservations();
        },
        error: () => {
          this.isProcessing = false;
        }
      });
    } else {
      this.parkingService.checkOut({
        qrCode: code,
        attendantNotes: this.attendantNotes
      }).subscribe({
        next: (res) => {
          this.isProcessing = false;
          this.checkOutResult = res.data;
          this.lastResult = true;
          this.notify.success(`Check-out completed! Duration: ${res.data.durationMinutes} mins`);
          this.loadTodayReservations();
        },
        error: () => {
          this.isProcessing = false;
        }
      });
    }
  }
}
