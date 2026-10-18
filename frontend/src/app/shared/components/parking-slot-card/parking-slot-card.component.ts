import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ParkingSlotDto } from '../../../core/models';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';

@Component({
  selector: 'app-parking-slot-card',
  standalone: true,
  imports: [CommonModule, StatusBadgeComponent],
  template: `
    <div 
      class="slot-card"
      [class.selected]="selected"
      [class.selectable]="isSelectable"
      [class.disabled]="isDisabled"
      [ngClass]="statusClass"
      (click)="onClick()">
      
      <!-- Top header: Floor and Status Badge -->
      <div class="card-header">
        <div class="floor-pill" [title]="displayFloor">
          <span class="material-symbols-outlined floor-icon">stairs</span>
          <span class="floor-label">{{ displayFloor }}</span>
        </div>

        <div class="header-status">
          <app-status-badge [status]="slot.status"></app-status-badge>
        </div>
      </div>

      <!-- Main center: Slot Number & Selection Indicator -->
      <div class="card-center">
        <div class="slot-number-wrap">
          <span class="slot-label-bay">BAY</span>
          <span class="slot-id">{{ slot.slotNumber }}</span>
        </div>

        <div class="selection-indicator" *ngIf="selectable && isSelectable">
          <span class="material-symbols-outlined check-icon">
            {{ selected ? 'check_circle' : 'radio_button_unchecked' }}
          </span>
        </div>
      </div>

      <!-- Vehicle Type & Compatibility -->
      <div class="card-meta">
        <div class="meta-item vehicle-compat" [title]="'Vehicle: ' + displayVehicleType">
          <span class="material-symbols-outlined meta-icon">{{ vehicleIcon }}</span>
          <span class="meta-text">{{ displayVehicleType }}</span>
        </div>
        <div class="meta-item slot-type-pill" *ngIf="slot.slotType && slot.slotType !== displayVehicleType">
          <span class="type-text">{{ slot.slotType }}</span>
        </div>
      </div>

      <!-- Card Footer: Price / Hour and Action Hint -->
      <div class="card-footer">
        <div class="price-box">
          <span class="price-val">\${{ displayPrice | number:'1.2-2' }}</span>
          <span class="price-unit">/hour</span>
        </div>

        <div class="slot-action-hint">
          <span *ngIf="isSelectable" class="hint-available">
            {{ selected ? 'Selected' : 'Click to Reserve' }}
          </span>
          <span *ngIf="isDisabled" class="hint-unavailable">
            {{ (slot.status || 'Unavailable') }}
          </span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .slot-card {
      position: relative;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      min-height: 172px;
      padding: 16px;
      background: #ffffff;
      border: 2px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-lg, 16px);
      box-shadow: 0 2px 6px rgba(15, 23, 42, 0.04);
      transition: all 0.22s cubic-bezier(0.16, 1, 0.3, 1);
      user-select: none;

      /* Available → Green */
      &.status-available {
        border-color: #10b981;
        background: #ffffff;
        box-shadow: 0 2px 8px rgba(16, 185, 129, 0.08);

        &.selectable {
          cursor: pointer;

          &:hover {
            transform: translateY(-3px);
            border-color: #059669;
            background: #f0fdf4;
            box-shadow: 0 10px 24px -2px rgba(16, 185, 129, 0.22);
          }
        }

        &.selected {
          border-color: #047857;
          background: #ecfdf5;
          box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.35), 0 10px 20px rgba(16, 185, 129, 0.15);

          .selection-indicator .check-icon {
            color: #059669;
          }
        }
      }

      /* Reserved → Yellow / Amber */
      &.status-reserved {
        border-color: #f59e0b;
        background: #fffdf5;
        cursor: not-allowed;
        box-shadow: 0 2px 6px rgba(245, 158, 11, 0.06);

        .slot-id {
          color: #92400e;
        }
      }

      /* Occupied → Red */
      &.status-occupied {
        border-color: #ef4444;
        background: #fef2f2;
        cursor: not-allowed;
        box-shadow: 0 2px 6px rgba(239, 68, 68, 0.06);

        .slot-id {
          color: #991b1b;
        }
      }

      /* Maintenance → Gray */
      &.status-maintenance {
        border-color: #94a3b8;
        background: #f8fafc;
        border-style: dashed;
        cursor: not-allowed;
        opacity: 0.85;

        .slot-id {
          color: #475569;
        }
      }

      &.disabled {
        cursor: not-allowed;
      }
    }

    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;

      .floor-pill {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        font-size: 0.75rem;
        font-weight: 600;
        color: var(--text-secondary, #475569);
        background: var(--bg-subtle, #f1f5f9);
        padding: 3px 8px;
        border-radius: var(--radius-sm, 6px);

        .floor-icon {
          font-size: 15px;
          color: #64748b;
        }
      }
    }

    .card-center {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin: 12px 0 6px 0;

      .slot-number-wrap {
        display: flex;
        flex-direction: column;

        .slot-label-bay {
          font-size: 0.6875rem;
          font-weight: 700;
          letter-spacing: 0.08em;
          color: var(--text-muted, #64748b);
          text-transform: uppercase;
        }

        .slot-id {
          font-size: 1.85rem;
          font-weight: 800;
          color: var(--text-primary, #0f172a);
          letter-spacing: -0.03em;
          line-height: 1.1;
        }
      }

      .selection-indicator {
        .check-icon {
          font-size: 24px;
          color: var(--border-medium, #cbd5e1);
          transition: color 0.15s ease, transform 0.15s ease;
        }
      }
    }

    .card-meta {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
      flex-wrap: wrap;

      .meta-item {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        font-size: 0.75rem;
        font-weight: 600;
        padding: 2px 7px;
        border-radius: 4px;

        &.vehicle-compat {
          background: #eff6ff;
          color: #1e40af;

          .meta-icon {
            font-size: 15px;
            color: #2563eb;
          }
        }

        &.slot-type-pill {
          background: #f8fafc;
          border: 1px solid #e2e8f0;
          color: #475569;
        }
      }
    }

    .card-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      padding-top: 10px;
      border-top: 1px solid var(--border-light, #f1f5f9);

      .price-box {
        display: flex;
        align-items: baseline;
        gap: 2px;

        .price-val {
          font-size: 0.9375rem;
          font-weight: 800;
          color: var(--text-primary, #0f172a);
        }

        .price-unit {
          font-size: 0.6875rem;
          font-weight: 600;
          color: var(--text-muted, #64748b);
        }
      }

      .slot-action-hint {
        font-size: 0.6875rem;
        font-weight: 700;

        .hint-available {
          color: #059669;
        }

        .hint-unavailable {
          color: #94a3b8;
          text-transform: uppercase;
          letter-spacing: 0.04em;
        }
      }
    }
  `]
})
export class ParkingSlotCardComponent {
  @Input({ required: true }) slot!: ParkingSlotDto;
  @Input() selected: boolean = false;
  @Input() selectable: boolean = true;
  @Input() showHourlyRate: boolean = false;
  @Input() hourlyRate?: number;

