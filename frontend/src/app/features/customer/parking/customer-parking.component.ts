import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ParkingAreaService } from '../../../core/services/parking-area.service';
import { ParkingSlotService } from '../../../core/services/parking-slot.service';
import { VehicleService } from '../../../core/services/vehicle.service';
import { ReservationService } from '../../../core/services/reservation.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ParkingAreaDto, ParkingSlotDto, VehicleDto } from '../../../core/models';
import { ParkingSlotCardComponent } from '../../../shared/components/parking-slot-card/parking-slot-card.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-customer-parking',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDialogModule,
    ParkingSlotCardComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="customer-parking-container">
      <!-- Hero Header -->
      <div class="search-header">
        <div class="header-titles">
          <div class="header-badge">
            <span class="material-symbols-outlined">search_check</span>
            <span>Real-Time Bay Discovery</span>
          </div>
          <h2>Find & Reserve Parking</h2>
          <p>Select your facility, schedule, and vehicle to explore live bay availability and secure guaranteed parking.</p>
        </div>
      </div>

      <!-- Search Criteria Panel: Facility, Date, Entry Time, Exit Time, Vehicle -->
      <div class="search-criteria-card">
        <div class="criteria-card-title">
          <span class="material-symbols-outlined title-icon">tune</span>
          <h3>Reservation Parameters</h3>
        </div>

        <div class="criteria-grid">
          <!-- 1. Parking Area Selection -->
          <div class="criteria-item area-item">
            <label class="control-label">Parking Area</label>
            <mat-form-field appearance="outline" class="w-full">
              <mat-select 
                [(ngModel)]="selectedAreaId" 
                (selectionChange)="onAreaSelected($event.value)"
                placeholder="Choose facility">
                <mat-select-trigger *ngIf="selectedArea">
                  <div class="select-value-display">
                    <span class="material-symbols-outlined select-icon">domain</span>
                    <span class="select-name">{{ selectedArea.name }}</span>
                    <span class="select-rate">(Birr {{ selectedArea.hourlyRate | number:'1.2-2' }}/hr)</span>
                  </div>
                </mat-select-trigger>
                <mat-option *ngFor="let area of areas" [value]="area.id">
                  <div class="area-option-row">
                    <div class="option-info">
                      <span class="option-name">{{ area.name }}</span>
                      <span class="option-location">{{ area.location }}</span>
                    </div>
                    <div class="option-metrics">
                      <span class="option-rate">Birr {{ area.hourlyRate | number:'1.2-2' }}/hr</span>
                      <span class="option-capacity">{{ area.availableSlots }} free</span>
                    </div>
                  </div>
                </mat-option>
              </mat-select>
            </mat-form-field>
          </div>

          <!-- 2. Date Selection -->
          <div class="criteria-item">
            <label class="control-label">Reservation Date</label>
            <mat-form-field appearance="outline" class="w-full">
              <input 
                matInput 
                type="date" 
                [(ngModel)]="selectedDate" 
                [min]="todayDateString" 
                (change)="onScheduleChanged()">
            </mat-form-field>
          </div>

          <!-- 3. Entry Time Selection -->
          <div class="criteria-item">
            <label class="control-label">Entry Time</label>
            <mat-form-field appearance="outline" class="w-full">
              <input 
                matInput 
                type="time" 
                [(ngModel)]="entryTime" 
                (change)="onScheduleChanged()">
            </mat-form-field>
          </div>

          <!-- 4. Exit Time Selection -->
          <div class="criteria-item">
            <div class="label-with-duration">
              <label class="control-label">Exit Time</label>
              <span class="duration-badge" *ngIf="calculatedHours > 0">
                {{ calculatedHours | number:'1.1-1' }} hrs
              </span>
            </div>
            <mat-form-field appearance="outline" class="w-full">
              <input 
                matInput 
                type="time" 
                [(ngModel)]="exitTime" 
                (change)="onScheduleChanged()">
            </mat-form-field>
          </div>

          <!-- 5. Vehicle Selection -->
          <div class="criteria-item vehicle-item">
            <div class="label-with-action">
              <label class="control-label">Assigned Vehicle</label>
              <button type="button" class="quick-add-link" (click)="openAddVehicleDialog(newVehicleModal)">
                + Register New
              </button>
            </div>
            <mat-form-field appearance="outline" class="w-full" *ngIf="vehicles.length > 0">
              <mat-select 
                [(ngModel)]="selectedVehicleId" 
                (selectionChange)="onVehicleSelected($event.value)"
                placeholder="Select vehicle">
                <mat-select-trigger *ngIf="selectedVehicle">
                  <div class="vehicle-select-value">
                    <span class="plate-badge">{{ selectedVehicle.licensePlate }}</span>
                    <span class="vehicle-model">{{ selectedVehicle.make }} {{ selectedVehicle.model }}</span>
                  </div>
                </mat-select-trigger>
                <mat-option *ngFor="let v of vehicles" [value]="v.id">
                  <div class="vehicle-option-row">
                    <span class="plate-badge">{{ v.licensePlate }}</span>
                    <span class="v-desc">{{ v.make }} {{ v.model }} ({{ v.vehicleType }})</span>
                  </div>
                </mat-option>
              </mat-select>
            </mat-form-field>

            <div class="no-vehicle-alert" *ngIf="vehicles.length === 0">
              <span class="material-symbols-outlined alert-icon">warning</span>
              <span>No vehicle registered yet.</span>
              <button mat-button color="primary" class="alert-action-btn" (click)="openAddVehicleDialog(newVehicleModal)">
                Register Vehicle
              </button>
            </div>
          </div>
        </div>

        <!-- Duration Quick Preset Buttons -->
        <div class="duration-shortcuts">
          <span class="shortcuts-label">Quick Duration:</span>
          <button 
            type="button" 
            *ngFor="let preset of durationPresets" 
            class="preset-btn"
            [class.active]="selectedPresetHours === preset.hours"
            (click)="applyDurationPreset(preset.hours)">
            {{ preset.label }}
          </button>
        </div>
      </div>

      <!-- Facility Quick Switcher Strip -->
      <div class="facility-strip" *ngIf="areas.length > 1">
        <div 
          *ngFor="let area of areas" 
          class="strip-card" 
          [class.active]="selectedArea?.id === area.id"
          (click)="onAreaSelected(area.id)">
          <div class="strip-header">
            <span class="strip-name">{{ area.name }}</span>
            <span class="strip-rate">Birr {{ area.hourlyRate | number:'1.2-2' }}/hr</span>
          </div>
          <div class="strip-footer">
            <span class="strip-slots">
              <span class="dot" [class.low]="area.availableSlots < 5"></span>
              <strong>{{ area.availableSlots }}</strong> of {{ area.totalSlots }} slots free
            </span>
            <span class="strip-action">{{ selectedArea?.id === area.id ? 'Viewing Layout' : 'Select' }}</span>
          </div>
        </div>
      </div>

      <!-- Main Slot Workspace -->
      <div class="slots-workspace" *ngIf="selectedArea">
        <!-- Workspace Header: Title, Floor Filters & Status Legend -->
        <div class="workspace-header">
          <div class="workspace-left">
            <h3>{{ selectedArea.name }} — Interactive Bay Layout</h3>
            <p>Live floor plan of all slots. Select an available bay to book your scheduled stay.</p>
          </div>

          <!-- Color Status Legend (Available, Reserved, Occupied, Maintenance) -->
          <div class="status-legend-bar">
            <div class="legend-item legend-available" title="Clickable and ready to book">
              <span class="legend-color-box available-box"></span>
              <span class="legend-text">Available (Green)</span>
            </div>
            <div class="legend-item legend-reserved" title="Reserved by other drivers">
              <span class="legend-color-box reserved-box"></span>
              <span class="legend-text">Reserved (Yellow)</span>
            </div>
            <div class="legend-item legend-occupied" title="Vehicle currently parked on-site">
              <span class="legend-color-box occupied-box"></span>
              <span class="legend-text">Occupied (Red)</span>
            </div>
            <div class="legend-item legend-maintenance" title="Temporarily offline for service">
              <span class="legend-color-box maintenance-box"></span>
              <span class="legend-text">Maintenance (Gray)</span>
            </div>
          </div>
        </div>

        <!-- Filter Row: Floors & Slot Types -->
        <div class="filter-controls-row">
          <!-- Floor Filter Tabs -->
          <div class="floor-tabs" *ngIf="availableFloors.length > 1">
            <button 
              class="floor-tab-btn" 
              [class.active]="selectedFloor === null"
              (click)="selectedFloor = null">
              All Floors ({{ slots.length }})
            </button>
            <button 
              *ngFor="let f of availableFloors" 
              class="floor-tab-btn" 
              [class.active]="selectedFloor === f"
              (click)="selectedFloor = f">
              Floor {{ f }} ({{ getFloorSlotsCount(f) }})
            </button>
          </div>

          <!-- Bay Type Chips -->
          <div class="type-filter-chips">
            <button 
              *ngFor="let t of typeFilters" 
              class="type-chip" 
              [class.active]="selectedType === t"
              (click)="selectedType = t">
              {{ t }}
            </button>
          </div>
        </div>

        <!-- Loading Slots Indicator -->
        <app-loading-spinner *ngIf="loadingSlots" message="Retrieving real-time bay telemetry..."></app-loading-spinner>

        <!-- Visual Slots Grid -->
        <div class="slots-grid-wrapper" *ngIf="!loadingSlots && filteredSlots.length > 0">
          <app-parking-slot-card
            *ngFor="let slot of filteredSlots"
            [slot]="slot"
            [selectable]="true"
            [selected]="selectedSlot?.id === slot.id"
            [hourlyRate]="selectedArea.hourlyRate"
            [showHourlyRate]="true"
            (slotSelect)="onSlotSelected(slot)">
          </app-parking-slot-card>
        </div>

        <!-- Empty State if no slots match filters -->
        <app-empty-state 
          *ngIf="!loadingSlots && filteredSlots.length === 0"
          icon="garage"
          title="No slots found for this filter"
          description="Try changing the floor level or vehicle type filter to find open bays.">
        </app-empty-state>
      </div>

      <!-- Persistent Booking Confirmation Drawer / Card -->
      <div class="reservation-drawer" *ngIf="selectedSlot && selectedArea">
        <div class="drawer-inner">
          <!-- Left: Bay Details & Schedule -->
          <div class="drawer-details">
            <div class="bay-tag">
              <span class="bay-num">BAY {{ selectedSlot.slotNumber }}</span>
              <span class="bay-floor">Floor {{ selectedSlot.floorNumber || 1 }}</span>
              <span class="bay-type">{{ selectedSlot.slotType }}</span>
            </div>

            <div class="drawer-info-grid">
              <div class="info-cell">
                <span class="cell-label">Facility</span>
                <span class="cell-val">{{ selectedArea.name }}</span>
              </div>
              <div class="info-cell">
                <span class="cell-label">Schedule</span>
                <span class="cell-val">{{ selectedDate | date:'mediumDate' }}, {{ entryTime }} – {{ exitTime }}</span>
              </div>
              <div class="info-cell">
                <span class="cell-label">Assigned Vehicle</span>
                <span class="cell-val font-mono" *ngIf="selectedVehicle">
                  {{ selectedVehicle.licensePlate }} ({{ selectedVehicle.make }} {{ selectedVehicle.model }})
                </span>
                <span class="cell-val text-amber" *ngIf="!selectedVehicle">None Selected</span>
              </div>
              <div class="info-cell">
                <span class="cell-label">Hourly Rate</span>
                <span class="cell-val">Birr {{ (selectedSlot.pricePerHour || selectedArea.hourlyRate) | number:'1.2-2' }} / hr</span>
              </div>
            </div>
          </div>

          <!-- Right: Price & Confirm Button -->
          <div class="drawer-actions">
            <div class="price-breakdown">
              <span class="price-sub">{{ calculatedHours | number:'1.1-1' }} hrs estimated</span>
              <span class="price-total">Birr {{ calculatedTotal | number:'1.2-2' }}</span>
            </div>

            <button 
              mat-flat-button 
              color="primary" 
              class="confirm-booking-btn"
              [disabled]="!selectedVehicle || submitting"
              (click)="confirmReservation()">
              <span class="material-symbols-outlined">verified</span>
              <span>{{ submitting ? 'Securing Bay...' : 'Confirm & Reserve Spot' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal to Register a New Vehicle without leaving the page -->
    <ng-template #newVehicleModal let-dialogRef="dialogRef">
      <div class="modal-dialog-box">
        <div class="modal-header">
          <h3>Register Vehicle</h3>
          <button class="close-btn" (click)="dialogRef.close()">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>

        <form [formGroup]="vehicleForm" (ngSubmit)="saveVehicle(dialogRef)" class="modal-form">
          <div class="form-row">
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>License Plate</mat-label>
              <input matInput formControlName="licensePlate" placeholder="ABC-1234" style="text-transform: uppercase;">
              <mat-error *ngIf="vehicleForm.get('licensePlate')?.hasError('required')">Plate is required</mat-error>
            </mat-form-field>

            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Vehicle Type</mat-label>
              <mat-select formControlName="vehicleType">
                <mat-option value="Sedan">Sedan / Standard Car</mat-option>
                <mat-option value="SUV">SUV / Crossover</mat-option>
                <mat-option value="Electric">Electric Vehicle (EV)</mat-option>
                <mat-option value="Motorcycle">Motorcycle</mat-option>
                <mat-option value="Truck">Truck / Van</mat-option>
              </mat-select>
            </mat-form-field>
          </div>

          <div class="form-row">
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Make</mat-label>
              <input matInput formControlName="make" placeholder="e.g. Tesla, Toyota">
              <mat-error *ngIf="vehicleForm.get('make')?.hasError('required')">Make is required</mat-error>
            </mat-form-field>

            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Model</mat-label>
              <input matInput formControlName="model" placeholder="e.g. Model 3, Camry">
              <mat-error *ngIf="vehicleForm.get('model')?.hasError('required')">Model is required</mat-error>
            </mat-form-field>
          </div>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Color</mat-label>
            <input matInput formControlName="color" placeholder="e.g. Midnight Silver">
          </mat-form-field>

          <div class="modal-actions">
            <button mat-button type="button" (click)="dialogRef.close()">Cancel</button>
            <button mat-flat-button color="primary" type="submit" [disabled]="vehicleForm.invalid || savingVehicle">
              {{ savingVehicle ? 'Registering...' : 'Register Vehicle' }}
            </button>
          </div>
        </form>
      </div>
    </ng-template>
  `,
  styles: [`
    .customer-parking-container {
      display: flex;
      flex-direction: column;
      gap: 28px;
      padding-bottom: 96px; /* space for bottom drawer */
    }

    .search-header {
      .header-titles {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .header-badge {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          background: #eff6ff;
          color: #1d4ed8;
          border: 1px solid #bfdbfe;
          padding: 4px 12px;
          border-radius: 9999px;
          font-size: 0.8125rem;
          font-weight: 700;
          width: fit-content;

          .material-symbols-outlined {
            font-size: 16px;
          }
        }

        h2 {
          margin: 0;
          font-size: 1.875rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.025em;
        }

        p {
          margin: 0;
          color: #64748b;
          font-size: 0.9375rem;
        }
      }
    }

    /* Criteria Card */
    .search-criteria-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-xl, 20px);
      padding: 24px;
      box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
      display: flex;
      flex-direction: column;
      gap: 18px;

      .criteria-card-title {
        display: flex;
        align-items: center;
        gap: 8px;

        .title-icon {
          color: #2563eb;
          font-size: 20px;
        }

        h3 {
          margin: 0;
          font-size: 1.0625rem;
          font-weight: 700;
          color: #0f172a;
        }
      }

      .criteria-grid {
        display: grid;
        grid-template-columns: 1.8fr 1.2fr 1fr 1fr 1.6fr;
        gap: 16px;

        @media (max-width: 1200px) {
          grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        }

        .criteria-item {
          display: flex;
          flex-direction: column;
          gap: 6px;

          .control-label {
            font-size: 0.8125rem;
            font-weight: 700;
            color: #334155;
          }

          .label-with-duration,
          .label-with-action {
            display: flex;
            align-items: center;
            justify-content: space-between;

            .duration-badge {
              font-size: 0.75rem;
              font-weight: 700;
              color: #2563eb;
              background: #eff6ff;
              padding: 2px 8px;
              border-radius: 9999px;
            }

            .quick-add-link {
              background: transparent;
              border: none;
              color: #2563eb;
              font-size: 0.75rem;
              font-weight: 700;
              cursor: pointer;
              padding: 0;

              &:hover {
                text-decoration: underline;
              }
            }
          }

          mat-form-field {
            width: 100%;

            ::ng-deep .mat-mdc-text-field-wrapper {
              background-color: #f8fafc;
              border-radius: 12px;
            }
          }

          .select-value-display {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 0.875rem;

            .select-icon {
              font-size: 18px;
              color: #2563eb;
            }

            .select-name {
              font-weight: 700;
              color: #0f172a;
            }

            .select-rate {
              font-size: 0.75rem;
              color: #64748b;
            }
          }

          .vehicle-select-value {
            display: flex;
            align-items: center;
            gap: 8px;

            .plate-badge {
              background: #0f172a;
              color: #ffffff;
              padding: 2px 6px;
              border-radius: 4px;
              font-family: monospace;
              font-weight: 700;
              font-size: 0.75rem;
            }

            .vehicle-model {
              font-size: 0.8125rem;
              font-weight: 600;
              color: #1e293b;
            }
          }

          .no-vehicle-alert {
            display: flex;
            align-items: center;
            gap: 8px;
            background: #fffbeb;
            border: 1px solid #fde68a;
            padding: 8px 12px;
            border-radius: 10px;
            font-size: 0.8125rem;
            color: #92400e;

            .alert-icon {
              font-size: 18px;
              color: #d97706;
            }

            .alert-action-btn {
              margin-left: auto;
              font-size: 0.75rem;
              padding: 0 8px;
            }
          }
        }
      }

      .duration-shortcuts {
        display: flex;
        align-items: center;
        gap: 8px;
        flex-wrap: wrap;
        padding-top: 10px;
        border-top: 1px solid #f1f5f9;

        .shortcuts-label {
          font-size: 0.8125rem;
          font-weight: 600;
          color: #64748b;
        }

        .preset-btn {
          padding: 6px 14px;
          border-radius: 9999px;
          border: 1px solid var(--border-light, #e2e8f0);
          background: #f8fafc;
          color: #475569;
          font-size: 0.8125rem;
          font-weight: 600;
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
            box-shadow: 0 2px 6px rgba(37, 99, 235, 0.25);
          }
        }
      }
    }

    /* Facility Strip */
    .facility-strip {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 16px;

      .strip-card {
        background: #ffffff;
        border: 2px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-lg, 16px);
        padding: 16px 20px;
        cursor: pointer;
        transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
        display: flex;
        flex-direction: column;
        gap: 8px;

        &:hover {
          border-color: #3b82f6;
          transform: translateY(-2px);
          box-shadow: 0 8px 16px rgba(37, 99, 235, 0.08);
        }

        &.active {
          border-color: #2563eb;
          background: #f8faff;
          box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.16);

          .strip-action {
            color: #2563eb;
            font-weight: 700;
          }
        }

        .strip-header {
          display: flex;
          justify-content: space-between;
          align-items: center;

          .strip-name {
            font-size: 1.05rem;
            font-weight: 800;
            color: #0f172a;
          }

          .strip-rate {
            font-size: 0.9375rem;
            font-weight: 800;
            color: #2563eb;
          }
        }

        .strip-footer {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding-top: 8px;
          border-top: 1px solid #f1f5f9;

          .strip-slots {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 0.8125rem;
            color: #475569;

            .dot {
              width: 8px;
              height: 8px;
              border-radius: 50%;
              background: #10b981;

              &.low {
                background: #f59e0b;
              }
            }
          }

          .strip-action {
            font-size: 0.75rem;
            color: #94a3b8;
          }
        }
      }
    }

    /* Slots Workspace */
    .slots-workspace {
      display: flex;
      flex-direction: column;
      gap: 18px;

      .workspace-header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 20px;
        flex-wrap: wrap;

        .workspace-left {
          h3 {
            margin: 0 0 4px 0;
            font-size: 1.35rem;
            font-weight: 800;
            color: #0f172a;
          }

          p {
            margin: 0;
            color: #64748b;
            font-size: 0.875rem;
          }
        }

        /* Status Legend (Available, Reserved, Occupied, Maintenance) */
        .status-legend-bar {
          display: flex;
          align-items: center;
          gap: 12px;
          flex-wrap: wrap;
          background: #ffffff;
          padding: 8px 16px;
          border-radius: 12px;
          border: 1px solid var(--border-light, #e2e8f0);

          .legend-item {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 0.75rem;
            font-weight: 600;

            .legend-color-box {
              width: 12px;
              height: 12px;
              border-radius: 3px;
            }

            .available-box {
              background: #10b981;
              box-shadow: 0 0 0 1px #059669;
            }

            .reserved-box {
              background: #f59e0b;
              box-shadow: 0 0 0 1px #d97706;
            }

            .occupied-box {
              background: #ef4444;
              box-shadow: 0 0 0 1px #dc2626;
            }

            .maintenance-box {
              background: #94a3b8;
              box-shadow: 0 0 0 1px #64748b;
            }

            &.legend-available { color: #065f46; }
            &.legend-reserved { color: #92400e; }
            &.legend-occupied { color: #991b1b; }
            &.legend-maintenance { color: #475569; }
          }
        }
      }

      .filter-controls-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;

        .floor-tabs {
          display: flex;
          align-items: center;
          gap: 6px;
          background: #f1f5f9;
          padding: 4px;
          border-radius: 12px;

          .floor-tab-btn {
            border: none;
            background: transparent;
            padding: 6px 14px;
            border-radius: 8px;
            font-size: 0.8125rem;
            font-weight: 700;
            color: #64748b;
            cursor: pointer;
            transition: all 0.15s ease;

            &:hover {
              color: #0f172a;
            }

            &.active {
              background: #ffffff;
              color: #2563eb;
              box-shadow: 0 2px 6px rgba(0, 0, 0, 0.06);
            }
          }
        }

        .type-filter-chips {
          display: flex;
          align-items: center;
          gap: 6px;
          flex-wrap: wrap;

          .type-chip {
            border: 1px solid var(--border-light, #e2e8f0);
            background: #ffffff;
            color: #64748b;
            padding: 6px 14px;
            border-radius: 9999px;
            font-size: 0.75rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.15s ease;

            &:hover {
              border-color: #cbd5e1;
              color: #0f172a;
            }

            &.active {
              background: #0f172a;
              color: #ffffff;
              border-color: #0f172a;
            }
          }
        }
      }

      .slots-grid-wrapper {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(210px, 1fr));
        gap: 16px;
      }
    }

    /* Fixed Bottom Reservation Drawer */
    .reservation-drawer {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      background: #ffffff;
      border-top: 1px solid #cbd5e1;
      box-shadow: 0 -8px 24px rgba(15, 23, 42, 0.12);
      padding: 16px 24px;
      z-index: 999;
      animation: slideUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);

      .drawer-inner {
        max-width: 1400px;
        margin: 0 auto;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 24px;
        flex-wrap: wrap;
      }

      .drawer-details {
        display: flex;
        align-items: center;
        gap: 20px;
        flex-wrap: wrap;

        .bay-tag {
          display: flex;
          flex-direction: column;
          background: #ecfdf5;
          border: 1px solid #a7f3d0;
          padding: 8px 14px;
          border-radius: 12px;

          .bay-num {
            font-size: 1.15rem;
            font-weight: 800;
            color: #065f46;
          }

          .bay-floor {
            font-size: 0.6875rem;
            font-weight: 700;
            color: #059669;
          }

          .bay-type {
            font-size: 0.625rem;
            color: #047857;
            text-transform: uppercase;
          }
        }

        .drawer-info-grid {
          display: flex;
          align-items: center;
          gap: 20px;
          flex-wrap: wrap;

          .info-cell {
            display: flex;
            flex-direction: column;
            gap: 2px;

            .cell-label {
              font-size: 0.6875rem;
              font-weight: 700;
              color: #64748b;
              text-transform: uppercase;
              letter-spacing: 0.04em;
            }

            .cell-val {
              font-size: 0.875rem;
              font-weight: 700;
              color: #0f172a;
            }
          }
        }
      }

      .drawer-actions {
        display: flex;
        align-items: center;
        gap: 20px;

        .price-breakdown {
          display: flex;
          flex-direction: column;
          align-items: flex-end;

          .price-sub {
            font-size: 0.75rem;
            color: #64748b;
          }

          .price-total {
            font-size: 1.5rem;
            font-weight: 800;
            color: #0f172a;
            line-height: 1;
          }
        }

        .confirm-booking-btn {
          height: 48px;
          padding: 0 24px;
          border-radius: 12px;
          font-weight: 700;
          font-size: 0.9375rem;
          display: flex;
          align-items: center;
          gap: 8px;
          box-shadow: 0 4px 14px rgba(37, 99, 235, 0.3);
        }
      }
    }

    @keyframes slideUp {
      from {
        transform: translateY(100%);
        opacity: 0;
      }
      to {
        transform: translateY(0);
        opacity: 1;
      }
    }

    /* Modal Form Styles */
    .modal-dialog-box {
      padding: 24px;
      display: flex;
      flex-direction: column;
      gap: 20px;

      .modal-header {
        display: flex;
        align-items: center;
        justify-content: space-between;

        h3 {
          margin: 0;
          font-size: 1.25rem;
          font-weight: 800;
          color: #0f172a;
        }

        .close-btn {
          border: none;
          background: transparent;
          color: #94a3b8;
          cursor: pointer;
        }
      }

      .modal-form {
        display: flex;
        flex-direction: column;
        gap: 12px;

        .form-row {
          display: flex;
          gap: 12px;
        }

        .modal-actions {
          display: flex;
          justify-content: flex-end;
          gap: 12px;
          margin-top: 12px;
        }
      }
    }

    .area-option-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      padding: 4px 0;

      .option-info {
        display: flex;
        flex-direction: column;
        .option-name { font-weight: 700; color: #0f172a; }
        .option-location { font-size: 0.75rem; color: #64748b; }
      }

      .option-metrics {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        .option-rate { font-weight: 700; color: #2563eb; }
        .option-capacity { font-size: 0.75rem; color: #059669; }
      }
    }

    .vehicle-option-row {
      display: flex;
      align-items: center;
      gap: 8px;

      .plate-badge {
        background: #0f172a;
        color: #ffffff;
        padding: 2px 6px;
        border-radius: 4px;
        font-family: monospace;
        font-weight: 700;
        font-size: 0.75rem;
      }

      .v-desc {
        font-size: 0.8125rem;
        color: #334155;
      }
    }
  `]
})
export class CustomerParkingComponent implements OnInit {
  private areaService = inject(ParkingAreaService);
  private slotService = inject(ParkingSlotService);
  private vehicleService = inject(VehicleService);
  private reservationService = inject(ReservationService);
  private notification = inject(NotificationService);
  private dialog = inject(MatDialog);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  // Search parameters
  areas: ParkingAreaDto[] = [];
  selectedAreaId?: string;
  selectedArea?: ParkingAreaDto;

  todayDateString: string = '';
  selectedDate: string = '';
  entryTime: string = '10:00';
  exitTime: string = '12:00';

  vehicles: VehicleDto[] = [];
  selectedVehicleId?: string;
  selectedVehicle?: VehicleDto;

  // Duration Presets
  selectedPresetHours: number = 2;
  durationPresets = [
    { label: '1 Hour', hours: 1 },
    { label: '2 Hours', hours: 2 },
    { label: '4 Hours', hours: 4 },
    { label: '8 Hours', hours: 8 },
    { label: 'All Day (12h)', hours: 12 }
  ];

  // Slots Data & Filtering
  slots: ParkingSlotDto[] = [];
  selectedSlot?: ParkingSlotDto;
  selectedFloor: number | null = null;
  selectedType: string = 'All';
  typeFilters = ['All', 'Standard', 'EV', 'VIP', 'Disabled'];

  loadingSlots: boolean = false;
  submitting: boolean = false;
  savingVehicle: boolean = false;

  vehicleForm: FormGroup = this.fb.group({
    licensePlate: ['', [Validators.required]],
    make: ['', [Validators.required]],
    model: ['', [Validators.required]],
    color: ['Silver', [Validators.required]],
    vehicleType: ['Sedan', [Validators.required]]
  });

  ngOnInit(): void {
    this.initScheduleTimes();
    this.loadData();
  }

  private initScheduleTimes(): void {
    const now = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');

    this.todayDateString = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
    this.selectedDate = this.todayDateString;

    // Default entry: next round hour
    const startHour = (now.getHours() + 1) % 24;
    const endHour = (startHour + this.selectedPresetHours) % 24;

    this.entryTime = `${pad(startHour)}:00`;
    this.exitTime = `${pad(endHour)}:00`;
  }

  loadData(): void {
    this.areaService.getAll().subscribe({
      next: (res) => {
        this.areas = res.data || [];
        if (this.areas.length > 0) {
          const areaParam = this.route.snapshot.queryParams['areaId'];
          const matched = this.areas.find(a => a.id === areaParam) || this.areas[0];
          this.onAreaSelected(matched.id);
        }
      }
    });

    this.loadVehicles();
  }

  loadVehicles(): void {
    this.vehicleService.getMyVehicles().subscribe({
      next: (res: any) => {
        this.vehicles = res.data || [];
        if (this.vehicles.length > 0) {
          this.selectedVehicleId = this.vehicles[0].id;
          this.selectedVehicle = this.vehicles[0];
        }
      }
    });
  }

  onAreaSelected(areaId: string): void {
    this.selectedAreaId = areaId;
    this.selectedArea = this.areas.find(a => a.id === areaId);
    this.selectedSlot = undefined;
    this.loadSlotsForArea(areaId);
  }

  loadSlotsForArea(areaId: string): void {
    this.loadingSlots = true;
    this.slotService.getAll(areaId).subscribe({
      next: (res) => {
        this.loadingSlots = false;
        this.slots = res.data || [];
      },
      error: () => {
        this.loadingSlots = false;
      }
    });
  }

  onVehicleSelected(vehicleId: string): void {
    this.selectedVehicleId = vehicleId;
    this.selectedVehicle = this.vehicles.find(v => v.id === vehicleId);
  }

  onScheduleChanged(): void {
    this.selectedPresetHours = 0; // custom interval
  }

  applyDurationPreset(hours: number): void {
    this.selectedPresetHours = hours;
    const [h, m] = this.entryTime.split(':').map(Number);
    const startMinutes = (h * 60) + (m || 0);
    const endMinutes = startMinutes + (hours * 60);

    const endH = Math.floor(endMinutes / 60) % 24;
    const endM = endMinutes % 60;
    const pad = (n: number) => n.toString().padStart(2, '0');

    this.exitTime = `${pad(endH)}:${pad(endM)}`;
  }

  get calculatedHours(): number {
    if (!this.selectedDate || !this.entryTime || !this.exitTime) return 1;
    const [sh, sm] = this.entryTime.split(':').map(Number);
    const [eh, em] = this.exitTime.split(':').map(Number);

    let startMinutes = (sh * 60) + sm;
    let endMinutes = (eh * 60) + em;

    if (endMinutes <= startMinutes) {
      endMinutes += 24 * 60; // wraps around to next day
    }

    const durationHrs = (endMinutes - startMinutes) / 60;
    return Math.max(0.5, durationHrs);
  }

  get calculatedTotal(): number {
    const rate = this.selectedSlot?.pricePerHour || this.selectedArea?.hourlyRate || 5;
    return rate * this.calculatedHours;
  }

  get availableFloors(): number[] {
    const floorsSet = new Set<number>();
    for (const slot of this.slots) {
      if (slot.floorNumber !== undefined && slot.floorNumber !== null) {
        floorsSet.add(slot.floorNumber);
      }
    }
    return Array.from(floorsSet).sort((a, b) => a - b);
  }

  getFloorSlotsCount(floor: number): number {
    return this.slots.filter(s => s.floorNumber === floor).length;
  }

  get filteredSlots(): ParkingSlotDto[] {
    return this.slots.filter(s => {
      // Floor filter
      if (this.selectedFloor !== null && s.floorNumber !== this.selectedFloor) {
        return false;
      }
      // Type filter
      if (this.selectedType !== 'All') {
        const t = (s.slotType || '').toLowerCase();
        const v = (s.vehicleType || '').toLowerCase();
        const q = this.selectedType.toLowerCase();
        if (!t.includes(q) && !v.includes(q)) {
          return false;
        }
      }
      return true;
    });
  }

  onSlotSelected(slot: ParkingSlotDto): void {
    if ((slot.status || '').toLowerCase() !== 'available') {
      return; // Customer can select only Available slots
    }
    this.selectedSlot = slot;
  }

  confirmReservation(): void {
    if (!this.selectedSlot || !this.selectedVehicle || !this.selectedArea) {
      this.notification.warning('Please select an available parking bay and assign a vehicle.');
      return;
    }

    this.submitting = true;

    const startDateTime = new Date(`${this.selectedDate}T${this.entryTime}:00`);
    let endDateTime = new Date(`${this.selectedDate}T${this.exitTime}:00`);

    if (endDateTime.getTime() <= startDateTime.getTime()) {
      // Add 1 day
      endDateTime = new Date(endDateTime.getTime() + 24 * 3600000);
    }

    const req = {
      parkingSlotId: this.selectedSlot.id,
      vehicleId: this.selectedVehicle.id,
      startTime: startDateTime.toISOString(),
      endTime: endDateTime.toISOString()
    };

    this.reservationService.create(req).subscribe({
      next: (res) => {
        this.submitting = false;
        this.notification.success(`Bay ${this.selectedSlot?.slotNumber} reserved! FastPass QR generated.`);
        const bookingId = res.data?.id;
        if (bookingId) {
          this.router.navigate(['/qr-code'], { queryParams: { id: bookingId } });
        } else {
          this.router.navigate(['/customer/reservations']);
        }
      },
      error: (err) => {
        this.submitting = false;
        this.notification.error('Reservation failed: ' + (err.error?.message || err.message));
      }
    });
  }

  openAddVehicleDialog(templateRef: any): void {
    this.vehicleForm.reset({
      licensePlate: '',
      make: '',
      model: '',
      color: 'Silver',
      vehicleType: 'Sedan'
    });
    this.dialog.open(templateRef, { width: '480px' });
  }

  saveVehicle(dialogRef: any): void {
    if (this.vehicleForm.invalid) return;

    this.savingVehicle = true;
    const req = {
      ...this.vehicleForm.value,
      licensePlate: (this.vehicleForm.value.licensePlate || '').toUpperCase().trim()
    };

    this.vehicleService.createVehicle(req).subscribe({
      next: (res) => {
        this.savingVehicle = false;
        this.notification.success('Vehicle registered successfully.');
        dialogRef.close();
        this.loadVehicles();
      },
      error: (err) => {
        this.savingVehicle = false;
        this.notification.error('Failed to register vehicle: ' + (err.error?.message || err.message));
      }
    });
  }
}
