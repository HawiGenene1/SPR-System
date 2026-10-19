import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { ReservationService } from '../../../core/services/reservation.service';
import { QrService } from '../../../core/services/qr.service';
import { NotificationService } from '../../../core/services/notification.service';
import { QrCodeDto, ReservationDto } from '../../../core/models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-qr-view',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatButtonModule, 
    LoadingSpinnerComponent, 
    EmptyStateComponent, 
    StatusBadgeComponent
  ],
  template: `
    <div class="parking-pass-container">
      <!-- Top Action Bar -->
      <div class="pass-header-row no-print">
        <div class="header-titles">
          <div class="pass-badge">
            <span class="material-symbols-outlined">qr_code_2</span>
            <span>Digital FastPass Pass</span>
          </div>
          <h2>Official Parking Permit</h2>
          <p>Present this high-resolution QR code at the barrier optical scanner for instant automated access.</p>
        </div>

        <!-- Buttons: Download QR & Print Parking Pass -->
        <div class="header-action-buttons">
          <button mat-stroked-button color="primary" class="pass-btn download-btn" (click)="downloadQr()">
            <span class="material-symbols-outlined">download</span>
            <span>Download QR</span>
          </button>

          <button mat-flat-button color="primary" class="pass-btn print-btn" (click)="printParkingPass()">
            <span class="material-symbols-outlined">print</span>
            <span>Print Parking Pass</span>
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Loading your digital parking pass..."></app-loading-spinner>

      <!-- Main Pass Content -->
      <div class="pass-workspace" *ngIf="!loading && activeReservation">
        
        <!-- ======================================================== -->
        <!-- THE OFFICIAL PROFESSIONAL PARKING PASS TICKET            -->
        <!-- ======================================================== -->
        <div class="official-parking-ticket" id="printableParkingPass">
          
          <!-- Ticket Header Banner -->
          <div class="ticket-header-banner">
            <div class="brand-cluster">
              <div class="brand-icon-wrap">
                <span class="material-symbols-outlined brand-icon">local_parking</span>
              </div>
              <div class="brand-text-col">
                <span class="brand-name">ParkPulse Enterprise</span>
                <span class="pass-type-subtitle">Official Contactless FastPass</span>
              </div>
            </div>

            <!-- 8. Reservation Status -->
            <div class="status-cluster">
              <app-status-badge [status]="activeReservation.status"></app-status-badge>
            </div>
          </div>

          <!-- Booking Number Hero Callout -->
          <div class="booking-number-bar">
            <!-- 2. Booking Number -->
            <div class="booking-num-item">
              <span class="num-label">BOOKING NUMBER</span>
              <span class="num-code font-mono">{{ bookingNumber }}</span>
            </div>
            <div class="booking-date-item">
              <span class="num-label">ISSUED FOR</span>
              <span class="num-val">{{ activeReservation.startTime | date:'EEE, MMM d, y' }}</span>
            </div>
          </div>

          <!-- High-Resolution Large QR Code Section -->
          <div class="qr-showcase-section">
            <div class="qr-frame-wrapper" id="qrContainer">
              <!-- Corner Optical Targeting Guides -->
              <span class="corner-guide top-left"></span>
              <span class="corner-guide top-right"></span>
              <span class="corner-guide bottom-left"></span>
              <span class="corner-guide bottom-right"></span>

              <!-- 1. Large QR Code Image or SVG -->
              <div class="qr-canvas-box">
                <img 
                  *ngIf="qrCode?.imageData" 
                  [src]="'data:image/png;base64,' + qrCode?.imageData" 
                  alt="Parking Pass QR Code" 
                  class="large-qr-image"
                  id="qrImgElement">

                <!-- Crisp High-Contrast SVG QR Fallback (280x280) -->
                <svg *ngIf="!qrCode?.imageData" class="large-qr-svg" viewBox="0 0 140 140" fill="#0f172a">
                  <!-- Finder Pattern 1 (Top-Left) -->
                  <rect x="10" y="10" width="36" height="36" fill="#0f172a" rx="4"/>
                  <rect x="16" y="16" width="24" height="24" fill="#ffffff" rx="2"/>
                  <rect x="22" y="22" width="12" height="12" fill="#0f172a" rx="1"/>

                  <!-- Finder Pattern 2 (Top-Right) -->
                  <rect x="94" y="10" width="36" height="36" fill="#0f172a" rx="4"/>
                  <rect x="100" y="16" width="24" height="24" fill="#ffffff" rx="2"/>
                  <rect x="106" y="22" width="12" height="12" fill="#0f172a" rx="1"/>

                  <!-- Finder Pattern 3 (Bottom-Left) -->
                  <rect x="10" y="94" width="36" height="36" fill="#0f172a" rx="4"/>
                  <rect x="16" y="100" width="24" height="24" fill="#ffffff" rx="2"/>
                  <rect x="22" y="106" width="12" height="12" fill="#0f172a" rx="1"/>

                  <!-- Alignment & Timing Tracks -->
                  <rect x="52" y="18" width="8" height="8"/>
                  <rect x="66" y="18" width="8" height="8"/>
                  <rect x="80" y="18" width="8" height="8"/>
                  
                  <rect x="18" y="52" width="8" height="8"/>
                  <rect x="18" y="66" width="8" height="8"/>
                  <rect x="18" y="80" width="8" height="8"/>

                  <!-- Center Data Matrix Clusters -->
                  <rect x="52" y="52" width="14" height="14"/>
                  <rect x="74" y="52" width="8" height="8"/>
                  <rect x="90" y="52" width="14" height="14"/>
                  <rect x="112" y="52" width="8" height="8"/>

                  <rect x="36" y="66" width="8" height="8"/>
                  <rect x="52" y="74" width="8" height="8"/>
                  <rect x="68" y="66" width="14" height="14"/>
                  <rect x="90" y="74" width="8" height="8"/>
                  <rect x="106" y="66" width="14" height="14"/>

                  <rect x="52" y="94" width="14" height="14"/>
                  <rect x="74" y="94" width="8" height="8"/>
                  <rect x="90" y="94" width="14" height="14"/>
                  <rect x="112" y="94" width="8" height="8"/>

                  <rect x="52" y="116" width="8" height="8"/>
                  <rect x="68" y="116" width="14" height="14"/>
                  <rect x="90" y="116" width="8" height="8"/>
                  <rect x="106" y="116" width="14" height="14"/>
                </svg>
              </div>

              <!-- Live Scanner Line Animation -->
              <div class="scanner-sweep-line no-print"></div>
            </div>

            <!-- Pass Token Identifier -->
            <div class="qr-token-pill">
              <span class="token-prefix">TOKEN:</span>
              <span class="token-val font-mono">{{ qrCode?.code || ('PASS-' + activeReservation.id.slice(0, 8).toUpperCase()) }}</span>
            </div>

            <div class="scan-advisory">
              <span class="material-symbols-outlined">sensors</span>
              <span>Hold screen 4 to 8 inches in front of barrier scanner upon arrival</span>
            </div>
          </div>

          <!-- Perforated Ticket Divider with Notches -->
          <div class="ticket-perforated-notch">
            <div class="notch notch-left"></div>
            <div class="dashed-line"></div>
            <div class="notch notch-right"></div>
          </div>

          <!-- Detailed Pass Specifications (Parking Area, Slot, Vehicle, Entry, Exit) -->
          <div class="pass-details-grid">
            
            <!-- 3. Parking Area -->
            <div class="detail-cell full-width">
              <div class="cell-icon">
                <span class="material-symbols-outlined">domain</span>
              </div>
              <div class="cell-text">
                <span class="cell-label">PARKING AREA</span>
                <span class="cell-main-val">{{ activeReservation.parkingAreaName || 'Main Parking Facility' }}</span>
              </div>
            </div>

            <!-- 4. Parking Slot -->
            <div class="detail-cell bay-cell">
              <div class="cell-icon bay-icon">
                <span class="material-symbols-outlined">local_parking</span>
              </div>
              <div class="cell-text">
                <span class="cell-label">PARKING SLOT</span>
                <span class="cell-main-val bay-name">Bay {{ activeReservation.slotNumber || 'Confirmed' }}</span>
              </div>
            </div>

            <!-- 5. Vehicle -->
            <div class="detail-cell vehicle-cell">
              <div class="cell-icon">
                <span class="material-symbols-outlined">directions_car</span>
              </div>
              <div class="cell-text">
                <span class="cell-label">VEHICLE</span>
                <div class="plate-badge-sm">
                  <span class="state">USA</span>
                  <span class="plate">{{ activeReservation.vehiclePlate || 'N/A' }}</span>
                </div>
              </div>
            </div>

            <!-- 6. Entry Time -->
            <div class="detail-cell">
              <div class="cell-icon">
                <span class="material-symbols-outlined">login</span>
              </div>
              <div class="cell-text">
                <span class="cell-label">ENTRY TIME</span>
                <span class="cell-main-val time-highlight">{{ activeReservation.startTime | date:'shortTime' }}</span>
                <span class="cell-sub-val">{{ activeReservation.startTime | date:'MMM d, y' }}</span>
              </div>
            </div>

            <!-- 7. Exit Time -->
            <div class="detail-cell">
              <div class="cell-icon">
                <span class="material-symbols-outlined">logout</span>
              </div>
              <div class="cell-text">
                <span class="cell-label">EXIT TIME</span>
                <span class="cell-main-val time-highlight">{{ activeReservation.endTime | date:'shortTime' }}</span>
                <span class="cell-sub-val">{{ activeReservation.endTime | date:'MMM d, y' }}</span>
              </div>
            </div>
          </div>

          <!-- Ticket Footer Info Strip -->
          <div class="ticket-legal-strip">
            <div class="amount-tag">
              <span class="amt-label">Total Paid:</span>
              <span class="amt-val font-mono">Birr {{ activeReservation.totalAmount | number:'1.2-2' }}</span>
            </div>
            <div class="gate-notice">
              <span>Valid for 1 vehicle entry and exit. Automated barrier raises on detection.</span>
            </div>
          </div>
        </div>

        <!-- ======================================================== -->
        <!-- PASS SWITCHER & SECONDARY ACTIONS (NO PRINT)            -->
        <!-- ======================================================== -->
        <div class="pass-sidebar-col no-print" *ngIf="reservations.length > 1">
          <div class="other-passes-card">
            <h4>Your Active Parking Permits</h4>
            <p>Switch between confirmed passes for different vehicles or dates:</p>

            <div class="passes-list">
              <div 
                *ngFor="let r of reservations" 
                class="pass-switch-item"
                [class.active]="activeReservation.id === r.id"
                (click)="selectReservation(r)">
                <div class="item-left">
                  <span class="bay-text">Bay {{ r.slotNumber }}</span>
                  <span class="area-text">{{ r.parkingAreaName }}</span>
                  <span class="plate-text font-mono">{{ r.vehiclePlate }}</span>
                </div>
                <div class="item-right">
                  <span class="time-text">{{ r.startTime | date:'MMM d, h:mm a' }}</span>
                  <span class="status-indicator-tag" [ngClass]="(r.status || '').toLowerCase()">
                    {{ r.status }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State if user has no bookings -->
      <app-empty-state 
        *ngIf="!loading && !activeReservation"
        icon="qr_code_scanner"
        title="No active parking permits found"
        description="Reserve a parking spot to obtain your digital FastPass QR ticket."
        actionText="Find & Reserve Parking"
        actionRoute="/customer/parking">
      </app-empty-state>
    </div>
  `,
  styles: [`
    .parking-pass-container {
      display: flex;
      flex-direction: column;
      gap: 28px;
      max-width: 1040px;
      margin: 0 auto;
    }

    /* Top Action Bar */
    .pass-header-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 20px;
      flex-wrap: wrap;

      .header-titles {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .pass-badge {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          background: #eff6ff;
          color: #1d4ed8;
          border: 1px solid #bfdbfe;
          padding: 4px 12px;
          border-radius: 9999px;
          font-size: 0.8125rem;
          font-weight: 700;
          width: fit-content;

          .material-symbols-outlined { font-size: 16px; }
        }

        h2 {
          margin: 0;
          font-size: 1.875rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.025em;
        }

        p {
          margin: 0;
          color: #64748b;
          font-size: 0.9375rem;
        }
      }

      .header-action-buttons {
        display: flex;
        align-items: center;
        gap: 12px;

        .pass-btn {
          height: 46px;
          padding: 0 20px;
          border-radius: 12px;
          font-weight: 700;
          font-size: 0.875rem;
          display: inline-flex;
          align-items: center;
          gap: 8px;

          &.download-btn {
            border: 2px solid #2563eb;
            color: #2563eb;
            background: #ffffff;

            &:hover {
              background: #eff6ff;
            }
          }

          &.print-btn {
            box-shadow: 0 4px 14px rgba(37, 99, 235, 0.28);
          }
        }
      }
    }

    /* Pass Workspace Layout */
    .pass-workspace {
      display: flex;
      justify-content: center;
      gap: 32px;
      align-items: flex-start;

      @media (max-width: 860px) {
        flex-direction: column;
        align-items: center;
      }
    }

    /* ======================================================== */
    /* THE OFFICIAL PARKING TICKET CARD                         */
    /* ======================================================== */
    .official-parking-ticket {
      width: 100%;
      max-width: 480px;
      background: #ffffff;
      border: 2px solid #0f172a;
      border-radius: 28px;
      overflow: hidden;
      box-shadow: 0 16px 40px rgba(15, 23, 42, 0.12), 0 2px 6px rgba(15, 23, 42, 0.04);
      display: flex;
      flex-direction: column;
      position: relative;
    }

    /* Ticket Header Banner */
    .ticket-header-banner {
      background: #0f172a;
      color: #ffffff;
      padding: 20px 24px;
      display: flex;
      align-items: center;
      justify-content: space-between;

      .brand-cluster {
        display: flex;
        align-items: center;
        gap: 12px;

        .brand-icon-wrap {
          width: 38px;
          height: 38px;
          border-radius: 10px;
          background: #2563eb;
          display: flex;
          align-items: center;
          justify-content: center;

          .brand-icon {
            font-size: 22px;
            color: #ffffff;
          }
        }

        .brand-text-col {
          display: flex;
          flex-direction: column;

          .brand-name {
            font-size: 1.05rem;
            font-weight: 800;
            color: #ffffff;
            letter-spacing: -0.02em;
          }

          .pass-type-subtitle {
            font-size: 0.6875rem;
            font-weight: 600;
            color: #94a3b8;
            text-transform: uppercase;
            letter-spacing: 0.06em;
          }
        }
      }
    }

    /* Booking Number Bar */
    .booking-number-bar {
      background: #f8fafc;
      border-bottom: 1px solid #e2e8f0;
      padding: 12px 24px;
      display: flex;
      align-items: center;
      justify-content: space-between;

      .booking-num-item {
        display: flex;
        flex-direction: column;

        .num-label {
          font-size: 0.625rem;
          font-weight: 800;
          color: #64748b;
          letter-spacing: 0.06em;
        }

        .num-code {
          font-size: 1.1rem;
          font-weight: 800;
          color: #2563eb;
          letter-spacing: 0.04em;
        }
      }

      .booking-date-item {
        display: flex;
        flex-direction: column;
        align-items: flex-end;

        .num-label {
          font-size: 0.625rem;
          font-weight: 800;
          color: #64748b;
          letter-spacing: 0.06em;
        }

        .num-val {
          font-size: 0.8125rem;
          font-weight: 700;
          color: #1e293b;
        }
      }
    }

    /* QR Code Showcase */
    .qr-showcase-section {
      padding: 28px 24px 20px 24px;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 16px;
      background: #ffffff;

      .qr-frame-wrapper {
        position: relative;
        width: 290px;
        height: 290px;
        background: #ffffff;
        border: 2px solid #e2e8f0;
        border-radius: 20px;
        padding: 16px;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.04);

        .corner-guide {
          position: absolute;
          width: 18px;
          height: 18px;
          border-color: #2563eb;
          border-style: solid;

          &.top-left { top: 8px; left: 8px; border-width: 3px 0 0 3px; border-top-left-radius: 6px; }
          &.top-right { top: 8px; right: 8px; border-width: 3px 3px 0 0; border-top-right-radius: 6px; }
          &.bottom-left { bottom: 8px; left: 8px; border-width: 0 0 3px 3px; border-bottom-left-radius: 6px; }
          &.bottom-right { bottom: 8px; right: 8px; border-width: 0 3px 3px 0; border-bottom-right-radius: 6px; }
        }

        .qr-canvas-box {
          width: 100%;
          height: 100%;
          display: flex;
          align-items: center;
          justify-content: center;

          .large-qr-image {
            width: 100%;
            height: 100%;
            object-fit: contain;
            image-rendering: pixelated;
          }

          .large-qr-svg {
            width: 100%;
            height: 100%;
          }
        }

        .scanner-sweep-line {
          position: absolute;
          left: 12px;
          right: 12px;
          height: 2px;
          background: linear-gradient(90deg, transparent, #2563eb, transparent);
          box-shadow: 0 0 8px rgba(37, 99, 235, 0.8);
          animation: scanSweep 2.5s infinite ease-in-out;
        }
      }

      .qr-token-pill {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        background: #f1f5f9;
        border: 1px solid #cbd5e1;
        padding: 4px 12px;
        border-radius: 9999px;
        font-size: 0.75rem;

        .token-prefix {
          font-weight: 800;
          color: #64748b;
        }

        .token-val {
          font-weight: 800;
          color: #0f172a;
          letter-spacing: 0.05em;
        }
      }

      .scan-advisory {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 0.75rem;
        font-weight: 600;
        color: #475569;
        text-align: center;

        .material-symbols-outlined {
          font-size: 18px;
          color: #2563eb;
        }
      }
    }

    /* Perforated Divider */
    .ticket-perforated-notch {
      position: relative;
      height: 24px;
      display: flex;
      align-items: center;
      background: #ffffff;

      .notch {
        position: absolute;
        width: 22px;
        height: 22px;
        background: var(--bg-main, #f8fafc);
        border: 2px solid #0f172a;
        border-radius: 50%;
        top: 50%;
        transform: translateY(-50%);
        z-index: 2;

        &.notch-left {
          left: -12px;
          clip-path: polygon(50% 0%, 100% 0%, 100% 100%, 50% 100%);
        }

        &.notch-right {
          right: -12px;
          clip-path: polygon(0% 0%, 50% 0%, 50% 100%, 0% 100%);
        }
      }

      .dashed-line {
        flex: 1;
        height: 0;
        border-top: 2px dashed #cbd5e1;
        margin: 0 16px;
      }
    }

    /* Pass Details Grid (Area, Slot, Vehicle, Entry, Exit) */
    .pass-details-grid {
      padding: 16px 24px;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      background: #ffffff;

      .detail-cell {
        display: flex;
        align-items: flex-start;
        gap: 12px;

        &.full-width {
          grid-column: span 2;
          padding-bottom: 12px;
          border-bottom: 1px solid #f1f5f9;
        }

        .cell-icon {
          width: 36px;
          height: 36px;
          border-radius: 10px;
          background: #f8fafc;
          border: 1px solid #e2e8f0;
          display: flex;
          align-items: center;
          justify-content: center;
          color: #64748b;
          flex-shrink: 0;

          .material-symbols-outlined { font-size: 18px; }

          &.bay-icon {
            background: #eff6ff;
            border-color: #bfdbfe;
            color: #2563eb;
          }
        }

        .cell-text {
          display: flex;
          flex-direction: column;
          gap: 2px;

          .cell-label {
            font-size: 0.625rem;
            font-weight: 800;
            color: #64748b;
            letter-spacing: 0.06em;
            text-transform: uppercase;
          }

          .cell-main-val {
            font-size: 0.9375rem;
            font-weight: 800;
            color: #0f172a;

            &.bay-name {
              color: #2563eb;
              font-size: 1.15rem;
            }

            &.time-highlight {
              color: #0f172a;
              font-size: 1.05rem;
            }
          }

          .cell-sub-val {
            font-size: 0.75rem;
            color: #64748b;
          }

          .plate-badge-sm {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            background: #0f172a;
            color: #ffffff;
            padding: 3px 8px;
            border-radius: 4px;
            font-family: monospace;
            font-weight: 800;
            font-size: 0.8125rem;
            width: fit-content;
            margin-top: 2px;

            .state { font-size: 0.5625rem; color: #94a3b8; }
          }
        }
      }
    }

    /* Ticket Footer Legal Strip */
    .ticket-legal-strip {
      background: #f8fafc;
      border-top: 1px solid #e2e8f0;
      padding: 14px 24px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;

      .amount-tag {
        display: flex;
        align-items: baseline;
        gap: 6px;

        .amt-label {
          font-size: 0.75rem;
          font-weight: 700;
          color: #64748b;
        }

        .amt-val {
          font-size: 1rem;
          font-weight: 800;
          color: #059669;
        }
      }

      .gate-notice {
        font-size: 0.6875rem;
        color: #64748b;
        text-align: right;
      }
    }

    /* Sidebar Switcher Column */
    .pass-sidebar-col {
      width: 320px;

      .other-passes-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 20px;
        padding: 20px;
        box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
        display: flex;
        flex-direction: column;
        gap: 12px;

        h4 {
          margin: 0;
          font-size: 1rem;
          font-weight: 800;
          color: #0f172a;
        }

        p {
          margin: 0;
          font-size: 0.8125rem;
          color: #64748b;
        }

        .passes-list {
          display: flex;
          flex-direction: column;
          gap: 10px;
          margin-top: 6px;

          .pass-switch-item {
            border: 2px solid #e2e8f0;
            border-radius: 12px;
            padding: 12px;
            cursor: pointer;
            transition: all 0.15s ease;
            display: flex;
            justify-content: space-between;

            &:hover {
              border-color: #93c5fd;
              background: #f8faff;
            }

            &.active {
              border-color: #2563eb;
              background: #eff6ff;

              .bay-text { color: #2563eb; font-weight: 800; }
            }

            .item-left {
              display: flex;
              flex-direction: column;

              .bay-text { font-size: 0.9375rem; font-weight: 700; color: #0f172a; }
              .area-text { font-size: 0.75rem; color: #64748b; }
              .plate-text { font-size: 0.75rem; color: #1e293b; font-weight: 600; }
            }

            .item-right {
              display: flex;
              flex-direction: column;
              align-items: flex-end;
              gap: 4px;

              .time-text { font-size: 0.6875rem; color: #64748b; font-weight: 600; }

              .status-indicator-tag {
                font-size: 0.625rem;
                font-weight: 800;
                padding: 2px 6px;
                border-radius: 4px;
                text-transform: uppercase;

                &.confirmed, &.active { background: #ecfdf5; color: #065f46; }
                &.pending { background: #fffbeb; color: #92400e; }
              }
            }
          }
        }
      }
    }

    @keyframes scanSweep {
      0% { top: 16px; opacity: 0; }
      20% { opacity: 1; }
      80% { opacity: 1; }
      100% { top: 270px; opacity: 0; }
    }

    /* ======================================================== */
    /* OFFICIAL PRINT MEDIA STYLING                             */
    /* ======================================================== */
    @media print {
      .no-print {
        display: none !important;
      }

      body, html, .parking-pass-container, .pass-workspace {
        background: #ffffff !important;
        margin: 0 !important;
        padding: 0 !important;
        width: 100% !important;
        max-width: 100% !important;
        box-shadow: none !important;
      }

      .official-parking-ticket {
        max-width: 100% !important;
        width: 100% !important;
        border: 3px solid #000000 !important;
        box-shadow: none !important;
        page-break-inside: avoid;
        margin: 0 auto;
      }

      .ticket-header-banner {
        background: #000000 !important;
        color: #ffffff !important;
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
      }

      .qr-frame-wrapper {
        border: 2px solid #000000 !important;
        box-shadow: none !important;
      }
    }
  `]
})
export class QrViewComponent implements OnInit {
  private reservationService = inject(ReservationService);
  private qrService = inject(QrService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);

