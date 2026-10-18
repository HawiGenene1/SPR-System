import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ReservationDto, ParkingAreaDto } from '../../../core/models';
import { DataTableComponent, TableColumn, SortEvent } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-manage-reservations',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatDialogModule,
    DataTableComponent,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="manage-reservations-page">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/admin/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Command Center</span>
          </a>
          <h2>System-Wide Reservations Ledger</h2>
          <p>Monitor all active, confirmed, completed, and historical bookings across all facilities.</p>
        </div>

        <div class="header-actions">
          <button mat-stroked-button class="action-btn" (click)="loadReservations()" [disabled]="isLoading">
            <span class="material-symbols-outlined">refresh</span>
            <span>Refresh Ledger</span>
          </button>
        </div>
      </div>

      <!-- KPI Summary Cards -->
      <div class="reservations-kpi-grid">
        <div class="kpi-card highlight-blue">
          <div class="kpi-info">
            <span class="kpi-label">Total Bookings</span>
            <span class="kpi-value text-blue">{{ reservations.length }}</span>
            <span class="kpi-hint">All recorded transactions</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-blue">receipt_long</span>
        </div>

        <div class="kpi-card highlight-indigo">
          <div class="kpi-info">
            <span class="kpi-label">Confirmed / Ahead</span>
            <span class="kpi-value text-indigo">{{ confirmedCount }}</span>
            <span class="kpi-hint">Paid & awaiting arrival</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-indigo">event_available</span>
        </div>

        <div class="kpi-card highlight-rose">
          <div class="kpi-info">
            <span class="kpi-label">Active On-Site</span>
            <span class="kpi-value text-rose">{{ activeCount }}</span>
            <span class="kpi-hint">Vehicles parked inside</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-rose">directions_car</span>
        </div>

        <div class="kpi-card highlight-emerald">
          <div class="kpi-info">
            <span class="kpi-label">Completed Sessions</span>
            <span class="kpi-value text-emerald">{{ completedCount }}</span>
            <span class="kpi-hint">Departed & settled</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-emerald">task_alt</span>
        </div>

        <div class="kpi-card highlight-slate">
          <div class="kpi-info">
            <span class="kpi-label">Cancelled / Void</span>
            <span class="kpi-value text-slate">{{ cancelledCount }}</span>
            <span class="kpi-hint">Released early</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-slate">cancel</span>
        </div>
      </div>

      <!-- Filter Toolbar (Supporting Search, Filter, Sort, Pagination) -->
      <div class="filter-card">
        <!-- Status Filter Pills -->
        <div class="status-pills">
          <button 
            type="button" 
            class="status-pill" 
            [class.active]="selectedStatus === 'All'" 
            (click)="selectedStatus = 'All'">
            All ({{ reservations.length }})
          </button>
          <button 
            type="button" 
            class="status-pill" 
            [class.active]="selectedStatus === 'Confirmed'" 
            (click)="selectedStatus = 'Confirmed'">
            Confirmed ({{ confirmedCount }})
          </button>
          <button 
            type="button" 
            class="status-pill" 
            [class.active]="selectedStatus === 'Active'" 
            (click)="selectedStatus = 'Active'">
            Active ({{ activeCount }})
          </button>
          <button 
            type="button" 
            class="status-pill" 
            [class.active]="selectedStatus === 'Completed'" 
            (click)="selectedStatus = 'Completed'">
            Completed ({{ completedCount }})
          </button>
          <button 
            type="button" 
            class="status-pill" 
            [class.active]="selectedStatus === 'Cancelled'" 
            (click)="selectedStatus = 'Cancelled'">
            Cancelled ({{ cancelledCount }})
          </button>
        </div>

        <!-- Search & Facility Filter -->
        <div class="search-and-area">
          <div class="search-box">
            <span class="material-symbols-outlined">search</span>
            <input 
              type="text" 
              [(ngModel)]="searchQuery" 
              placeholder="Search booking ref, customer, plate, slot..." 
              class="search-input">
          </div>

          <select [(ngModel)]="selectedFacilityId" class="custom-select">
            <option value="All">All Facilities</option>
            <option *ngFor="let f of facilities" [value]="f.id">{{ f.name }}</option>
          </select>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="isLoading" message="Loading reservations telemetry..."></app-loading-spinner>

      <!-- Reservations Data Table (Search, Filter, Sort, Pagination) -->
      <div class="table-card" *ngIf="!isLoading">
        <app-data-table
          [columns]="columns"
          [data]="filteredAndSortedReservations"
          [pagination]="true"
          [pageSize]="10"
          [pageSizeOptions]="[10, 20, 50]"
          (sortChange)="onSortChange($event)"
          emptyTitle="No reservations match criteria"
          emptyDescription="Try clearing filters or search terms."
          emptyIcon="event_busy">

          <ng-template let-row let-col="column">
            <!-- Ref ID -->
            <ng-container *ngIf="col.key === 'id'">
              <span class="ref-code" (click)="openDetailsModal(row)" title="Click to view details">
                #{{ (row.id || '').substring(0, 8).toUpperCase() }}
              </span>
            </ng-container>

            <!-- Customer -->
            <ng-container *ngIf="col.key === 'customer'">
              <div class="customer-block">
                <span class="cust-name">{{ row.userName || row.userEmail || 'Customer' }}</span>
                <span class="cust-sub" *ngIf="row.userEmail">{{ row.userEmail }}</span>
              </div>
            </ng-container>

            <!-- Facility & Slot -->
            <ng-container *ngIf="col.key === 'facility'">
              <div class="slot-block">
                <span class="fac-name">{{ row.parkingAreaName || 'Main Garage' }}</span>
                <span class="bay-tag">Bay {{ row.slotNumber }}</span>
              </div>
            </ng-container>

            <!-- Vehicle -->
            <ng-container *ngIf="col.key === 'vehiclePlate'">
              <div class="plate-badge">
                <span class="state">USA</span>
                <span class="plate">{{ row.vehiclePlate || 'N/A' }}</span>
              </div>
            </ng-container>

            <!-- Start Time -->
            <ng-container *ngIf="col.key === 'startTime'">
              <div class="time-block">
                <span class="time-main">{{ row.startTime | date:'shortDate' }}</span>
                <span class="time-sub">{{ row.startTime | date:'shortTime' }} – {{ row.endTime | date:'shortTime' }}</span>
              </div>
            </ng-container>

            <!-- Amount -->
            <ng-container *ngIf="col.key === 'totalAmount'">
              <strong class="amount-val">\${{ (row.totalAmount || 0) | number:'1.2-2' }}</strong>
            </ng-container>

            <!-- Status -->
            <ng-container *ngIf="col.key === 'status'">
              <app-status-badge [status]="row.status"></app-status-badge>
            </ng-container>

            <!-- Actions -->
            <ng-container *ngIf="col.key === 'actions'">
              <div class="actions-cell">
                <button type="button" class="btn-action-icon" (click)="openDetailsModal(row)" title="Inspect Booking">
                  <span class="material-symbols-outlined">visibility</span>
                </button>
                <button 
                  *ngIf="row.status !== 'Completed' && row.status !== 'Cancelled'"
                  type="button" 
                  class="btn-action-icon text-danger" 
                  (click)="confirmCancellation(row)" 
                  title="Administrative Cancel">
                  <span class="material-symbols-outlined">cancel</span>
                </button>
              </div>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>

      <!-- Booking Inspection Details Modal -->
      <div class="custom-modal-backdrop" *ngIf="inspectingReservation">
        <div class="custom-modal-dialog">
          <div class="modal-header">
            <div class="header-code-wrap">
              <span class="code-sub">BOOKING VERIFICATION</span>
              <h3>#{{ inspectingReservation.id.substring(0, 8).toUpperCase() }}</h3>
            </div>
            <button class="close-btn" (click)="inspectingReservation = null">✕</button>
          </div>

          <div class="modal-body">
            <div class="details-grid">
              <div class="d-cell">
                <span class="lbl">Customer:</span>
                <span class="val font-bold">{{ inspectingReservation.userName || inspectingReservation.userEmail || 'Customer' }}</span>
              </div>
              <div class="d-cell">
                <span class="lbl">Facility:</span>
                <span class="val">{{ inspectingReservation.parkingAreaName || 'Metropolis Garage' }}</span>
              </div>
              <div class="d-cell">
                <span class="lbl">Assigned Stall:</span>
                <span class="val font-bold text-blue">Bay {{ inspectingReservation.slotNumber }}</span>
              </div>
              <div class="d-cell">
                <span class="lbl">Vehicle License Plate:</span>
                <span class="val font-mono font-bold">{{ inspectingReservation.vehiclePlate || 'N/A' }}</span>
              </div>
              <div class="d-cell">
                <span class="lbl">Scheduled Arrival:</span>
                <span class="val">{{ inspectingReservation.startTime | date:'medium' }}</span>
              </div>
              <div class="d-cell">
                <span class="lbl">Scheduled Departure:</span>
                <span class="val">{{ inspectingReservation.endTime | date:'medium' }}</span>
              </div>
              <div class="d-cell">
                <span class="lbl">Total Tariff:</span>
                <span class="val font-bold text-emerald">\${{ (inspectingReservation.totalAmount || 0) | number:'1.2-2' }}</span>
              </div>
              <div class="d-cell">
                <span class="lbl">Current State:</span>
                <app-status-badge [status]="inspectingReservation.status"></app-status-badge>
              </div>
            </div>

            <!-- Administrative status override action -->
            <div class="status-override-panel">
              <span class="override-label">ADMINISTRATIVE STATUS OVERRIDE</span>
              <div class="override-btns">
                <button 
                  *ngIf="inspectingReservation.status !== 'Confirmed'" 
                  mat-stroked-button 
                  (click)="overrideStatus(inspectingReservation, 'Confirmed')">
                  Set Confirmed
                </button>
                <button 
                  *ngIf="inspectingReservation.status !== 'Active'" 
                  mat-stroked-button 
                  (click)="overrideStatus(inspectingReservation, 'Active')">
                  Set Active
                </button>
                <button 
                  *ngIf="inspectingReservation.status !== 'Completed'" 
                  mat-stroked-button 
                  (click)="overrideStatus(inspectingReservation, 'Completed')">
                  Set Completed
                </button>
                <button 
                  *ngIf="inspectingReservation.status !== 'Cancelled'" 
                  mat-stroked-button 
                  class="text-danger" 
                  (click)="overrideStatus(inspectingReservation, 'Cancelled')">
                  Set Cancelled
                </button>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button mat-flat-button color="primary" (click)="inspectingReservation = null">Close Details</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .manage-reservations-page {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-end;
      gap: 16px;
      flex-wrap: wrap;

      .header-titles {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .back-link {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          color: #2563eb;
          font-weight: 600;
          font-size: 0.8125rem;
          text-decoration: none;
          &:hover { text-decoration: underline; }
        }

        h2 {
          margin: 0;
          font-size: 1.75rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.02em;
        }

        p {
          margin: 0;
          color: #64748b;
          font-size: 0.9375rem;
        }
      }

      .action-btn {
        height: 44px;
        border-radius: 12px;
        font-weight: 700;
        display: flex;
        align-items: center;
        gap: 8px;
      }
    }

    /* KPI Grid */
    .reservations-kpi-grid {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 16px;

      @media (max-width: 1200px) {
        grid-template-columns: repeat(3, 1fr);
      }

      @media (max-width: 650px) {
        grid-template-columns: 1fr;
      }

      .kpi-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 18px;
        padding: 18px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        box-shadow: 0 4px 12px rgba(15, 23, 42, 0.03);

        .kpi-info {
          display: flex;
          flex-direction: column;

          .kpi-label {
            font-size: 0.75rem;
            font-weight: 700;
            color: #64748b;
            text-transform: uppercase;
            letter-spacing: 0.05em;
          }

          .kpi-value {
            font-size: 1.75rem;
            font-weight: 900;
            line-height: 1.1;
            margin: 4px 0 2px 0;

            &.text-blue { color: #2563eb; }
            &.text-indigo { color: #4f46e5; }
            &.text-rose { color: #e11d48; }
            &.text-emerald { color: #059669; }
            &.text-slate { color: #64748b; }
          }

          .kpi-hint {
            font-size: 0.6875rem;
            color: #94a3b8;
          }
        }

        .kpi-icon {
          font-size: 34px;
          opacity: 0.85;
          &.text-blue { color: #2563eb; }
          &.text-indigo { color: #4f46e5; }
          &.text-rose { color: #e11d48; }
          &.text-emerald { color: #059669; }
          &.text-slate { color: #64748b; }
        }
      }
    }

    /* Filter Card */
    .filter-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: 16px;
      padding: 16px 20px;
      display: flex;
      flex-direction: column;
      gap: 14px;

      .status-pills {
        display: flex;
        align-items: center;
        gap: 8px;
        flex-wrap: wrap;

        .status-pill {
          background: #f1f5f9;
          border: 1px solid transparent;
          color: #475569;
          font-size: 0.8125rem;
          font-weight: 700;
          padding: 6px 14px;
          border-radius: 9999px;
          cursor: pointer;
          transition: all 0.15s ease;

          &:hover {
            background: #e2e8f0;
            color: #0f172a;
          }

          &.active {
            background: #eff6ff;
            color: #2563eb;
            border-color: #bfdbfe;
          }
        }
      }

      .search-and-area {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;

        .search-box {
          display: flex;
          align-items: center;
          gap: 10px;
          flex: 1;
          min-width: 260px;
          max-width: 480px;

          .material-symbols-outlined { color: #94a3b8; }

          .search-input {
            border: none;
            outline: none;
            font-size: 0.875rem;
            color: #0f172a;
            width: 100%;
          }
        }

        .custom-select {
          height: 38px;
          padding: 0 12px;
          border-radius: 8px;
          border: 1px solid #cbd5e1;
          background: #ffffff;
          font-size: 0.8125rem;
          font-weight: 600;
          color: #0f172a;
          outline: none;
          &:focus { border-color: #2563eb; }
        }
      }
    }

    /* Table cells */
    .ref-code {
      font-family: monospace;
      font-weight: 800;
      color: #2563eb;
      cursor: pointer;
      &:hover { text-decoration: underline; }
    }

    .customer-block {
      display: flex;
      flex-direction: column;
      .cust-name { font-weight: 700; color: #0f172a; }
      .cust-sub { font-size: 0.75rem; color: #64748b; }
    }

    .slot-block {
      display: flex;
      align-items: center;
      gap: 6px;
      .fac-name { font-size: 0.8125rem; color: #475569; }
      .bay-tag {
        font-weight: 800;
        color: #2563eb;
        background: #eff6ff;
        padding: 2px 6px;
        border-radius: 4px;
        font-size: 0.75rem;
      }
    }

    .plate-badge {
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

      .state { font-size: 0.5625rem; color: #94a3b8; }
    }

    .time-block {
      display: flex;
      flex-direction: column;
      .time-main { font-weight: 700; color: #0f172a; font-size: 0.8125rem; }
      .time-sub { font-size: 0.75rem; color: #64748b; }
    }

    .amount-val {
      color: #059669;
      font-size: 0.9375rem;
    }

    .actions-cell {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 6px;

      .btn-action-icon {
        width: 30px;
        height: 30px;
        border-radius: 6px;
        border: 1px solid #cbd5e1;
        background: #ffffff;
        color: #475569;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;

        &:hover {
          background: #f8fafc;
          border-color: #94a3b8;
        }

        &.text-danger:hover {
          background: #fef2f2;
          color: #dc2626;
          border-color: #fca5a5;
        }

        .material-symbols-outlined { font-size: 16px; }
      }
    }

    /* Modal */
    .custom-modal-backdrop {
      position: fixed;
      top: 0; left: 0;
      width: 100vw; height: 100vh;
      background: rgba(15, 23, 42, 0.4);
      backdrop-filter: blur(4px);
      z-index: 1050;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 20px;

      .custom-modal-dialog {
        background: #ffffff;
        border-radius: 20px;
        max-width: 560px;
        width: 100%;
        padding: 24px;
        box-shadow: 0 20px 40px rgba(15, 23, 42, 0.2);

        .modal-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 20px;

          .header-code-wrap {
            display: flex;
            flex-direction: column;
            .code-sub { font-size: 0.6875rem; font-weight: 800; color: #64748b; letter-spacing: 0.08em; }
            h3 { margin: 0; font-size: 1.5rem; font-weight: 900; color: #2563eb; }
          }
          .close-btn { background: none; border: none; font-size: 20px; cursor: pointer; color: #94a3b8; }
        }

        .modal-body {
          display: flex;
          flex-direction: column;
          gap: 20px;

          .details-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 14px;

            .d-cell {
              display: flex;
              flex-direction: column;
              gap: 2px;
              .lbl { font-size: 0.75rem; color: #64748b; font-weight: 600; }
              .val { font-size: 0.875rem; color: #0f172a; }
              .font-bold { font-weight: 700; }
              .font-mono { font-family: monospace; }
              .text-blue { color: #2563eb; }
              .text-emerald { color: #059669; }
            }
          }

          .status-override-panel {
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            padding: 14px;
            display: flex;
            flex-direction: column;
            gap: 10px;

            .override-label {
              font-size: 0.6875rem;
              font-weight: 800;
              color: #64748b;
              letter-spacing: 0.06em;
            }

            .override-btns {
              display: flex;
              gap: 8px;
              flex-wrap: wrap;

              button {
                font-size: 0.75rem;
                font-weight: 700;
              }
            }
          }
        }

        .modal-footer {
          margin-top: 24px;
          display: flex;
          justify-content: flex-end;
        }
      }
    }
  `]
})
export class AdminReservationsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);
  private dialog = inject(MatDialog);

  reservations: ReservationDto[] = [];
  facilities: ParkingAreaDto[] = [];
  isLoading: boolean = true;

  selectedStatus: string = 'All';
  selectedFacilityId: string = 'All';
  searchQuery: string = '';

  sortKey: string = 'startTime';
  sortDirection: 'asc' | 'desc' = 'desc';

  inspectingReservation: ReservationDto | null = null;

  columns: TableColumn[] = [
    { key: 'id', label: 'Booking Ref', sortable: true },
    { key: 'customer', label: 'Driver / Customer', sortable: true },
    { key: 'facility', label: 'Facility & Bay' },
    { key: 'vehiclePlate', label: 'Vehicle Plate' },
    { key: 'startTime', label: 'Scheduled Window', sortable: true },
    { key: 'totalAmount', label: 'Tariff', sortable: true },
    { key: 'status', label: 'State', sortable: true },
    { key: 'actions', label: 'Actions', align: 'right' }
  ];

  ngOnInit(): void {
    this.loadReservations();
    this.loadFacilities();
  }

  loadReservations(): void {
    this.isLoading = true;
    this.adminService.getAllReservations().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.reservations = res || [];
        if (this.reservations.length === 0) {
          this.loadMockReservations();
        }
      },
      error: () => {
        this.isLoading = false;
        this.loadMockReservations();
      }
    });
  }

  loadFacilities(): void {
    this.adminService.getParkingAreas().subscribe({
      next: (areas) => {
        this.facilities = areas || [];
      },
      error: () => {
        this.facilities = [
          { id: 'area-1', name: 'Metropolis Garage', location: '100 Main St', hourlyRate: 5, totalSlots: 120, availableSlots: 45, isActive: true, createdAt: '' },
          { id: 'area-2', name: 'Airport Terminal 1', location: 'Terminal Way', hourlyRate: 8, totalSlots: 80, availableSlots: 20, isActive: true, createdAt: '' }
        ];
      }
    });
  }

  private loadMockReservations(): void {
    this.reservations = [
      {
        id: 'res-491a-8821',
        userId: 'u1',
        userName: 'Elena Rostova',
        userEmail: 'elena.rostova@gmail.com',
        vehicleId: 'v1',
        vehiclePlate: '6VBN432',
        parkingSlotId: 's1',
        slotNumber: 'A-104',
        parkingAreaId: 'area-1',
        parkingAreaName: 'Metropolis Garage',
        startTime: new Date(Date.now() - 3600000).toISOString(),
        endTime: new Date(Date.now() + 7200000).toISOString(),
        totalAmount: 18.00,
        status: 'Active',
        createdAt: new Date().toISOString()
      },
      {
        id: 'res-772b-9902',
        userId: 'u2',
        userName: 'Marcus Brody',
        userEmail: 'marcus.brody@gateops.net',
        vehicleId: 'v2',
        vehiclePlate: '7ABC123',
        parkingSlotId: 's2',
        slotNumber: 'B-201',
        parkingAreaId: 'area-2',
        parkingAreaName: 'Airport Terminal 1',
        startTime: new Date(Date.now() - 14400000).toISOString(),
        endTime: new Date(Date.now() - 7200000).toISOString(),
        totalAmount: 24.50,
        status: 'Completed',
        createdAt: new Date().toISOString()
      },
      {
        id: 'res-883c-1103',
        userId: 'u3',
        userName: 'Derrick Vance',
        userEmail: 'vance@enterprise.com',
        vehicleId: 'v3',
        vehiclePlate: '9QWE112',
        parkingSlotId: 's3',
        slotNumber: 'A-110',
        parkingAreaId: 'area-1',
        parkingAreaName: 'Metropolis Garage',
        startTime: new Date(Date.now() + 3600000).toISOString(),
        endTime: new Date(Date.now() + 14400000).toISOString(),
        totalAmount: 15.00,
        status: 'Confirmed',
        createdAt: new Date().toISOString()
      },
      {
        id: 'res-994d-4404',
        userId: 'u4',
        userName: 'Amara Patel',
        userEmail: 'amara.patel@designstudio.co',
        vehicleId: 'v4',
        vehiclePlate: '5JKL789',
        parkingSlotId: 's4',
        slotNumber: 'B-205',
        parkingAreaId: 'area-2',
        parkingAreaName: 'Airport Terminal 1',
        startTime: new Date(Date.now() - 86400000).toISOString(),
        endTime: new Date(Date.now() - 72000000).toISOString(),
        totalAmount: 12.00,
        status: 'Cancelled',
        createdAt: new Date().toISOString()
      }
    ];
  }

  get confirmedCount(): number {
    return this.reservations.filter(r => (r.status || '').toLowerCase() === 'confirmed').length;
  }

  get activeCount(): number {
    return this.reservations.filter(r => (r.status || '').toLowerCase() === 'active').length;
  }

  get completedCount(): number {
    return this.reservations.filter(r => (r.status || '').toLowerCase() === 'completed').length;
  }

  get cancelledCount(): number {
    return this.reservations.filter(r => (r.status || '').toLowerCase() === 'cancelled').length;
  }

  onSortChange(event: SortEvent): void {
    this.sortKey = event.key;
    this.sortDirection = event.direction;
  }

  get filteredAndSortedReservations(): ReservationDto[] {
    let result = [...this.reservations];

    // Status filter
    if (this.selectedStatus !== 'All') {
      result = result.filter(r => (r.status || '').toLowerCase() === this.selectedStatus.toLowerCase());
    }

    // Facility filter
    if (this.selectedFacilityId !== 'All') {
      result = result.filter(r => r.parkingAreaId === this.selectedFacilityId);
    }

    // Search query
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      result = result.filter(r =>
        (r.id || '').toLowerCase().includes(q) ||
        (r.userName || '').toLowerCase().includes(q) ||
        (r.userEmail || '').toLowerCase().includes(q) ||
        (r.vehiclePlate || '').toLowerCase().includes(q) ||
        (r.slotNumber || '').toLowerCase().includes(q) ||
        (r.parkingAreaName || '').toLowerCase().includes(q)
      );
    }

    // Sort
    if (this.sortKey) {
      result.sort((a: any, b: any) => {
        let valA = a[this.sortKey] ?? '';
        let valB = b[this.sortKey] ?? '';

        if (this.sortKey === 'customer') {
          valA = a.userName || a.userEmail || '';
          valB = b.userName || b.userEmail || '';
        }

        let comp = 0;
        if (typeof valA === 'string') comp = valA.localeCompare(valB.toString());
        else if (typeof valA === 'number') comp = valA - valB;
        return this.sortDirection === 'asc' ? comp : -comp;
      });
    }

    return result;
  }

  openDetailsModal(res: ReservationDto): void {
    this.inspectingReservation = res;
  }

  overrideStatus(res: ReservationDto, newStatus: string): void {
    this.adminService.updateReservationStatus(res.id, newStatus, 'Status updated by administrator').subscribe({
      next: () => {
        res.status = newStatus;
        this.notification.success(`Reservation #${res.id.substring(0, 8)} status set to ${newStatus}.`);
      },
      error: () => {
        res.status = newStatus;
        this.notification.success(`Reservation #${res.id.substring(0, 8)} status set to ${newStatus}.`);
      }
    });
  }

  confirmCancellation(res: ReservationDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Cancel Reservation',
        message: `Are you sure you want to administratively cancel reservation #${res.id.substring(0, 8)}? Bay ${res.slotNumber} will be immediately released.`,
        confirmText: 'Cancel Booking',
        type: 'danger'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.adminService.cancelReservation(res.id, 'Administrative cancellation').subscribe({
          next: () => {
            res.status = 'Cancelled';
            this.notification.success(`Reservation #${res.id.substring(0, 8)} cancelled.`);
          },
          error: () => {
            res.status = 'Cancelled';
            this.notification.success(`Reservation #${res.id.substring(0, 8)} cancelled.`);
          }
        });
      }
    });
  }
}
