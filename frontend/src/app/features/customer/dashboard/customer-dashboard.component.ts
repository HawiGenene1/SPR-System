import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { DashboardService } from '../../../core/services/dashboard.service';
import { ParkingAreaService } from '../../../core/services/parking-area.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CustomerDashboardDto, ParkingAreaDto, ReservationDto, ParkingRecordDto } from '../../../core/models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { DataTableComponent, TableColumn } from '../../../shared/components/data-table/data-table.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatButtonModule, 
    LoadingSpinnerComponent, 
    StatusBadgeComponent, 
    DataTableComponent,
    EmptyStateComponent
  ],
  template: `
    <div class="customer-dashboard-container">
      <!-- Welcome Hero Header -->
      <div class="welcome-hero-banner">
        <div class="hero-text-col">
          <div class="hero-badge">Customer Self-Service Hub</div>
          <h2>Welcome back, {{ auth.currentUserValue?.fullName || 'Driver' }} 👋</h2>
          <p>Real-time slot availability, contactless QR fast-passes, and live vehicle dwell status.</p>
        </div>
        <div class="hero-actions-col">
          <a routerLink="/customer/parking" mat-flat-button color="primary" class="hero-cta-btn">
            <span class="material-symbols-outlined">search_check</span>
            <span>Find & Reserve Bay</span>
          </a>
          <a routerLink="/customer/vehicles" mat-stroked-button class="hero-secondary-btn">
            <span class="material-symbols-outlined">directions_car</span>
            <span>My Vehicles</span>
          </a>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Loading your personalized parking portal..."></app-loading-spinner>

      <!-- Dashboard Body -->
      <div class="dashboard-body" *ngIf="!loading">
        <!-- 5 Required Core KPI Cards -->
        <div class="kpi-grid">
          <!-- 1. Upcoming Booking -->
          <div class="kpi-card highlight-blue" [class.has-data]="!!upcomingBooking">
            <div class="kpi-top">
              <span class="kpi-label">Upcoming Booking</span>
              <span class="material-symbols-outlined kpi-icon">event_upcoming</span>
            </div>
            <div class="kpi-main">
              <span class="kpi-value text-blue" *ngIf="upcomingBooking">
                Bay {{ upcomingBooking.slotNumber || 'Confirmed' }}
              </span>
              <span class="kpi-value empty-text" *ngIf="!upcomingBooking">None</span>
              <span class="kpi-sub" *ngIf="upcomingBooking">
                {{ upcomingBooking.startTime | date:'shortTime' }} ({{ upcomingBooking.parkingAreaName || 'Main Facility' }})
              </span>
              <span class="kpi-sub" *ngIf="!upcomingBooking">No future reservations scheduled</span>
            </div>
            <a *ngIf="upcomingBooking" [routerLink]="['/qr-code']" [queryParams]="{ id: upcomingBooking.id }" class="kpi-action-link">
              <span>View FastPass Pass</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>

          <!-- 2. Active Parking -->
          <div class="kpi-card highlight-green" [class.has-data]="!!activeParking">
            <div class="kpi-top">
              <span class="kpi-label">Active Parking</span>
              <span class="material-symbols-outlined kpi-icon">local_parking</span>
            </div>
            <div class="kpi-main">
              <span class="kpi-value text-green" *ngIf="activeParking">
                Bay {{ activeParking.slotNumber || 'Parked' }}
              </span>
              <span class="kpi-value empty-text" *ngIf="!activeParking">Not Parked</span>
              <span class="kpi-sub" *ngIf="activeParking">
                Plate: {{ activeParking.vehiclePlate || 'Vehicle' }} • {{ activeParking.parkingAreaName }}
              </span>
              <span class="kpi-sub" *ngIf="!activeParking">Vehicle is not currently on-site</span>
            </div>
            <a *ngIf="activeParking" routerLink="/customer/history" class="kpi-action-link">
              <span>Live Parking Status</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>

          <!-- 3. Available Parking -->
          <div class="kpi-card highlight-cyan">
            <div class="kpi-top">
              <span class="kpi-label">Available Parking</span>
              <span class="material-symbols-outlined kpi-icon">garage</span>
            </div>
            <div class="kpi-main">
              <span class="kpi-value text-cyan">{{ totalAvailableSlots }} Free</span>
              <span class="kpi-sub">Across {{ parkingAreas.length }} modern facilities</span>
            </div>
            <a routerLink="/customer/parking" class="kpi-action-link">
              <span>Browse Real-Time Bays</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>

          <!-- 4. Total Reservations -->
          <div class="kpi-card highlight-amber">
            <div class="kpi-top">
              <span class="kpi-label">Total Reservations</span>
              <span class="material-symbols-outlined kpi-icon">confirmation_number</span>
            </div>
            <div class="kpi-main">
              <span class="kpi-value text-amber">{{ totalReservationsCount }}</span>
              <span class="kpi-sub">{{ completedCount }} completed • \${{ totalSpent | number:'1.2-2' }} spent</span>
            </div>
            <a routerLink="/customer/reservations" class="kpi-action-link">
              <span>View All Tickets</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>
        </div>

        <!-- Active or Upcoming Booking Spotlight Banner -->
        <div class="spotlight-banner" *ngIf="featuredBooking">
          <div class="spotlight-left">
            <div class="status-indicator-badge">
              <span class="indicator-dot"></span>
              <span>{{ (featuredBooking.status || '').toUpperCase() }} RESERVATION</span>
            </div>
            <h3>Bay {{ featuredBooking.slotNumber }} at {{ featuredBooking.parkingAreaName || 'Main Parking Facility' }}</h3>
            <div class="meta-pills">
              <span class="meta-pill">
                <span class="material-symbols-outlined">directions_car</span>
                <span>{{ featuredBooking.vehiclePlate || 'Vehicle' }}</span>
              </span>
              <span class="meta-pill">
                <span class="material-symbols-outlined">schedule</span>
                <span>{{ featuredBooking.startTime | date:'short' }} - {{ featuredBooking.endTime | date:'shortTime' }}</span>
              </span>
              <span class="meta-pill">
                <span class="material-symbols-outlined">payments</span>
                <span>\${{ featuredBooking.totalAmount | number:'1.2-2' }}</span>
              </span>
            </div>
          </div>
          <div class="spotlight-right">
            <a [routerLink]="['/qr-code']" [queryParams]="{ id: featuredBooking.id }" mat-flat-button color="primary" class="fastpass-btn">
              <span class="material-symbols-outlined">qr_code</span>
              <span>Open FastPass QR</span>
            </a>
          </div>
        </div>

        <!-- Available Facilities Grid Preview -->
        <div class="facilities-section">
          <div class="section-header">
            <div>
              <h3>Available Facilities & Live Bays</h3>
              <p>Reserve in advance to guarantee an open spot with automated barrier entry.</p>
            </div>
            <a routerLink="/customer/parking" class="view-all-link">
              <span>View All Facilities</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>

          <div class="facilities-cards-grid">
            <div *ngFor="let area of parkingAreas.slice(0, 3)" class="facility-card">
              <div class="f-card-top">
                <div class="f-info">
                  <h4>{{ area.name }}</h4>
                  <span class="f-location">{{ area.location || 'Metropolis Center' }}</span>
                </div>
                <div class="f-rate-box">
                  <span class="f-rate">\${{ area.hourlyRate | number:'1.2-2' }}</span>
                  <span class="f-unit">/hr</span>
                </div>
              </div>

              <div class="f-meter-bar">
                <div 
                  class="f-meter-fill" 
                  [style.width.%]="getOccupancyPercent(area)">
                </div>
              </div>

              <div class="f-card-bottom">
                <span class="f-slots-count">
                  <strong class="text-green">{{ area.availableSlots }}</strong> of {{ area.totalSlots }} slots free
                </span>
                <a [routerLink]="['/customer/parking']" [queryParams]="{ areaId: area.id }" mat-flat-button color="primary" class="quick-reserve-btn">
                  Reserve
                </a>
              </div>
            </div>
          </div>
        </div>

        <!-- 5. Recent History Table -->
        <div class="history-section">
          <div class="section-header">
            <div>
              <h3>Recent Parking Activity</h3>
              <p>Your recent parking entries, departures, and settled visits.</p>
            </div>
            <a routerLink="/customer/history" class="view-all-link">
              <span>Full Parking History</span>
              <span class="material-symbols-outlined">arrow_forward</span>
            </a>
          </div>

          <app-data-table
            [columns]="historyColumns"
            [data]="recentHistory"
            [pagination]="false"
            emptyTitle="No recent parking history"
            emptyDescription="Your completed and active parking check-ins will be logged here."
            emptyIcon="history"
            emptyActionText="Find & Reserve a Bay"
            emptyActionRoute="/customer/parking">

            <ng-template let-row let-col="column">
              <ng-container *ngIf="col.key === 'slotNumber'">
                <strong class="bay-tag">Bay {{ row.slotNumber }}</strong>
              </ng-container>

              <ng-container *ngIf="col.key === 'licensePlate'">
                <span class="plate-mini">{{ row.licensePlate }}</span>
              </ng-container>

              <ng-container *ngIf="col.key === 'checkInTime'">
                <span class="date-txt">{{ row.checkInTime | date:'MMM d, h:mm a' }}</span>
              </ng-container>

              <ng-container *ngIf="col.key === 'checkOutTime'">
                <span class="date-txt" *ngIf="row.checkOutTime">{{ row.checkOutTime | date:'MMM d, h:mm a' }}</span>
                <span class="active-pulse-txt" *ngIf="!row.checkOutTime">Active On-Site</span>
              </ng-container>

              <ng-container *ngIf="col.key === 'duration'">
                <span>{{ row.durationMinutes ? row.durationMinutes + ' mins' : 'In Progress' }}</span>
              </ng-container>

              <ng-container *ngIf="col.key === 'status'">
                <app-status-badge [status]="row.status || (row.checkOutTime ? 'Completed' : 'Active')"></app-status-badge>
              </ng-container>
            </ng-template>
          </app-data-table>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .customer-dashboard-container {
      display: flex;
      flex-direction: column;
      gap: 28px;
    }

    .welcome-hero-banner {
      background: linear-gradient(135deg, #1e3a8a 0%, #2563eb 100%);
      border-radius: var(--radius-xl, 20px);
      padding: 32px 36px;
      color: #ffffff;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 24px;
      box-shadow: 0 10px 25px -5px rgba(37, 99, 235, 0.25);
      flex-wrap: wrap;

      .hero-text-col {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .hero-badge {
          display: inline-block;
          font-size: 0.6875rem;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.1em;
          background: rgba(255, 255, 255, 0.18);
          padding: 4px 12px;
          border-radius: 9999px;
          width: fit-content;
        }

        h2 {
          margin: 0;
          font-size: 1.875rem;
          font-weight: 800;
          letter-spacing: -0.02em;
          color: #ffffff;
        }

        p {
          margin: 0;
          color: #bfdbfe;
          font-size: 0.9375rem;
          max-width: 560px;
        }
      }

      .hero-actions-col {
        display: flex;
        align-items: center;
        gap: 12px;

        .hero-cta-btn {
          background: #ffffff;
          color: #1e3a8a;
          font-weight: 700;
          height: 44px;
          border-radius: 10px;
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .hero-secondary-btn {
          border-color: rgba(255, 255, 255, 0.4);
          color: #ffffff;
          font-weight: 600;
          height: 44px;
          border-radius: 10px;
          display: flex;
          align-items: center;
          gap: 8px;

          &:hover {
            background: rgba(255, 255, 255, 0.1);
          }
        }
      }
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 16px;

      .kpi-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-lg, 16px);
        padding: 22px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        box-shadow: 0 1px 3px rgba(15, 23, 42, 0.04);
        transition: transform 0.2s ease, box-shadow 0.2s ease;

        &:hover {
          transform: translateY(-2px);
          box-shadow: 0 8px 16px rgba(15, 23, 42, 0.06);
        }

        .kpi-top {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 12px;

          .kpi-label {
            font-size: 0.75rem;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.06em;
            color: #64748b;
          }

          .kpi-icon {
            font-size: 22px;
            color: #94a3b8;
          }
        }

        .kpi-main {
          display: flex;
          flex-direction: column;
          margin-bottom: 14px;

          .kpi-value {
            font-size: 1.75rem;
            font-weight: 800;
            line-height: 1.1;
            margin-bottom: 4px;

            &.text-blue { color: #2563eb; }
            &.text-green { color: #059669; }
            &.text-cyan { color: #0284c7; }
            &.text-amber { color: #d97706; }
            &.empty-text { color: #94a3b8; font-size: 1.25rem; font-weight: 700; }
          }

          .kpi-sub {
            font-size: 0.8125rem;
            color: #64748b;
          }
        }

        .kpi-action-link {
          display: inline-flex;
          align-items: center;
          gap: 4px;
          font-size: 0.8125rem;
          font-weight: 700;
          color: #2563eb;
          text-decoration: none;
          padding-top: 10px;
          border-top: 1px solid var(--border-light, #f1f5f9);

          .material-symbols-outlined { font-size: 16px; }

          &:hover {
            color: #1d4ed8;
            text-decoration: underline;
          }
        }
      }
    }

    .spotlight-banner {
      background: #eff6ff;
      border: 1px solid #bfdbfe;
      border-radius: var(--radius-lg, 16px);
      padding: 24px 28px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 20px;
      flex-wrap: wrap;

      .spotlight-left {
        display: flex;
        flex-direction: column;
        gap: 8px;

        .status-indicator-badge {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          background: #dbeafe;
          color: #1e40af;
          font-size: 0.6875rem;
          font-weight: 800;
          letter-spacing: 0.08em;
          padding: 3px 10px;
          border-radius: 9999px;
          width: fit-content;

          .indicator-dot {
            width: 6px;
            height: 6px;
            border-radius: 50%;
            background: #2563eb;
          }
        }

        h3 {
          margin: 0;
          font-size: 1.25rem;
          font-weight: 800;
          color: #0f172a;
        }

        .meta-pills {
          display: flex;
          align-items: center;
          gap: 12px;
          flex-wrap: wrap;

          .meta-pill {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            font-size: 0.8125rem;
            color: #475569;
            font-weight: 600;

            .material-symbols-outlined { font-size: 18px; color: #2563eb; }
          }
        }
      }

      .fastpass-btn {
        height: 44px;
        font-weight: 700;
        border-radius: 10px;
        display: flex;
        align-items: center;
        gap: 8px;
      }
    }

    .facilities-section, .history-section {
      display: flex;
      flex-direction: column;
      gap: 16px;

      .section-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;

        h3 {
          margin: 0 0 2px 0;
          font-size: 1.25rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.01em;
        }

        p {
          margin: 0;
          color: #64748b;
          font-size: 0.8125rem;
        }

        .view-all-link {
          display: inline-flex;
          align-items: center;
          gap: 4px;
          color: #2563eb;
          font-weight: 700;
          font-size: 0.875rem;
          text-decoration: none;

          .material-symbols-outlined { font-size: 18px; }
          &:hover { text-decoration: underline; }
        }
      }
    }

    .facilities-cards-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 16px;

      .facility-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-lg, 16px);
        padding: 20px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        box-shadow: 0 2px 6px rgba(15, 23, 42, 0.04);

        .f-card-top {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: 12px;

          .f-info {
            h4 { margin: 0 0 4px 0; font-size: 1.05rem; font-weight: 700; color: #0f172a; }
            .f-location { font-size: 0.75rem; color: #64748b; }
          }

          .f-rate-box {
            display: flex;
            align-items: baseline;
            gap: 2px;
            .f-rate { font-size: 1.15rem; font-weight: 800; color: #2563eb; }
            .f-unit { font-size: 0.6875rem; color: #64748b; }
          }
        }

        .f-meter-bar {
          height: 6px;
          background: #f1f5f9;
          border-radius: 9999px;
          overflow: hidden;
          margin-bottom: 14px;

          .f-meter-fill {
            height: 100%;
            background: linear-gradient(90deg, #10b981, #2563eb);
            border-radius: 9999px;
          }
        }

        .f-card-bottom {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding-top: 10px;
          border-top: 1px solid var(--border-light, #f1f5f9);

          .f-slots-count { font-size: 0.8125rem; color: #475569; }
          .quick-reserve-btn { border-radius: 8px; font-weight: 700; height: 36px; }
        }
      }
    }

    .bay-tag {
      font-weight: 700;
      color: #2563eb;
    }

    .plate-mini {
      font-family: 'Courier New', monospace;
      font-weight: 700;
      color: #0f172a;
      background: #f1f5f9;
      padding: 2px 6px;
      border-radius: 4px;
      font-size: 0.8125rem;
    }

    .date-txt {
      font-size: 0.8125rem;
      color: #64748b;
    }

    .active-pulse-txt {
      color: #10b981;
      font-weight: 700;
      font-size: 0.8125rem;
    }
  `]
})
export class CustomerDashboardComponent implements OnInit {
  auth = inject(AuthService);
  private dashboardService = inject(DashboardService);
  private parkingAreaService = inject(ParkingAreaService);
  private notification = inject(NotificationService);