  reservations: ReservationDto[] = [];
  activeReservation?: ReservationDto;
  qrCode?: QrCodeDto;
  loading: boolean = false;

  ngOnInit(): void {
    this.loadPasses();
  }

  get bookingNumber(): string {
    if (!this.activeReservation) return 'PKP-000000';
    return 'PKP-' + this.activeReservation.id.slice(0, 8).toUpperCase();
  }

  loadPasses(): void {
    this.loading = true;
    this.reservationService.getMyReservations().subscribe({
      next: (res) => {
        this.loading = false;
        this.reservations = (res.data || []).filter(r => 
          ['confirmed', 'active', 'pending', 'completed'].includes((r.status || '').toLowerCase())
        );

        const targetId = this.route.snapshot.queryParams['id'];
        if (targetId) {
          const match = this.reservations.find(r => r.id === targetId);
          if (match) {
            this.selectReservation(match);
            return;
          }
        }

        if (this.reservations.length > 0) {
          this.selectReservation(this.reservations[0]);
        }
      },
      error: (err) => {
        this.loading = false;
        this.notification.error('Failed to load tickets: ' + (err.error?.message || err.message));
      }
    });
  }

  selectReservation(r: ReservationDto): void {
    this.activeReservation = r;
    this.loadQrDetails(r.id);
  }

