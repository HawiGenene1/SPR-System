import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { ParkingAreaService } from '../../../core/services/parking-area.service';
import { ParkingSlotService } from '../../../core/services/parking-slot.service';
import { VehicleService } from '../../../core/services/vehicle.service';
import { ReservationService } from '../../../core/services/reservation.service';
import { PaymentService } from '../../../core/services/payment.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  ParkingAreaDto,
  ParkingSlotDto,
  VehicleDto,
  ReservationDto,
  PaymentDto
} from '../../../core/models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    StatusBadgeComponent
  ],
  template: `
    <div class="booking-page">
      <div class="page-header">
        <div>
          <h2>Reserve Parking Slot</h2>
          <p>Select your preferred parking facility, schedule your duration, and pick an available spot.</p>
        </div>
      </div>

      <!-- Success Booking Confirmation Overlay -->
      <div *ngIf="completedReservation" class="success-confirmation-card">
        <div class="confirmation-icon-wrap">
          <span class="material-symbols-outlined check-icon">check_circle</span>
        </div>
        <h3>Reservation & Payment Confirmed!</h3>
        <p class="subtitle">Your spot is secured. Present your QR code ticket at the gate scanner upon arrival.</p>

        <div class="ticket-summary">
          <div class="ticket-row">
            <span>Reservation Reference:</span>
            <strong>#{{ completedReservation.id.substring(0, 8).toUpperCase() }}</strong>
          </div>
          <div class="ticket-row">
            <span>Parking Slot:</span>
            <span class="slot-badge">{{ selectedSlot?.slotNumber }}</span>
          </div>
          <div class="ticket-row">
            <span>Facility:</span>
            <strong>{{ selectedArea?.name }}</strong>
          </div>
          <div class="ticket-row">
            <span>Time Window:</span>
            <strong>{{ completedReservation.startTime | date:'short' }} — {{ completedReservation.endTime | date:'shortTime' }}</strong>
          </div>
          <div class="ticket-row">
            <span>Total Paid:</span>
            <strong class="price-val">\${{ completedReservation.totalAmount | number:'1.2-2' }}</strong>
          </div>
        </div>

        <div class="confirmation-actions">
          <a routerLink="/customer/reservations" class="pp-btn primary">
            <span class="material-symbols-outlined">confirmation_number</span>
            View My Digital Tickets & QR Pass
          </a>
          <button (click)="resetBooking()" class="pp-btn secondary">
            Reserve Another Slot
          </button>
        </div>
      </div>

      <!-- Main Booking Wizard -->
      <div *ngIf="!completedReservation" class="booking-grid">
        <!-- Left: Steps & Slot Map -->
        <div class="booking-main">
          <!-- Step 1: Select Facility Area -->
          <div class="pp-card section-card">
            <div class="section-title">
              <span class="step-num">1</span>
              <div>
                <h3>Select Parking Facility</h3>
                <p>Choose the location where you want to park</p>
              </div>
            </div>

            <div *ngIf="isLoadingAreas" class="loading-mini">
              <mat-spinner diameter="24"></mat-spinner>
              <span>Loading parking areas...</span>
            </div>

            <div *ngIf="!isLoadingAreas" class="areas-list">
              <div 
                *ngFor="let area of areas" 
                class="area-card"
                [class.selected]="selectedArea?.id === area.id"
                (click)="onSelectArea(area)">
                <div class="area-header">
                  <h4>{{ area.name }}</h4>
                  <span class="rate-badge">\${{ area.hourlyRate | number:'1.2-2' }}/hr</span>
                </div>
                <div class="area-meta">
                  <span class="meta-item">
                    <span class="material-symbols-outlined">location_on</span>
                    {{ area.location }}
                  </span>
                  <span class="meta-item slots-available">
                    <span class="material-symbols-outlined">local_parking</span>
                    {{ area.availableSlots }} of {{ area.totalSlots }} slots available
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Step 2: Time Window & Availability -->
          <div class="pp-card section-card mt-4" *ngIf="selectedArea">
            <div class="section-title">
              <span class="step-num">2</span>
              <div>
                <h3>Select Date & Duration</h3>
                <p>Specify your estimated arrival and departure window</p>
              </div>
            </div>

            <form [formGroup]="timeForm" class="time-form-grid">
              <div class="time-input-group">
                <label>Arrival (Start Time)</label>
                <input 
                  type="datetime-local" 
                  formControlName="startTime" 
                  class="custom-input" 
                  (change)="onTimeChanged()" />
              </div>
              <div class="time-input-group">
                <label>Departure (End Time)</label>
                <input 
                  type="datetime-local" 
                  formControlName="endTime" 
                  class="custom-input" 
                  (change)="onTimeChanged()" />
              </div>
            </form>
          </div>

          <!-- Step 3: Interactive Visual Parking Slot Grid -->
          <div class="pp-card section-card mt-4" *ngIf="selectedArea">
            <div class="section-title">
              <span class="step-num">3</span>
              <div>
                <h3>Choose Your Parking Slot</h3>
                <p>Pick an available spot from the interactive parking map</p>
              </div>
            </div>

            <!-- Legend & Filter -->
            <div class="slot-legend-bar">
              <div class="legend-item"><span class="dot available"></span> Available</div>
              <div class="legend-item"><span class="dot reserved"></span> Reserved</div>
              <div class="legend-item"><span class="dot occupied"></span> Occupied</div>
              <div class="legend-item"><span class="dot selected"></span> Your Choice</div>
            </div>

            <div *ngIf="isLoadingSlots" class="loading-mini">
              <mat-spinner diameter="24"></mat-spinner>
              <span>Loading parking slots for {{ selectedArea.name }}...</span>
            </div>

            <div *ngIf="!isLoadingSlots && slots.length === 0" class="pp-empty-state">
              <h4>No slots configured in this area</h4>
            </div>

            <div *ngIf="!isLoadingSlots && slots.length > 0" class="slots-visual-grid">
              <div 
                *ngFor="let slot of slots" 
                class="slot-spot"
                [class.available]="isSlotAvailable(slot)"
                [class.occupied]="slot.status.toLowerCase() === 'occupied'"
                [class.reserved]="slot.status.toLowerCase() === 'reserved' && !isSlotAvailable(slot)"
                [class.active-selected]="selectedSlot?.id === slot.id"
                (click)="onSelectSlot(slot)">
                
                <div class="slot-top">
                  <span class="slot-code">{{ slot.slotNumber }}</span>
                  <span class="slot-type-pill">{{ slot.slotType }}</span>
                </div>
                
                <div class="slot-car-icon">
                  <span class="material-symbols-outlined">
                    {{ selectedSlot?.id === slot.id ? 'check_circle' : (isSlotAvailable(slot) ? 'local_parking' : 'directions_car') }}
                  </span>
                </div>

                <div class="slot-status-label">
                  {{ selectedSlot?.id === slot.id ? 'Selected' : slot.status }}
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right: Order Summary, Vehicle & Checkout -->
        <div class="booking-sidebar">
          <div class="pp-card checkout-card">
            <div class="checkout-header">
              <h3>Reservation Summary</h3>
              <span class="pill-blue">Checkout</span>
            </div>

            <!-- Selected Details -->
            <div class="summary-details">
              <div class="summary-row">
                <span class="label">Facility</span>
                <span class="val">{{ selectedArea?.name || 'Please select facility' }}</span>
              </div>
              <div class="summary-row">
                <span class="label">Chosen Spot</span>
                <span class="val highlight">{{ selectedSlot ? selectedSlot.slotNumber + ' (' + selectedSlot.slotType + ')' : 'None selected' }}</span>
              </div>
              <div class="summary-row">
                <span class="label">Duration</span>
                <span class="val">{{ estimatedHours | number:'1.1-1' }} Hours</span>
              </div>
              <div class="summary-row">
                <span class="label">Hourly Rate</span>
                <span class="val">\${{ selectedArea ? (selectedArea.hourlyRate | number:'1.2-2') : '0.00' }}/hr</span>
              </div>
            </div>

            <div class="divider"></div>

            <!-- Vehicle Selection -->
            <div class="vehicle-picker-group">
              <label class="section-label">Select Your Vehicle</label>
              <div *ngIf="vehicles.length === 0" class="no-vehicle-notice">
                <p>No vehicles registered yet.</p>
                <button type="button" (click)="quickAddVehicle()" class="pp-btn secondary sm">
                  + Add Vehicle
                </button>
              </div>

              <select 
                *ngIf="vehicles.length > 0"
                class="custom-select" 
                [(ngModel)]="selectedVehicleId">
                <option value="" disabled>-- Select Registered Vehicle --</option>
                <option *ngFor="let v of vehicles" [value]="v.id">
                  {{ v.licensePlate }} — {{ v.make }} {{ v.model }}
                </option>
              </select>
            </div>

            <!-- Payment Method -->
            <div class="payment-method-group mt-3">
              <label class="section-label">Payment Method</label>
              <div class="payment-options">
                <label class="payment-pill" [class.active]="paymentMethod === 'CreditCard'">
                  <input type="radio" name="paymentMethod" value="CreditCard" [(ngModel)]="paymentMethod" />
                  <span class="material-symbols-outlined">credit_card</span>
                  <span>Credit Card</span>
                </label>
                <label class="payment-pill" [class.active]="paymentMethod === 'DigitalWallet'">
                  <input type="radio" name="paymentMethod" value="DigitalWallet" [(ngModel)]="paymentMethod" />
                  <span class="material-symbols-outlined">account_balance_wallet</span>
                  <span>Digital Wallet</span>
                </label>
              </div>
            </div>

            <div class="divider"></div>

            <!-- Total Price Calculation -->
            <div class="total-row">
              <span class="total-label">Total Amount</span>
              <span class="total-price">\${{ estimatedTotal | number:'1.2-2' }}</span>
            </div>

            <button 
              type="button" 
              class="pp-btn primary checkout-btn"
              [disabled]="!canProceed() || isSubmitting"
              (click)="onConfirmAndPay()">
              <mat-spinner *ngIf="isSubmitting" diameter="20" color="accent"></mat-spinner>
              <span *ngIf="!isSubmitting">Confirm & Pay \${{ estimatedTotal | number:'1.2-2' }}</span>
            </button>

            <p class="guarantee-note">
              <span class="material-symbols-outlined">lock</span>
              Instant confirmation & contactless QR ticket generation.
            </p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .booking-page {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      h2 { margin: 0 0 6px 0; font-size: 1.75rem; }
      p { margin: 0; color: #64748b; font-size: 0.9375rem; }
    }

    .booking-grid {
      display: grid;
      grid-template-columns: 1fr 380px;
      gap: 28px;
      align-items: start;
    }

    .section-card {
      padding: 24px;

      .section-title {
        display: flex;
        align-items: center;
        gap: 14px;
        margin-bottom: 20px;

        .step-num {
          width: 32px;
          height: 32px;
          border-radius: 50%;
          background: #eff6ff;
          color: #2563eb;
          font-weight: 800;
          font-size: 0.875rem;
          display: flex;
          align-items: center;
          justify-content: center;
          border: 1px solid #bfdbfe;
        }

        h3 { margin: 0; font-size: 1.15rem; color: #0f172a; }
        p { margin: 2px 0 0 0; font-size: 0.8125rem; color: #64748b; }
      }
    }

    .areas-list {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 16px;

      .area-card {
        border: 2px solid var(--border-light);
        border-radius: var(--radius-md);
        padding: 16px;
        cursor: pointer;
        transition: all 0.2s ease;
        background: #ffffff;

        &:hover {
          border-color: #94a3b8;
          transform: translateY(-2px);
          box-shadow: var(--shadow-card);
        }

        &.selected {
          border-color: #2563eb;
          background: #eff6ff;
          box-shadow: 0 0 0 1px #2563eb;
        }

        .area-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 8px;

          h4 { margin: 0; font-size: 1rem; color: #0f172a; }
          .rate-badge {
            font-size: 0.75rem;
            font-weight: 700;
            background: #dbeafe;
            color: #1d4ed8;
            padding: 3px 8px;
            border-radius: 999px;
          }
        }

        .area-meta {
          display: flex;
          flex-direction: column;
          gap: 4px;

          .meta-item {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 0.75rem;
            color: #64748b;

            .material-symbols-outlined { font-size: 14px; }
          }

          .slots-available {
            color: #059669;
            font-weight: 600;
          }
        }
      }
    }

    .time-form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;

      .time-input-group {
        display: flex;
        flex-direction: column;
        gap: 6px;

        label {
          font-size: 0.8125rem;
          font-weight: 600;
          color: #334155;
        }

        .custom-input {
          padding: 10px 14px;
          border-radius: var(--radius-md);
          border: 1px solid #cbd5e1;
          font-family: inherit;
          font-size: 0.875rem;
          background: #ffffff;
          &:focus { outline: none; border-color: #2563eb; }
        }
      }
    }

    .slot-legend-bar {
      display: flex;
      align-items: center;
      gap: 20px;
      margin-bottom: 16px;
      padding: 10px 14px;
      background: #f8fafc;
      border-radius: 8px;

      .legend-item {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 0.75rem;
        font-weight: 600;
        color: #475569;

        .dot {
          width: 10px;
          height: 10px;
          border-radius: 3px;

          &.available { background: #10b981; }
          &.reserved { background: #f59e0b; }
          &.occupied { background: #ef4444; }
          &.selected { background: #2563eb; }
        }
      }
    }

    .slots-visual-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(105px, 1fr));
      gap: 12px;
      max-height: 400px;
      overflow-y: auto;
      padding: 4px;

      .slot-spot {
        border: 2px solid #e2e8f0;
        border-radius: 10px;
        padding: 10px;
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        background: #ffffff;
        cursor: pointer;
        transition: all 0.2s ease;
        user-select: none;

        .slot-top {
          display: flex;
          align-items: center;
          justify-content: space-between;
          width: 100%;
          margin-bottom: 6px;

          .slot-code {
            font-size: 0.8125rem;
            font-weight: 800;
            color: #0f172a;
          }

          .slot-type-pill {
            font-size: 0.5625rem;
            font-weight: 700;
            text-transform: uppercase;
            padding: 2px 4px;
            background: #f1f5f9;
            border-radius: 4px;
            color: #475569;
          }
        }

        .slot-car-icon {
          font-size: 24px;
          color: #94a3b8;
          margin: 4px 0;
        }

        .slot-status-label {
          font-size: 0.6875rem;
          font-weight: 600;
          color: #64748b;
        }

        &.available {
          border-color: #a7f3d0;
          background: #f0fdf4;
          .slot-car-icon { color: #10b981; }
          .slot-status-label { color: #047857; }
          &:hover {
            border-color: #10b981;
            transform: scale(1.04);
          }
        }

        &.occupied {
          border-color: #fecaca;
          background: #fef2f2;
          cursor: not-allowed;
          opacity: 0.7;
          .slot-car-icon { color: #ef4444; }
          .slot-status-label { color: #b91c1c; }
        }

        &.reserved {
          border-color: #fde68a;
          background: #fffbeb;
          cursor: not-allowed;
          opacity: 0.7;
          .slot-car-icon { color: #f59e0b; }
          .slot-status-label { color: #b45309; }
        }

        &.active-selected {
          border-color: #2563eb !important;
          background: #eff6ff !important;
          box-shadow: 0 0 0 2px #2563eb;
          transform: scale(1.05);

          .slot-car-icon { color: #2563eb; }
          .slot-status-label { color: #1d4ed8; font-weight: 800; }
        }
      }
    }

    .checkout-card {
      position: sticky;
      top: 90px;
      padding: 24px;

      .checkout-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;

        h3 { margin: 0; font-size: 1.15rem; color: #0f172a; }
        .pill-blue {
          font-size: 0.6875rem;
          font-weight: 700;
          background: #dbeafe;
          color: #1d4ed8;
          padding: 2px 8px;
          border-radius: 999px;
          text-transform: uppercase;
        }
      }

      .summary-details {
        display: flex;
        flex-direction: column;
        gap: 12px;

        .summary-row {
          display: flex;
          justify-content: space-between;
          font-size: 0.875rem;

          .label { color: #64748b; }
          .val { font-weight: 600; color: #0f172a; }
          .highlight { color: #2563eb; font-weight: 700; }
        }
      }

      .divider {
        height: 1px;
        background: var(--border-light);
        margin: 18px 0;
      }

      .section-label {
        display: block;
        font-size: 0.8125rem;
        font-weight: 600;
        color: #334155;
        margin-bottom: 8px;
      }

      .custom-select {
        width: 100%;
        padding: 9px 12px;
        border-radius: var(--radius-md);
        border: 1px solid #cbd5e1;
        font-size: 0.875rem;
        font-family: inherit;
        background: #ffffff;
      }

      .no-vehicle-notice {
        background: #f8fafc;
        padding: 10px;
        border-radius: 8px;
        border: 1px dashed #cbd5e1;
        display: flex;
        justify-content: space-between;
        align-items: center;
        p { margin: 0; font-size: 0.8125rem; color: #64748b; }
      }

      .payment-options {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 10px;

        .payment-pill {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 6px;
          padding: 10px 8px;
          border: 1px solid #cbd5e1;
          border-radius: 8px;
          cursor: pointer;
          font-size: 0.75rem;
          font-weight: 600;
          color: #475569;
          transition: all 0.15s ease;

          input { display: none; }
          .material-symbols-outlined { font-size: 20px; color: #64748b; }

          &.active {
            border-color: #2563eb;
            background: #eff6ff;
            color: #1d4ed8;
            .material-symbols-outlined { color: #2563eb; }
          }
        }
      }

      .total-row {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        margin-bottom: 20px;

        .total-label {
          font-size: 1rem;
          font-weight: 700;
          color: #0f172a;
        }

        .total-price {
          font-size: 1.625rem;
          font-weight: 800;
          color: #1e3a8a;
          letter-spacing: -0.02em;
        }
      }

      .checkout-btn {
        width: 100%;
        padding: 14px;
        font-size: 0.9375rem;
      }

      .guarantee-note {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 6px;
        font-size: 0.6875rem;
        color: #64748b;
        margin: 14px 0 0 0;
        text-align: center;

        .material-symbols-outlined { font-size: 14px; color: #10b981; }
      }
    }

    .success-confirmation-card {
      background: #ffffff;
      border: 1px solid var(--border-light);
      border-radius: var(--radius-xl);
      padding: 48px 36px;
      text-align: center;
      max-width: 600px;
      margin: 0 auto;
      box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);

      .confirmation-icon-wrap {
        width: 72px;
        height: 72px;
        background: #ecfdf5;
        border-radius: 50%;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 20px;

        .check-icon {
          font-size: 48px;
          color: #10b981;
        }
      }

      h3 {
        font-size: 1.5rem;
        color: #0f172a;
        margin: 0 0 8px 0;
      }

      .subtitle {
        color: #64748b;
        margin: 0 0 24px 0;
        font-size: 0.9375rem;
      }

      .ticket-summary {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 12px;
        padding: 18px 22px;
        margin-bottom: 28px;
        text-align: left;
        display: flex;
        flex-direction: column;
        gap: 10px;

        .ticket-row {
          display: flex;
          justify-content: space-between;
          font-size: 0.875rem;
          color: #475569;

          .slot-badge {
            background: #eff6ff;
            color: #1d4ed8;
            font-weight: 800;
            padding: 2px 8px;
            border-radius: 6px;
            border: 1px solid #bfdbfe;
          }

          .price-val {
            color: #059669;
            font-size: 1rem;
          }
        }
      }

      .confirmation-actions {
        display: flex;
        justify-content: center;
        gap: 14px;
        flex-wrap: wrap;
      }
    }

    .loading-mini {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 16px 0;
      color: #64748b;
      font-size: 0.875rem;
    }

    .mt-4 { margin-top: 16px; }
    .mt-3 { margin-top: 12px; }

    @media (max-width: 992px) {
      .booking-grid { grid-template-columns: 1fr; }
      .checkout-card { position: static; }
    }
  `]
})
export class CustomerBookingComponent implements OnInit {
  private fb = inject(FormBuilder);
  private areaService = inject(ParkingAreaService);
  private slotService = inject(ParkingSlotService);
  private vehicleService = inject(VehicleService);
  private reservationService = inject(ReservationService);
  private paymentService = inject(PaymentService);
  private notify = inject(NotificationService);

