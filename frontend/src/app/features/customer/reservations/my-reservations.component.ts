import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ReservationService } from '../../../core/services/reservation.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ReservationDto } from '../../../core/models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { QrTicketModalComponent } from './qr-ticket-modal.component';

@Component({
  selector: 'app-my-reservations',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    StatusBadgeComponent
  ],
  template: `
    <div class="reservations-page">
      <div class="page-header">
        <div>
          <h2>My Parking Reservations</h2>
          <p>Manage your booked spaces, access QR gate passes, and review parking history.</p>
        </div>
        <a routerLink="/customer/book" class="pp-btn primary">
          <span class="material-symbols-outlined">add</span>
          New Reservation
        </a>
      </div>

      <!-- Filter Controls Bar -->
      <div class="filter-bar pp-card">
        <div class="tab-filters">
          <button 
            class="filter-tab" 
            [class.active]="activeTab === 'all'" 
            (click)="setTab('all')">
            All Passes
          </button>
          <button 
            class="filter-tab" 
            [class.active]="activeTab === 'active'" 
            (click)="setTab('active')">
            Active & Upcoming
          </button>
          <button 
            class="filter-tab" 
            [class.active]="activeTab === 'completed'" 
            (click)="setTab('completed')">
            Completed
          </button>
          <button 
            class="filter-tab" 
            [class.active]="activeTab === 'cancelled'" 
            (click)="setTab('cancelled')">
            Cancelled
          </button>
        </div>

        <div class="search-input-wrap">
          <span class="material-symbols-outlined search-icon">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchQuery" 
            placeholder="Search by slot, plate, area..." 
            class="search-input" />
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="loading-state">
        <mat-spinner diameter="36"></mat-spinner>
        <span>Retrieving your reservations...</span>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && filteredReservations.length === 0" class="pp-empty-state">
        <div class="empty-icon">
          <span class="material-symbols-outlined">confirmation_number</span>
        </div>
        <h4>No reservations found</h4>
        <p>No matching reservations were found for the selected filter.</p>
        <a routerLink="/customer/book" class="pp-btn primary">Book a Spot Now</a>
      </div>

      <!-- Reservations List -->
      <div *ngIf="!isLoading && filteredReservations.length > 0" class="reservations-list">
        <div *ngFor="let res of filteredReservations" class="reservation-card pp-card interactive">
          <div class="card-left">
            <div class="slot-display">
              <span class="slot-tag">SLOT</span>
              <span class="slot-number">{{ res.slotNumber || 'P1' }}</span>
            </div>
            <div class="reservation-info">
              <div class="title-row">
                <h4>{{ res.parkingAreaName || 'Main Parking Facility' }}</h4>
                <app-status-badge [status]="res.status"></app-status-badge>
              </div>

              <div class="meta-grid">
                <div class="meta-item">
                  <span class="material-symbols-outlined">directions_car</span>
                  <span>Vehicle: <strong>{{ res.vehiclePlate || 'N/A' }}</strong></span>
                </div>
                <div class="meta-item">
                  <span class="material-symbols-outlined">calendar_today</span>
                  <span>Arrival: <strong>{{ res.startTime | date:'MMM d, y, h:mm a' }}</strong></span>
                </div>
                <div class="meta-item">
                  <span class="material-symbols-outlined">timer</span>
                  <span>Departure: <strong>{{ res.endTime | date:'MMM d, y, h:mm a' }}</strong></span>
                </div>
                <div class="meta-item">
                  <span class="material-symbols-outlined">payments</span>
                  <span>Amount: <strong class="price-val">Birr {{ res.totalAmount | number:'1.2-2' }}</strong></span>
                </div>
              </div>
            </div>
          </div>

          <div class="card-actions">
            <!-- Open QR Pass Modal -->
            <button (click)="openQrModal(res)" class="pp-btn primary sm">
              <span class="material-symbols-outlined">qr_code</span>
              View QR Pass
            </button>

            <!-- Cancel Button if Active/Confirmed/Pending -->
            <button 
              *ngIf="canCancel(res)" 
              (click)="cancelReservation(res)" 
              class="pp-btn secondary sm cancel-btn">
              <span class="material-symbols-outlined">cancel</span>
              Cancel Pass
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .reservations-page {
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

    .filter-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 20px;
      padding: 14px 20px;
      flex-wrap: wrap;

      .tab-filters {
        display: flex;
        gap: 8px;
        background: #f1f5f9;
        padding: 4px;
        border-radius: 10px;

        .filter-tab {
          border: none;
          background: transparent;
          padding: 6px 14px;
          border-radius: 7px;
          font-size: 0.8125rem;
          font-weight: 600;
          color: #64748b;
          cursor: pointer;
          transition: all 0.15s ease;

          &.active {
            background: #ffffff;
            color: #1e3a8a;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
          }
        }
      }

      .search-input-wrap {
        position: relative;
        min-width: 260px;

        .search-icon {
          position: absolute;
          left: 10px;
          top: 50%;
          transform: translateY(-50%);
          color: #94a3b8;
          font-size: 18px;
        }

        .search-input {
          width: 100%;
          padding: 8px 12px 8px 34px;
          border-radius: 8px;
          border: 1px solid #cbd5e1;
          font-size: 0.8125rem;
          font-family: inherit;
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

    .reservations-list {
      display: flex;
      flex-direction: column;
      gap: 16px;

      .reservation-card {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 24px;
        padding: 20px 24px;

        .card-left {
          display: flex;
          align-items: center;
          gap: 20px;
          flex: 1;

          .slot-display {
            background: #eff6ff;
            border: 1px solid #bfdbfe;
            border-radius: 12px;
            padding: 10px 16px;
            text-align: center;
            min-width: 80px;

            .slot-tag {
              display: block;
              font-size: 0.625rem;
              font-weight: 800;
              color: #2563eb;
              letter-spacing: 0.05em;
            }

            .slot-number {
              display: block;
              font-size: 1.35rem;
              font-weight: 800;
              color: #1e3a8a;
            }
          }

          .reservation-info {
            flex: 1;

            .title-row {
              display: flex;
              align-items: center;
              gap: 12px;
              margin-bottom: 8px;

              h4 {
                margin: 0;
                font-size: 1.1rem;
                color: #0f172a;
              }
            }

            .meta-grid {
              display: flex;
              align-items: center;
              gap: 20px;
              flex-wrap: wrap;

              .meta-item {
                display: flex;
                align-items: center;
                gap: 6px;
                font-size: 0.8125rem;
                color: #475569;

                .material-symbols-outlined { font-size: 16px; color: #64748b; }
                .price-val { color: #059669; }
              }
            }
          }
        }

        .card-actions {
          display: flex;
          align-items: center;
          gap: 10px;
          flex-shrink: 0;

          .cancel-btn {
            color: #dc2626;
            &:hover { background: #fef2f2; border-color: #fca5a5; }
          }
        }
      }
    }

    @media (max-width: 900px) {
      .reservation-card {
        flex-direction: column;
        align-items: flex-start !important;
      }
      .card-left {
        width: 100%;
        flex-direction: column;
        align-items: flex-start !important;
      }
      .card-actions {
        width: 100%;
        justify-content: flex-end;
      }
    }
  `]
})
export class CustomerReservationsComponent implements OnInit {
  private reservationService = inject(ReservationService);
  private notify = inject(NotificationService);
  private dialog = inject(MatDialog);

