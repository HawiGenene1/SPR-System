import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatRadioModule } from '@angular/material/radio';
import { ParkingAreaService } from '../../../core/services/parking-area.service';
import { ParkingSlotService } from '../../../core/services/parking-slot.service';
import { VehicleService } from '../../../core/services/vehicle.service';
import { ReservationService } from '../../../core/services/reservation.service';
import { PaymentService } from '../../../core/services/payment.service';
import { QrService } from '../../../core/services/qr.service';
import { NotificationService } from '../../../core/services/notification.service';
import { 
  ParkingAreaDto, 
  ParkingSlotDto, 
  VehicleDto, 
  ReservationDto, 
  PaymentDto, 
  QrCodeDto 
} from '../../../core/models';
import { ParkingSlotCardComponent } from '../../../shared/components/parking-slot-card/parking-slot-card.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

export interface WizardStep {
  number: number;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-reservation-create',
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
    MatRadioModule,
    ParkingSlotCardComponent, 
    LoadingSpinnerComponent, 
    EmptyStateComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="wizard-container">
      <!-- Top Breadcrumb & Title -->
      <div class="wizard-top-nav">
        <a routerLink="/customer/reservations" class="back-link">
          <span class="material-symbols-outlined">arrow_back</span>
          <span>Back to Reservations</span>
        </a>
        <div class="nav-title-group">
          <h2>Reservation Booking Wizard</h2>
          <p>Complete 7 simple steps to select your facility, reserve a bay, settle payment, and obtain your contactless FastPass QR.</p>
        </div>
      </div>

      <!-- 7-Step Progress Indicator (Stepper) -->
      <div class="stepper-card">
        <div class="stepper-track">
          <div 
            class="stepper-progress-fill" 
            [style.width.%]="((currentStep - 1) / (steps.length - 1)) * 100">
          </div>
        </div>

        <div class="stepper-nodes">
          <div 
            *ngFor="let s of steps" 
            class="step-node"
            [class.active]="currentStep === s.number"
            [class.completed]="currentStep > s.number"
            [class.clickable]="s.number < currentStep || isStepAccessible(s.number)"
            (click)="goToStepIfAllowed(s.number)">
            