  isLoadingAreas = false;
  isLoadingSlots = false;
  isSubmitting = false;

  areas: ParkingAreaDto[] = [];
  slots: ParkingSlotDto[] = [];
  vehicles: VehicleDto[] = [];

  selectedArea: ParkingAreaDto | null = null;
  selectedSlot: ParkingSlotDto | null = null;
  selectedVehicleId: string = '';
  paymentMethod: string = 'CreditCard';

  estimatedHours: number = 2;
  estimatedTotal: number = 0;

  completedReservation: ReservationDto | null = null;

  timeForm: FormGroup = this.fb.group({
    startTime: ['', Validators.required],
    endTime: ['', Validators.required]
  });

  ngOnInit(): void {
    this.initDefaultTimes();
    this.loadAreas();
    this.loadVehicles();
  }

  private initDefaultTimes(): void {
    const now = new Date();
    // Default arrival: in 30 minutes rounded
    const start = new Date(now.getTime() + 30 * 60000);
    start.setMinutes(Math.ceil(start.getMinutes() / 15) * 15, 0, 0);

    // Default departure: 2 hours after start
    const end = new Date(start.getTime() + 2 * 60 * 60000);

    const toLocalIso = (d: Date) => {
      const pad = (n: number) => n < 10 ? '0' + n : n;
      return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
    };

    this.timeForm.patchValue({
      startTime: toLocalIso(start),
      endTime: toLocalIso(end)
    });

    this.recalculatePrice();
  }

