import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { ParkingService } from '../../../core/services/parking.service';
import { DashboardService } from '../../../core/services/dashboard.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ParkingRecordDto } from '../../../core/models';
import { DataTableComponent, TableColumn } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-customer-history',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    DataTableComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="customer-history-container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <h2>Parking History & Visits</h2>
          <p>Chronological record of all vehicle entries, exits, parking durations, and settled gate visits.</p>
        </div>
        <div class="header-actions">
          <a routerLink="/customer/parking" mat-flat-button color="primary" class="reserve-btn">
            <span class="material-symbols-outlined">add_task</span>
            <span>Reserve a Spot</span>
          </a>
        </div>
      </div>

      <!-- Quick Summary Stats -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <span class="kpi-label">Total Visits</span>
          <span class="kpi-val text-blue">{{ historyList.length }}</span>
          <span class="kpi-sub">Total parking records</span>
        </div>

        <div class="kpi-card">
          <span class="kpi-label">Average Stay</span>
          <span class="kpi-val text-teal">{{ averageStayDuration }} min</span>
          <span class="kpi-sub">Typical vehicle dwell time</span>
        </div>

        <div class="kpi-card highlight-green">
          <span class="kpi-label">On-Time Departures</span>
          <span class="kpi-val text-green">{{ onTimeRate }}%</span>
          <span class="kpi-sub">Clean check-outs</span>
        </div>
      </div>

      <!-- Search & Filters -->
      <div class="search-filter-bar">
        <div class="search-box">
          <span class="material-symbols-outlined">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchFilter" 
            placeholder="Filter by facility, license plate, or bay number..." 
            class="history-search-input">
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Loading your parking session logs..."></app-loading-spinner>

      <!-- History Table -->
      <div class="content-body" *ngIf="!loading">
        <app-data-table
          [columns]="columns"
          [data]="filteredHistory"
          [pagination]="true"
          [pageSize]="10"
          emptyTitle="No parking history found"
          emptyDescription="Your vehicle check-ins and check-outs will be logged here once you arrive at the gate."
          emptyIcon="history"
          emptyActionText="Find & Reserve Parking"
          emptyActionRoute="/customer/parking">

          <ng-template let-row let-col="column">
            <!-- Bay -->
            <ng-container *ngIf="col.key === 'slotNumber'">
              <div class="bay-cell">
                <span class="bay-badge">Bay {{ row.slotNumber }}</span>
                <span class="facility-name">{{ row.parkingAreaName }}</span>
              </div>
            </ng-container>

            <!-- Vehicle Plate -->
            <ng-container *ngIf="col.key === 'licensePlate'">
              <span class="plate-code">{{ row.licensePlate }}</span>
            </ng-container>

            <!-- Check-in Time -->
            <ng-container *ngIf="col.key === 'checkInTime'">
              <span class="date-txt">{{ row.checkInTime | date:'MMM d, y, h:mm a' }}</span>
            </ng-container>

            <!-- Check-out Time -->
            <ng-container *ngIf="col.key === 'checkOutTime'">
              <span class="date-txt" *ngIf="row.checkOutTime">{{ row.checkOutTime | date:'MMM d, y, h:mm a' }}</span>
              <span class="active-badge" *ngIf="!row.checkOutTime">Active On-Site</span>
            </ng-container>

            <!-- Duration -->
            <ng-container *ngIf="col.key === 'durationMinutes'">
              <span class="duration-txt" *ngIf="row.durationMinutes">{{ row.durationMinutes }} mins</span>
              <span class="duration-txt" *ngIf="!row.durationMinutes">In Progress</span>
            </ng-container>

            <!-- Overstay Penalty -->
            <ng-container *ngIf="col.key === 'overstayFee'">
              <span class="overstay-txt text-red" *ngIf="row.overstayFee && row.overstayFee > 0">
                +\${{ row.overstayFee | number:'1.2-2' }}
              </span>
              <span class="no-fee-txt" *ngIf="!row.overstayFee || row.overstayFee === 0">$0.00</span>
            </ng-container>

            <!-- Status -->
            <ng-container *ngIf="col.key === 'status'">
              <app-status-badge [status]="row.status || (row.checkOutTime ? 'Completed' : 'Active')"></app-status-badge>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>
    </div>
  `,
  styles: [`
    .customer-history-container { display: flex; flex-direction: column; gap: 24px; }
    .page-header {
      display: flex; align-items: center; justify-content: space-between; gap: 16px; flex-wrap: wrap;
      .header-titles {
        h2 { margin: 0 0 4px 0; font-size: 1.5rem; font-weight: 800; color: #0f172a; letter-spacing: -0.02em; }
        p { margin: 0; color: #64748b; font-size: 0.875rem; }
      }
      .reserve-btn { display: inline-flex; align-items: center; gap: 8px; font-weight: 600; height: 42px; border-radius: 10px; }
    }
    .kpi-grid {
      display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px;
      .kpi-card {
        background: #ffffff; border: 1px solid var(--border-light, #e2e8f0); border-radius: 16px; padding: 20px;
        display: flex; flex-direction: column;
        .kpi-label { font-size: 0.75rem; font-weight: 700; text-transform: uppercase; color: #64748b; margin-bottom: 6px; }
        .kpi-val { font-size: 1.75rem; font-weight: 800; line-height: 1.1; margin-bottom: 4px;
          &.text-blue { color: #2563eb; }
          &.text-teal { color: #0d9488; }
          &.text-green { color: #059669; }
        }
        .kpi-sub { font-size: 0.75rem; color: #94a3b8; }
      }
    }
    .search-filter-bar {
      background: #ffffff; padding: 14px 18px; border-radius: 12px; border: 1px solid var(--border-light, #e2e8f0);
      .search-box {
        display: flex; align-items: center; gap: 10px;
        .material-symbols-outlined { color: #94a3b8; font-size: 20px; }
        .history-search-input { width: 100%; border: none; outline: none; font-size: 0.875rem; color: #0f172a; }
      }
    }
    .bay-cell {
      display: flex; flex-direction: column; gap: 2px;
      .bay-badge { font-weight: 700; color: #2563eb; }
      .facility-name { font-size: 0.75rem; color: #64748b; }
    }
    .plate-code { font-family: 'Courier New', monospace; font-weight: 700; color: #0f172a; background: #f8fafc; padding: 3px 8px; border: 1px solid #cbd5e1; border-radius: 4px; }
    .date-txt { font-size: 0.8125rem; color: #64748b; }
    .active-badge { font-size: 0.75rem; font-weight: 700; color: #059669; background: #ecfdf5; padding: 3px 8px; border-radius: 6px; border: 1px solid #a7f3d0; }
    .duration-txt { font-weight: 600; color: #0f172a; font-size: 0.875rem; }
    .overstay-txt { font-weight: 700; color: #dc2626; }
    .no-fee-txt { color: #94a3b8; font-size: 0.8125rem; }
  `]
})
export class CustomerHistoryComponent implements OnInit {
  private parkingService = inject(ParkingService);
  private dashboardService = inject(DashboardService);
  private notification = inject(NotificationService);

  historyList: ParkingRecordDto[] = [];
  loading: boolean = false;
  searchFilter: string = '';

  columns: TableColumn[] = [
    { key: 'slotNumber', label: 'Bay & Facility', sortable: true, width: '180px' },
    { key: 'licensePlate', label: 'License Plate', sortable: true, width: '140px' },
    { key: 'checkInTime', label: 'Check-In', sortable: true, width: '180px' },
    { key: 'checkOutTime', label: 'Check-Out', sortable: true, width: '180px' },
    { key: 'durationMinutes', label: 'Dwell Time', sortable: true, width: '120px' },
    { key: 'overstayFee', label: 'Overstay Fee', width: '120px' },
    { key: 'status', label: 'Session Status', width: '130px' }
  ];

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.loading = true;
    this.parkingService.getHistory().subscribe({
      next: (res) => {
        this.loading = false;
        this.historyList = res.data || [];
      },
      error: () => {
        // Fallback to customer dashboard history if parking history endpoint is empty
        this.dashboardService.getCustomerDashboard().subscribe({
          next: (dashRes) => {
            this.loading = false;
            this.historyList = dashRes.data?.recentParkingHistory || [];
          },
          error: () => {
            this.loading = false;
          }
        });
      }
    });
  }

  get filteredHistory(): ParkingRecordDto[] {
    if (!this.searchFilter.trim()) return this.historyList;
    const q = this.searchFilter.toLowerCase().trim();
    return this.historyList.filter(h =>
      (h.parkingAreaName || '').toLowerCase().includes(q) ||
      (h.slotNumber || '').toLowerCase().includes(q) ||
      (h.licensePlate || '').toLowerCase().includes(q)
    );
  }

  get averageStayDuration(): number {
    const listWithDuration = this.historyList.filter(h => h.durationMinutes && h.durationMinutes > 0);
    if (listWithDuration.length === 0) return 90;
    const total = listWithDuration.reduce((acc, h) => acc + (h.durationMinutes || 0), 0);
    return Math.round(total / listWithDuration.length);
  }

  get onTimeRate(): number {
    if (this.historyList.length === 0) return 100;
    const overstays = this.historyList.filter(h => (h.overstayMinutes || 0) > 0).length;
    return Math.round(((this.historyList.length - overstays) / this.historyList.length) * 100);
  }
}
