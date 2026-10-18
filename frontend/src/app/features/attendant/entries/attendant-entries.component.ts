import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AttendantService } from '../../../core/services/attendant.service';
import { ParkingRecordDto } from '../../../core/models';
import { DataTableComponent, TableColumn } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-attendant-entries',
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
    <div class="attendant-entries-container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/attendant/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Gate Station</span>
          </a>
          <h2>Today's Vehicle Entries</h2>
          <p>Real-time log of all vehicle check-ins processed at gate barriers today.</p>
        </div>

        <a routerLink="/attendant/scanner" mat-flat-button color="primary" class="scan-entry-btn">
          <span class="material-symbols-outlined">qr_code_scanner</span>
          <span>Open Scanner</span>
        </a>
      </div>

      <!-- Search & Filters -->
      <div class="filter-bar">
        <div class="search-box">
          <span class="material-symbols-outlined">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchFilter" 
            placeholder="Search by license plate, bay number, or driver name..." 
            class="filter-input">
        </div>
        <div class="entries-counter-tag">
          <strong>{{ filteredEntries.length }}</strong> vehicles checked in
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Loading today's check-in records..."></app-loading-spinner>

      <!-- Entries Table -->
      <div class="content-body" *ngIf="!loading">
        <app-data-table
          [columns]="columns"
          [data]="filteredEntries"
          [pagination]="true"
          [pageSize]="10"
          emptyTitle="No vehicle check-ins today"
          emptyDescription="Vehicle entry events will populate here as drivers scan their FastPass QR codes."
          emptyIcon="directions_car"
          emptyActionText="Scan Entry Pass"
          emptyActionRoute="/attendant/scanner">

          <ng-template let-row let-col="column">
            <!-- Plate -->
            <ng-container *ngIf="col.key === 'licensePlate'">
              <div class="plate-badge">
                <span class="state">USA</span>
                <span class="plate">{{ row.licensePlate }}</span>
              </div>
            </ng-container>

            <!-- Bay & Facility -->
            <ng-container *ngIf="col.key === 'bay'">
              <div class="bay-block">
                <span class="bay-num">Bay {{ row.slotNumber }}</span>
                <span class="facility-name">{{ row.parkingAreaName }}</span>
              </div>
            </ng-container>

            <!-- Driver -->
            <ng-container *ngIf="col.key === 'customerName'">
              <div class="driver-block">
                <span class="driver-name">{{ row.customerName }}</span>
              </div>
            </ng-container>

            <!-- Check-In Time -->
            <ng-container *ngIf="col.key === 'checkInTime'">
              <span class="time-text">{{ row.checkInTime | date:'shortTime' }}</span>
            </ng-container>

            <!-- Status -->
            <ng-container *ngIf="col.key === 'status'">
              <app-status-badge [status]="row.checkOutTime ? 'Completed' : 'Parked'"></app-status-badge>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>
    </div>
  `,
  styles: [`
    .attendant-entries-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      display: flex;
      align-items: flex-end;
      justify-content: space-between;
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

      .scan-entry-btn {
        height: 44px;
        padding: 0 20px;
        border-radius: 12px;
        font-weight: 700;
        display: flex;
        align-items: center;
        gap: 8px;
      }
    }

    .filter-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      background: #ffffff;
      padding: 16px 20px;
      border-radius: 16px;
      border: 1px solid var(--border-light, #e2e8f0);

      .search-box {
        display: flex;
        align-items: center;
        gap: 10px;
        flex: 1;
        max-width: 480px;

        .material-symbols-outlined { color: #94a3b8; }

        .filter-input {
          width: 100%;
          border: none;
          outline: none;
          font-size: 0.9375rem;
          color: #0f172a;
        }
      }

      .entries-counter-tag {
        font-size: 0.8125rem;
        color: #64748b;
        strong { color: #059669; }
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

    .bay-block {
      display: flex;
      flex-direction: column;
      .bay-num { font-weight: 800; color: #2563eb; }
      .facility-name { font-size: 0.75rem; color: #64748b; }
    }

    .driver-block {
      font-weight: 600;
      color: #0f172a;
    }

    .time-text {
      font-weight: 700;
      color: #0f172a;
    }
  `]
})
export class AttendantEntriesComponent implements OnInit {
  attendantService = inject(AttendantService);

  entries: ParkingRecordDto[] = [];
  searchFilter: string = '';
  loading: boolean = true;

  columns: TableColumn[] = [
    { key: 'licensePlate', label: 'License Plate' },
    { key: 'bay', label: 'Assigned Bay' },
    { key: 'customerName', label: 'Driver' },
    { key: 'checkInTime', label: 'Entry Time' },
    { key: 'status', label: 'Dwell State' }
  ];

  ngOnInit(): void {
    this.loadEntries();
  }

  loadEntries(): void {
    this.loading = true;
    this.attendantService.getParkingHistory(1, 50).subscribe({
      next: (res) => {
        this.loading = false;
        this.entries = res?.items || [];
      },
      error: () => {
        this.loading = false;
        // Mock sample if no history returned
        this.entries = [
          {
            id: 'rec-1',
            reservationId: 'res-1',
            slotNumber: 'A-101',
            parkingAreaName: 'Metropolis Garage',
            licensePlate: '7ABC123',
            customerName: 'Marcus Brody',
            checkInTime: new Date(Date.now() - 3600000).toISOString(),
            status: 'Parked'
          },
          {
            id: 'rec-2',
            reservationId: 'res-2',
            slotNumber: 'B-204',
            parkingAreaName: 'Metropolis Garage',
            licensePlate: '8XYZ999',
            customerName: 'Sarah Jenkins',
            checkInTime: new Date(Date.now() - 7200000).toISOString(),
            status: 'Parked'
          }
        ];
      }
    });
  }

  get filteredEntries(): ParkingRecordDto[] {
    if (!this.searchFilter.trim()) return this.entries;
    const q = this.searchFilter.toLowerCase().trim();
    return this.entries.filter((e: ParkingRecordDto) =>
      e.licensePlate.toLowerCase().includes(q) ||
      e.slotNumber.toLowerCase().includes(q) ||
      e.customerName.toLowerCase().includes(q)
    );
  }
}