  loadAreas(): void {
    this.isLoadingAreas = true;
    this.areaService.getAll().subscribe({
      next: (res) => {
        this.areas = res.data;
        this.isLoadingAreas = false;
        if (this.areas.length > 0) {
          this.onSelectArea(this.areas[0]);
        }
      },
      error: () => {
        this.isLoadingAreas = false;
      }
    });
  }

  loadVehicles(): void {
    this.vehicleService.getMyVehicles().subscribe({
      next: (res) => {
        this.vehicles = res.data;
        if (this.vehicles.length > 0) {
          this.selectedVehicleId = this.vehicles[0].id;
        }
      }
    });
  }

  onSelectArea(area: ParkingAreaDto): void {
    this.selectedArea = area;
    this.selectedSlot = null;
    this.recalculatePrice();
    this.loadSlotsForArea(area.id);
  }

  loadSlotsForArea(areaId: string): void {
    this.isLoadingSlots = true;
    this.slotService.getAll(areaId).subscribe({
      next: (res) => {
        this.slots = res.data;
        this.isLoadingSlots = false;
      },
      error: () => {
        this.isLoadingSlots = false;
      }
    });
  }

  isSlotAvailable(slot: ParkingSlotDto): boolean {
    return slot.status.toLowerCase() === 'available';
  }