            <div class="node-circle">
              <span class="material-symbols-outlined check-icon" *ngIf="currentStep > s.number">check</span>
              <span class="node-num" *ngIf="currentStep <= s.number">{{ s.number }}</span>
            </div>
            <div class="node-text">
              <span class="node-label">{{ s.label }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Main Two-Column Layout -->
      <div class="wizard-layout">
        <!-- Left: Dynamic Step Viewport -->
        <div class="wizard-step-content">

          <!-- ========================================== -->
          <!-- STEP 1: PARKING AREA                       -->
          <!-- ========================================== -->
          <div class="step-card" *ngIf="currentStep === 1">
            <div class="step-card-header">
              <div class="step-badge">Step 1 of 7</div>
              <h3>Select Parking Facility</h3>
              <p>Choose the commercial garage or outdoor parking facility you wish to park at.</p>
            </div>

            <!-- Search Filter Bar -->
            <div class="area-search-box">
              <span class="material-symbols-outlined">search</span>
              <input 
                type="text" 
                [(ngModel)]="areaSearchQuery" 
                placeholder="Search facility by name, landmark, or location..."
                class="search-input">
            </div>

            <app-loading-spinner *ngIf="loadingAreas" message="Loading parking facilities..."></app-loading-spinner>

            <div class="areas-grid" *ngIf="!loadingAreas && filteredAreas.length > 0">
              <div 
                *ngFor="let area of filteredAreas" 
                class="area-card"
                [class.selected]="selectedArea?.id === area.id"
                (click)="selectArea(area)">
                
                <div class="area-card-top">
                  <div class="area-name-block">
                    <h4>{{ area.name }}</h4>
                    <span class="area-address">{{ area.location }}</span>
                  </div>
                  <div class="area-rate-tag">
                    <span class="rate-val">\${{ area.hourlyRate | number:'1.2-2' }}</span>
                    <span class="rate-unit">/hr</span>
                  </div>
                </div>

                <div class="area-capacity-bar">
                  <div class="capacity-track">
                    <div 
                      class="capacity-fill" 
                      [style.width.%]="(area.availableSlots / (area.totalSlots || 1)) * 100">
                    </div>
                  </div>
                  <div class="capacity-stats">
                    <span class="slots-avail"><strong>{{ area.availableSlots }}</strong> available</span>
                    <span class="slots-total">{{ area.totalSlots }} total bays</span>
                  </div>
                </div>

                <div class="area-card-footer">
                  <span class="select-indicator">
                    <span class="material-symbols-outlined">
                      {{ selectedArea?.id === area.id ? 'check_circle' : 'radio_button_unchecked' }}
                    </span>
                    <span>{{ selectedArea?.id === area.id ? 'Facility Selected' : 'Click to Select' }}</span>
                  </span>
                </div>
              </div>
            </div>

            <app-empty-state 
              *ngIf="!loadingAreas && filteredAreas.length === 0"
              icon="domain_disabled"
              title="No facilities found"
              description="No parking facilities match your search criteria.">
            </app-empty-state>

            <div class="step-actions">
              <div></div>
              <button 
                mat-flat-button 
                color="primary" 
                class="nav-btn next-btn"
                [disabled]="!selectedArea"
                (click)="nextStep()">
                <span>Continue to Vehicle</span>
                <span class="material-symbols-outlined">arrow_forward</span>
              </button>
            </div>
          </div>

          <!-- ========================================== -->
          <!-- STEP 2: VEHICLE                            -->
          <!-- ========================================== -->
          <div class="step-card" *ngIf="currentStep === 2">
            <div class="step-card-header">
              <div class="step-badge">Step 2 of 7</div>
              <h3>Select Vehicle</h3>
              <p>Pick the registered vehicle you will be driving, or quickly add a new vehicle.</p>
            </div>

            <app-loading-spinner *ngIf="loadingVehicles" message="Loading your vehicles..."></app-loading-spinner>

            <div class="vehicles-section" *ngIf="!loadingVehicles">
              <div class="vehicles-grid" *ngIf="vehicles.length > 0">
                <div 
                  *ngFor="let v of vehicles" 
                  class="vehicle-select-card"
                  [class.selected]="selectedVehicle?.id === v.id"
                  (click)="selectVehicle(v)">
                  
                  <div class="v-card-top">
                    <div class="plate-badge">
                      <span class="state-label">USA</span>
                      <span class="plate-number">{{ v.licensePlate }}</span>
                    </div>
                    <span class="material-symbols-outlined check-icon">
                      {{ selectedVehicle?.id === v.id ? 'check_circle' : 'radio_button_unchecked' }}
                    </span>
                  </div>

                  <div class="v-details">
                    <h4 class="v-model">{{ v.make }} {{ v.model }}</h4>
                    <div class="v-meta">
                      <span class="v-color">{{ v.color }}</span>
                      <span class="v-type-badge">{{ v.vehicleType }}</span>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Inline Quick Register Vehicle Box -->
              <div class="add-vehicle-inline-card">
                <div class="inline-header" (click)="showAddVehicleForm = !showAddVehicleForm">
                  <div class="inline-title">
                    <span class="material-symbols-outlined">add_circle</span>
                    <span>Register a New Vehicle</span>
                  </div>
                  <span class="material-symbols-outlined toggle-chevron">
                    {{ showAddVehicleForm ? 'expand_less' : 'expand_more' }}
                  </span>
                </div>

                <div class="inline-form-body" *ngIf="showAddVehicleForm">
                  <form [formGroup]="vehicleForm" (ngSubmit)="registerVehicleInline()">
                    <div class="form-row-grid">
                      <mat-form-field appearance="outline">
                        <mat-label>License Plate</mat-label>
                        <input matInput formControlName="licensePlate" placeholder="ABC-1234" style="text-transform: uppercase;">
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Vehicle Type</mat-label>
                        <mat-select formControlName="vehicleType">
                          <mat-option value="Sedan">Sedan / Standard Car</mat-option>
                          <mat-option value="SUV">SUV / Crossover</mat-option>
                          <mat-option value="Electric">Electric Vehicle (EV)</mat-option>
                          <mat-option value="Motorcycle">Motorcycle</mat-option>
                          <mat-option value="Truck">Truck</mat-option>
                        </mat-select>
                      </mat-form-field>
                    </div>

                    <div class="form-row-grid">
                      <mat-form-field appearance="outline">
                        <mat-label>Make</mat-label>
                        <input matInput formControlName="make" placeholder="Tesla, Toyota...">
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Model</mat-label>
                        <input matInput formControlName="model" placeholder="Model 3, RAV4...">
                      </mat-form-field>
                    </div>

                    <div class="form-row-actions">
                      <button 
                        mat-flat-button 
                        color="primary" 
                        type="submit" 
                        [disabled]="vehicleForm.invalid || savingVehicle">
                        {{ savingVehicle ? 'Registering...' : 'Save & Select Vehicle' }}
                      </button>
                    </div>
                  </form>
                </div>
              </div>
            </div>

            <div class="step-actions">
              <button mat-stroked-button class="nav-btn prev-btn" (click)="prevStep()">
                <span class="material-symbols-outlined">arrow_back</span>
                <span>Back</span>
              </button>
              <button 
                mat-flat-button 
                color="primary" 
                class="nav-btn next-btn"
                [disabled]="!selectedVehicle"
                (click)="nextStep()">
                <span>Continue to Date & Time</span>
                <span class="material-symbols-outlined">arrow_forward</span>
              </button>
            </div>
          </div>

          <!-- ========================================== -->
          <!-- STEP 3: DATE AND TIME                      -->
          <!-- ========================================== -->
          <div class="step-card" *ngIf="currentStep === 3">
            <div class="step-card-header">
              <div class="step-badge">Step 3 of 7</div>
              <h3>Select Date & Parking Schedule</h3>
              <p>Specify your arrival date, entry time, and anticipated departure time.</p>
            </div>

            <div class="schedule-form-container">
              <!-- Date Selector -->
              <div class="form-field-block">
                <label class="block-label">Reservation Date</label>
                <mat-form-field appearance="outline" class="w-full">
                  <input 
                    matInput 
                    type="date" 
                    [(ngModel)]="selectedDate" 
                    [min]="todayDateString"
                    (change)="onScheduleFieldChanged()">
                </mat-form-field>
              </div>

              <!-- Time Inputs Grid -->
              <div class="time-inputs-row">
                <div class="form-field-block">
                  <label class="block-label">Entry Time (Arrival)</label>
                  <mat-form-field appearance="outline" class="w-full">
                    <input 
                      matInput 
                      type="time" 
                      [(ngModel)]="entryTime"
                      (change)="onScheduleFieldChanged()">
                  </mat-form-field>
                </div>

                <div class="form-field-block">
                  <label class="block-label">Exit Time (Departure)</label>
                  <mat-form-field appearance="outline" class="w-full">
                    <input 
                      matInput 
                      type="time" 
                      [(ngModel)]="exitTime"
                      (change)="onScheduleFieldChanged()">
                  </mat-form-field>
                </div>
              </div>

              <!-- Duration Shortcuts -->
              <div class="duration-presets-block">
                <span class="presets-title">Quick Duration Shortcuts:</span>
                <div class="presets-buttons">
                  <button 
                    type="button"
                    *ngFor="let p of durationPresets" 
                    class="preset-chip"
                    [class.active]="selectedPresetHours === p.hours"
                    (click)="applyPreset(p.hours)">
                    {{ p.label }}
                  </button>
                </div>
              </div>

              <!-- Schedule Metrics Summary Box -->
              <div class="schedule-stat-box" [class.has-error]="!isScheduleValid">
                <div class="stat-left">
                  <span class="material-symbols-outlined stat-icon">
                    {{ isScheduleValid ? 'schedule' : 'error' }}
                  </span>
                  <div>
                    <span class="stat-title">{{ isScheduleValid ? 'Selected Duration' : 'Invalid Schedule Interval' }}</span>
                    <span class="stat-desc">
                      {{ isScheduleValid ? (calculatedDurationHours | number:'1.1-1') + ' hours (' + entryTime + ' to ' + exitTime + ')' : scheduleErrorMessage }}
                    </span>
                  </div>
                </div>
                <div class="stat-right" *ngIf="isScheduleValid && selectedArea">
                  <span class="est-label">Estimated:</span>
                  <span class="est-price">\${{ estimatedPrice | number:'1.2-2' }}</span>
                </div>
              </div>
            </div>

            <div class="step-actions">
              <button mat-stroked-button class="nav-btn prev-btn" (click)="prevStep()">
                <span class="material-symbols-outlined">arrow_back</span>
                <span>Back</span>
              </button>
              <button 
                mat-flat-button 
                color="primary" 
                class="nav-btn next-btn"
                [disabled]="!isScheduleValid"
                (click)="onScheduleCompleted()">
                <span>Continue to Parking Slot</span>
                <span class="material-symbols-outlined">arrow_forward</span>
              </button>
            </div>
          </div>

          <!-- ========================================== -->
          <!-- STEP 4: PARKING SLOT                       -->
          <!-- ========================================== -->
          <div class="step-card" *ngIf="currentStep === 4">
            <div class="step-card-header">
              <div class="step-badge">Step 4 of 7</div>
              <h3>Select Available Parking Slot</h3>
              <p>Choose an available slot. Only green "Available" slots can be selected.</p>
            </div>

            <!-- Filters Bar (Floor, Type, Legend) -->
            <div class="slot-filters-bar">
              <!-- Floor Filter -->
              <div class="floor-pills" *ngIf="floorsList.length > 1">
                <button 
                  class="pill-btn" 
                  [class.active]="selectedFloor === null"
                  (click)="selectedFloor = null">
                  All Floors
                </button>
                <button 
                  *ngFor="let f of floorsList" 
                  class="pill-btn"
                  [class.active]="selectedFloor === f"
                  (click)="selectedFloor = f">
                  Floor {{ f }}
                </button>
              </div>

              <!-- Status Color Legend -->
              <div class="status-legend">
                <span class="legend-chip green">
                  <span class="dot"></span> Available
                </span>
                <span class="legend-chip yellow">
                  <span class="dot"></span> Reserved
                </span>
                <span class="legend-chip red">
                  <span class="dot"></span> Occupied
                </span>
                <span class="legend-chip gray">
                  <span class="dot"></span> Maintenance
                </span>
              </div>
            </div>

            <app-loading-spinner *ngIf="loadingSlots" message="Retrieving real-time bay telemetry..."></app-loading-spinner>

            <div class="slots-layout-grid" *ngIf="!loadingSlots && filteredSlots.length > 0">
              <app-parking-slot-card
                *ngFor="let slot of filteredSlots"
                [slot]="slot"
                [selectable]="true"
                [selected]="selectedSlot?.id === slot.id"
                [hourlyRate]="selectedArea?.hourlyRate"
                [showHourlyRate]="true"
                (slotSelect)="onSlotSelected(slot)">
              </app-parking-slot-card>
            </div>

            <app-empty-state 
              *ngIf="!loadingSlots && filteredSlots.length === 0"
              icon="garage"
              title="No bays found"
              description="No slots match your selected floor or category.">
            </app-empty-state>

            <div class="step-actions">
              <button mat-stroked-button class="nav-btn prev-btn" (click)="prevStep()">
                <span class="material-symbols-outlined">arrow_back</span>
                <span>Back</span>
              </button>
              <button 
                mat-flat-button 
                color="primary" 
                class="nav-btn next-btn"
                [disabled]="!selectedSlot"
                (click)="nextStep()">
                <span>Review Summary</span>
                <span class="material-symbols-outlined">arrow_forward</span>
              </button>
            </div>
          </div>

          <!-- ========================================== -->
          <!-- STEP 5: RESERVATION SUMMARY               -->
          <!-- ========================================== -->
          <div class="step-card" *ngIf="currentStep === 5">
            <div class="step-card-header">
              <div class="step-badge">Step 5 of 7</div>
              <h3>Reservation Summary & Authoritative Pricing</h3>
              <p>Review your booking details. The official authoritative price is calculated strictly by the backend engine.</p>
            </div>

            <div class="summary-breakdown-card">
              <!-- Item 1: Facility -->
              <div class="summary-row">
                <div class="row-icon-col">
                  <span class="material-symbols-outlined">domain</span>
                </div>
                <div class="row-content">
                  <span class="row-label">Facility</span>
                  <span class="row-val">{{ selectedArea?.name }}</span>
                  <span class="row-sub">{{ selectedArea?.location }}</span>
                </div>
              </div>

              <!-- Item 2: Bay -->
              <div class="summary-row">
                <div class="row-icon-col">
                  <span class="material-symbols-outlined">local_parking</span>
                </div>
                <div class="row-content">
                  <span class="row-label">Reserved Bay</span>
                  <span class="row-val">Bay {{ selectedSlot?.slotNumber }} (Floor {{ selectedSlot?.floorNumber || 1 }})</span>
                  <span class="row-sub">{{ selectedSlot?.slotType }} Classification</span>
                </div>
              </div>

              <!-- Item 3: Vehicle -->
              <div class="summary-row">
                <div class="row-icon-col">
                  <span class="material-symbols-outlined">directions_car</span>
                </div>
                <div class="row-content">
                  <span class="row-label">Vehicle</span>
                  <span class="row-val font-mono">{{ selectedVehicle?.licensePlate }}</span>
                  <span class="row-sub">{{ selectedVehicle?.make }} {{ selectedVehicle?.model }} ({{ selectedVehicle?.vehicleType }})</span>
                </div>
              </div>

              <!-- Item 4: Schedule Interval -->
              <div class="summary-row">
                <div class="row-icon-col">
                  <span class="material-symbols-outlined">schedule</span>
                </div>
                <div class="row-content">
                  <span class="row-label">Schedule Window</span>
                  <span class="row-val">{{ selectedDate | date:'fullDate' }}</span>
                  <span class="row-sub">{{ entryTime }} – {{ exitTime }} ({{ calculatedDurationHours | number:'1.1-1' }} hrs)</span>
                </div>
              </div>
            </div>

            <!-- Server-Side Source of Truth Price Card -->
            <div class="authoritative-price-box">
              <div class="auth-box-header">
                <div class="auth-badge">
                  <span class="material-symbols-outlined">verified</span>
                  <span>{{ backendReservation ? 'Backend Source of Truth' : 'Pre-Confirmation Estimate' }}</span>
                </div>
                <span class="auth-policy-note">Authoritative price determined by server engine</span>
              </div>

              <div class="auth-price-body">
                <div class="price-line">
                  <span>Facility Base Rate:</span>
                  <span>\${{ selectedArea?.hourlyRate | number:'1.2-2' }} / hr</span>
                </div>
                <div class="price-line">
                  <span>Total Duration:</span>
                  <span>{{ calculatedDurationHours | number:'1.1-1' }} hours</span>
                </div>
                <div class="divider"></div>
                <div class="price-line total-highlight">
                  <span>Authoritative Amount Due:</span>
                  <span class="big-amount">
                    \${{ (backendReservation?.totalAmount || estimatedPrice) | number:'1.2-2' }}
                  </span>
                </div>
              </div>

              <div class="server-status-pill" *ngIf="backendReservation">
                <span class="indicator-dot"></span>
                <span>Reservation ID: #{{ backendReservation.id.slice(0, 8).toUpperCase() }} (Status: {{ backendReservation.status }})</span>
              </div>
            </div>

            <div class="step-actions">
              <button 
                mat-stroked-button 
                class="nav-btn prev-btn" 
                [disabled]="lockingInReservation" 
                (click)="prevStep()">
                <span class="material-symbols-outlined">arrow_back</span>
                <span>Back</span>
              </button>

              <!-- If not yet created in backend, lock it in -->
              <button 
                *ngIf="!backendReservation"
                mat-flat-button 
                color="primary" 
                class="nav-btn next-btn"
                [disabled]="lockingInReservation"
                (click)="lockInReservationWithBackend()">
                <span class="material-symbols-outlined">lock</span>
                <span>{{ lockingInReservation ? 'Calculating Server Price...' : 'Lock In & Authorize Price' }}</span>
              </button>

              <!-- If already verified by backend, proceed to payment -->
              <button 
                *ngIf="backendReservation"
                mat-flat-button 
                color="primary" 
                class="nav-btn next-btn"
                (click)="nextStep()">
                <span>Proceed to Payment</span>
                <span class="material-symbols-outlined">payment</span>
              </button>
            </div>
          </div>

          <!-- ========================================== -->
          <!-- STEP 6: PAYMENT                            -->
          <!-- ========================================== -->
          <div class="step-card" *ngIf="currentStep === 6">
            <div class="step-card-header">
              <div class="step-badge">Step 6 of 7</div>
              <h3>Secure Payment & Settlement</h3>
              <p>Settle the backend-authorized fee to confirm your reservation and generate your entry QR pass.</p>
            </div>

            <!-- Amount Due Banner -->
            <div class="amount-due-banner">
              <div class="banner-texts">
                <span class="banner-sub">Authoritative Total Due</span>
                <span class="banner-val">\${{ backendReservation?.totalAmount | number:'1.2-2' }}</span>
              </div>
              <div class="banner-secure">
                <span class="material-symbols-outlined">lock</span>
                <span>256-Bit Encrypted</span>
              </div>
            </div>

            <!-- Payment Method Selection -->
            <div class="payment-methods-grid">
              <div 
                class="method-card" 
                [class.active]="selectedPaymentMethod === 'CreditCard'"
                (click)="selectedPaymentMethod = 'CreditCard'">
                <span class="material-symbols-outlined method-icon">credit_card</span>
                <span class="method-name">Credit Card</span>
              </div>

              <div 
                class="method-card" 
                [class.active]="selectedPaymentMethod === 'DebitCard'"
                (click)="selectedPaymentMethod = 'DebitCard'">
                <span class="material-symbols-outlined method-icon">account_balance_wallet</span>
                <span class="method-name">Debit Card</span>
              </div>

              <div 
                class="method-card" 
                [class.active]="selectedPaymentMethod === 'DigitalWallet'"
                (click)="selectedPaymentMethod = 'DigitalWallet'">
                <span class="material-symbols-outlined method-icon">smartphone</span>
                <span class="method-name">Digital Wallet</span>
              </div>

              <div 
                class="method-card" 
                [class.active]="selectedPaymentMethod === 'Cash'"
                (click)="selectedPaymentMethod = 'Cash'">
                <span class="material-symbols-outlined method-icon">payments</span>
                <span class="method-name">Cash at Gate</span>
              </div>
            </div>

            <!-- Card Simulation Fields -->
            <div class="card-details-form" *ngIf="selectedPaymentMethod === 'CreditCard' || selectedPaymentMethod === 'DebitCard'">
              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Cardholder Name</mat-label>
                <input matInput [(ngModel)]="paymentForm.cardholderName" placeholder="Full name as on card">
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Card Number</mat-label>
                <input matInput [(ngModel)]="paymentForm.cardNumber" placeholder="•••• •••• •••• 4242" maxlength="19">
              </mat-form-field>

              <div class="card-split-row">
                <mat-form-field appearance="outline">
                  <mat-label>Expiry (MM/YY)</mat-label>
                  <input matInput [(ngModel)]="paymentForm.expiry" placeholder="12/28" maxlength="5">
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>CVC / CVV</mat-label>
                  <input matInput [(ngModel)]="paymentForm.cvv" placeholder="•••" maxlength="4">
                </mat-form-field>
              </div>
            </div>

            <div class="step-actions">
              <button mat-stroked-button class="nav-btn prev-btn" [disabled]="processingPayment" (click)="prevStep()">
                <span class="material-symbols-outlined">arrow_back</span>
                <span>Back</span>
              </button>

              <button 
                mat-flat-button 
                color="primary" 
                class="nav-btn next-btn pay-action-btn"
                [disabled]="processingPayment"
                (click)="processPayment()">
                <span class="material-symbols-outlined">check_circle</span>
                <span>
                  {{ processingPayment ? 'Processing Settlement...' : 'Authorize & Pay \$' + ((backendReservation?.totalAmount || estimatedPrice) | number:'1.2-2') }}
                </span>
              </button>
            </div>
          </div>

          <!-- ========================================== -->
          <!-- STEP 7: QR CODE FASTPASS                  -->
          <!-- ========================================== -->
          <div class="step-card" *ngIf="currentStep === 7">
            <div class="success-banner-top">
              <div class="success-circle">
                <span class="material-symbols-outlined">check</span>
              </div>
              <h3>Reservation Confirmed & Paid!</h3>
              <p>Your contactless FastPass entry barcode is activated and ready for gate arrival.</p>
            </div>

            <!-- Digital FastPass Ticket Card -->
            <div class="fastpass-ticket-card" id="printable-fastpass">
              <div class="ticket-header">
                <div class="ticket-brand">
                  <span class="material-symbols-outlined brand-logo">local_parking</span>
                  <span class="brand-text">ParkPulse FastPass</span>
                </div>
                <span class="pass-active-pill">VALID & ACTIVE</span>
              </div>

              <div class="ticket-body">
                <!-- Visual QR Code Box -->
                <div class="ticket-qr-col">
                  <div class="qr-code-frame">
                    <img 
                      *ngIf="qrCodeData?.imageData" 
                      [src]="'data:image/png;base64,' + qrCodeData?.imageData" 
                      alt="FastPass QR Code"
                      class="qr-img">
                    
                    <div *ngIf="!qrCodeData?.imageData" class="qr-placeholder-box">
                      <span class="material-symbols-outlined qr-big-icon">qr_code_2</span>
                      <span class="qr-token-str">{{ qrCodeData?.code || ('PASS-' + (backendReservation?.id?.slice(0, 8)?.toUpperCase() || 'VALID')) }}</span>
                    </div>
                  </div>
                  <span class="scan-instruction">Scan at gate barrier sensor</span>
                </div>

                <!-- Ticket Details -->
                <div class="ticket-meta-col">
                  <div class="meta-row">
                    <span class="meta-label">Ticket ID</span>
                    <span class="meta-val font-mono">TICKET-{{ (backendReservation?.id || '').slice(0, 8).toUpperCase() }}</span>
                  </div>

                  <div class="meta-row">
                    <span class="meta-label">Assigned Bay</span>
                    <span class="meta-val highlight-val">Bay {{ selectedSlot?.slotNumber }} (Floor {{ selectedSlot?.floorNumber || 1 }})</span>
                  </div>

                  <div class="meta-row">
                    <span class="meta-label">Facility</span>
                    <span class="meta-val">{{ selectedArea?.name }}</span>
                  </div>

                  <div class="meta-row">
                    <span class="meta-label">Vehicle</span>
                    <span class="meta-val font-mono">{{ selectedVehicle?.licensePlate }} ({{ selectedVehicle?.make }})</span>
                  </div>

                  <div class="meta-row">
                    <span class="meta-label">Schedule Window</span>
                    <span class="meta-val">{{ selectedDate | date:'mediumDate' }}, {{ entryTime }} – {{ exitTime }}</span>
                  </div>

                  <div class="meta-row">
                    <span class="meta-label">Amount Paid</span>
                    <span class="meta-val text-green font-mono">\${{ backendReservation?.totalAmount | number:'1.2-2' }} (Paid)</span>
                  </div>
                </div>
              </div>

              <div class="ticket-footer-strip">
                <span class="material-symbols-outlined">info</span>
                <span>Keep this pass accessible on your phone or printed for automated gate entry.</span>
              </div>
            </div>

            <!-- Post-Booking Actions -->
            <div class="post-booking-actions">
              <button mat-stroked-button class="action-btn" (click)="printTicket()">
                <span class="material-symbols-outlined">print</span>
                <span>Print FastPass</span>
              </button>

              <a routerLink="/customer/reservations" mat-flat-button color="primary" class="action-btn primary">
                <span class="material-symbols-outlined">confirmation_number</span>
                <span>View My Reservations</span>
              </a>

              <a routerLink="/customer/dashboard" mat-button class="action-btn">
                <span>Go to Dashboard</span>
              </a>
            </div>
          </div>
        </div>

        <!-- Right: Persistent Price & Selection Summary Panel -->
        <div class="wizard-summary-col">
          <div class="summary-sticky-card">
            <div class="summary-header">
              <span class="material-symbols-outlined">receipt_long</span>
              <h4>Price & Booking Summary</h4>
            </div>

            <div class="summary-items-list">
              <div class="summary-item">
                <span class="item-label">Facility</span>
                <span class="item-val">{{ selectedArea ? selectedArea.name : 'Not selected' }}</span>
              </div>

              <div class="summary-item">
                <span class="item-label">Vehicle</span>
                <span class="item-val">{{ selectedVehicle ? selectedVehicle.licensePlate : 'Not selected' }}</span>
              </div>

              <div class="summary-item">
                <span class="item-label">Schedule</span>
                <span class="item-val" *ngIf="selectedDate">
                  {{ selectedDate | date:'MMM d' }} ({{ entryTime }} - {{ exitTime }})
                </span>
                <span class="item-val" *ngIf="!selectedDate">Not selected</span>
              </div>

              <div class="summary-item">
                <span class="item-label">Assigned Bay</span>
                <span class="item-val" *ngIf="selectedSlot">
                  Bay {{ selectedSlot.slotNumber }} (Floor {{ selectedSlot.floorNumber || 1 }})
                </span>
                <span class="item-val" *ngIf="!selectedSlot">Not selected</span>
              </div>
            </div>

            <div class="summary-divider"></div>

            <!-- Price Computation Box -->
            <div class="price-breakdown-box">
              <div class="price-row">
                <span class="label">Hourly Rate:</span>
                <span class="val">\${{ (selectedArea?.hourlyRate || 0) | number:'1.2-2' }}/hr</span>
              </div>
              <div class="price-row">
                <span class="label">Estimated Duration:</span>
                <span class="val">{{ calculatedDurationHours | number:'1.1-1' }} hrs</span>
              </div>

              <div class="price-divider"></div>

              <!-- Price State: Estimated vs Authoritative -->
              <div class="final-price-row">
                <div class="price-title-group">
                  <span class="price-type-label">
                    {{ backendReservation ? 'Authoritative Total' : 'Estimated Total' }}
                  </span>
                  <span class="price-source-note">
                    {{ backendReservation ? '✓ Confirmed by Server' : 'Angular Estimate' }}
                  </span>
                </div>
                <span class="price-figure" [class.verified]="!!backendReservation">
                  \${{ (backendReservation?.totalAmount || estimatedPrice) | number:'1.2-2' }}
                </span>
              </div>
            </div>

            <div class="trust-notice">
              <span class="material-symbols-outlined">security</span>
              <span>Final authoritative pricing is calculated on the server. Zero hidden fees.</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .wizard-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
      max-width: 1320px;
      margin: 0 auto;
    }

