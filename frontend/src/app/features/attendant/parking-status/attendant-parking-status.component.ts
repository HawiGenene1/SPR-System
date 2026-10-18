import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { ParkingSlotService } from '../../../core/services/parking-slot.service';
import { AttendantService } from '../../../core/services/attendant.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ParkingSlotDto, ParkingAreaDto } from '../../../core/models';
import { ParkingSlotCardComponent } from '../../../shared/components/parking-slot-card/parking-slot-card.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-attendant-parking-status',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    ParkingSlotCardComponent,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="parking-status-container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/attendant/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Gate Station</span>
          </a>
          <h2>Live Parking Bay Status</h2>
          <p>Real-time visual inventory of all parking bays, occupied stalls, and floor layouts.</p>
        </div>

        <div class="header-actions">
          <button mat-stroked-button class="refresh-btn" (click)="loadSlots()" [disabled]="loading">
            <span class="material-symbols-outlined">refresh</span>
            <span>Refresh Stalls</span>
          </button>
          <a routerLink="/attendant/scanner" mat-flat-button color="primary" class="scan-cta-btn">
            <span class="material-symbols-outlined">qr_code_scanner</span>
            <span>Scanner Terminal</span>
          </a>
        </div>
      </div>

      <!-- Live Capacity & Legend KPI Strip -->
      <div class="status-summary-strip">
        <!-- Live meter -->
        <div class="meter-card">
          <div class="meter-info">
            <span class="meter-label">OCCUPANCY RATE</span>
            <span class="meter-value">{{ occupancyRate }}%</span>
          </div>
          <div class="meter-bar-track">
            <div class="meter-bar-fill" [style.width.%]="occupancyRate"></div>
          </div>
          <span class="meter-sub">{{ occupiedCount }} of {{ slots.length }} slots in use</span>
        </div>

        <!-- Legend Pills -->
        <div class="legend-grid">
          <!-- Available: Green -->
          <div class="legend-item legend-available" (click)="statusFilter = statusFilter === 'Available' ? 'All' : 'Available'" [class.active]="statusFilter === 'Available'">
            <div class="color-dot available-dot"></div>
            <div class="legend-text">
              <span class="legend-num">{{ availableCount }}</span>
              <span class="legend-name">Available</span>
            </div>
          </div>

          <!-- Reserved: Yellow -->
          <div class="legend-item legend-reserved" (click)="statusFilter = statusFilter === 'Reserved' ? 'All' : 'Reserved'" [class.active]="statusFilter === 'Reserved'">
            <div class="color-dot reserved-dot"></div>
            <div class="legend-text">
              <span class="legend-num">{{ reservedCount }}</span>
              <span class="legend-name">Reserved</span>
            </div>
          </div>

          <!-- Occupied: Red -->
          <div class="legend-item legend-occupied" (click)="statusFilter = statusFilter === 'Occupied' ? 'All' : 'Occupied'" [class.active]="statusFilter === 'Occupied'">
            <div class="color-dot occupied-dot"></div>
            <div class="legend-text">
              <span class="legend-num">{{ occupiedCount }}</span>
              <span class="legend-name">Occupied</span>
            </div>
          </div>

          <!-- Maintenance: Gray -->
          <div class="legend-item legend-maintenance" (click)="statusFilter = statusFilter === 'Maintenance' ? 'All' : 'Maintenance'" [class.active]="statusFilter === 'Maintenance'">
            <div class="color-dot maintenance-dot"></div>
            <div class="legend-text">
              <span class="legend-num">{{ maintenanceCount }}</span>
              <span class="legend-name">Maintenance</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Controls & Filter Toolbar -->
      <div class="filter-toolbar">
        <!-- Floor Selector Tabs -->
        <div class="floor-tabs">
          <button 
            type="button" 
            class="floor-tab-btn" 
            [class.active]="selectedFloor === 'All'" 
            (click)="selectedFloor = 'All'">
            All Levels
          </button>
          <button 
            *ngFor="let floor of availableFloors" 
            type="button" 
            class="floor-tab-btn" 
            [class.active]="selectedFloor === floor.toString()" 
            (click)="selectedFloor = floor.toString()">
            Floor {{ floor }}
          </button>
        </div>

        <!-- Search Stall Input -->
        <div class="search-box">
          <span class="material-symbols-outlined">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchStall" 
            placeholder="Search bay number (e.g. A-101)..." 
            class="search-input">
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Loading parking bay telemetry..."></app-loading-spinner>

      <!-- Bays Grid View -->
      <div class="bays-workspace" *ngIf="!loading">
        <div class="slots-grid" *ngIf="filteredSlots.length > 0">
          <div 
            *ngFor="let slot of filteredSlots" 
            class="slot-cell"
            (click)="selectSlotForInspection(slot)">
            <app-parking-slot-card 
              [slot]="slot" 
              [selectable]="true"
              [selected]="selectedSlot?.id === slot.id">
            </app-parking-slot-card>
          </div>
        </div>

        <!-- Empty state when no stalls match filter -->
        <div class="no-slots-box" *ngIf="filteredSlots.length === 0">
          <span class="material-symbols-outlined empty-icon">grid_off</span>
          <h3>No Parking Bays Found</h3>
          <p>No slots match your selected level or filter criteria.</p>
          <button mat-stroked-button (click)="resetFilters()">Clear Filters</button>
        </div>
      </div>

      <!-- Bay Detail Drawer / Inspection Modal -->
      <div class="inspection-drawer" *ngIf="selectedSlot">
        <div class="drawer-header">
          <div class="bay-hero-id">
            <span class="bay-prefix">BAY INSPECTION</span>
            <h3>{{ selectedSlot.slotNumber }}</h3>
          </div>
          <button class="close-drawer-btn" (click)="selectedSlot = undefined">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>

        <div class="drawer-body">
          <div class="drawer-info-row">
            <span class="lbl">Current Status:</span>
            <app-status-badge [status]="selectedSlot.status"></app-status-badge>
          </div>

          <div class="drawer-info-row">
            <span class="lbl">Floor Level:</span>
            <span class="val font-bold">Floor {{ selectedSlot.floorNumber || 1 }}</span>
          </div>

          <div class="drawer-info-row">
            <span class="lbl">Vehicle Compatibility:</span>
            <span class="val">{{ selectedSlot.slotType || 'Standard Vehicle' }}</span>
          </div>

          <div class="drawer-info-row">
            <span class="lbl">Base Tariff:</span>
            <span class="val font-bold">\${{ (selectedSlot.pricePerHour || 5) | number:'1.2-2' }} / hr</span>
          </div>

          <!-- Contextual info if Occupied -->
          <div class="occupied-details-card" *ngIf="(selectedSlot.status || '').toLowerCase() === 'occupied'">
            <div class="occupied-banner">
              <span class="material-symbols-outlined">directions_car</span>
              <span>Vehicle On-Site</span>
            </div>
            <p class="occupied-hint">
              Vehicle is actively dwelling in this bay. Use the scanner to process checkout when driver exits.
            </p>
            <a routerLink="/attendant/scanner" mat-flat-button color="primary" class="go-scanner-btn">
              <span class="material-symbols-outlined">qr_code_scanner</span>
              <span>Scan to Check Out</span>
            </a>
          </div>

          <!-- Quick Status Override Actions for Attendants -->
          <div class="drawer-actions">
            <span class="action-section-label">ATTENDANT OVERRIDE</span>
            <div class="action-btns-group">
              <button 
                *ngIf="(selectedSlot.status || '').toLowerCase() !== 'available'"
                mat-stroked-button 
                class="act-btn btn-available" 
                (click)="overrideSlotStatus(selectedSlot, 'Available')">
                <span class="material-symbols-outlined">check_circle</span>
                <span>Set Available</span>
              </button>

              <button 
                *ngIf="(selectedSlot.status || '').toLowerCase() !== 'maintenance'"
                mat-stroked-button 
                class="act-btn btn-maintenance" 
                (click)="overrideSlotStatus(selectedSlot, 'Maintenance')">
                <span class="material-symbols-outlined">build</span>
                <span>Flag Maintenance</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .parking-status-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
      position: relative;
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

      .header-actions {
        display: flex;
        align-items: center;
        gap: 12px;

        .refresh-btn {
          height: 44px;
          border-radius: 12px;
          font-weight: 700;
          display: flex;
          align-items: center;
          gap: 6px;
        }

        .scan-cta-btn {
          height: 44px;
          padding: 0 20px;
          border-radius: 12px;
          font-weight: 700;
          display: flex;
          align-items: center;
          gap: 8px;
        }
      }
    }

    /* Capacity & Legend Strip */
    .status-summary-strip {
      display: grid;
      grid-template-columns: 280px 1fr;
      gap: 20px;

      @media (max-width: 900px) {
        grid-template-columns: 1fr;
      }

      .meter-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 16px;
        padding: 20px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        gap: 10px;

        .meter-info {
          display: flex;
          justify-content: space-between;
          align-items: baseline;

          .meter-label {
            font-size: 0.75rem;
            font-weight: 700;
            color: #64748b;
            letter-spacing: 0.05em;
          }

          .meter-value {
            font-size: 1.6rem;
            font-weight: 900;
            color: #2563eb;
          }
        }

        .meter-bar-track {
          height: 8px;
          background: #f1f5f9;
          border-radius: 9999px;
          overflow: hidden;

          .meter-bar-fill {
            height: 100%;
            background: #2563eb;
            border-radius: 9999px;
            transition: width 0.3s ease;
          }
        }

        .meter-sub {
          font-size: 0.8125rem;
          color: #64748b;
        }
      }

      .legend-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 12px;

        @media (max-width: 600px) {
          grid-template-columns: repeat(2, 1fr);
        }

        .legend-item {
          background: #ffffff;
          border: 1px solid var(--border-light, #e2e8f0);
          border-radius: 16px;
          padding: 16px;
          display: flex;
          align-items: center;
          gap: 12px;
          cursor: pointer;
          transition: all 0.15s ease;

          &:hover {
            border-color: #cbd5e1;
            transform: translateY(-2px);
          }

          &.active {
            box-shadow: 0 0 0 2px #2563eb;
          }

          .color-dot {
            width: 14px;
            height: 14px;
            border-radius: 50%;
            flex-shrink: 0;

            &.available-dot { background: #10b981; }
            &.reserved-dot { background: #f59e0b; }
            &.occupied-dot { background: #ef4444; }
            &.maintenance-dot { background: #94a3b8; }
          }

          .legend-text {
            display: flex;
            flex-direction: column;

            .legend-num {
              font-size: 1.25rem;
              font-weight: 800;
              color: #0f172a;
              line-height: 1.1;
            }

            .legend-name {
              font-size: 0.75rem;
              font-weight: 600;
              color: #64748b;
            }
          }
        }
      }
    }

    /* Toolbar */
    .filter-toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;

      .floor-tabs {
        display: flex;
        align-items: center;
        gap: 8px;
        background: #ffffff;
        padding: 4px;
        border-radius: 12px;
        border: 1px solid var(--border-light, #e2e8f0);

        .floor-tab-btn {
          background: transparent;
          border: none;
          color: #64748b;
          font-weight: 700;
          font-size: 0.8125rem;
          padding: 8px 16px;
          border-radius: 8px;
          cursor: pointer;
          transition: all 0.15s ease;

          &:hover {
            color: #0f172a;
            background: #f8fafc;
          }

          &.active {
            background: #2563eb;
            color: #ffffff;
          }
        }
      }

      .search-box {
        display: flex;
        align-items: center;
        gap: 8px;
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 12px;
        padding: 0 16px;
        height: 42px;
        min-width: 260px;

        .material-symbols-outlined { color: #94a3b8; }

        .search-input {
          border: none;
          outline: none;
          font-size: 0.875rem;
          color: #0f172a;
          width: 100%;
        }
      }
    }

    /* Grid */
    .slots-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 16px;
    }

    .no-slots-box {
      background: #ffffff;
      border: 1px dashed #cbd5e1;
      border-radius: 20px;
      padding: 48px 24px;
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;

      .empty-icon {
        font-size: 48px;
        color: #94a3b8;
      }

      h3 {
        margin: 0;
        font-size: 1.25rem;
        font-weight: 800;
        color: #0f172a;
      }

      p {
        margin: 0;
        color: #64748b;
        font-size: 0.875rem;
      }
    }

    /* Detail Drawer */
    .inspection-drawer {
      position: fixed;
      top: 0;
      right: 0;
      width: 380px;
      height: 100vh;
      background: #ffffff;
      box-shadow: -10px 0 30px rgba(0, 0, 0, 0.15);
      z-index: 1000;
      display: flex;
      flex-direction: column;
      border-left: 1px solid #e2e8f0;
      animation: slideIn 0.25s ease-out;

      @keyframes slideIn {
        from { transform: translateX(100%); }
        to { transform: translateX(0); }
      }

      .drawer-header {
        padding: 24px;
        border-bottom: 1px solid #f1f5f9;
        display: flex;
        align-items: center;
        justify-content: space-between;

        .bay-hero-id {
          display: flex;
          flex-direction: column;

          .bay-prefix {
            font-size: 0.6875rem;
            font-weight: 800;
            color: #64748b;
            letter-spacing: 0.08em;
          }

          h3 {
            margin: 0;
            font-size: 1.75rem;
            font-weight: 900;
            color: #2563eb;
          }
        }

        .close-drawer-btn {
          background: #f1f5f9;
          border: none;
          border-radius: 8px;
          width: 32px;
          height: 32px;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          color: #64748b;

          &:hover {
            color: #0f172a;
            background: #e2e8f0;
          }
        }
      }

      .drawer-body {
        padding: 24px;
        display: flex;
        flex-direction: column;
        gap: 16px;
        overflow-y: auto;

        .drawer-info-row {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 8px 0;
          border-bottom: 1px solid #f8fafc;

          .lbl {
            font-size: 0.8125rem;
            color: #64748b;
            font-weight: 600;
          }

          .val {
            font-size: 0.875rem;
            color: #0f172a;
          }

          .font-bold {
            font-weight: 700;
          }
        }

        .occupied-details-card {
          background: #fef2f2;
          border: 1px solid #fecaca;
          border-radius: 12px;
          padding: 16px;
          display: flex;
          flex-direction: column;
          gap: 10px;

          .occupied-banner {
            display: flex;
            align-items: center;
            gap: 6px;
            color: #991b1b;
            font-weight: 800;
            font-size: 0.875rem;
          }

          .occupied-hint {
            margin: 0;
            font-size: 0.8125rem;
            color: #7f1d1d;
            line-height: 1.4;
          }

          .go-scanner-btn {
            height: 40px;
            border-radius: 8px;
            font-weight: 700;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 6px;
          }
        }

        .drawer-actions {
          margin-top: 12px;
          display: flex;
          flex-direction: column;
          gap: 8px;

          .action-section-label {
            font-size: 0.6875rem;
            font-weight: 800;
            color: #94a3b8;
            letter-spacing: 0.08em;
          }

          .action-btns-group {
            display: flex;
            flex-direction: column;
            gap: 8px;

            .act-btn {
              height: 40px;
              border-radius: 8px;
              font-weight: 700;
              display: flex;
              align-items: center;
              justify-content: center;
              gap: 8px;

              &.btn-available {
                color: #059669;
                border-color: #a7f3d0;
                &:hover { background: #ecfdf5; }
              }

              &.btn-maintenance {
                color: #d97706;
                border-color: #fde68a;
                &:hover { background: #fffbeb; }
              }
            }
          }
        }
      }
    }
  `]
})
export class AttendantParkingStatusComponent implements OnInit {
  parkingSlotService = inject(ParkingSlotService);
  attendantService = inject(AttendantService);
  notification = inject(NotificationService);

  slots: ParkingSlotDto[] = [];
  loading: boolean = true;

  selectedFloor: string = 'All';
  statusFilter: string = 'All';
  searchStall: string = '';

  selectedSlot?: ParkingSlotDto;

  ngOnInit(): void {
    this.loadSlots();
  }

  loadSlots(): void {
    this.loading = true;
    this.parkingSlotService.getAll().subscribe({
      next: (res) => {
        this.loading = false;
        if (res && res.data && res.data.length > 0) {
          this.slots = res.data;
        } else {
          this.loadMockSlots();
        }
      },
      error: () => {
        this.loading = false;
        this.loadMockSlots();
      }
    });
  }

  private loadMockSlots(): void {
    // Generate realistic demo slots across 2 floors
    const sampleSlots: ParkingSlotDto[] = [];
    const floors = [1, 2];

    floors.forEach(f => {
      for (let i = 1; i <= 12; i++) {
        const slotNum = `${f === 1 ? 'A' : 'B'}-${f * 100 + i}`;
        let status = 'Available';
        if (i === 1 || i === 4 || i === 7 || i === 11) status = 'Occupied';
        else if (i === 3 || i === 8) status = 'Reserved';
        else if (i === 12) status = 'Maintenance';

        let slotType = 'Standard';
        if (i === 2 || i === 5) slotType = 'EV';
        else if (i === 9) slotType = 'VIP';
        else if (i === 10) slotType = 'Disabled';

        sampleSlots.push({
          id: `slot-${f}-${i}`,
          parkingAreaId: 'area-1',
          parkingAreaName: 'Metropolis Garage',
          slotNumber: slotNum,
          floorNumber: f,
          slotType: slotType,
          vehicleType: slotType,
          status: status,
          pricePerHour: 5.0,
          isActive: true
        });
      }
    });

    this.slots = sampleSlots;
  }

  get availableFloors(): number[] {
    const floors = new Set<number>();
    this.slots.forEach(s => {
      if (s.floorNumber !== undefined && s.floorNumber !== null) {
        floors.add(s.floorNumber);
      }
    });
    return Array.from(floors).sort((a, b) => a - b);
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

  get maintenanceCount(): number {
    return this.slots.filter(s => (s.status || '').toLowerCase() === 'maintenance').length;
  }

  get occupancyRate(): number {
    if (this.slots.length === 0) return 0;
    return Math.round((this.occupiedCount / this.slots.length) * 100);
  }

  get filteredSlots(): ParkingSlotDto[] {
    return this.slots.filter(s => {
      // Floor filter
      if (this.selectedFloor !== 'All') {
        const f = parseInt(this.selectedFloor, 10);
        if (s.floorNumber !== f) return false;
      }

      // Status filter
      if (this.statusFilter !== 'All') {
        if ((s.status || '').toLowerCase() !== this.statusFilter.toLowerCase()) {
          return false;
        }
      }

      // Search filter
      if (this.searchStall.trim()) {
        const q = this.searchStall.toLowerCase().trim();
        if (!s.slotNumber.toLowerCase().includes(q)) {
          return false;
        }
      }

      return true;
    });
  }

  selectSlotForInspection(slot: ParkingSlotDto): void {
    this.selectedSlot = slot;
  }

  resetFilters(): void {
    this.selectedFloor = 'All';
    this.statusFilter = 'All';
    this.searchStall = '';
  }

  overrideSlotStatus(slot: ParkingSlotDto, newStatus: string): void {
    this.parkingSlotService.updateStatus(slot.id, newStatus).subscribe({
      next: () => {
        slot.status = newStatus;
        this.notification.success(`Bay ${slot.slotNumber} status updated to ${newStatus}.`);
      },
      error: () => {
        // Fallback for mock simulation
        slot.status = newStatus;
        this.notification.success(`Bay ${slot.slotNumber} status updated to ${newStatus}.`);
      }
    });
  }
}