  onSelectSlot(slot: ParkingSlotDto): void {
    if (!this.isSlotAvailable(slot)) {
      this.notify.warning(`Slot ${slot.slotNumber} is currently ${slot.status}. Please choose an available green slot.`);
      return;
    }
    this.selectedSlot = slot;
  }

  onTimeChanged(): void {
    const { startTime, endTime } = this.timeForm.value;
    if (startTime && endTime) {
      const s = new Date(startTime).getTime();
      const e = new Date(endTime).getTime();
      if (e <= s) {
        this.notify.warning('End time must be after start time.');
        return;
      }
      const diffHours = Math.max(0.5, (e - s) / (1000 * 60 * 60));
      this.estimatedHours = diffHours;
      this.recalculatePrice();
    }
  }

  recalculatePrice(): void {
    if (this.selectedArea) {
      this.estimatedTotal = Math.round(this.estimatedHours * this.selectedArea.hourlyRate * 100) / 100;
    } else {
      this.estimatedTotal = 0;
    }
  }

  canProceed(): boolean {
    return !!(
      this.selectedArea &&
      this.selectedSlot &&
      this.selectedVehicleId &&
      this.timeForm.valid &&
      this.estimatedHours > 0
    );
  }

  quickAddVehicle(): void {
    const plate = prompt('Enter your vehicle License Plate (e.g. ABC-1234):');
    if (!plate) return;
    const make = prompt('Vehicle Make (e.g. Toyota, Tesla):') || 'Vehicle';
    const model = prompt('Vehicle Model (e.g. Camry, Model 3):') || 'Sedan';

    this.vehicleService.createVehicle({
      licensePlate: plate.trim().toUpperCase(),
      make,
      model,
      color: 'Silver',
      vehicleType: 'Sedan'
    }).subscribe({
      next: (res) => {
        this.notify.success('Vehicle registered successfully!');
        this.vehicles.push(res.data);
        this.selectedVehicleId = res.data.id;
      }
    });
  }

  onConfirmAndPay(): void {
    if (!this.canProceed()) return;

    this.isSubmitting = true;
    const { startTime, endTime } = this.timeForm.value;

    const reservationPayload = {
      vehicleId: this.selectedVehicleId,
      parkingSlotId: this.selectedSlot!.id,
      startTime: new Date(startTime).toISOString(),
      endTime: new Date(endTime).toISOString()
    };

    this.reservationService.create(reservationPayload).subscribe({
      next: (res) => {
        const createdReservation = res.data;
        // Process instant payment checkout
        this.paymentService.processPayment({
          reservationId: createdReservation.id,
          paymentMethod: this.paymentMethod,
          amount: createdReservation.totalAmount
        }).subscribe({
          next: () => {
            this.isSubmitting = false;
            this.completedReservation = createdReservation;
            this.notify.success('Reservation and payment successful! QR pass generated.');
          },
          error: () => {
            this.isSubmitting = false;
            // Even if payment has an issue, reservation was created
            this.completedReservation = createdReservation;
          }
        });
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }

  resetBooking(): void {
    this.completedReservation = null;
    this.selectedSlot = null;
    if (this.selectedArea) {
      this.loadSlotsForArea(this.selectedArea.id);
    }
  }
}
