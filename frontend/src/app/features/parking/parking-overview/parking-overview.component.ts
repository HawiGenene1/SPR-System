import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { ParkingAreaService } from '../../../core/services/parking-area.service';
import { ParkingSlotService } from '../../../core/services/parking-slot.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ParkingAreaDto, ParkingSlotDto } from '../../../core/models';
import { ParkingSlotCardComponent } from '../../../shared/components/parking-slot-card/parking-slot-card.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-parking-overview',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    RouterModule,
    MatButtonModule, 
    MatSelectModule, 
    MatFormFieldModule,
    ParkingSlotCardComponent, 
    LoadingSpinnerComponent, 
    EmptyStateComponent, 
    StatusBadgeComponent
  ],
  template: `
    <div class="parking-page-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-titles">
          <h2>Parking Facilities & Live Slot Grid</h2>
          <p>Inspect real-time parking bay availability, EV charging bays, and live facility telemetry.</p>
        </div>
        <div class="header-actions">
          <a routerLink="/reservations/create" mat-flat-button color="primary" class="book-cta-btn">
            <span class="material-symbols-outlined">add_task</span>
            <span>Reserve a Bay</span>
          </a>
        </div>
      </div>

      <!-- Facility Selection Tabs -->
      <div class="facility-tabs-bar" *ngIf="areas.length > 0">
        <button 
          *ngFor="let area of areas" 
          class="facility-tab" 
          [class.active]="selectedArea?.id === area.id"
          (click)="selectArea(area)">
          <div class="tab-content">
            <span class="tab-name">{{ area.name }}</span>
            <div class="tab-metrics">
              <span class="rate-tag">Birr {{ area.hourlyRate | number:'1.2-2' }}/hr</span>
              <span class="slots-tag">{{ area.availableSlots }}/{{ area.totalSlots }} Free</span>
            </div>
          </div>
        </button>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Querying live parking bay statuses..."></app-loading-spinner>

      <!-- Facility Content Area -->
      <div class="facility-body" *ngIf="!loading && selectedArea">
        <!-- Telemetry Summary Cards -->
        <div class="telemetry-grid">
          <div class="telemetry-card">
            <span class="telemetry-label">Total Bays</span>
            <span class="telemetry-val">{{ selectedArea.totalSlots }}</span>
            <span class="telemetry-sub">Licensed facility capacity</span>
          </div>

          <div class="telemetry-card highlight-green">
            <span class="telemetry-label">Available Now</span>
            <span class="telemetry-val text-green">{{ availableCount }}</span>
            <span class="telemetry-sub">Ready for immediate booking</span>
          </div>

          <div class="telemetry-card highlight-amber">
            <span class="telemetry-label">Reserved</span>
            <span class="telemetry-val text-amber">{{ reservedCount }}</span>
            <span class="telemetry-sub">Arrival scheduled today</span>
          </div>

          <div class="telemetry-card highlight-red">
            <span class="telemetry-label">Occupied</span>
            <span class="telemetry-val text-red">{{ occupiedCount }}</span>
            <span class="telemetry-sub">Currently parked</span>
          </div>
        </div>

        <!-- Filter Controls Bar -->
        <div class="filters-bar">
          <div class="status-filters">
            <span class="filter-label">Status:</span>
            <button 
              *ngFor="let s of statusOptions" 
              class="filter-chip" 
              [class.active]="selectedStatus === s"
              (click)="setStatusFilter(s)">
              {{ s }}
            </button>
          </div>

          <div class="type-filters">
            <span class="filter-label">Bay Type:</span>
            <button 
              *ngFor="let t of typeOptions" 
              class="filter-chip" 
              [class.active]="selectedType === t"
              (click)="setTypeFilter(t)">
              {{ t }}
            </button>
          </div>
        </div>

        <!-- Slot Cards Grid -->
        <div class="slots-grid" *ngIf="filteredSlots.length > 0">
          <app-parking-slot-card 
            *ngFor="let slot of filteredSlots" 
            [slot]="slot"
            [showHourlyRate]="true"
            [hourlyRate]="selectedArea.hourlyRate"
            [selected]="selectedSlot?.id === slot.id"
            (slotSelect)="onSlotSelected($event)">
          </app-parking-slot-card>
        </div>

        <!-- Empty State -->
        <app-empty-state 
          *ngIf="filteredSlots.length === 0"
          icon="garage"
          title="No slots match your filters"
          description="Try selecting a different status or bay type filter to view available bays.">
        </app-empty-state>
      </div>

      <!-- If no areas configured -->
      <app-empty-state 
        *ngIf="!loading && areas.length === 0"
        icon="domain_disabled"
        title="No parking areas found"
        description="Please contact the facility administrator to initialize parking zones.">
      </app-empty-state>
    </div>
  `,
  styles: [`
    .parking-page-container {
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

      .book-cta-btn {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        height: 42px;
        border-radius: 10px;
      }
    }

    .facility-tabs-bar {
      display: flex;
      align-items: center;
      gap: 12px;
      overflow-x: auto;
      padding-bottom: 6px;

      &::-webkit-scrollbar {
        height: 4px;
      }
      &::-webkit-scrollbar-thumb {
        background: #cbd5e1;
        border-radius: 4px;
      }

      .facility-tab {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-md, 10px);
        padding: 12px 18px;
        cursor: pointer;
        transition: all 0.2s ease;
        text-align: left;
        min-width: 180px;

        &:hover {
          border-color: var(--primary-500, #3b82f6);
          background: #f8faff;
        }

        &.active {
          border-color: var(--primary-600, #2563eb);
          background: #eff6ff;
          box-shadow: 0 2px 8px rgba(37, 99, 235, 0.12);

          .tab-name {
            color: #2563eb;
          }
        }

        .tab-content {
          display: flex;
          flex-direction: column;
          gap: 6px;

          .tab-name {
            font-size: 0.9375rem;
            font-weight: 700;
            color: #0f172a;
          }

          .tab-metrics {
            display: flex;
            align-items: center;
            justify-content: space-between;
            font-size: 0.75rem;

            .rate-tag {
              color: #475569;
              font-weight: 600;
            }

            .slots-tag {
              color: #059669;
              font-weight: 700;
            }
          }
        }
      }
    }

    .telemetry-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
      gap: 16px;
      margin-bottom: 24px;

      .telemetry-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-lg, 16px);
        padding: 18px 20px;
        display: flex;
        flex-direction: column;
        box-shadow: 0 1px 3px rgba(15, 23, 42, 0.04);

        .telemetry-label {
          font-size: 0.75rem;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.06em;
          color: #64748b;
          margin-bottom: 6px;
        }

        .telemetry-val {
          font-size: 1.875rem;
          font-weight: 800;
          color: #0f172a;
          line-height: 1.1;
          margin-bottom: 4px;

          &.text-green { color: #059669; }
          &.text-amber { color: #d97706; }
          &.text-red { color: #dc2626; }
        }

        .telemetry-sub {
          font-size: 0.75rem;
          color: #94a3b8;
        }
      }
    }

    .filters-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;
      background: #ffffff;
      padding: 14px 18px;
      border-radius: var(--radius-md, 10px);
      border: 1px solid var(--border-light, #e2e8f0);
      margin-bottom: 20px;

      .status-filters, .type-filters {
        display: flex;
        align-items: center;
        gap: 6px;
        flex-wrap: wrap;

        .filter-label {
          font-size: 0.8125rem;
          font-weight: 700;
          color: #475569;
          margin-right: 4px;
        }

        .filter-chip {
          border: 1px solid var(--border-light, #e2e8f0);
          background: #f8fafc;
          border-radius: 9999px;
          padding: 4px 12px;
          font-size: 0.75rem;
          font-weight: 600;
          color: #64748b;
          cursor: pointer;
          transition: all 0.15s ease;

          &:hover {
            border-color: #cbd5e1;
            color: #0f172a;
          }

          &.active {
            background: #2563eb;
            color: #ffffff;
            border-color: #2563eb;
          }
        }
      }
    }

    .slots-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(210px, 1fr));
      gap: 16px;
    }
  `]
})
export class ParkingOverviewComponent implements OnInit {
  private areaService = inject(ParkingAreaService);
  private slotService = inject(ParkingSlotService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  areas: ParkingAreaDto[] = [];
  slots: ParkingSlotDto[] = [];
  selectedArea?: ParkingAreaDto;
  selectedSlot?: ParkingSlotDto;
  loading: boolean = false;

  statusOptions = ['All', 'Available', 'Reserved', 'Occupied', 'Maintenance'];
  typeOptions = ['All', 'Standard', 'EV', 'Disabled', 'VIP'];

  selectedStatus: string = 'All';
  selectedType: string = 'All';

  ngOnInit(): void {
    this.loadAreas();
  }

  loadAreas(): void {
    this.loading = true;
    this.areaService.getAll().subscribe({
      next: (res) => {
        this.areas = res.data || [];
        if (this.areas.length > 0) {
          this.selectArea(this.areas[0]);
        } else {
          this.loading = false;
        }
      },
      error: (err) => {
        this.loading = false;
        this.notification.error('Failed to load parking areas: ' + (err.error?.message || err.message));
      }
    });
  }

  selectArea(area: ParkingAreaDto): void {
    this.selectedArea = area;
    this.loadSlots(area.id);
  }

  loadSlots(areaId: string): void {
    this.loading = true;
    this.slotService.getAll(areaId).subscribe({
      next: (res) => {
        this.loading = false;
        this.slots = res.data || [];
      },
      error: (err) => {
        this.loading = false;
        this.notification.error('Failed to load slots: ' + (err.error?.message || err.message));
      }
    });
  }

  get availableCount(): number {
    return this.slots.filter(s => (s.status || '').toLowerCase() === 'available').length;
  }

  get reservedCount(): number {
    return this.slots.filter(s => (s.status || '').toLowerCase() === 'reserved').length;
  }

  get occupiedCount(): number {
    return this.slots.filter(s => (s.status || '').toLowerCase() === 'occupied').length;
  }

  get filteredSlots(): ParkingSlotDto[] {
    return this.slots.filter(s => {
      const matchStatus = this.selectedStatus === 'All' || 
        (s.status || '').toLowerCase() === this.selectedStatus.toLowerCase();
      const matchType = this.selectedType === 'All' || 
        (s.slotType || '').toLowerCase().includes(this.selectedType.toLowerCase());
      return matchStatus && matchType;
    });
  }

  setStatusFilter(status: string): void {
    this.selectedStatus = status;
  }

  setTypeFilter(type: string): void {
    this.selectedType = type;
  }

  onSlotSelected(slot: ParkingSlotDto): void {
    this.selectedSlot = slot;
    this.router.navigate(['/reservations/create'], {
      queryParams: {
        areaId: this.selectedArea?.id,
        slotId: slot.id
      }
    });
  }
}
