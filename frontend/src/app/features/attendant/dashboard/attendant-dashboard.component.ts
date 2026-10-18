import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AttendantService } from '../../../core/services/attendant.service';
import { AuthService } from '../../../core/services/auth.service';
import { AttendantDashboardDto } from '../../../core/models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-attendant-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="attendant-dashboard-container">
      
      <!-- HERO SCAN CTA BANNER -->
      <div class="hero-scan-banner">
        <div class="hero-left">
          <div class="terminal-badge">
            <span class="live-pulse"></span>
            <span>GATE OPERATIONS TERMINAL</span>
          </div>
          <h2>{{ metrics?.assignedParkingAreaName || 'Metropolis Garage' }}</h2>
          <p>
            Attendant: <strong>{{ auth.currentUserValue?.fullName || 'Station Staff' }}</strong> • 
            {{ metrics?.facilityAddress || 'Downtown Parking District' }}
          </p>
        </div>

        <!-- LARGE SCAN QR CODE BUTTON (KEY REQUIREMENT) -->
        <div class="hero-cta-col">
          <a routerLink="/attendant/scanner" class="giant-scan-button" id="scanQrCtaBtn">
            <div class="scan-button-inner">
              <span class="material-symbols-outlined scan-icon">qr_code_scanner</span>
              <div class="btn-text-block">
                <span class="btn-primary-title">SCAN QR CODE</span>
                <span class="btn-sub-hint">Instant Check-In / Check-Out</span>
              </div>
            </div>
            <div class="button-beam"></div>
          </a>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Connecting to gate terminal telemetry..."></app-loading-spinner>

      <div class="dashboard-body" *ngIf="!loading">
        <!-- Live Facility Occupancy & Operations KPIs -->
        <div class="kpi-grid">
          <!-- KPI 1: Live Occupancy -->
          <div class="kpi-card highlight-blue">
            <div class="kpi-top">
              <span class="kpi-label">Facility Occupancy</span>
              <span class="material-symbols-outlined kpi-icon">garage</span>
            </div>
            <div class="kpi-middle">
              <span class="kpi-value text-blue">{{ metrics?.occupancyRatePercentage || 0 }}%</span>
              <span class="kpi-sub">
                <strong>{{ metrics?.occupiedSlots || 0 }}</strong> of {{ metrics?.totalSlots || 0 }} bays occupied
              </span>
            </div>
            <div class="occupancy-bar-track">
              <div class="occupancy-bar-fill" [style.width.%]="metrics?.occupancyRatePercentage || 0"></div>
            </div>
          </div>

          <!-- KPI 2: Available Slots -->
          <div class="kpi-card highlight-green">
            <div class="kpi-top">
              <span class="kpi-label">Available Bays</span>
              <span class="material-symbols-outlined kpi-icon">local_parking</span>
            </div>
            <div class="kpi-middle">
              <span class="kpi-value text-green">{{ metrics?.availableSlots || 0 }}</span>
              <span class="kpi-sub">Free for immediate entry</span>
            </div>
            <a routerLink="/attendant/parking-status" class="kpi-link">
              <span>View Bay Status Map</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>

          <!-- KPI 3: Check-Ins Today -->
          <div class="kpi-card highlight-teal">
            <div class="kpi-top">
              <span class="kpi-label">Today's Check-Ins</span>
              <span class="material-symbols-outlined kpi-icon">login</span>
            </div>
            <div class="kpi-middle">
              <span class="kpi-value text-teal">{{ metrics?.checkInsToday || metrics?.todayCheckIns || 0 }}</span>
              <span class="kpi-sub">Vehicles entered today</span>
            </div>
            <a routerLink="/attendant/entries" class="kpi-link">
              <span>View Check-In Log</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>

          <!-- KPI 4: Check-Outs Today -->
          <div class="kpi-card highlight-indigo">
            <div class="kpi-top">
              <span class="kpi-label">Today's Check-Outs</span>
              <span class="material-symbols-outlined kpi-icon">logout</span>
            </div>
            <div class="kpi-middle">
              <span class="kpi-value text-indigo">{{ metrics?.checkOutsToday || metrics?.todayCheckOuts || 0 }}</span>
              <span class="kpi-sub">Vehicles departed today</span>
            </div>
            <a routerLink="/attendant/exits" class="kpi-link">
              <span>View Check-Out Log</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>
        </div>

        <!-- Quick Access Gate Terminal Cards -->
        <div class="quick-terminal-grid">
          <!-- Card 1: Scanner Station -->
          <a routerLink="/attendant/scanner" class="quick-card scan-card">
            <div class="q-icon-wrap">
              <span class="material-symbols-outlined">qr_code_scanner</span>
            </div>
            <div class="q-texts">
              <h4>Scanner Terminal</h4>
              <p>Optical scanner station to validate QR tokens for rapid gate entry or exit.</p>
            </div>
            <span class="q-action-pill">Launch Scanner →</span>
          </a>

          <!-- Card 2: Parking Status Map -->
          <a routerLink="/attendant/parking-status" class="quick-card">
            <div class="q-icon-wrap">
              <span class="material-symbols-outlined">grid_view</span>
            </div>
            <div class="q-texts">
              <h4>Live Bay Status Map</h4>
              <p>Visual floor plan of all slots. Inspect occupied vehicles and vacant bays.</p>
            </div>
            <span class="q-action-pill">Inspect Bays →</span>
          </a>

          <!-- Card 3: Entries Log -->
          <a routerLink="/attendant/entries" class="quick-card">
            <div class="q-icon-wrap">
              <span class="material-symbols-outlined">directions_car</span>
            </div>
            <div class="q-texts">
              <h4>Today's Entries</h4>
              <p>Monitor all parked cars currently on-site and their arrival timestamps.</p>
            </div>
            <span class="q-action-pill">View Entries →</span>
          </a>

          <!-- Card 4: Exits Log -->
          <a routerLink="/attendant/exits" class="quick-card">
            <div class="q-icon-wrap">
              <span class="material-symbols-outlined">history</span>
            </div>
            <div class="q-texts">
              <h4>Today's Exits</h4>
              <p>Review completed parking sessions, stay durations, and overstay settlements.</p>
            </div>
            <span class="q-action-pill">View Exits →</span>
          </a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .attendant-dashboard-container {
      display: flex;
      flex-direction: column;
      gap: 28px;
    }

    /* Giant Hero Scan Banner */
    .hero-scan-banner {
      background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
      color: #ffffff;
      border-radius: var(--radius-xl, 24px);
      padding: 36px 40px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 32px;
      box-shadow: 0 12px 32px rgba(15, 23, 42, 0.2);
      border: 1px solid rgba(255, 255, 255, 0.08);
      position: relative;
      overflow: hidden;

      @media (max-width: 900px) {
        flex-direction: column;
        align-items: flex-start;
        padding: 28px;
      }

      .hero-left {
        display: flex;
        flex-direction: column;
        gap: 8px;
        z-index: 2;

        .terminal-badge {
          display: inline-flex;
          align-items: center;
          gap: 8px;
          background: rgba(37, 99, 235, 0.2);
          border: 1px solid rgba(59, 130, 246, 0.4);
          color: #93c5fd;
          padding: 4px 12px;
          border-radius: 9999px;
          font-size: 0.75rem;
          font-weight: 800;
          letter-spacing: 0.08em;
          width: fit-content;

          .live-pulse {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: #10b981;
            box-shadow: 0 0 8px #10b981;
          }
        }

        h2 {
          margin: 0;
          font-size: 2rem;
          font-weight: 800;
          color: #ffffff;
          letter-spacing: -0.025em;
        }

        p {
          margin: 0;
          color: #94a3b8;
          font-size: 0.9375rem;

          strong {
            color: #ffffff;
          }
        }
      }

      /* LARGE SCAN QR CODE BUTTON */
      .hero-cta-col {
        z-index: 2;
        flex-shrink: 0;

        .giant-scan-button {
          display: block;
          position: relative;
          background: #2563eb;
          color: #ffffff;
          text-decoration: none;
          padding: 20px 36px;
          border-radius: 18px;
          box-shadow: 0 8px 24px rgba(37, 99, 235, 0.4), 0 0 0 1px rgba(255, 255, 255, 0.2);
          transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
          overflow: hidden;

          &:hover {
            background: #1d4ed8;
            transform: translateY(-3px) scale(1.02);
            box-shadow: 0 16px 36px rgba(37, 99, 235, 0.5), 0 0 0 2px rgba(255, 255, 255, 0.4);
          }

          &:active {
            transform: translateY(1px);
          }

          .scan-button-inner {
            display: flex;
            align-items: center;
            gap: 16px;

            .scan-icon {
              font-size: 44px;
              color: #ffffff;
            }

            .btn-text-block {
              display: flex;
              flex-direction: column;

              .btn-primary-title {
                font-size: 1.35rem;
                font-weight: 900;
                letter-spacing: 0.04em;
                color: #ffffff;
                text-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
              }

              .btn-sub-hint {
                font-size: 0.8125rem;
                font-weight: 600;
                color: #bfdbfe;
              }
            }
          }
        }
      }
    }

    /* KPI Grid */
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 20px;

      @media (max-width: 1100px) {
        grid-template-columns: repeat(2, 1fr);
      }

      @media (max-width: 600px) {
        grid-template-columns: 1fr;
      }

      .kpi-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-xl, 20px);
        padding: 22px;
        box-shadow: 0 4px 12px rgba(15, 23, 42, 0.03);
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        min-height: 140px;
        gap: 12px;

        .kpi-top {
          display: flex;
          justify-content: space-between;
          align-items: center;

          .kpi-label {
            font-size: 0.8125rem;
            font-weight: 700;
            color: #64748b;
            text-transform: uppercase;
            letter-spacing: 0.05em;
          }

          .kpi-icon {
            font-size: 24px;
            color: #94a3b8;
          }
        }

        .kpi-middle {
          display: flex;
          flex-direction: column;

          .kpi-value {
            font-size: 2.1rem;
            font-weight: 900;
            line-height: 1;

            &.text-blue { color: #2563eb; }
            &.text-green { color: #059669; }
            &.text-teal { color: #0d9488; }
            &.text-indigo { color: #4f46e5; }
          }

          .kpi-sub {
            font-size: 0.8125rem;
            color: #64748b;
            margin-top: 4px;
          }
        }

        .occupancy-bar-track {
          height: 8px;
          background: #f1f5f9;
          border-radius: 9999px;
          overflow: hidden;

          .occupancy-bar-fill {
            height: 100%;
            background: #2563eb;
            border-radius: 9999px;
            transition: width 0.3s ease;
          }
        }

        .kpi-link {
          display: inline-flex;
          align-items: center;
          gap: 4px;
          font-size: 0.75rem;
          font-weight: 700;
          color: #2563eb;
          text-decoration: none;

          .material-symbols-outlined { font-size: 16px; }

          &:hover {
            text-decoration: underline;
          }
        }
      }
    }

    /* Quick Terminal Grid */
    .quick-terminal-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 20px;
      margin-top: 4px;

      @media (max-width: 1100px) {
        grid-template-columns: repeat(2, 1fr);
      }

      @media (max-width: 600px) {
        grid-template-columns: 1fr;
      }

      .quick-card {
        background: #ffffff;
        border: 2px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-xl, 20px);
        padding: 24px;
        text-decoration: none;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        gap: 16px;
        transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);

        &:hover {
          border-color: #3b82f6;
          transform: translateY(-3px);
          box-shadow: 0 10px 24px rgba(37, 99, 235, 0.08);

          .q-action-pill {
            background: #2563eb;
            color: #ffffff;
          }
        }

        &.scan-card {
          border-color: #bfdbfe;
          background: #f8faff;

          .q-icon-wrap {
            background: #2563eb;
            color: #ffffff;
          }
        }

        .q-icon-wrap {
          width: 48px;
          height: 48px;
          border-radius: 14px;
          background: #eff6ff;
          color: #2563eb;
          display: flex;
          align-items: center;
          justify-content: center;

          .material-symbols-outlined { font-size: 26px; }
        }

        .q-texts {
          h4 {
            margin: 0 0 6px 0;
            font-size: 1.1rem;
            font-weight: 800;
            color: #0f172a;
          }

          p {
            margin: 0;
            color: #64748b;
            font-size: 0.8125rem;
            line-height: 1.4;
          }
        }

        .q-action-pill {
          padding: 6px 12px;
          border-radius: 8px;
          background: #f1f5f9;
          color: #475569;
          font-size: 0.75rem;
          font-weight: 700;
          width: fit-content;
          transition: all 0.15s ease;
        }
      }
    }
  `]
})
export class AttendantDashboardComponent implements OnInit {
  attendantService = inject(AttendantService);
  auth = inject(AuthService);

  metrics?: AttendantDashboardDto;
  loading: boolean = true;

  ngOnInit(): void {
    this.loadMetrics();
  }

  loadMetrics(): void {
    this.loading = true;
    this.attendantService.getOccupancy().subscribe({
      next: (res) => {
        this.loading = false;
        this.metrics = res;
      },
      error: () => {
        this.loading = false;
      }
    });
  }
}
