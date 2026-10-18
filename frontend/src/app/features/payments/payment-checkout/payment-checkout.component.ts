import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { PaymentService } from '../../../core/services/payment.service';
import { ReservationService } from '../../../core/services/reservation.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ReservationDto, PaymentDto, PaymentMethod } from '../../../core/models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

export type PaymentState = 'idle' | 'processing' | 'success' | 'failed';

@Component({
  selector: 'app-payment-checkout',
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
    LoadingSpinnerComponent,
    EmptyStateComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="checkout-page-container">
      <!-- Breadcrumb Header -->
      <div class="checkout-header">
        <a routerLink="/customer/reservations" class="back-link">
          <span class="material-symbols-outlined">arrow_back</span>
          <span>Back to Reservations</span>
        </a>
        <div class="header-titles">
          <h2>Payment & Settlement</h2>
          <p>Review your reservation schedule, select a payment method, and activate your digital FastPass ticket.</p>
        </div>
      </div>

      <!-- Loading Reservation State -->
      <app-loading-spinner *ngIf="loadingReservation" message="Retrieving reservation details..."></app-loading-spinner>

      <!-- Empty State if no reservation found -->
      <app-empty-state 
        *ngIf="!loadingReservation && !reservation"
        icon="receipt_long"
        title="No reservation ready for payment"
        description="We couldn't locate a pending reservation. Please choose a parking spot to proceed to checkout."
        actionText="Find & Reserve Parking"
        actionRoute="/customer/parking">
      </app-empty-state>

      <!-- Main Checkout Content -->
      <div class="checkout-layout" *ngIf="!loadingReservation && reservation">

        <!-- ======================================================== -->
        <!-- PROCESSING STATE VIEW                                    -->
        <!-- ======================================================== -->
        <div class="state-card processing-card" *ngIf="paymentState === 'processing'">
          <div class="processing-spinner-wrap">
            <div class="radar-pulse"></div>
            <span class="material-symbols-outlined secure-icon">lock</span>
          </div>

          <h3>Authorizing Payment...</h3>
          <p class="processing-subtext">
            Connecting to secure gateway to process <strong>\${{ reservation.totalAmount | number:'1.2-2' }}</strong> via {{ getMethodLabel(selectedMethod) }}.
          </p>

          <div class="processing-steps">
            <div class="step-item completed">
              <span class="material-symbols-outlined">check_circle</span>
              <span>Reservation locked & verified</span>
            </div>
            <div class="step-item in-progress">
              <span class="pulse-dot"></span>
              <span>Securing banking transaction</span>
            </div>
            <div class="step-item pending">
              <span class="circle-dot"></span>
              <span>Activating FastPass QR barcode</span>
            </div>
          </div>

          <div class="security-disclaimer">
            <span class="material-symbols-outlined">shield</span>
            <span>Do not refresh or close this window. Your transaction is protected by 256-bit encryption.</span>
          </div>
        </div>

        <!-- ======================================================== -->
        <!-- SUCCESS STATE VIEW                                       -->
        <!-- ======================================================== -->
        <div class="state-card success-card" *ngIf="paymentState === 'success'">
          <div class="success-icon-wrap">
            <span class="material-symbols-outlined">verified</span>
          </div>

          <div class="success-badge">PAYMENT CONFIRMED</div>
          <h3>Payment Successfully Settled!</h3>
          <p class="success-desc">
            Your transaction has been approved. Your reservation is now <strong>CONFIRMED</strong> and your contactless QR FastPass is active.
          </p>

          <!-- Receipt Details -->
          <div class="receipt-box">
            <div class="receipt-row">
              <span class="r-label">Transaction Reference:</span>
              <span class="r-val font-mono">{{ paymentResult?.transactionId || ('TXN-' + (paymentResult?.id?.slice(0, 8)?.toUpperCase() || 'SUCCESS')) }}</span>
            </div>
            <div class="receipt-row">
              <span class="r-label">Amount Paid:</span>
              <span class="r-val price-highlight">\${{ reservation.totalAmount | number:'1.2-2' }}</span>
            </div>
            <div class="receipt-row">
              <span class="r-label">Payment Method:</span>
              <span class="r-val">{{ getMethodLabel(selectedMethod) }}</span>
            </div>
            <div class="receipt-row">
              <span class="r-label">Assigned Bay:</span>
              <span class="r-val">Bay {{ reservation.slotNumber }} ({{ reservation.parkingAreaName }})</span>
            </div>
            <div class="receipt-row">
              <span class="r-label">Vehicle Plate:</span>
              <span class="r-val font-mono">{{ reservation.vehiclePlate }}</span>
            </div>
          </div>

          <!-- Countdown to QR Code -->
          <div class="redirect-countdown-row">
            <span class="material-symbols-outlined">schedule</span>
            <span>Redirecting to your FastPass QR code in <strong>{{ countdownSeconds }}s</strong>...</span>
          </div>

          <div class="success-actions">
            <button mat-flat-button color="primary" class="go-qr-btn" (click)="redirectToQrPage()">
              <span class="material-symbols-outlined">qr_code_2</span>
              <span>Open FastPass QR Code Now</span>
            </button>
            <a routerLink="/customer/reservations" mat-stroked-button class="view-res-btn">
              <span>View All Reservations</span>
            </a>
          </div>
        </div>

        <!-- ======================================================== -->
        <!-- FAILED STATE VIEW                                        -->
        <!-- ======================================================== -->
        <div class="state-card failed-card" *ngIf="paymentState === 'failed'">
          <div class="failed-icon-wrap">
            <span class="material-symbols-outlined">error</span>
          </div>

          <div class="failed-badge">TRANSACTION DECLINED</div>
          <h3>Payment Could Not Be Processed</h3>
          <p class="failed-desc">
            {{ errorMessage || 'The payment gateway could not authorize this transaction. Your reservation is still tentatively held.' }}
          </p>

          <div class="failed-reasons-box">
            <h4>Common causes:</h4>
            <ul>
              <li>Card expiry or CVV verification mismatch</li>
              <li>Insufficient funds or daily card transaction limits</li>
              <li>Bank fraud protection temporarily holding the charge</li>
            </ul>
          </div>

          <div class="failed-actions">
            <button mat-flat-button color="primary" class="retry-btn" (click)="retryPayment()">
              <span class="material-symbols-outlined">refresh</span>
              <span>Try Again</span>
            </button>
            <button mat-stroked-button class="switch-method-btn" (click)="paymentState = 'idle'">
              <span class="material-symbols-outlined">credit_card</span>
              <span>Choose Another Payment Method</span>
            </button>
            <a routerLink="/customer/reservations" mat-button class="cancel-link">
              <span>Cancel & Return</span>
            </a>
          </div>
        </div>

        <!-- ======================================================== -->
        <!-- IDLE / FORM VIEW: INVOICE & METHOD SELECTION             -->
        <!-- ======================================================== -->
        <div class="checkout-grid" *ngIf="paymentState === 'idle'">
          
          <!-- LEFT: 8 Required Reservation Display Fields -->
          <div class="invoice-col">
            <div class="invoice-card">
              <div class="card-title-row">
                <span class="material-symbols-outlined">receipt_long</span>
                <h3>Reservation Details</h3>
              </div>

              <!-- 8 Required Fields Breakdown -->
              <div class="details-table">
                <!-- 1. Parking Area -->
                <div class="detail-row">
                  <div class="detail-icon">
                    <span class="material-symbols-outlined">domain</span>
                  </div>
                  <div class="detail-content">
                    <span class="label">Parking Area</span>
                    <span class="value">{{ reservation.parkingAreaName || 'Main Parking Facility' }}</span>
                  </div>
                </div>

                <!-- 2. Slot -->
                <div class="detail-row">
                  <div class="detail-icon">
                    <span class="material-symbols-outlined">local_parking</span>
                  </div>
                  <div class="detail-content">
                    <span class="label">Slot</span>
                    <span class="value bay-highlight">Bay {{ reservation.slotNumber || 'Confirmed' }}</span>
                  </div>
                </div>

                <!-- 3. Vehicle -->
                <div class="detail-row">
                  <div class="detail-icon">
                    <span class="material-symbols-outlined">directions_car</span>
                  </div>
                  <div class="detail-content">
                    <span class="label">Vehicle</span>
                    <div class="vehicle-plate-pill">
                      <span class="country">USA</span>
                      <span class="plate">{{ reservation.vehiclePlate || 'Vehicle Plate' }}</span>
                    </div>
                  </div>
                </div>

                <!-- 4. Date -->
                <div class="detail-row">
                  <div class="detail-icon">
                    <span class="material-symbols-outlined">calendar_month</span>
                  </div>
                  <div class="detail-content">
                    <span class="label">Date</span>
                    <span class="value">{{ reservation.startTime | date:'fullDate' }}</span>
                  </div>
                </div>

                <!-- 5. Entry -->
                <div class="detail-row">
                  <div class="detail-icon">
                    <span class="material-symbols-outlined">login</span>
                  </div>
                  <div class="detail-content">
                    <span class="label">Entry</span>
                    <span class="value">{{ reservation.startTime | date:'shortTime' }}</span>
                  </div>
                </div>

                <!-- 6. Exit -->
                <div class="detail-row">
                  <div class="detail-icon">
                    <span class="material-symbols-outlined">logout</span>
                  </div>
                  <div class="detail-content">
                    <span class="label">Exit</span>
                    <span class="value">{{ reservation.endTime | date:'shortTime' }}</span>
                  </div>
                </div>

                <!-- 7. Duration -->
                <div class="detail-row">
                  <div class="detail-icon">
                    <span class="material-symbols-outlined">schedule</span>
                  </div>
                  <div class="detail-content">
                    <span class="label">Duration</span>
                    <span class="value duration-val">{{ durationHours | number:'1.1-1' }} Hours</span>
                  </div>
                </div>

                <!-- 8. Amount -->
                <div class="detail-row amount-row">
                  <div class="detail-icon">
                    <span class="material-symbols-outlined">payments</span>
                  </div>
                  <div class="detail-content">
                    <span class="label">Authoritative Amount</span>
                    <span class="value total-amount">\${{ reservation.totalAmount | number:'1.2-2' }}</span>
                  </div>
                </div>
              </div>

              <!-- Multi-reservation switch selector if user has more unpaid -->
              <div class="switch-reservation-bar" *ngIf="pendingReservations.length > 1">
                <label>Pay for a different booking:</label>
                <mat-select [value]="reservation.id" (selectionChange)="onSwitchReservation($event.value)">
                  <mat-option *ngFor="let res of pendingReservations" [value]="res.id">
                    Bay {{ res.slotNumber }} ({{ res.parkingAreaName }}) - \${{ res.totalAmount | number:'1.2-2' }}
                  </mat-option>
                </mat-select>
              </div>
            </div>
          </div>

          <!-- RIGHT: Payment Method Selection & Checkout Action -->
          <div class="payment-method-col">
            <div class="payment-selection-card">
              <div class="card-title-row">
                <span class="material-symbols-outlined">credit_card</span>
                <h3>Payment Method</h3>
              </div>

              <!-- Method Cards Grid -->
              <div class="methods-grid">
                <!-- 1. Credit Card -->
                <div 
                  class="method-option-card"
                  [class.active]="selectedMethod === 'CreditCard'"
                  (click)="selectedMethod = 'CreditCard'">
                  <div class="card-radio">
                    <span class="radio-circle"></span>
                  </div>
                  <span class="material-symbols-outlined method-icon">credit_card</span>
                  <div class="method-text">
                    <span class="method-title">Credit Card</span>
                    <span class="method-sub">Visa, Mastercard, Amex</span>
                  </div>
                </div>

                <!-- 2. Debit Card -->
                <div 
                  class="method-option-card"
                  [class.active]="selectedMethod === 'DebitCard'"
                  (click)="selectedMethod = 'DebitCard'">
                  <div class="card-radio">
                    <span class="radio-circle"></span>
                  </div>
                  <span class="material-symbols-outlined method-icon">account_balance_wallet</span>
                  <div class="method-text">
                    <span class="method-title">Debit Card</span>
                    <span class="method-sub">Instant Bank Debit</span>
                  </div>
                </div>

                <!-- 3. Digital Wallet -->
                <div 
                  class="method-option-card"
                  [class.active]="selectedMethod === 'DigitalWallet'"
                  (click)="selectedMethod = 'DigitalWallet'">
                  <div class="card-radio">
                    <span class="radio-circle"></span>
                  </div>
                  <span class="material-symbols-outlined method-icon">smartphone</span>
                  <div class="method-text">
                    <span class="method-title">Digital Wallet</span>
                    <span class="method-sub">Apple Pay, Google Pay</span>
                  </div>
                </div>

                <!-- 4. Cash on Site -->
                <div 
                  class="method-option-card"
                  [class.active]="selectedMethod === 'Cash'"
                  (click)="selectedMethod = 'Cash'">
                  <div class="card-radio">
                    <span class="radio-circle"></span>
                  </div>
                  <span class="material-symbols-outlined method-icon">payments</span>
                  <div class="method-text">
                    <span class="method-title">Cash at Gate</span>
                    <span class="method-sub">Pay attendant upon entry</span>
                  </div>
                </div>
              </div>

              <!-- Interactive Card Inputs Form (for Card options) -->
              <div class="card-fields-box" *ngIf="selectedMethod === 'CreditCard' || selectedMethod === 'DebitCard'">
                <form [formGroup]="cardForm" class="card-form-grid">
                  <mat-form-field appearance="outline" class="w-full">
                    <mat-label>Cardholder Name</mat-label>
                    <input matInput formControlName="cardholderName" placeholder="Full name as printed on card">
                    <mat-error *ngIf="cardForm.get('cardholderName')?.hasError('required')">Name is required</mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="w-full">
                    <mat-label>Card Number</mat-label>
                    <input 
                      matInput 
                      formControlName="cardNumber" 
                      placeholder="4242 4242 4242 4242" 
                      maxlength="19"
                      (input)="formatCardNumber($event)">
                    <span matSuffix class="material-symbols-outlined suffix-card-icon">credit_card</span>
                    <mat-error *ngIf="cardForm.get('cardNumber')?.hasError('required')">Card number is required</mat-error>
                  </mat-form-field>

                  <div class="row-2-cols">
                    <mat-form-field appearance="outline">
                      <mat-label>Expiry (MM/YY)</mat-label>
                      <input 
                        matInput 
                        formControlName="expiry" 
                        placeholder="12/28" 
                        maxlength="5"
                        (input)="formatExpiry($event)">
                      <mat-error *ngIf="cardForm.get('expiry')?.hasError('required')">Required</mat-error>
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>CVC / CVV</mat-label>
                      <input matInput formControlName="cvv" placeholder="•••" maxlength="4" type="password">
                      <mat-error *ngIf="cardForm.get('cvv')?.hasError('required')">Required</mat-error>
                    </mat-form-field>
                  </div>
                </form>
              </div>

              <!-- Digital Wallet / Cash message -->
              <div class="alternative-message" *ngIf="selectedMethod === 'DigitalWallet'">
                <span class="material-symbols-outlined">smartphone</span>
                <span>You will be prompted with biometric authorization (FaceID/TouchID or Google Sign-In) upon clicking Pay.</span>
              </div>

              <div class="alternative-message" *ngIf="selectedMethod === 'Cash'">
                <span class="material-symbols-outlined">info</span>
                <span>Your reservation will be confirmed. Please present your FastPass QR code to the booth attendant to settle in cash at the gate.</span>
              </div>

              <!-- Security Row -->
              <div class="security-badges-row">
                <div class="badge-item">
                  <span class="material-symbols-outlined">lock</span>
                  <span>256-Bit SSL Encrypted</span>
                </div>
                <div class="badge-item">
                  <span class="material-symbols-outlined">verified_user</span>
                  <span>PCI-DSS Level 1 Compliant</span>
                </div>
              </div>

              <!-- Checkout Submission Button -->
              <div class="checkout-action-box">
                <button 
                  mat-flat-button 
                  color="primary" 
                  class="pay-submit-btn"
                  [disabled]="isPayButtonDisabled"
                  (click)="submitPayment()">
                  <span class="material-symbols-outlined">lock</span>
                  <span>Pay \${{ reservation.totalAmount | number:'1.2-2' }} & Activate Pass</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .checkout-page-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .checkout-header {
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

      .header-titles {
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

    /* Checkout Layout Grid */
    .checkout-grid {
      display: grid;
      grid-template-columns: 460px 1fr;
      gap: 24px;
      align-items: start;

      @media (max-width: 992px) {
        grid-template-columns: 1fr;
      }
    }

    /* Left: Invoice Summary Card */
    .invoice-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-xl, 20px);
      padding: 28px;
      box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
      display: flex;
      flex-direction: column;
      gap: 20px;

      .card-title-row {
        display: flex;
        align-items: center;
        gap: 8px;

        .material-symbols-outlined {
          color: #2563eb;
          font-size: 24px;
        }

        h3 {
          margin: 0;
          font-size: 1.2rem;
          font-weight: 800;
          color: #0f172a;
        }
      }

      .details-table {
        display: flex;
        flex-direction: column;
        gap: 14px;

        .detail-row {
          display: flex;
          align-items: center;
          gap: 14px;
          padding-bottom: 12px;
          border-bottom: 1px solid #f1f5f9;

          .detail-icon {
            width: 36px;
            height: 36px;
            border-radius: 10px;
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            display: flex;
            align-items: center;
            justify-content: center;
            color: #64748b;
            flex-shrink: 0;

            .material-symbols-outlined {
              font-size: 18px;
            }
          }

          .detail-content {
            display: flex;
            flex-direction: column;
            gap: 2px;
            flex: 1;

            .label {
              font-size: 0.6875rem;
              font-weight: 700;
              text-transform: uppercase;
              letter-spacing: 0.05em;
              color: #64748b;
            }

            .value {
              font-size: 0.9375rem;
              font-weight: 700;
              color: #0f172a;

              &.bay-highlight {
                color: #2563eb;
              }

              &.duration-val {
                color: #0d9488;
              }
            }

            .vehicle-plate-pill {
              display: inline-flex;
              align-items: center;
              gap: 6px;
              background: #0f172a;
              color: #ffffff;
              padding: 2px 8px;
              border-radius: 4px;
              font-family: monospace;
              font-weight: 700;
              font-size: 0.8125rem;
              width: fit-content;

              .country {
                font-size: 0.625rem;
                color: #94a3b8;
              }
            }
          }

          &.amount-row {
            background: #eff6ff;
            border: 1px solid #bfdbfe;
            border-radius: 12px;
            padding: 14px 16px;
            margin-top: 4px;

            .detail-icon {
              background: #2563eb;
              color: #ffffff;
              border-color: #2563eb;
            }

            .detail-content {
              .label { color: #1e40af; }
              .total-amount {
                font-size: 1.5rem;
                font-weight: 800;
                color: #1e3a8a;
              }
            }
          }
        }
      }

      .switch-reservation-bar {
        display: flex;
        flex-direction: column;
        gap: 6px;
        background: #f8fafc;
        padding: 12px;
        border-radius: 10px;
        font-size: 0.75rem;

        label {
          font-weight: 700;
          color: #475569;
        }
      }
    }

    /* Right: Payment Method Selection Card */
    .payment-selection-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-xl, 20px);
      padding: 28px;
      box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
      display: flex;
      flex-direction: column;
      gap: 20px;

      .card-title-row {
        display: flex;
        align-items: center;
        gap: 8px;

        .material-symbols-outlined {
          color: #2563eb;
          font-size: 24px;
        }

        h3 {
          margin: 0;
          font-size: 1.2rem;
          font-weight: 800;
          color: #0f172a;
        }
      }

      .methods-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;

        @media (max-width: 600px) {
          grid-template-columns: 1fr;
        }

        .method-option-card {
          border: 2px solid #e2e8f0;
          border-radius: 14px;
          padding: 14px;
          cursor: pointer;
          transition: all 0.15s cubic-bezier(0.16, 1, 0.3, 1);
          display: flex;
          align-items: center;
          gap: 12px;

          &:hover {
            border-color: #93c5fd;
            transform: translateY(-2px);
          }

          &.active {
            border-color: #2563eb;
            background: #f8faff;
            box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.15);

            .card-radio .radio-circle {
              border-color: #2563eb;
              background: #2563eb;
              box-shadow: inset 0 0 0 3px #ffffff;
            }

            .method-icon {
              color: #2563eb;
            }

            .method-title {
              color: #2563eb;
              font-weight: 800;
            }
          }

          .card-radio {
            .radio-circle {
              width: 18px;
              height: 18px;
              border-radius: 50%;
              border: 2px solid #cbd5e1;
              display: block;
              transition: all 0.15s ease;
            }
          }

          .method-icon {
            font-size: 24px;
            color: #64748b;
          }

          .method-text {
            display: flex;
            flex-direction: column;

            .method-title {
              font-size: 0.875rem;
              font-weight: 700;
              color: #0f172a;
            }

            .method-sub {
              font-size: 0.6875rem;
              color: #64748b;
            }
          }
        }
      }

      .card-fields-box {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 14px;
        padding: 20px;

        .card-form-grid {
          display: flex;
          flex-direction: column;
          gap: 4px;

          .row-2-cols {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 12px;
          }

          .suffix-card-icon {
            color: #94a3b8;
          }
        }
      }

      .alternative-message {
        display: flex;
        align-items: center;
        gap: 10px;
        background: #eff6ff;
        border: 1px solid #bfdbfe;
        padding: 14px;
        border-radius: 12px;
        font-size: 0.8125rem;
        color: #1e40af;

        .material-symbols-outlined {
          font-size: 22px;
          color: #2563eb;
          flex-shrink: 0;
        }
      }

      .security-badges-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        padding-top: 8px;
        border-top: 1px solid #f1f5f9;

        .badge-item {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 0.75rem;
          font-weight: 600;
          color: #64748b;

          .material-symbols-outlined {
            font-size: 16px;
            color: #059669;
          }
        }
      }

      .checkout-action-box {
        .pay-submit-btn {
          width: 100%;
          height: 52px;
          border-radius: 14px;
          font-weight: 800;
          font-size: 1.05rem;
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 8px;
          box-shadow: 0 4px 14px rgba(37, 99, 235, 0.3);
        }
      }
    }

    /* ========================================================= */
    /* STATE CARDS (Processing, Success, Failed)                 */
    /* ========================================================= */
    .state-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-xl, 24px);
      padding: 48px 32px;
      box-shadow: 0 10px 30px rgba(15, 23, 42, 0.06);
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      max-width: 680px;
      margin: 0 auto;
      animation: fadeIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
    }

    /* 1. Processing State */
    .processing-card {
      .processing-spinner-wrap {
        position: relative;
        width: 80px;
        height: 80px;
        margin-bottom: 24px;
        display: flex;
        align-items: center;
        justify-content: center;

        .secure-icon {
          font-size: 36px;
          color: #2563eb;
          z-index: 2;
        }

        .radar-pulse {
          position: absolute;
          width: 100%;
          height: 100%;
          border-radius: 50%;
          background: rgba(37, 99, 235, 0.15);
          animation: radarWave 1.8s infinite ease-out;
        }
      }

      h3 {
        margin: 0 0 8px 0;
        font-size: 1.625rem;
        font-weight: 800;
        color: #0f172a;
      }

      .processing-subtext {
        color: #64748b;
        font-size: 0.9375rem;
        margin: 0 0 24px 0;
      }

      .processing-steps {
        display: flex;
        flex-direction: column;
        gap: 12px;
        width: 100%;
        max-width: 380px;
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 14px;
        padding: 16px 20px;
        margin-bottom: 24px;

        .step-item {
          display: flex;
          align-items: center;
          gap: 10px;
          font-size: 0.8125rem;
          font-weight: 600;

          &.completed {
            color: #059669;
            .material-symbols-outlined { font-size: 18px; color: #10b981; }
          }

          &.in-progress {
            color: #2563eb;
            .pulse-dot {
              width: 10px;
              height: 10px;
              border-radius: 50%;
              background: #2563eb;
              animation: blink 1s infinite alternate;
            }
          }

          &.pending {
            color: #94a3b8;
            .circle-dot {
              width: 10px;
              height: 10px;
              border-radius: 50%;
              border: 2px solid #cbd5e1;
            }
          }
        }
      }

      .security-disclaimer {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 0.75rem;
        color: #64748b;

        .material-symbols-outlined {
          font-size: 16px;
          color: #2563eb;
        }
      }
    }

    /* 2. Success State */
    .success-card {
      .success-icon-wrap {
        width: 72px;
        height: 72px;
        border-radius: 50%;
        background: #ecfdf5;
        border: 2px solid #10b981;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #059669;
        margin-bottom: 16px;

        .material-symbols-outlined {
          font-size: 42px;
        }
      }

      .success-badge {
        font-size: 0.75rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        color: #059669;
        margin-bottom: 4px;
      }

      h3 {
        margin: 0 0 8px 0;
        font-size: 1.75rem;
        font-weight: 800;
        color: #0f172a;
      }

      .success-desc {
        color: #64748b;
        font-size: 0.9375rem;
        margin: 0 0 24px 0;
      }

      .receipt-box {
        width: 100%;
        background: #f8fafc;
        border: 1px dashed #cbd5e1;
        border-radius: 14px;
        padding: 16px 20px;
        display: flex;
        flex-direction: column;
        gap: 10px;
        margin-bottom: 20px;

        .receipt-row {
          display: flex;
          justify-content: space-between;
          font-size: 0.8125rem;

          .r-label { color: #64748b; font-weight: 600; }
          .r-val { color: #0f172a; font-weight: 700; }
          .price-highlight { color: #059669; font-size: 1rem; }
        }
      }

      .redirect-countdown-row {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 0.875rem;
        color: #2563eb;
        margin-bottom: 24px;

        .material-symbols-outlined { font-size: 18px; }
      }

      .success-actions {
        display: flex;
        gap: 14px;
        flex-wrap: wrap;
        justify-content: center;

        .go-qr-btn {
          height: 48px;
          padding: 0 24px;
          border-radius: 12px;
          font-weight: 700;
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .view-res-btn {
          height: 48px;
          padding: 0 20px;
          border-radius: 12px;
          font-weight: 700;
        }
      }
    }

    /* 3. Failed State */
    .failed-card {
      .failed-icon-wrap {
        width: 72px;
        height: 72px;
        border-radius: 50%;
        background: #fef2f2;
        border: 2px solid #ef4444;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #dc2626;
        margin-bottom: 16px;

        .material-symbols-outlined {
          font-size: 42px;
        }
      }

      .failed-badge {
        font-size: 0.75rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        color: #dc2626;
        margin-bottom: 4px;
      }

      h3 {
        margin: 0 0 8px 0;
        font-size: 1.75rem;
        font-weight: 800;
        color: #0f172a;
      }

      .failed-desc {
        color: #64748b;
        font-size: 0.9375rem;
        margin: 0 0 20px 0;
      }

      .failed-reasons-box {
        width: 100%;
        background: #fffcfc;
        border: 1px solid #fecaca;
        border-radius: 12px;
        padding: 16px;
        text-align: left;
        margin-bottom: 24px;

        h4 {
          margin: 0 0 6px 0;
          font-size: 0.8125rem;
          font-weight: 700;
          color: #991b1b;
        }

        ul {
          margin: 0;
          padding-left: 18px;
          font-size: 0.75rem;
          color: #7f1d1d;
          display: flex;
          flex-direction: column;
          gap: 4px;
        }
      }

      .failed-actions {
        display: flex;
        align-items: center;
        gap: 12px;
        flex-wrap: wrap;
        justify-content: center;

        .retry-btn {
          height: 48px;
          padding: 0 24px;
          border-radius: 12px;
          font-weight: 700;
          background: #dc2626;
          &:hover { background: #b91c1c; }
        }

        .switch-method-btn {
          height: 48px;
          padding: 0 20px;
          border-radius: 12px;
          font-weight: 700;
        }
      }
    }

    @keyframes radarWave {
      0% {
        transform: scale(0.6);
        opacity: 0.8;
      }
      100% {
        transform: scale(1.6);
        opacity: 0;
      }
    }

    @keyframes blink {
      from { opacity: 0.4; }
      to { opacity: 1; }
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(8px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class PaymentCheckoutComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private paymentService = inject(PaymentService);
  private reservationService = inject(ReservationService);
  private notification = inject(NotificationService);
  private fb = inject(FormBuilder);

  // Core Data
  reservation?: ReservationDto;
  pendingReservations: ReservationDto[] = [];
  loadingReservation: boolean = true;

  // Payment Method Selection
  selectedMethod: string = 'CreditCard';

  // State Management (idle | processing | success | failed)
  paymentState: PaymentState = 'idle';
  errorMessage: string = '';
  paymentResult?: PaymentDto;

  // Countdown timer
  countdownSeconds: number = 3;
  private countdownTimer?: any;

  // Card Form
  cardForm: FormGroup = this.fb.group({
    cardholderName: ['John Doe', [Validators.required]],
    cardNumber: ['4242 4242 4242 4242', [Validators.required]],
    expiry: ['12/28', [Validators.required]],
    cvv: ['123', [Validators.required]]
  });

  ngOnInit(): void {
    this.resolveReservation();
  }

  ngOnDestroy(): void {
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
    }
  }

  resolveReservation(): void {
    this.loadingReservation = true;
    const routeId = this.route.snapshot.params['reservationId'] || this.route.snapshot.queryParams['reservationId'];

    if (routeId) {
      this.reservationService.getById(routeId).subscribe({
        next: (res) => {
          this.loadingReservation = false;
          this.reservation = res.data;
        },
        error: () => {
          this.fallbackToPendingReservations();
        }
      });
    } else {
      this.fallbackToPendingReservations();
    }
  }

  fallbackToPendingReservations(): void {
    this.reservationService.getMyReservations().subscribe({
      next: (res) => {
        this.loadingReservation = false;
        const all = res.data || [];
        // Filter pending or confirmed without payment
        this.pendingReservations = all.filter(r => 
          (r.status || '').toLowerCase() === 'pending' || 
          ((r.status || '').toLowerCase() === 'confirmed' && !r.payment)
        );

        if (this.pendingReservations.length > 0) {
          this.reservation = this.pendingReservations[0];
        } else if (all.length > 0) {
          // If all are already paid/active, pick the latest
          this.reservation = all[0];
        }
      },
      error: () => {
        this.loadingReservation = false;
      }
    });
  }

  onSwitchReservation(id: string): void {
    const match = this.pendingReservations.find(r => r.id === id);
    if (match) {
      this.reservation = match;
    }
  }

  get durationHours(): number {
    if (!this.reservation) return 1;
    const start = new Date(this.reservation.startTime).getTime();
    const end = new Date(this.reservation.endTime).getTime();
    if (!start || !end || end <= start) return 1;
    return Math.max(0.5, (end - start) / (1000 * 60 * 60));
  }

  getMethodLabel(method: string): string {
    switch (method) {
      case 'CreditCard': return 'Credit Card';
      case 'DebitCard': return 'Debit Card';
      case 'DigitalWallet': return 'Digital Wallet (Apple/Google Pay)';
      case 'Cash': return 'Cash at Gate';
      default: return method;
    }
  }

  get isPayButtonDisabled(): boolean {
    if (this.selectedMethod === 'CreditCard' || this.selectedMethod === 'DebitCard') {
      return this.cardForm.invalid;
    }
    return false;
  }

  formatCardNumber(event: any): void {
    let input = event.target.value.replace(/\D/g, '');
    let formatted = '';
    for (let i = 0; i < input.length; i++) {
      if (i > 0 && i % 4 === 0) formatted += ' ';
      formatted += input[i];
    }
    this.cardForm.patchValue({ cardNumber: formatted.slice(0, 19) }, { emitEvent: false });
  }

  formatExpiry(event: any): void {
    let input = event.target.value.replace(/\D/g, '');
    if (input.length >= 2) {
      input = input.slice(0, 2) + '/' + input.slice(2, 4);
    }
    this.cardForm.patchValue({ expiry: input.slice(0, 5) }, { emitEvent: false });
  }

  submitPayment(): void {
    if (!this.reservation) return;

    this.paymentState = 'processing';
    this.errorMessage = '';

    const req = {
      reservationId: this.reservation.id,
      amount: this.reservation.totalAmount,
      paymentMethod: this.selectedMethod
    };

    // Simulate 1.2s realistic gateway handshake
    setTimeout(() => {
      this.paymentService.processPayment(req).subscribe({
        next: (res) => {
          this.paymentResult = res.data;
          this.paymentState = 'success';
          this.notification.success('Payment approved! FastPass QR code ready.');
          this.startRedirectCountdown();
        },
        error: (err) => {
          this.paymentState = 'failed';
          this.errorMessage = err.error?.message || err.message || 'Payment authorization declined.';
          this.notification.error('Transaction declined: ' + this.errorMessage);
        }
      });
    }, 1200);
  }

  retryPayment(): void {
    this.paymentState = 'idle';
  }

  startRedirectCountdown(): void {
    this.countdownSeconds = 3;
    this.countdownTimer = setInterval(() => {
      this.countdownSeconds--;
      if (this.countdownSeconds <= 0) {
        clearInterval(this.countdownTimer);
        this.redirectToQrPage();
      }
    }, 1000);
  }

  redirectToQrPage(): void {
    if (this.countdownTimer) clearInterval(this.countdownTimer);
    if (this.reservation) {
      this.router.navigate(['/qr-code'], { queryParams: { id: this.reservation.id } });
    } else {
      this.router.navigate(['/qr-code']);
    }
  }
}