  dashboardData?: CustomerDashboardDto;
  parkingAreas: ParkingAreaDto[] = [];
  recentHistory: any[] = [];
  loading: boolean = false;

  historyColumns: TableColumn[] = [
    { key: 'slotNumber', label: 'Bay Ref', sortable: true, width: '120px' },
    { key: 'parkingAreaName', label: 'Parking Facility', sortable: true },
    { key: 'licensePlate', label: 'License Plate', sortable: true, width: '130px' },
    { key: 'checkInTime', label: 'Check-In', sortable: true, width: '160px' },
    { key: 'checkOutTime', label: 'Check-Out', sortable: true, width: '160px' },
    { key: 'duration', label: 'Duration', width: '120px' },
    { key: 'status', label: 'Session Status', width: '130px' }
  ];

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.dashboardService.getCustomerDashboard().subscribe({
      next: (res) => {
        this.dashboardData = res.data;
        this.recentHistory = res.data?.recentParkingHistory || [];
        this.loadParkingAreas();
      },
      error: (err) => {
        this.loading = false;
        this.notification.error('Failed to load dashboard: ' + (err.error?.message || err.message));
      }
    });
  }

  loadParkingAreas(): void {
    this.parkingAreaService.getAll().subscribe({
      next: (res) => {
        this.loading = false;
        this.parkingAreas = res.data || [];
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  get upcomingBooking(): ReservationDto | null | undefined {
    return this.dashboardData?.upcomingReservation;
  }

  get activeParking(): ReservationDto | null | undefined {
    return this.dashboardData?.activeReservation;
  }

  get featuredBooking(): ReservationDto | null | undefined {
    return this.activeParking || this.upcomingBooking;
  }

  get totalAvailableSlots(): number {
    return this.parkingAreas.reduce((sum, a) => sum + (a.availableSlots || 0), 0);
  }

  get totalReservationsCount(): number {
    return this.dashboardData?.totalReservations || 0;
  }

  get completedCount(): number {
    return this.dashboardData?.completedReservations || 0;
  }

  get totalSpent(): number {
    return this.dashboardData?.totalPayments || this.dashboardData?.totalSpent || 0;
  }

  getOccupancyPercent(area: ParkingAreaDto): number {
    if (!area.totalSlots) return 0;
    const occupied = area.totalSlots - area.availableSlots;
    return Math.min(100, Math.round((occupied / area.totalSlots) * 100));
  }
}