  @Output() slotSelect = new EventEmitter<ParkingSlotDto>();

  get isSelectable(): boolean {
    return this.selectable && (this.slot.status || '').toLowerCase() === 'available';
  }

  get isDisabled(): boolean {
    return (this.slot.status || '').toLowerCase() !== 'available';
  }

  get statusClass(): string {
    const s = (this.slot.status || '').toLowerCase();
    if (s === 'available') return 'status-available';
    if (s === 'reserved') return 'status-reserved';
    if (s === 'occupied') return 'status-occupied';
    if (s === 'maintenance') return 'status-maintenance';
    return 'status-maintenance';
  }

  get displayPrice(): number {
    if (this.slot.pricePerHour !== undefined && this.slot.pricePerHour > 0) {
      return this.slot.pricePerHour;
    }
    if (this.slot.hourlyRate !== undefined && this.slot.hourlyRate > 0) {
      return this.slot.hourlyRate;
    }
    if (this.hourlyRate !== undefined && this.hourlyRate > 0) {
      return this.hourlyRate;
    }
    return 5.0;
  }

  get displayFloor(): string {
    const f = this.slot.floorNumber;
    if (f !== undefined && f !== null) {
      return f === 0 ? 'Ground Floor' : `Floor ${f}`;
    }
    return 'Floor 1';
  }

  get displayVehicleType(): string {
    return this.slot.vehicleType || this.slot.slotType || 'Car';
  }

  get vehicleIcon(): string {
    const v = (this.slot.vehicleType || this.slot.slotType || '').toLowerCase();
    if (v.includes('ev') || v.includes('electric')) return 'bolt';
    if (v.includes('motorcycle') || v.includes('bike')) return 'two_wheeler';
    if (v.includes('suv') || v.includes('truck')) return 'airport_shuttle';
    if (v.includes('disabled') || v.includes('accessible')) return 'accessible';
    return 'directions_car';
  }

  onClick(): void {
    if (this.isSelectable) {
      this.slotSelect.emit(this.slot);
    }
  }
}