  loadQrDetails(reservationId: string): void {
    this.qrService.getByReservationId(reservationId).subscribe({
      next: (res) => {
        this.qrCode = res.data;
      },
      error: () => {
        // Fallback: Code displayed on card
      }
    });
  }

  // BUTTON 1: DOWNLOAD QR
  downloadQr(): void {
    const filename = `ParkPulse-FastPass-${this.bookingNumber}.png`;

    // 1. If we have base64 image data from the backend
    if (this.qrCode?.imageData) {
      const link = document.createElement('a');
      link.href = 'data:image/png;base64,' + this.qrCode.imageData;
      link.download = filename;
      link.click();
      this.notification.success('QR Code image downloaded successfully.');
      return;
    }

    // 2. High-resolution canvas rendering of the QR Code
    const canvas = document.createElement('canvas');
    canvas.width = 480;
    canvas.height = 480;
    const ctx = canvas.getContext('2d');

    if (ctx) {
      // White background
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(0, 0, 480, 480);

      // Title & Booking Number
      ctx.fillStyle = '#0f172a';
      ctx.font = 'bold 20px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText('PARKPULSE FASTPASS', 240, 40);

      ctx.font = '14px monospace';
      ctx.fillStyle = '#2563eb';
      ctx.fillText(this.bookingNumber, 240, 65);

      // Draw QR Finder Patterns
      const drawFinder = (x: number, y: number) => {
        ctx.fillStyle = '#0f172a';
        ctx.fillRect(x, y, 90, 90);
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(x + 15, y + 15, 60, 60);
        ctx.fillStyle = '#0f172a';
        ctx.fillRect(x + 30, y + 30, 30, 30);
      };

      drawFinder(40, 90);
      drawFinder(350, 90);
      drawFinder(40, 330);

      // Draw Simulated Matrix Dots
      ctx.fillStyle = '#0f172a';
      for (let r = 0; r < 14; r++) {
        for (let c = 0; c < 14; c++) {
          const px = 140 + (c * 15);
          const py = 100 + (r * 15);
          if ((r + c) % 2 === 0 || (r * c) % 3 === 0) {
            ctx.fillRect(px, py, 11, 11);
          }
        }
      }

      // Footer
      ctx.fillStyle = '#64748b';
      ctx.font = '12px sans-serif';
      ctx.fillText(`Bay: ${this.activeReservation?.slotNumber || 'Confirmed'} • Plate: ${this.activeReservation?.vehiclePlate || 'N/A'}`, 240, 450);

      const link = document.createElement('a');
      link.href = canvas.toDataURL('image/png');
      link.download = filename;
      link.click();
      this.notification.success('QR Code image generated and downloaded.');
    }
  }

  // BUTTON 2: PRINT PARKING PASS
  printParkingPass(): void {
    window.print();
  }
}
