import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { PaymentService } from '../../../core/services/payment.service';
import { ReservationService } from '../../../core/services/reservation.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ReservationDto } from '../../../core/models';
import { DataTableComponent, TableColumn } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-customer-payments',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    DataTableComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="customer-payments-container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <h2>Billing & Payment Ledger</h2>
          <p>Review your processed parking fees, transaction receipts, and settle pending passes.</p>
        </div>
        <button mat-flat-button color="primary" class="pay-btn" (click)="openQuickPay(payModal)">
          <span class="material-symbols-outlined">payments</span>
          <span>Settle Booking</span>
        </button>
      </div>

      <!-- Financial Metrics -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <span class="kpi-label">Total Amount Settled</span>
          <span class="kpi-val text-blue">Birr {{ totalSpent | number:'1.2-2' }}</span>
          <span class="kpi-sub">Total lifetime parking payments</span>
        </div>

        <div class="kpi-card">
          <span class="kpi-label">Completed Transactions</span>
          <span class="kpi-val text-green">{{ completedCount }}</span>
          <span class="kpi-sub">Verified payments</span>
        </div>

        <div class="kpi-card highlight-amber" *ngIf="pendingReservations.length > 0">
          <span class="kpi-label">Awaiting Settlement</span>
          <span class="kpi-val text-amber">{{ pendingReservations.length }}</span>
          <span class="kpi-sub">Pending confirmation</span>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Loading payment transactions..."></app-loading-spinner>

      <!-- Content Table -->
      <div class="content-body" *ngIf="!loading">
        <app-data-table
          [columns]="columns"
          [data]="paymentsList"
          [pagination]="true"
          [pageSize]="10"
          emptyTitle="No payment transactions"
          emptyDescription="Payments will appear here when you reserve slots and settle parking intervals."
          emptyIcon="receipt_long">

          <ng-template let-row let-col="column">
            <!-- Txn ID -->
            <ng-container *ngIf="col.key === 'transactionId'">
              <span class="txn-code">{{ row.transactionId || ('TXN-' + row.id.slice(0, 8).toUpperCase()) }}</span>
            </ng-container>

            <!-- Bay & Facility -->
            <ng-container *ngIf="col.key === 'bay'">
              <span class="bay-text">{{ row.reservationInfo || 'Parking Bay' }}</span>
            </ng-container>

            <!-- Method -->
            <ng-container *ngIf="col.key === 'paymentMethod'">
              <div class="method-cell">
                <span class="material-symbols-outlined method-icon">{{ getMethodIcon(row.paymentMethod) }}</span>
                <span>{{ row.paymentMethod || 'CreditCard' }}</span>
              </div>
            </ng-container>

            <!-- Amount -->
            <ng-container *ngIf="col.key === 'amount'">
              <span class="amount-val">Birr {{ row.amount | number:'1.2-2' }}</span>
            </ng-container>

            <!-- Status -->
            <ng-container *ngIf="col.key === 'status'">
              <app-status-badge [status]="row.status"></app-status-badge>
            </ng-container>

            <!-- Date -->
            <ng-container *ngIf="col.key === 'paidAt'">
              <span class="date-text">{{ (row.paidAt || row.createdAt) | date:'MMM d, y, h:mm a' }}</span>
            </ng-container>

            <!-- Actions -->
            <ng-container *ngIf="col.key === 'actions'">
              <button class="icon-btn" (click)="openReceipt(row, receiptModal)" title="View Receipt">
                <span class="material-symbols-outlined">receipt</span>
              </button>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>
    </div>

    <!-- Quick Pay Modal -->
    <ng-template #payModal let-dialogRef="dialogRef">
      <div class="modal-wrapper">
        <div class="modal-header">
          <div class="modal-icon">
            <span class="material-symbols-outlined">credit_card</span>
          </div>
          <div>
            <h3>Settle Parking Fee</h3>
            <p>Select your pending reservation and authorized payment method.</p>
          </div>
        </div>

        <form [formGroup]="payForm" (ngSubmit)="processPay(dialogRef)" class="modal-form">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Pending Reservation</mat-label>
            <mat-select formControlName="reservationId" (selectionChange)="onReservationSelect()">
              <mat-option *ngFor="let r of pendingReservations" [value]="r.id">
                Bay {{ r.slotNumber }} ({{ r.parkingAreaName }}) - Birr {{ r.totalAmount | number:'1.2-2' }}
              </mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Payment Method</mat-label>
            <mat-select formControlName="paymentMethod">
              <mat-option value="CreditCard">Credit / Debit Card</mat-option>
              <mat-option value="DigitalWallet">Digital Wallet (Apple / Google Pay)</mat-option>
              <mat-option value="Cash">Cash at Gate Station</mat-option>
            </mat-select>
          </mat-form-field>

          <div class="amount-due-box" *ngIf="selectedReservationForPay">
            <span class="due-label">Total Amount Due:</span>
            <span class="due-val">Birr {{ selectedReservationForPay.totalAmount | number:'1.2-2' }}</span>
          </div>

          <div class="modal-actions">
            <button type="button" mat-button (click)="dialogRef.close()">Cancel</button>
            <button type="submit" mat-flat-button color="primary" [disabled]="payForm.invalid || paying">
              {{ paying ? 'Processing...' : 'Authorize Payment' }}
            </button>
          </div>
        </form>
      </div>
    </ng-template>

    <!-- Receipt Modal -->
    <ng-template #receiptModal let-dialogRef="dialogRef">
      <div class="receipt-wrapper" *ngIf="selectedReceipt">
        <div class="receipt-top">
          <div class="receipt-brand">
            <span class="material-symbols-outlined logo-icon">local_parking</span>
            <h4>ParkPulse Official Receipt</h4>
          </div>
          <span class="receipt-date">{{ (selectedReceipt.paidAt || selectedReceipt.createdAt) | date:'medium' }}</span>
        </div>

        <div class="receipt-divider"></div>

        <div class="receipt-details">
          <div class="r-row">
            <span>Transaction Ref:</span>
            <strong class="font-mono">{{ selectedReceipt.transactionId || selectedReceipt.id }}</strong>
          </div>
          <div class="r-row">
            <span>Method:</span>
            <strong>{{ selectedReceipt.paymentMethod }}</strong>
          </div>
          <div class="r-row">
            <span>Status:</span>
            <strong class="text-green">{{ selectedReceipt.status }}</strong>
          </div>
          <div class="r-row total-row">
            <span>Amount Paid:</span>
            <strong class="receipt-price">Birr {{ selectedReceipt.amount | number:'1.2-2' }}</strong>
          </div>
        </div>

        <div class="receipt-footer">
          <button mat-stroked-button (click)="dialogRef.close()">Close Receipt</button>
        </div>
      </div>
    </ng-template>
  `,
  styles: [`
    .customer-payments-container { display: flex; flex-direction: column; gap: 24px; }
    .page-header {
      display: flex; align-items: center; justify-content: space-between; gap: 16px; flex-wrap: wrap;
      .header-titles {
        h2 { margin: 0 0 4px 0; font-size: 1.5rem; font-weight: 800; color: #0f172a; letter-spacing: -0.02em; }
        p { margin: 0; color: #64748b; font-size: 0.875rem; }
      }
      .pay-btn { display: inline-flex; align-items: center; gap: 8px; font-weight: 600; height: 42px; border-radius: 10px; }
    }
    .kpi-grid {
      display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px;
      .kpi-card {
        background: #ffffff; border: 1px solid var(--border-light, #e2e8f0); border-radius: 16px; padding: 20px;
        display: flex; flex-direction: column;
        .kpi-label { font-size: 0.75rem; font-weight: 700; text-transform: uppercase; color: #64748b; margin-bottom: 6px; }
        .kpi-val { font-size: 1.75rem; font-weight: 800; line-height: 1.1; margin-bottom: 4px;
          &.text-blue { color: #2563eb; }
          &.text-green { color: #059669; }
          &.text-amber { color: #d97706; }
        }
        .kpi-sub { font-size: 0.75rem; color: #94a3b8; }
      }
    }
    .txn-code { font-family: 'Courier New', monospace; font-weight: 700; color: #0f172a; background: #f1f5f9; padding: 3px 8px; border-radius: 4px; font-size: 0.8125rem; }
    .bay-text { font-weight: 600; color: #0f172a; }
    .method-cell { display: inline-flex; align-items: center; gap: 6px; font-size: 0.8125rem; color: #475569; .method-icon { font-size: 18px; color: #2563eb; } }
    .amount-val { font-weight: 700; color: #0f172a; }
    .date-text { font-size: 0.8125rem; color: #64748b; }
    .icon-btn {
      width: 32px; height: 32px; border-radius: 6px; border: none; background: transparent; cursor: pointer;
      display: flex; align-items: center; justify-content: center; color: #2563eb;
      &:hover { background: #eff6ff; }
      .material-symbols-outlined { font-size: 20px; }
    }
    .modal-wrapper { padding: 24px; min-width: 400px; }
    .modal-header {
      display: flex; align-items: center; gap: 14px; margin-bottom: 20px;
      .modal-icon { width: 44px; height: 44px; border-radius: 12px; background: #ecfdf5; color: #059669; display: flex; align-items: center; justify-content: center; .material-symbols-outlined { font-size: 24px; } }
      h3 { margin: 0; font-size: 1.25rem; font-weight: 700; color: #0f172a; }
      p { margin: 2px 0 0 0; font-size: 0.8125rem; color: #64748b; }
    }
    .modal-form { display: flex; flex-direction: column; gap: 14px; .w-full { width: 100%; } .modal-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 10px; } }
    .amount-due-box {
      display: flex; align-items: center; justify-content: space-between; padding: 14px 18px; background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 10px;
      .due-label { font-weight: 700; color: #1e40af; }
      .due-val { font-size: 1.25rem; font-weight: 800; color: #1e3a8a; }
    }
    .receipt-wrapper {
      padding: 24px; min-width: 380px;
      .receipt-top { display: flex; flex-direction: column; align-items: center; gap: 6px; text-align: center;
        .receipt-brand { display: flex; align-items: center; gap: 8px; color: #2563eb; h4 { margin: 0; font-size: 1.15rem; font-weight: 800; color: #0f172a; } }
        .receipt-date { font-size: 0.75rem; color: #64748b; }
      }
      .receipt-divider { height: 1px; background: #e2e8f0; margin: 16px 0; }
      .receipt-details {
        display: flex; flex-direction: column; gap: 10px; font-size: 0.875rem;
        .r-row { display: flex; justify-content: space-between; color: #64748b; strong { color: #0f172a; } .text-green { color: #059669; }
          &.total-row { padding-top: 8px; border-top: 1px dashed #cbd5e1; font-size: 1rem; .receipt-price { color: #2563eb; font-size: 1.25rem; font-weight: 800; } }
        }
      }
      .receipt-footer { display: flex; justify-content: center; margin-top: 20px; }
    }
  `]
})
export class CustomerPaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);
  private reservationService = inject(ReservationService);
  private notification = inject(NotificationService);
  private dialog = inject(MatDialog);
  private fb = inject(FormBuilder);

  paymentsList: any[] = [];
  pendingReservations: ReservationDto[] = [];
  selectedReservationForPay?: ReservationDto;
  selectedReceipt?: any;
  loading: boolean = false;
  paying: boolean = false;

  totalSpent: number = 0;
  completedCount: number = 0;

  columns: TableColumn[] = [
    { key: 'transactionId', label: 'Transaction Ref', sortable: true, width: '180px' },
    { key: 'bay', label: 'Reservation / Bay', sortable: true },
    { key: 'paymentMethod', label: 'Method', sortable: true, width: '160px' },
    { key: 'amount', label: 'Amount', sortable: true, width: '120px' },
    { key: 'status', label: 'Status', sortable: true, width: '120px' },
    { key: 'paidAt', label: 'Date', sortable: true, width: '180px' },
    { key: 'actions', label: 'Receipt', align: 'center', width: '80px' }
  ];

  payForm: FormGroup = this.fb.group({
    reservationId: ['', Validators.required],
    paymentMethod: ['CreditCard', Validators.required]
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.reservationService.getMyReservations().subscribe({
      next: (res) => {
        this.loading = false;
        const reservations = res.data || [];
        this.pendingReservations = reservations.filter(r =>
          !r.payment || (r.payment.status || '').toLowerCase() === 'pending'
        );

        const list: any[] = [];
        let total = 0;
        let count = 0;

        for (const r of reservations) {
          if (r.payment) {
            list.push({
              ...r.payment,
              reservationInfo: `Bay ${r.slotNumber} (${r.parkingAreaName || 'Main Facility'})`,
              createdAt: r.payment.paidAt || r.createdAt
            });
            if (r.payment.status === 'Completed') {
              total += r.payment.amount;
              count++;
            }
          }
        }

        this.paymentsList = list;
        this.totalSpent = total;
        this.completedCount = count;
      },
      error: (err) => {
        this.loading = false;
        this.notification.error('Failed to load payments: ' + (err.error?.message || err.message));
      }
    });
  }

  getMethodIcon(method: string): string {
    const m = (method || '').toLowerCase();
    if (m.includes('card')) return 'credit_card';
    if (m.includes('wallet')) return 'account_balance_wallet';
    return 'payments';
  }

  openQuickPay(templateRef: any): void {
    if (this.pendingReservations.length === 0) {
      this.notification.info('You have no reservations currently awaiting settlement.');
      return;
    }
    this.payForm.patchValue({
      reservationId: this.pendingReservations[0].id,
      paymentMethod: 'CreditCard'
    });
    this.onReservationSelect();
    this.dialog.open(templateRef, { width: '440px' });
  }

  onReservationSelect(): void {
    const resId = this.payForm.value.reservationId;
    this.selectedReservationForPay = this.pendingReservations.find(r => r.id === resId);
  }

  processPay(dialogRef: any): void {
    if (this.payForm.invalid || !this.selectedReservationForPay) return;

    this.paying = true;
    const req = {
      reservationId: this.selectedReservationForPay.id,
      paymentMethod: this.payForm.value.paymentMethod,
      amount: this.selectedReservationForPay.totalAmount
    };

    this.paymentService.processPayment(req).subscribe({
      next: () => {
        this.paying = false;
        this.notification.success('Payment approved! Ticket is confirmed.');
        dialogRef.close();
        this.loadData();
      },
      error: (err) => {
        this.paying = false;
        this.notification.error('Payment authorization failed: ' + (err.error?.message || err.message));
      }
    });
  }

  openReceipt(payment: any, templateRef: any): void {
    this.selectedReceipt = payment;
    this.dialog.open(templateRef, { width: '400px' });
  }
}