  isLoading = true;
  reservations: ReservationDto[] = [];
  activeTab: 'all' | 'active' | 'completed' | 'cancelled' = 'all';
  searchQuery = '';

  ngOnInit(): void {
    this.loadReservations();
  }

  loadReservations(): void {
    this.isLoading = true;
    this.reservationService.getMyReservations().subscribe({
      next: (res) => {
        this.reservations = res.data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  get filteredReservations(): ReservationDto[] {
    return this.reservations.filter(r => {
      const status = (r.status || '').toLowerCase();
      // Tab filter
      if (this.activeTab === 'active' && !['confirmed', 'active', 'pending'].includes(status)) return false;
      if (this.activeTab === 'completed' && status !== 'completed') return false;
      if (this.activeTab === 'cancelled' && status !== 'cancelled') return false;

      // Search query
      if (this.searchQuery.trim()) {
        const q = this.searchQuery.toLowerCase();
        const slot = (r.slotNumber || '').toLowerCase();
        const area = (r.parkingAreaName || '').toLowerCase();
        const plate = (r.vehiclePlate || '').toLowerCase();
        return slot.includes(q) || area.includes(q) || plate.includes(q);
      }

      return true;
    });
  }

  setTab(tab: 'all' | 'active' | 'completed' | 'cancelled'): void {
    this.activeTab = tab;
  }

  canCancel(res: ReservationDto): boolean {
    const s = (res.status || '').toLowerCase();
    return ['confirmed', 'pending'].includes(s);
  }

  openQrModal(res: ReservationDto): void {
    this.dialog.open(QrTicketModalComponent, {
      data: { reservation: res },
      width: '460px',
      panelClass: 'custom-dialog-panel'
    });
  }

  cancelReservation(res: ReservationDto): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Cancel Reservation',
        message: `Are you sure you want to cancel your reservation for Slot ${res.slotNumber}? This will release the slot.`,
        confirmText: 'Yes, Cancel Reservation',
        isDanger: true
      }
    });

    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.reservationService.cancel(res.id).subscribe({
          next: () => {
            this.notify.success('Reservation successfully cancelled.');
            this.loadReservations();
          }
        });
      }
    });
  }
}
