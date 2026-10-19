import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ReservationService } from '../../../core/services/reservation.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ReservationDto } from '../../../core/models';
import { DataTableComponent, TableColumn } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmationDialogComponent } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'app-reservation-list',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    MatButtonModule, 
    MatDialogModule,
    DataTableComponent, 
    LoadingSpinnerComponent, 
    EmptyStateComponent, 
    StatusBadgeComponent
  ],
  template: `
    <div class="reservations-page-container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <h2>Parking Reservations & Tickets</h2>
          <p>Track your active access passes, scheduled parking intervals, and contactless QR tickets.</p>
        </div>
        <a routerLink="/reservations/create" mat-flat-button color="primary" class="new-booking-btn">
          <span class="material-symbols-outlined">add_task</span>
          <span>New Reservation</span>
        </a>
      </div>

      <!-- Status Filter Tabs -->
      <div class="status-tabs-bar">
        <button 
          *ngFor="let tab of filterTabs" 
          class="status-tab" 
          [class.active]="selectedTab === tab"
          (click)="selectTab(tab)">
          <span>{{ tab }}</span>
          <span class="tab-badge" *ngIf="getTabCount(tab) > 0">{{ getTabCount(tab) }}</span>
        </button>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Loading your parking reservations..."></app-loading-spinner>

      <!-- Content Table -->
      <div class="content-body" *ngIf="!loading">
        <app-data-table
          [columns]="columns"
          [data]="filteredReservations"
          [pagination]="true"
          [pageSize]="10"
          emptyTitle="No reservations found"
          emptyDescription="You don't have any parking reservations matching the selected filter."
          emptyIcon="confirmation_number"
          emptyActionText="Book a Parking Bay"
          emptyActionRoute="/reservations/create">

          <ng-template let-row let-col="column">
            <!-- Ticket Reference -->
            <ng-container *ngIf="col.key === 'id'">
              <div class="ticket-cell">
                <span class="ticket-prefix">REF</span>
                <span class="ticket-code">{{ row.id.slice(0, 8).toUpperCase() }}</span>
              </div>
            </ng-container>

            <!-- Bay & Facility -->
            <ng-container *ngIf="col.key === 'parkingInfo'">
              <div class="parking-cell">
                <span class="area-name">{{ row.parkingAreaName || 'Main Facility' }}</span>
                <span class="bay-pill">Bay {{ row.slotNumber || 'Assigned' }}</span>
              </div>
            </ng-container>

            <!-- Vehicle Plate -->
            <ng-container *ngIf="col.key === 'vehiclePlate'">
              <span class="plate-code">{{ row.vehiclePlate || 'Vehicle Plate' }}</span>
            </ng-container>

            <!-- Schedule Window -->
            <ng-container *ngIf="col.key === 'schedule'">
              <div class="schedule-cell">
                <span class="time-range">{{ row.startTime | date:'shortTime' }} - {{ row.endTime | date:'shortTime' }}</span>
                <span class="date-label">{{ row.startTime | date:'EEE, MMM d, y' }}</span>
              </div>
            </ng-container>

            <!-- Total Amount -->
            <ng-container *ngIf="col.key === 'totalAmount'">
              <div class="amount-cell">
                <span class="price-val">Birr {{ row.totalAmount | number:'1.2-2' }}</span>
                <span class="pay-status" [class.paid]="row.payment?.status === 'Completed'">
                  {{ row.payment?.status === 'Completed' ? 'Paid' : 'Unpaid' }}
                </span>
              </div>
            </ng-container>

            <!-- Status Badge -->
            <ng-container *ngIf="col.key === 'status'">
              <app-status-badge [status]="row.status"></app-status-badge>
            </ng-container>

            <!-- Action Buttons -->
            <ng-container *ngIf="col.key === 'actions'">
              <div class="row-actions">
                <!-- QR Code FastPass Button -->
                <button 
                  class="action-icon-btn pass-btn" 
                  (click)="viewPass(row)" 
                  title="View Digital Pass">
                  <span class="material-symbols-outlined">qr_code</span>
                </button>

                <!-- Cancel Button -->
                <button 
                  *ngIf="canCancel(row)" 
                  class="action-icon-btn cancel-btn" 
                  (click)="cancelReservation(row)" 
                  title="Cancel Reservation">
                  <span class="material-symbols-outlined">cancel</span>
                </button>
              </div>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>
    </div>
  `,
  styles: [`
    .reservations-page-container {
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

      .new-booking-btn {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        height: 42px;
        border-radius: 10px;
      }
    }

    .status-tabs-bar {
      display: flex;
      align-items: center;
      gap: 8px;
      border-bottom: 1px solid var(--border-light, #e2e8f0);
      padding-bottom: 12px;
      overflow-x: auto;

      .status-tab {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 8px 16px;
        border-radius: 9999px;
        border: 1px solid transparent;
        background: transparent;
        color: #64748b;
        font-size: 0.875rem;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.15s ease;

        &:hover {
          background: #f1f5f9;
          color: #0f172a;
        }

        &.active {
          background: #eff6ff;
          color: #2563eb;
          border-color: #bfdbfe;

          .tab-badge {
            background: #2563eb;
            color: #ffffff;
          }
        }

        .tab-badge {
          background: #e2e8f0;
          color: #475569;
          font-size: 0.6875rem;
          font-weight: 700;
          padding: 2px 8px;
          border-radius: 9999px;
        }
      }
    }

    .ticket-cell {
      display: inline-flex;
      align-items: center;
      gap: 4px;

      .ticket-prefix {
        font-size: 0.6875rem;
        font-weight: 700;
        color: #94a3b8;
      }

      .ticket-code {
        font-family: 'Courier New', monospace;
        font-weight: 700;
        color: #0f172a;
        background: #f1f5f9;
        padding: 2px 6px;
        border-radius: 4px;
      }
    }

    .parking-cell {
      display: flex;
      flex-direction: column;
      gap: 2px;

      .area-name {
        font-weight: 600;
        color: #0f172a;
      }

      .bay-pill {
        font-size: 0.75rem;
        font-weight: 700;
        color: #2563eb;
      }
    }

    .plate-code {
      font-family: 'Courier New', monospace;
      font-weight: 700;
      color: #0f172a;
      background: #f8fafc;
      padding: 3px 8px;
      border: 1px solid #cbd5e1;
      border-radius: 4px;
    }

    .schedule-cell {
      display: flex;
      flex-direction: column;

      .time-range {
        font-weight: 600;
        color: #0f172a;
      }

      .date-label {
        font-size: 0.75rem;
        color: #64748b;
      }
    }

    .amount-cell {
      display: flex;
      flex-direction: column;

      .price-val {
        font-weight: 700;
        color: #0f172a;
      }

      .pay-status {
        font-size: 0.6875rem;
        font-weight: 700;
        color: #d97706;

        &.paid {
          color: #059669;
        }
      }
    }

    .row-actions {
      display: flex;
      align-items: center;
      gap: 6px;

      .action-icon-btn {
        width: 32px;
        height: 32px;
        border-radius: 6px;
        border: none;
        background: transparent;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: all 0.15s ease;

        &.pass-btn {
          color: #2563eb;
          &:hover {
            background: #eff6ff;
          }
        }

        &.cancel-btn {
          color: #dc2626;
          &:hover {
            background: #fef2f2;
          }
        }

        .material-symbols-outlined {
          font-size: 20px;
        }
      }
    }
  `]
})
export class ReservationListComponent implements OnInit {
  private reservationService = inject(ReservationService);
  private notification = inject(NotificationService);
  private dialog = inject(MatDialog);
  private router = inject(Router);