    .wizard-top-nav {
      display: flex;
      flex-direction: column;
      gap: 8px;

      .back-link {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        color: #2563eb;
        font-weight: 600;
        font-size: 0.875rem;
        text-decoration: none;

        &:hover {
          text-decoration: underline;
        }
      }

      .nav-title-group {
        h2 {
          margin: 0;
          font-size: 1.875rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.025em;
        }

        p {
          margin: 4px 0 0 0;
          color: #64748b;
          font-size: 0.9375rem;
        }
      }
    }

    /* Stepper Progress Bar */
    .stepper-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-xl, 20px);
      padding: 24px 32px;
      position: relative;
      box-shadow: 0 4px 12px rgba(15, 23, 42, 0.03);

      .stepper-track {
        position: absolute;
        top: 42px;
        left: 60px;
        right: 60px;
        height: 4px;
        background: #f1f5f9;
        z-index: 1;

        .stepper-progress-fill {
          height: 100%;
          background: #2563eb;
          transition: width 0.3s cubic-bezier(0.16, 1, 0.3, 1);
        }
      }

      .stepper-nodes {
        position: relative;
        z-index: 2;
        display: flex;
        justify-content: space-between;

        .step-node {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 8px;
          cursor: default;

          &.clickable {
            cursor: pointer;
          }

          .node-circle {
            width: 38px;
            height: 38px;
            border-radius: 50%;
            background: #ffffff;
            border: 3px solid #e2e8f0;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: 800;
            font-size: 0.875rem;
            color: #64748b;
            transition: all 0.2s ease;

            .check-icon {
              font-size: 18px;
            }
          }

          .node-text {
            .node-label {
              font-size: 0.75rem;
              font-weight: 700;
              color: #64748b;
              white-space: nowrap;
            }
          }

          &.active {
            .node-circle {
              border-color: #2563eb;
              background: #2563eb;
              color: #ffffff;
              box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.2);
            }

            .node-text .node-label {
              color: #2563eb;
              font-weight: 800;
            }
          }

          &.completed {
            .node-circle {
              border-color: #2563eb;
              background: #eff6ff;
              color: #2563eb;
            }

            .node-text .node-label {
              color: #1e293b;
            }
          }
        }
      }
    }

    /* Layout */
    .wizard-layout {
      display: grid;
      grid-template-columns: 1fr 340px;
      gap: 24px;
      align-items: start;

      @media (max-width: 992px) {
        grid-template-columns: 1fr;
      }
    }

    .step-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-xl, 20px);
      padding: 32px;
      box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
      display: flex;
      flex-direction: column;
      gap: 24px;

      .step-card-header {
        .step-badge {
          display: inline-block;
          font-size: 0.75rem;
          font-weight: 800;
          color: #2563eb;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          margin-bottom: 6px;
        }

        h3 {
          margin: 0 0 4px 0;
          font-size: 1.4rem;
          font-weight: 800;
          color: #0f172a;
        }

        p {
          margin: 0;
          color: #64748b;
          font-size: 0.9375rem;
        }
      }
    }

    /* Areas Grid (Step 1) */
    .area-search-box {
      display: flex;
      align-items: center;
      gap: 10px;
      background: #f8fafc;
      border: 2px solid #e2e8f0;
      border-radius: 12px;
      padding: 0 16px;
      height: 48px;

      .material-symbols-outlined {
        color: #94a3b8;
      }

      .search-input {
        flex: 1;
        border: none;
        background: transparent;
        outline: none;
        font-size: 0.9375rem;
        color: #0f172a;
      }
    }

    .areas-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 16px;

      .area-card {
        border: 2px solid #e2e8f0;
        border-radius: 16px;
        padding: 20px;
        cursor: pointer;
        transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
        display: flex;
        flex-direction: column;
        gap: 14px;

        &:hover {
          border-color: #93c5fd;
          transform: translateY(-2px);
          box-shadow: 0 6px 16px rgba(37, 99, 235, 0.08);
        }

        &.selected {
          border-color: #2563eb;
          background: #f8faff;
          box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.15);

          .select-indicator {
            color: #2563eb;
            font-weight: 700;
          }
        }

        .area-card-top {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;

          .area-name-block {
            h4 {
              margin: 0 0 2px 0;
              font-size: 1.05rem;
              font-weight: 800;
              color: #0f172a;
            }

            .area-address {
              font-size: 0.8125rem;
              color: #64748b;
            }
          }

          .area-rate-tag {
            text-align: right;
            .rate-val {
              font-size: 1.1rem;
              font-weight: 800;
              color: #2563eb;
            }
            .rate-unit {
              font-size: 0.75rem;
              color: #64748b;
            }
          }
        }

        .area-capacity-bar {
          display: flex;
          flex-direction: column;
          gap: 6px;

          .capacity-track {
            height: 6px;
            background: #f1f5f9;
            border-radius: 9999px;
            overflow: hidden;

            .capacity-fill {
              height: 100%;
              background: #10b981;
            }
          }

          .capacity-stats {
            display: flex;
            justify-content: space-between;
            font-size: 0.75rem;

            .slots-avail {
              color: #059669;
            }

            .slots-total {
              color: #94a3b8;
            }
          }
        }

        .area-card-footer {
          border-top: 1px solid #f1f5f9;
          padding-top: 10px;

          .select-indicator {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 0.8125rem;
            color: #94a3b8;

            .material-symbols-outlined {
              font-size: 18px;
            }
          }
        }
      }
    }

    /* Vehicles (Step 2) */
    .vehicles-section {
      display: flex;
      flex-direction: column;
      gap: 20px;

      .vehicles-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
        gap: 16px;

        .vehicle-select-card {
          border: 2px solid #e2e8f0;
          border-radius: 16px;
          padding: 18px;
          cursor: pointer;
          transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
          display: flex;
          flex-direction: column;
          gap: 12px;

          &:hover {
            border-color: #93c5fd;
            transform: translateY(-2px);
          }

          &.selected {
            border-color: #2563eb;
            background: #f8faff;
            box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.15);

            .check-icon {
              color: #2563eb;
            }
          }

          .v-card-top {
            display: flex;
            justify-content: space-between;
            align-items: center;

            .plate-badge {
              display: inline-flex;
              align-items: center;
              gap: 6px;
              background: #0f172a;
              color: #ffffff;
              padding: 4px 10px;
              border-radius: 6px;
              font-family: monospace;
              font-weight: 800;
              font-size: 0.875rem;

              .state-label {
                font-size: 0.625rem;
                color: #94a3b8;
              }
            }

            .check-icon {
              color: #cbd5e1;
              font-size: 22px;
            }
          }

          .v-details {
            .v-model {
              margin: 0 0 4px 0;
              font-size: 1.05rem;
              font-weight: 700;
              color: #0f172a;
            }

            .v-meta {
              display: flex;
              align-items: center;
              gap: 8px;
              font-size: 0.75rem;

              .v-color {
                color: #64748b;
              }

              .v-type-badge {
                background: #f1f5f9;
                padding: 2px 8px;
                border-radius: 4px;
                font-weight: 600;
                color: #334155;
              }
            }
          }
        }
      }

      .add-vehicle-inline-card {
        border: 2px dashed #cbd5e1;
        border-radius: 16px;
        padding: 16px 20px;
        background: #f8fafc;

        .inline-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          cursor: pointer;

          .inline-title {
            display: flex;
            align-items: center;
            gap: 8px;
            font-weight: 700;
            color: #2563eb;
            font-size: 0.9375rem;
          }
        }

        .inline-form-body {
          margin-top: 16px;
          padding-top: 16px;
          border-top: 1px solid #e2e8f0;

          .form-row-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 12px;
          }

          .form-row-actions {
            display: flex;
            justify-content: flex-end;
          }
        }
      }
    }

    /* Schedule (Step 3) */
    .schedule-form-container {
      display: flex;
      flex-direction: column;
      gap: 20px;

      .form-field-block {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .block-label {
          font-size: 0.8125rem;
          font-weight: 700;
          color: #334155;
        }
      }

      .time-inputs-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px;
      }

      .duration-presets-block {
        display: flex;
        flex-direction: column;
        gap: 8px;

        .presets-title {
          font-size: 0.8125rem;
          font-weight: 700;
          color: #64748b;
        }

        .presets-buttons {
          display: flex;
          gap: 8px;
          flex-wrap: wrap;

          .preset-chip {
            padding: 8px 16px;
            border-radius: 9999px;
            border: 1px solid #cbd5e1;
            background: #ffffff;
            color: #475569;
            font-size: 0.8125rem;
            font-weight: 600;
            cursor: pointer;

            &:hover {
              border-color: #2563eb;
              color: #2563eb;
            }

            &.active {
              background: #2563eb;
              color: #ffffff;
              border-color: #2563eb;
            }
          }
        }
      }

      .schedule-stat-box {
        display: flex;
        align-items: center;
        justify-content: space-between;
        background: #eff6ff;
        border: 1px solid #bfdbfe;
        border-radius: 12px;
        padding: 16px 20px;

        &.has-error {
          background: #fef2f2;
          border-color: #fecaca;

          .stat-icon { color: #dc2626; }
          .stat-title { color: #991b1b; }
          .stat-desc { color: #b91c1c; }
        }

        .stat-left {
          display: flex;
          align-items: center;
          gap: 12px;

          .stat-icon {
            font-size: 28px;
            color: #2563eb;
          }

          .stat-title {
            display: block;
            font-size: 0.875rem;
            font-weight: 700;
            color: #1e3a8a;
          }

          .stat-desc {
            font-size: 0.8125rem;
            color: #3b82f6;
          }
        }

        .stat-right {
          display: flex;
          flex-direction: column;
          align-items: flex-end;

          .est-label {
            font-size: 0.75rem;
            color: #64748b;
          }

          .est-price {
            font-size: 1.25rem;
            font-weight: 800;
            color: #0f172a;
          }
        }
      }
    }

    /* Slots (Step 4) */
    .slot-filters-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;

      .floor-pills {
        display: flex;
        gap: 6px;

        .pill-btn {
          border: 1px solid #e2e8f0;
          background: #ffffff;
          padding: 6px 14px;
          border-radius: 8px;
          font-size: 0.8125rem;
          font-weight: 600;
          cursor: pointer;

          &.active {
            background: #0f172a;
            color: #ffffff;
            border-color: #0f172a;
          }
        }
      }

      .status-legend {
        display: flex;
        align-items: center;
        gap: 12px;

        .legend-chip {
          display: flex;
          align-items: center;
          gap: 5px;
          font-size: 0.75rem;
          font-weight: 600;

          .dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
          }

          &.green .dot { background: #10b981; }
          &.yellow .dot { background: #f59e0b; }
          &.red .dot { background: #ef4444; }
          &.gray .dot { background: #94a3b8; }
        }
      }
    }

    .slots-layout-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 16px;
    }

    /* Summary (Step 5) */
    .summary-breakdown-card {
      border: 1px solid #e2e8f0;
      border-radius: 16px;
      background: #f8fafc;
      overflow: hidden;

      .summary-row {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 16px 20px;
        border-bottom: 1px solid #edf2f7;

        &:last-child {
          border-bottom: none;
        }

        .row-icon-col {
          width: 40px;
          height: 40px;
          border-radius: 10px;
          background: #ffffff;
          border: 1px solid #e2e8f0;
          display: flex;
          align-items: center;
          justify-content: center;
          color: #2563eb;
        }

        .row-content {
          display: flex;
          flex-direction: column;

          .row-label {
            font-size: 0.6875rem;
            font-weight: 700;
            text-transform: uppercase;
            color: #64748b;
          }

          .row-val {
            font-size: 1rem;
            font-weight: 700;
            color: #0f172a;
          }

          .row-sub {
            font-size: 0.75rem;
            color: #64748b;
          }
        }
      }
    }

    .authoritative-price-box {
      background: #eff6ff;
      border: 2px solid #bfdbfe;
      border-radius: 16px;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 12px;

      .auth-box-header {
        display: flex;
        align-items: center;
        justify-content: space-between;

        .auth-badge {
          display: flex;
          align-items: center;
          gap: 6px;
          background: #2563eb;
          color: #ffffff;
          padding: 4px 10px;
          border-radius: 9999px;
          font-size: 0.75rem;
          font-weight: 700;

          .material-symbols-outlined { font-size: 16px; }
        }

        .auth-policy-note {
          font-size: 0.75rem;
          color: #64748b;
        }
      }

      .auth-price-body {
        display: flex;
        flex-direction: column;
        gap: 8px;

        .price-line {
          display: flex;
          justify-content: space-between;
          font-size: 0.875rem;
          color: #334155;

          &.total-highlight {
            font-size: 1.125rem;
            font-weight: 800;
            color: #0f172a;

            .big-amount {
              font-size: 1.625rem;
              color: #2563eb;
            }
          }
        }

        .divider {
          height: 1px;
          background: #bfdbfe;
          margin: 4px 0;
        }
      }

      .server-status-pill {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 0.75rem;
        font-weight: 600;
        color: #065f46;
        background: #ecfdf5;
        border: 1px solid #a7f3d0;
        padding: 4px 10px;
        border-radius: 6px;

        .indicator-dot {
          width: 6px;
          height: 6px;
          border-radius: 50%;
          background: #10b981;
        }
      }
    }

    /* Payment (Step 6) */
    .amount-due-banner {
      background: #0f172a;
      color: #ffffff;
      padding: 24px;
      border-radius: 16px;
      display: flex;
      align-items: center;
      justify-content: space-between;

      .banner-texts {
        display: flex;
        flex-direction: column;

        .banner-sub {
          font-size: 0.8125rem;
          color: #94a3b8;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .banner-val {
          font-size: 2.25rem;
          font-weight: 800;
          color: #38bdf8;
        }
      }

      .banner-secure {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 0.8125rem;
        color: #94a3b8;
      }
    }

    .payment-methods-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 12px;

      @media (max-width: 768px) {
        grid-template-columns: 1fr 1fr;
      }

      .method-card {
        border: 2px solid #e2e8f0;
        border-radius: 12px;
        padding: 16px;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 8px;
        cursor: pointer;
        transition: all 0.15s ease;

        &:hover {
          border-color: #93c5fd;
        }

        &.active {
          border-color: #2563eb;
          background: #eff6ff;
          color: #2563eb;

          .method-name { font-weight: 800; }
        }

        .method-icon {
          font-size: 28px;
        }

        .method-name {
          font-size: 0.8125rem;
          font-weight: 600;
        }
      }
    }

    .card-details-form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      background: #f8fafc;
      padding: 20px;
      border-radius: 16px;
      border: 1px solid #e2e8f0;

      .card-split-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;
      }
    }

    /* QR Code Ticket (Step 7) */
    .success-banner-top {
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;

      .success-circle {
        width: 56px;
        height: 56px;
        border-radius: 50%;
        background: #ecfdf5;
        border: 2px solid #10b981;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #059669;

        .material-symbols-outlined {
          font-size: 32px;
        }
      }

      h3 {
        margin: 0;
        font-size: 1.5rem;
        font-weight: 800;
        color: #0f172a;
      }

      p {
        margin: 0;
        color: #64748b;
        font-size: 0.9375rem;
      }
    }

    .fastpass-ticket-card {
      background: #ffffff;
      border: 2px solid #0f172a;
      border-radius: 20px;
      overflow: hidden;
      box-shadow: 0 10px 25px rgba(15, 23, 42, 0.08);

      .ticket-header {
        background: #0f172a;
        color: #ffffff;
        padding: 16px 24px;
        display: flex;
        justify-content: space-between;
        align-items: center;

        .ticket-brand {
          display: flex;
          align-items: center;
          gap: 8px;
          font-weight: 800;
          font-size: 1.05rem;

          .brand-logo { color: #38bdf8; }
        }

        .pass-active-pill {
          background: #10b981;
          color: #ffffff;
          padding: 3px 10px;
          border-radius: 9999px;
          font-size: 0.6875rem;
          font-weight: 800;
          letter-spacing: 0.05em;
        }
      }

      .ticket-body {
        padding: 24px;
        display: grid;
        grid-template-columns: 200px 1fr;
        gap: 24px;

        @media (max-width: 600px) {
          grid-template-columns: 1fr;
        }

        .ticket-qr-col {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 8px;

          .qr-code-frame {
            width: 180px;
            height: 180px;
            background: #ffffff;
            border: 2px solid #e2e8f0;
            border-radius: 12px;
            padding: 8px;
            display: flex;
            align-items: center;
            justify-content: center;

            .qr-img {
              max-width: 100%;
              max-height: 100%;
            }

            .qr-placeholder-box {
              display: flex;
              flex-direction: column;
              align-items: center;
              gap: 4px;

              .qr-big-icon {
                font-size: 100px;
                color: #0f172a;
              }

              .qr-token-str {
                font-family: monospace;
                font-size: 0.75rem;
                font-weight: 700;
                color: #475569;
              }
            }
          }

          .scan-instruction {
            font-size: 0.6875rem;
            font-weight: 700;
            color: #64748b;
            text-align: center;
          }
        }

        .ticket-meta-col {
          display: flex;
          flex-direction: column;
          gap: 10px;

          .meta-row {
            display: flex;
            justify-content: space-between;
            padding-bottom: 6px;
            border-bottom: 1px dashed #e2e8f0;

            .meta-label {
              font-size: 0.75rem;
              font-weight: 600;
              color: #64748b;
            }

            .meta-val {
              font-size: 0.875rem;
              font-weight: 700;
              color: #0f172a;

              &.highlight-val {
                color: #2563eb;
              }
            }
          }
        }
      }

      .ticket-footer-strip {
        background: #f8fafc;
        border-top: 1px solid #e2e8f0;
        padding: 10px 24px;
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 0.75rem;
        color: #475569;

        .material-symbols-outlined {
          font-size: 16px;
          color: #2563eb;
        }
      }
    }

    .post-booking-actions {
      display: flex;
      justify-content: center;
      gap: 16px;
      flex-wrap: wrap;

      .action-btn {
        height: 44px;
        padding: 0 20px;
        border-radius: 10px;
        font-weight: 700;
        display: flex;
        align-items: center;
        gap: 6px;
      }
    }

    /* Step Navigation Actions */
    .step-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 16px;
      border-top: 1px solid #f1f5f9;

      .nav-btn {
        height: 48px;
        padding: 0 24px;
        border-radius: 12px;
        font-weight: 700;
        font-size: 0.9375rem;
        display: inline-flex;
        align-items: center;
        gap: 8px;

        &.pay-action-btn {
          background: #059669;
          &:hover { background: #047857; }
        }
      }
    }

    /* Right Sticky Summary */
    .wizard-summary-col {
      .summary-sticky-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-xl, 20px);
        padding: 24px;
        box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
        position: sticky;
        top: 88px;
        display: flex;
        flex-direction: column;
        gap: 16px;

        .summary-header {
          display: flex;
          align-items: center;
          gap: 8px;

          .material-symbols-outlined { color: #2563eb; }
          h4 { margin: 0; font-size: 1.05rem; font-weight: 800; color: #0f172a; }
        }

        .summary-items-list {
          display: flex;
          flex-direction: column;
          gap: 10px;

          .summary-item {
            display: flex;
            justify-content: space-between;
            font-size: 0.8125rem;

            .item-label { color: #64748b; font-weight: 600; }
            .item-val { color: #0f172a; font-weight: 700; text-align: right; }
          }
        }

        .summary-divider {
          height: 1px;
          background: #f1f5f9;
        }

        .price-breakdown-box {
          display: flex;
          flex-direction: column;
          gap: 8px;

          .price-row {
            display: flex;
            justify-content: space-between;
            font-size: 0.8125rem;
            color: #475569;
          }

          .price-divider {
            height: 1px;
            background: #e2e8f0;
            margin: 4px 0;
          }

          .final-price-row {
            display: flex;
            justify-content: space-between;
            align-items: flex-end;

            .price-title-group {
              display: flex;
              flex-direction: column;

              .price-type-label {
                font-size: 0.8125rem;
                font-weight: 800;
                color: #0f172a;
              }

              .price-source-note {
                font-size: 0.6875rem;
                color: #059669;
                font-weight: 700;
              }
            }

            .price-figure {
              font-size: 1.625rem;
              font-weight: 800;
              color: #0f172a;

              &.verified {
                color: #2563eb;
              }
            }
          }
        }

        .trust-notice {
          display: flex;
          align-items: flex-start;
          gap: 8px;
          background: #f8fafc;
          padding: 10px 12px;
          border-radius: 10px;
          font-size: 0.6875rem;
          color: #64748b;

          .material-symbols-outlined {
            font-size: 16px;
            color: #2563eb;
            flex-shrink: 0;
          }
        }
      }
    }
  `]
})
export class ReservationCreateComponent implements OnInit {
  private areaService = inject(ParkingAreaService);
  private slotService = inject(ParkingSlotService);
  private vehicleService = inject(VehicleService);
  private reservationService = inject(ReservationService);
  private paymentService = inject(PaymentService);
  private qrService = inject(QrService);
  private notification = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  // Stepper State (Steps 1 to 7)
  currentStep: number = 1;
  steps: WizardStep[] = [
    { number: 1, label: 'Parking Area', icon: 'domain' },
    { number: 2, label: 'Vehicle', icon: 'directions_car' },
    { number: 3, label: 'Date & Time', icon: 'calendar_month' },
    { number: 4, label: 'Parking Slot', icon: 'local_parking' },
    { number: 5, label: 'Summary', icon: 'receipt_long' },
    { number: 6, label: 'Payment', icon: 'credit_card' },
    { number: 7, label: 'QR Code', icon: 'qr_code' }
  ];

  // Step 1: Parking Area
  areas: ParkingAreaDto[] = [];
  selectedArea?: ParkingAreaDto;
  areaSearchQuery: string = '';
  loadingAreas: boolean = false;

  // Step 2: Vehicle
  vehicles: VehicleDto[] = [];
  selectedVehicle?: VehicleDto;
  loadingVehicles: boolean = false;
  showAddVehicleForm: boolean = false;
  savingVehicle: boolean = false;

  vehicleForm: FormGroup = this.fb.group({
    licensePlate: ['', [Validators.required]],
    make: ['', [Validators.required]],
    model: ['', [Validators.required]],
    color: ['Silver', [Validators.required]],
    vehicleType: ['Sedan', [Validators.required]]
  });

  // Step 3: Date & Time
  todayDateString: string = '';
  selectedDate: string = '';
  entryTime: string = '10:00';
  exitTime: string = '12:00';
  selectedPresetHours: number = 2;

  durationPresets = [
    { label: '1h', hours: 1 },
    { label: '2h', hours: 2 },
    { label: '4h', hours: 4 },
    { label: '8h', hours: 8 },
    { label: 'All Day (12h)', hours: 12 }
  ];

  // Step 4: Parking Slot
  slots: ParkingSlotDto[] = [];
  selectedSlot?: ParkingSlotDto;
  selectedFloor: number | null = null;
  loadingSlots: boolean = false;

  // Step 5: Reservation Summary & Server-Side Price Locking
  lockingInReservation: boolean = false;
  backendReservation?: ReservationDto;

  // Step 6: Payment
  selectedPaymentMethod: string = 'CreditCard';
  processingPayment: boolean = false;
  paymentResult?: PaymentDto;

  paymentForm = {
    cardholderName: '',
    cardNumber: '',
    expiry: '',
    cvv: ''
  };

  // Step 7: FastPass QR Code
  loadingQr: boolean = false;
  qrCodeData?: QrCodeDto;

  ngOnInit(): void {
    this.initScheduleTimes();
    this.loadAreas();
    this.loadVehicles();
  }

  private initScheduleTimes(): void {
    const now = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');

    this.todayDateString = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
    this.selectedDate = this.todayDateString;

    const startHour = (now.getHours() + 1) % 24;
    const endHour = (startHour + this.selectedPresetHours) % 24;

    this.entryTime = `${pad(startHour)}:00`;
    this.exitTime = `${pad(endHour)}:00`;
  }

  loadAreas(): void {
    this.loadingAreas = true;
    this.areaService.getAll().subscribe({
      next: (res) => {
        this.loadingAreas = false;
        this.areas = res.data || [];
        const preselectedId = this.route.snapshot.queryParams['areaId'];
        if (preselectedId) {
          const match = this.areas.find(a => a.id === preselectedId);
          if (match) this.selectArea(match);
        }
      },
      error: () => {
        this.loadingAreas = false;
      }
    });
  }

  loadVehicles(): void {
    this.loadingVehicles = true;
    this.vehicleService.getMyVehicles().subscribe({
      next: (res: any) => {
        this.loadingVehicles = false;
        this.vehicles = res.data || [];
        if (this.vehicles.length > 0 && !this.selectedVehicle) {
          this.selectedVehicle = this.vehicles[0];
        }
      },
      error: () => {
        this.loadingVehicles = false;
      }
    });
  }

  get filteredAreas(): ParkingAreaDto[] {
    if (!this.areaSearchQuery.trim()) return this.areas;
    const q = this.areaSearchQuery.toLowerCase().trim();
    return this.areas.filter(a =>
      a.name.toLowerCase().includes(q) || (a.location || '').toLowerCase().includes(q)
    );
  }

  selectArea(area: ParkingAreaDto): void {
    this.selectedArea = area;
    // reset downstream slot if facility changes
    if (this.selectedSlot && this.selectedSlot.parkingAreaId !== area.id) {
      this.selectedSlot = undefined;
    }
  }

  selectVehicle(vehicle: VehicleDto): void {
    this.selectedVehicle = vehicle;
  }

  registerVehicleInline(): void {
    if (this.vehicleForm.invalid) return;

    this.savingVehicle = true;
    const req = {
      ...this.vehicleForm.value,
      licensePlate: (this.vehicleForm.value.licensePlate || '').toUpperCase().trim()
    };

    this.vehicleService.createVehicle(req).subscribe({
      next: (res: any) => {
        this.savingVehicle = false;
        this.notification.success('Vehicle registered successfully!');
        this.showAddVehicleForm = false;
        this.vehicleForm.reset({ color: 'Silver', vehicleType: 'Sedan' });
        this.vehicleService.getMyVehicles().subscribe({
          next: (vRes: any) => {
            this.vehicles = vRes.data || [];
            const created = this.vehicles.find(v => v.licensePlate === req.licensePlate);
            if (created) this.selectedVehicle = created;
          }
        });
      },
      error: (err) => {
        this.savingVehicle = false;
        this.notification.error('Failed to register vehicle: ' + (err.error?.message || err.message));
      }
    });
  }

  applyPreset(hours: number): void {
    this.selectedPresetHours = hours;
    const [h, m] = this.entryTime.split(':').map(Number);
    const startMinutes = (h * 60) + (m || 0);
    const endMinutes = startMinutes + (hours * 60);

    const endH = Math.floor(endMinutes / 60) % 24;
    const endM = endMinutes % 60;
    const pad = (n: number) => n.toString().padStart(2, '0');

    this.exitTime = `${pad(endH)}:${pad(endM)}`;
  }

  onScheduleFieldChanged(): void {
    this.selectedPresetHours = 0;
  }

  get calculatedDurationHours(): number {
    if (!this.selectedDate || !this.entryTime || !this.exitTime) return 1;
    const [sh, sm] = this.entryTime.split(':').map(Number);
    const [eh, em] = this.exitTime.split(':').map(Number);

    let startMinutes = (sh * 60) + sm;
    let endMinutes = (eh * 60) + em;

    if (endMinutes <= startMinutes) {
      endMinutes += 24 * 60;
    }

    return Math.max(0.25, (endMinutes - startMinutes) / 60);
  }

  get isScheduleValid(): boolean {
    if (!this.selectedDate || !this.entryTime || !this.exitTime) return false;
    return this.calculatedDurationHours >= 0.25;
  }

  get scheduleErrorMessage(): string {
    if (!this.selectedDate) return 'Please pick a reservation date.';
    if (!this.entryTime || !this.exitTime) return 'Please specify entry and exit times.';
    return 'Exit time must be at least 15 minutes after entry time.';
  }

  get estimatedPrice(): number {
    const rate = this.selectedSlot?.pricePerHour || this.selectedArea?.hourlyRate || 5;
    return rate * this.calculatedDurationHours;
  }

  onScheduleCompleted(): void {
    if (!this.isScheduleValid) return;
    this.loadSlotsForArea();
    this.nextStep();
  }

  loadSlotsForArea(): void {
    if (!this.selectedArea) return;
    this.loadingSlots = true;
    this.slotService.getAll(this.selectedArea.id).subscribe({
      next: (res) => {
        this.loadingSlots = false;
        this.slots = res.data || [];
      },
      error: () => {
        this.loadingSlots = false;
      }
    });
  }

  get floorsList(): number[] {
    const set = new Set<number>();
    for (const slot of this.slots) {
      if (slot.floorNumber !== undefined && slot.floorNumber !== null) {
        set.add(slot.floorNumber);
      }
    }
    return Array.from(set).sort((a, b) => a - b);
  }

  get filteredSlots(): ParkingSlotDto[] {
    return this.slots.filter(s => {
      if (this.selectedFloor !== null && s.floorNumber !== this.selectedFloor) {
        return false;
      }
      return true;
    });
  }

  onSlotSelected(slot: ParkingSlotDto): void {
    if ((slot.status || '').toLowerCase() !== 'available') return;
    this.selectedSlot = slot;
  }

  // STEP 5: LOCK IN AUTHORITATIVE PRICE VIA BACKEND
  lockInReservationWithBackend(): void {
    if (!this.selectedSlot || !this.selectedVehicle || !this.selectedArea) {
      this.notification.warning('Missing booking parameters.');
      return;
    }

    this.lockingInReservation = true;

    const startDateTime = new Date(`${this.selectedDate}T${this.entryTime}:00`);
    let endDateTime = new Date(`${this.selectedDate}T${this.exitTime}:00`);
    if (endDateTime.getTime() <= startDateTime.getTime()) {
      endDateTime = new Date(endDateTime.getTime() + 24 * 3600000);
    }

    const req = {
      parkingSlotId: this.selectedSlot.id,
      vehicleId: this.selectedVehicle.id,
      startTime: startDateTime.toISOString(),
      endTime: endDateTime.toISOString()
    };

    // The backend calculates the final authoritative price!
    this.reservationService.create(req).subscribe({
      next: (res) => {
        this.lockingInReservation = false;
        this.backendReservation = res.data;
        this.notification.success(
          `Authoritative price verified: \$${this.backendReservation.totalAmount.toFixed(2)}. Ticket locked!`
        );
      },
      error: (err) => {
        this.lockingInReservation = false;
        this.notification.error('Server pricing calculation failed: ' + (err.error?.message || err.message));
      }
    });
  }

  // STEP 6: PROCESS PAYMENT
  processPayment(): void {
    if (!this.backendReservation) {
      this.notification.error('No locked backend reservation found.');
      return;
    }

    this.processingPayment = true;

    // Send authoritative amount from the backend reservation
    const req = {
      reservationId: this.backendReservation.id,
      amount: this.backendReservation.totalAmount,
      paymentMethod: this.selectedPaymentMethod
    };

    this.paymentService.processPayment(req).subscribe({
      next: (res) => {
        this.processingPayment = false;
        this.paymentResult = res.data;
        this.notification.success('Payment successfully authorized & settled!');
        this.loadQrCodeForPass(this.backendReservation!.id);
        this.currentStep = 7;
      },
      error: (err) => {
        this.processingPayment = false;
        this.notification.error('Payment processing failed: ' + (err.error?.message || err.message));
      }
    });
  }

  loadQrCodeForPass(reservationId: string): void {
    this.loadingQr = true;
    this.qrService.getByReservationId(reservationId).subscribe({
      next: (res) => {
        this.loadingQr = false;
        this.qrCodeData = res.data;
      },
      error: () => {
        this.loadingQr = false;
      }
    });
  }

  printTicket(): void {
    window.print();
  }

  // Navigation Helpers
  nextStep(): void {
    if (this.currentStep < 7) {
      this.currentStep++;
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  prevStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  isStepAccessible(stepNumber: number): boolean {
    if (stepNumber === 1) return true;
    if (stepNumber === 2) return !!this.selectedArea;
    if (stepNumber === 3) return !!this.selectedArea && !!this.selectedVehicle;
    if (stepNumber === 4) return !!this.selectedArea && !!this.selectedVehicle && this.isScheduleValid;
    if (stepNumber === 5) return !!this.selectedSlot;
    if (stepNumber === 6) return !!this.backendReservation;
    if (stepNumber === 7) return !!this.paymentResult;
    return false;
  }

  goToStepIfAllowed(stepNumber: number): void {
    if (stepNumber <= this.currentStep || this.isStepAccessible(stepNumber)) {
      this.currentStep = stepNumber;
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }
}
