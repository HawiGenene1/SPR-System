import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ParkingService } from '../../../core/services/parking.service';
import { ParkingRecordDto } from '../../../core/models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-attendant-history',
  standalone: true,
  imports: [CommonModule, FormsModule, MatProgressSpinnerModule, StatusBadgeComponent],
  template: `
    <div class="history-page">
      <div class="page-header">
        <div>
          <h2>Gate Access & Parking Logs</h2>
          <p>Auditable historical log of vehicle admissions, departures, and overstays.</p>
        </div>
        <button (click)="loadHistory()" class="pp-btn secondary sm">
          <span class="material-symbols-outlined">refresh</span>
          Refresh
        </button>
      </div>

      <!-- Search & Filters -->
      <div class="filter-card pp-card">
        <div class="search-input-wrap">
          <span class="material-symbols-outlined icon">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchQuery" 
            placeholder="Search by license plate, customer, slot number..." 
            class="search-input" />
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="loading-state">
        <mat-spinner diameter="36"></mat-spinner>
        <span>Loading parking audit records...</span>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && filteredRecords.length === 0" class="pp-empty-state">
        <div class="empty-icon">
          <span class="material-symbols-outlined">folder_open</span>
        </div>
        <h4>No parking records found</h4>
        <p>No activity records match the current search query.</p>
      </div>

      <!-- Data Table -->
      <div *ngIf="!isLoading && filteredRecords.length > 0" class="pp-card table-card">
        <div class="table-responsive">
          <table class="modern-table">
            <thead>
              <tr>
                <th>Vehicle Plate</th>
                <th>Customer</th>
                <th>Slot</th>
                <th>Facility Area</th>
                <th>Check-In</th>
                <th>Check-Out</th>
                <th>Duration</th>
                <th>Overstay Fee</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let rec of filteredRecords">
                <td>
                  <span class="plate-badge">{{ rec.licensePlate }}</span>
                </td>
                <td>{{ rec.customerName || 'Customer' }}</td>
                <td>
                  <span class="slot-badge">{{ rec.slotNumber }}</span>
                </td>
                <td>{{ rec.parkingAreaName }}</td>
                <td>{{ rec.checkInTime | date:'short' }}</td>
                <td>{{ rec.checkOutTime ? (rec.checkOutTime | date:'short') : '—' }}</td>
                <td>{{ rec.durationMinutes != null ? (rec.durationMinutes + ' min') : 'In Lot' }}</td>
                <td>
                  <span [class.danger-fee]="rec.overstayFee && rec.overstayFee > 0">
                    Birr {{ (rec.overstayFee || 0) | number:'1.2-2' }}
                  </span>
                </td>
                <td>
                  <app-status-badge [status]="rec.status"></app-status-badge>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .history-page {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
      flex-wrap: wrap;

      h2 { margin: 0 0 4px 0; font-size: 1.75rem; }
      p { margin: 0; color: #64748b; font-size: 0.9375rem; }
    }

    .filter-card {
      padding: 12px 18px;

      .search-input-wrap {
        position: relative;
        width: 100%;
        max-width: 450px;

        .icon {
          position: absolute;
          left: 10px;
          top: 50%;
          transform: translateY(-50%);
          color: #94a3b8;
          font-size: 18px;
        }

        .search-input {
          width: 100%;
          padding: 9px 12px 9px 36px;
          border-radius: 8px;
          border: 1px solid #cbd5e1;
          font-family: inherit;
          font-size: 0.875rem;
          background: #ffffff;
          &:focus { outline: none; border-color: #2563eb; }
        }
      }
    }

    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 60px 0;
      color: #64748b;
    }

    .table-card {
      padding: 0;
      overflow: hidden;

      .table-responsive {
        overflow-x: auto;
      }

      .modern-table {
        width: 100%;
        border-collapse: collapse;

        th {
          text-align: left;
          padding: 14px 18px;
          font-size: 0.75rem;
          font-weight: 700;
          color: #64748b;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          border-bottom: 1px solid var(--border-light);
          background: #f8fafc;
        }

        td {
          padding: 14px 18px;
          font-size: 0.875rem;
          color: #334155;
          border-bottom: 1px solid var(--border-light);
        }

        tr:last-child td {
          border-bottom: none;
        }

        tr:hover td {
          background: #f8fafc;
        }
      }

      .plate-badge {
        font-family: 'JetBrains Mono', monospace;
        font-weight: 800;
        background: #f1f5f9;
        border: 1px solid #cbd5e1;
        padding: 3px 8px;
        border-radius: 4px;
        font-size: 0.8125rem;
        color: #0f172a;
      }

      .slot-badge {
        font-weight: 800;
        color: #1d4ed8;
        background: #eff6ff;
        padding: 2px 8px;
        border-radius: 6px;
      }

      .danger-fee {
        color: #dc2626;
        font-weight: 700;
      }
    }
  `]
})
export class AttendantHistoryComponent implements OnInit {
  private parkingService = inject(ParkingService);

  isLoading = true;
  records: ParkingRecordDto[] = [];
  searchQuery = '';

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.isLoading = true;
    this.parkingService.getHistory().subscribe({
      next: (res) => {
        this.records = res.data || [];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  get filteredRecords(): ParkingRecordDto[] {
    if (!this.searchQuery.trim()) return this.records;
    const q = this.searchQuery.toLowerCase();
    return this.records.filter(r =>
      (r.licensePlate || '').toLowerCase().includes(q) ||
      (r.customerName || '').toLowerCase().includes(q) ||
      (r.slotNumber || '').toLowerCase().includes(q) ||
      (r.parkingAreaName || '').toLowerCase().includes(q)
    );
  }
}