  reservations: ReservationDto[] = [];
  loading: boolean = false;
  selectedTab: string = 'All';

  filterTabs = ['All', 'Active', 'Confirmed', 'Pending', 'Completed', 'Cancelled'];

  columns: TableColumn[] = [
    { key: 'id', label: 'Ticket Ref', sortable: true, width: '130px' },
    { key: 'parkingInfo', label: 'Facility & Bay', sortable: true },
    { key: 'vehiclePlate', label: 'Vehicle Plate', sortable: true, width: '140px' },
    { key: 'schedule', label: 'Time Window', sortable: true, width: '200px' },
    { key: 'totalAmount', label: 'Total Due', sortable: true, width: '120px' },
    { key: 'status', label: 'Status', sortable: true, width: '130px' },
    { key: 'actions', label: 'Actions', align: 'center', width: '90px' }
  ];

  ngOnInit(): void {
    this.loadReservations();
  }

  loadReservations(): void {
    this.loading = true;
    this.reservationService.getMyReservations().subscribe({
      next: (res) => {
        this.loading = false;
        this.reservations = res.data || [];
      },
      error: (err) => {
        this.loading = false;
        this.notification.error('Failed to load reservations: ' + (err.error?.message || err.message));
      }
    });
  }

  get filteredReservations(): ReservationDto[] {
    if (this.selectedTab === 'All') return this.reservations;
    return this.reservations.filter(r => 
      (r.status || '').toLowerCase() === this.selectedTab.toLowerCase()
    );
  }

  getTabCount(tab: string): number {
    if (tab === 'All') return this.reservations.length;
    return this.reservations.filter(r => 
      (r.status || '').toLowerCase() === tab.toLowerCase()
    ).length;
  }

  selectTab(tab: string): void {
    this.selectedTab = tab;
  }

  canCancel(reservation: ReservationDto): boolean {
    const s = (reservation.status || '').toLowerCase();
    return s === 'pending' || s === 'confirmed';
  }

  viewPass(reservation: ReservationDto): void {
    this.router.navigate(['/qr-code'], { queryParams: { id: reservation.id } });
  }

  cancelReservation(reservation: ReservationDto): void {
    const ref = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Cancel Reservation',
        message: `Are you sure you want to cancel reservation ${reservation.id.slice(0, 8).toUpperCase()} for slot ${reservation.slotNumber}?`,
        confirmText: 'Yes, Cancel Pass',
        cancelText: 'Keep Reservation',
        type: 'danger'
      }
    });

    ref.afterClosed().subscribe((confirmed) => {
      if (confirmed) {
        this.reservationService.cancel(reservation.id).subscribe({
          next: () => {
            this.notification.success('Reservation cancelled successfully.');
            this.loadReservations();
          },
          error: (err) => {
            this.notification.error('Failed to cancel reservation: ' + (err.error?.message || err.message));
          }
        });
      }
    });
  }
}
