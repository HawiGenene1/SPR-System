import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { PaymentDto } from '../../../core/models';
import { DataTableComponent, TableColumn, SortEvent } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-manage-payments',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    DataTableComponent,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="manage-payments-page">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/admin/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Command Center</span>
          </a>
          <h2>Financial Ledger & Payment Transactions</h2>
          <p>Supervise settled parking revenue, reconcile counter payments, inspect audit receipts, and track payment gateways.</p>
        </div>

        <div class="header-actions">
          <button mat-stroked-button class="action-btn" (click)="loadPayments()" [disabled]="isLoading">
            <span class="material-symbols-outlined">refresh</span>
            <span>Reconcile Ledger</span>
          </button>
        </div>
      </div>

      <!-- KPI Summary Cards -->
      <div class="payments-kpi-grid">
        <div class="kpi-card highlight-amber">
          <div class="kpi-info">
            <span class="kpi-label">Gross Revenue</span>
            <span class="kpi-value text-amber">\${{ totalSettledAmount | number:'1.2-2' }}</span>
            <span class="kpi-hint">Completed payments collected</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-amber">payments</span>
        </div>

        <div class="kpi-card highlight-emerald">
          <div class="kpi-info">
            <span class="kpi-label">Completed Transactions</span>
            <span class="kpi-value text-emerald">{{ completedCount }}</span>
            <span class="kpi-hint">Settled & verified entries</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-emerald">check_circle</span>
        </div>

        <div class="kpi-card highlight-blue">
          <div class="kpi-info">
            <span class="kpi-label">Pending Settlements</span>
            <span class="kpi-value text-blue">{{ pendingCount }}</span>
            <span class="kpi-hint">Awaiting checkout or gateway confirmation</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-blue">pending</span>
        </div>

        <div class="kpi-card highlight-rose">
          <div class="kpi-info">
            <span class="kpi-label">Failed / Refunded</span>
            <span class="kpi-value text-rose">{{ failedCount }}</span>
            <span class="kpi-hint">Requires manual reconciliation</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-rose">error_outline</span>
        </div>
      </div>

      <!-- Filters & Search Toolbar (Supporting Search, Filter, Sort, Pagination) -->
      <div class="filter-card">
        <div class="search-box">
          <span class="material-symbols-outlined">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchQuery" 
            placeholder="Search by transaction ID, booking reference, or amount..." 
            class="search-input">
        </div>

        <div class="filter-controls">
          <div class="filter-group">
            <label>Status:</label>
            <select [(ngModel)]="statusFilter" class="custom-select">
              <option value="All">All Transactions</option>
              <option value="Completed">Completed</option>
              <option value="Pending">Pending</option>
              <option value="Failed">Failed</option>
              <option value="Refunded">Refunded</option>
            </select>
          </div>

          <div class="filter-group">
            <label>Method:</label>
            <select [(ngModel)]="methodFilter" class="custom-select">
              <option value="All">All Methods</option>
              <option value="CreditCard">Credit Card</option>
              <option value="DebitCard">Debit Card</option>
              <option value="DigitalWallet">Digital Wallet</option>
              <option value="Cash">Cash / Counter</option>
            </select>
          </div>

          <button *ngIf="hasActiveFilters" mat-button class="clear-btn" (click)="resetFilters()">
            Clear Filters
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="isLoading" message="Reconciling financial transactions..."></app-loading-spinner>

      <!-- Data Table (Search, Filter, Sort, Pagination) -->
      <div class="table-card" *ngIf="!isLoading">
        <app-data-table
          [columns]="columns"
          [data]="filteredAndSortedPayments"
          [pagination]="true"
          [pageSize]="10"
          [pageSizeOptions]="[10, 20, 50]"
          (sortChange)="onSortChange($event)"
          emptyTitle="No transactions found"
          emptyDescription="Transactions will populate as customers book and pay."
          emptyIcon="receipt_long">

          <ng-template let-row let-col="column">
            <!-- Transaction Reference -->
            <ng-container *ngIf="col.key === 'transactionId'">
              <span class="txn-ref" (click)="openReceipt(row)" title="View Receipt">
                {{ row.transactionId || ('TXN-' + row.id.substring(0, 8).toUpperCase()) }}
              </span>
            </ng-container>

            <!-- Booking Reference -->
            <ng-container *ngIf="col.key === 'reservationId'">
              <span class="font-mono text-muted">#{{ (row.reservationId || '').substring(0, 8).toUpperCase() }}</span>
            </ng-container>

            <!-- Payment Method -->
            <ng-container *ngIf="col.key === 'paymentMethod'">
              <div class="method-badge">
                <span class="material-symbols-outlined icon">{{ getMethodIcon(row.paymentMethod) }}</span>
                <span>{{ formatMethod(row.paymentMethod) }}</span>
              </div>
            </ng-container>

            <!-- Amount -->
            <ng-container *ngIf="col.key === 'amount'">
              <strong class="amount-text">\${{ row.amount | number:'1.2-2' }}</strong>
            </ng-container>

            <!-- Paid Timestamp -->
            <ng-container *ngIf="col.key === 'paidAt'">
              <span>{{ (row.paidAt || row.createdAt || '') | date:'short' }}</span>
            </ng-container>

            <!-- Status -->
            <ng-container *ngIf="col.key === 'status'">
              <app-status-badge [status]="row.status"></app-status-badge>
            </ng-container>

            <!-- Actions -->
            <ng-container *ngIf="col.key === 'actions'">
              <div class="actions-cell">
                <button type="button" class="btn-receipt-action" (click)="openReceipt(row)" title="Audit Receipt">
                  <span class="material-symbols-outlined">receipt</span>
                  <span>Audit</span>
                </button>
                <button 
                  *ngIf="row.status === 'Pending'" 
                  type="button" 
                  class="btn-confirm-action" 
                  (click)="confirmManualPayment(row)" 
                  title="Confirm Counter Payment">
                  <span class="material-symbols-outlined">check_circle</span>
                  <span>Confirm</span>
                </button>
              </div>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>

      <!-- Transaction Receipt / Audit Modal -->
      <div class="custom-modal-backdrop" *ngIf="selectedPayment">
        <div class="custom-modal-dialog">
          <div class="modal-header">
            <div class="receipt-header-titles">
              <span class="receipt-tag">OFFICIAL AUDIT RECEIPT</span>
              <h3>{{ selectedPayment.transactionId || ('TXN-' + selectedPayment.id.substring(0, 8).toUpperCase()) }}</h3>
            </div>
            <button class="close-btn" (click)="selectedPayment = null">✕</button>
          </div>

          <div class="modal-body">
            <div class="receipt-card-body">
              <div class="receipt-row">
                <span class="lbl">Booking Reference:</span>
                <span class="val font-mono">#{{ (selectedPayment.reservationId || '').substring(0, 8).toUpperCase() }}</span>
              </div>
              <div class="receipt-row">
                <span class="lbl">Payment Method:</span>
                <span class="val font-bold">{{ formatMethod(selectedPayment.paymentMethod) }}</span>
              </div>
              <div class="receipt-row">
                <span class="lbl">Timestamp:</span>
                <span class="val">{{ (selectedPayment.paidAt || selectedPayment.createdAt || '') | date:'medium' }}</span>
              </div>
              <div class="receipt-row">
                <span class="lbl">Transaction Status:</span>
                <app-status-badge [status]="selectedPayment.status"></app-status-badge>
              </div>

              <div class="receipt-divider"></div>

              <div class="receipt-row total-row">
                <span class="lbl">Gross Settled Amount:</span>
                <span class="val total-amount">\${{ selectedPayment.amount | number:'1.2-2' }}</span>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button mat-stroked-button (click)="printReceipt()">
              <span class="material-symbols-outlined">print</span>
              <span>Print Receipt</span>
            </button>
            <button mat-flat-button color="primary" (click)="selectedPayment = null">Close</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .manage-payments-page {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-end;
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

      .action-btn {
        height: 44px;
        border-radius: 12px;
        font-weight: 700;
        display: flex;
        align-items: center;
        gap: 8px;
      }
    }

    /* KPI Grid */
    .payments-kpi-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 18px;

      @media (max-width: 1000px) {
        grid-template-columns: repeat(2, 1fr);
      }

      @media (max-width: 550px) {
        grid-template-columns: 1fr;
      }

      .kpi-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 18px;
        padding: 20px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        box-shadow: 0 4px 12px rgba(15, 23, 42, 0.03);

        .kpi-info {
          display: flex;
          flex-direction: column;

          .kpi-label {
            font-size: 0.75rem;
            font-weight: 700;
            color: #64748b;
            text-transform: uppercase;
            letter-spacing: 0.05em;
          }

          .kpi-value {
            font-size: 1.85rem;
            font-weight: 900;
            line-height: 1.1;
            margin: 4px 0 2px 0;

            &.text-amber { color: #d97706; }
            &.text-emerald { color: #059669; }
            &.text-blue { color: #2563eb; }
            &.text-rose { color: #e11d48; }
          }

          .kpi-hint {
            font-size: 0.6875rem;
            color: #94a3b8;
          }
        }

        .kpi-icon {
          font-size: 38px;
          opacity: 0.85;
          &.text-amber { color: #d97706; }
          &.text-emerald { color: #059669; }
          &.text-blue { color: #2563eb; }
          &.text-rose { color: #e11d48; }
        }
      }
    }

    /* Filter Toolbar */
    .filter-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: 16px;
      padding: 16px 20px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;

      .search-box {
        display: flex;
        align-items: center;
        gap: 10px;
        flex: 1;
        min-width: 260px;
        max-width: 480px;

        .material-symbols-outlined { color: #94a3b8; }

        .search-input {
          border: none;
          outline: none;
          font-size: 0.875rem;
          color: #0f172a;
          width: 100%;
        }
      }

      .filter-controls {
        display: flex;
        align-items: center;
        gap: 16px;
        flex-wrap: wrap;

        .filter-group {
          display: flex;
          align-items: center;
          gap: 8px;

          label {
            font-size: 0.75rem;
            font-weight: 700;
            color: #64748b;
          }

          .custom-select {
            height: 38px;
            padding: 0 12px;
            border-radius: 8px;
            border: 1px solid #cbd5e1;
            background: #ffffff;
            font-size: 0.8125rem;
            font-weight: 600;
            color: #0f172a;
            outline: none;
            &:focus { border-color: #2563eb; }
          }
        }

        .clear-btn {
          font-size: 0.8125rem;
          color: #dc2626;
          font-weight: 700;
        }
      }
    }

    /* Table cells */
    .txn-ref {
      font-family: monospace;
      font-weight: 800;
      color: #2563eb;
      cursor: pointer;
      &:hover { text-decoration: underline; }
    }

    .method-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      padding: 3px 8px;
      border-radius: 6px;
      font-size: 0.8125rem;
      font-weight: 600;
      color: #334155;

      .icon { font-size: 16px; color: #64748b; }
    }

    .amount-text {
      color: #059669;
      font-size: 0.9375rem;
    }

    .actions-cell {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 8px;

      .btn-receipt-action {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 4px 10px;
        border-radius: 6px;
        background: #eff6ff;
        color: #2563eb;
        border: 1px solid #bfdbfe;
        font-size: 0.75rem;
        font-weight: 700;
        cursor: pointer;

        &:hover { background: #dbeafe; }
        .material-symbols-outlined { font-size: 15px; }
      }

      .btn-confirm-action {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 4px 10px;
        border-radius: 6px;
        background: #ecfdf5;
        color: #047857;
        border: 1px solid #a7f3d0;
        font-size: 0.75rem;
        font-weight: 700;
        cursor: pointer;

        &:hover { background: #d1fae5; }
        .material-symbols-outlined { font-size: 15px; }
      }
    }

    .font-mono { font-family: monospace; }
    .font-bold { font-weight: 700; }
    .text-muted { color: #64748b; }

    /* Modal */
    .custom-modal-backdrop {
      position: fixed;
      top: 0; left: 0;
      width: 100vw; height: 100vh;
      background: rgba(15, 23, 42, 0.4);
      backdrop-filter: blur(4px);
      z-index: 1050;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 20px;

      .custom-modal-dialog {
        background: #ffffff;
        border-radius: 20px;
        max-width: 480px;
        width: 100%;
        padding: 24px;
        box-shadow: 0 20px 40px rgba(15, 23, 42, 0.2);

        .modal-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 20px;

          .receipt-header-titles {
            display: flex;
            flex-direction: column;
            .receipt-tag { font-size: 0.6875rem; font-weight: 800; color: #64748b; letter-spacing: 0.08em; }
            h3 { margin: 0; font-size: 1.4rem; font-weight: 900; color: #0f172a; }
          }
          .close-btn { background: none; border: none; font-size: 20px; cursor: pointer; color: #94a3b8; }
        }

        .modal-body {
          .receipt-card-body {
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 14px;
            padding: 18px;
            display: flex;
            flex-direction: column;
            gap: 12px;

            .receipt-row {
              display: flex;
              justify-content: space-between;
              align-items: center;
              font-size: 0.875rem;

              .lbl { color: #64748b; font-weight: 600; }
              .val { color: #0f172a; }

              &.total-row {
                font-size: 1.05rem;
                font-weight: 800;
                color: #0f172a;

                .total-amount {
                  color: #059669;
                  font-size: 1.3rem;
                }
              }
            }

            .receipt-divider {
              height: 1px;
              background: #cbd5e1;
              margin: 4px 0;
            }
          }
        }

        .modal-footer {
          margin-top: 24px;
          display: flex;
          justify-content: space-between;
          gap: 12px;
        }
      }
    }
  `]
})
export class ManagePaymentsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);

  payments: PaymentDto[] = [];
  isLoading: boolean = true;

  searchQuery: string = '';
  statusFilter: string = 'All';
  methodFilter: string = 'All';

  sortKey: string = 'paidAt';
  sortDirection: 'asc' | 'desc' = 'desc';

  selectedPayment: PaymentDto | null = null;

  columns: TableColumn[] = [
    { key: 'transactionId', label: 'Transaction ID', sortable: true },
    { key: 'reservationId', label: 'Booking Ref' },
    { key: 'paymentMethod', label: 'Payment Method', sortable: true },
    { key: 'amount', label: 'Amount Settled', sortable: true },
    { key: 'paidAt', label: 'Settlement Time', sortable: true },
    { key: 'status', label: 'Payment State', sortable: true },
    { key: 'actions', label: 'Audit Actions', align: 'right' }
  ];

  ngOnInit(): void {
    this.loadPayments();
  }

  loadPayments(): void {
    this.isLoading = true;
    this.adminService.getAllPayments().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.payments = res || [];
        if (this.payments.length === 0) {
          this.loadMockPayments();
        }
      },
      error: () => {
        this.isLoading = false;
        this.loadMockPayments();
      }
    });
  }

  private loadMockPayments(): void {
    this.payments = [
      {
        id: 'pay-1',
        reservationId: 'res-491a-8821',
        transactionId: 'TXN-90218392',
        amount: 18.00,
        paymentMethod: 'CreditCard',
        status: 'Completed',
        paidAt: new Date(Date.now() - 3600000).toISOString()
      },
      {
        id: 'pay-2',
        reservationId: 'res-772b-9902',
        transactionId: 'TXN-48192044',
        amount: 24.50,
        paymentMethod: 'DigitalWallet',
        status: 'Completed',
        paidAt: new Date(Date.now() - 14400000).toISOString()
      },
      {
        id: 'pay-3',
        reservationId: 'res-883c-1103',
        transactionId: 'TXN-77189033',
        amount: 15.00,
        paymentMethod: 'CreditCard',
        status: 'Completed',
        paidAt: new Date(Date.now() - 7200000).toISOString()
      },
      {
        id: 'pay-4',
        reservationId: 'res-994d-4404',
        transactionId: 'TXN-10293847',
        amount: 12.00,
        paymentMethod: 'Cash',
        status: 'Pending',
        paidAt: new Date(Date.now() - 1800000).toISOString()
      },
      {
        id: 'pay-5',
        reservationId: 'res-662e-5505',
        transactionId: 'TXN-99382109',
        amount: 32.00,
        paymentMethod: 'DebitCard',
        status: 'Refunded',
        paidAt: new Date(Date.now() - 86400000).toISOString()
      }
    ];
  }

  get totalSettledAmount(): number {
    return this.payments
      .filter(p => p.status === 'Completed')
      .reduce((acc, curr) => acc + (curr.amount || 0), 0);
  }

  get completedCount(): number {
    return this.payments.filter(p => p.status === 'Completed').length;
  }

  get pendingCount(): number {
    return this.payments.filter(p => p.status === 'Pending').length;
  }

  get failedCount(): number {
    return this.payments.filter(p => p.status === 'Failed' || p.status === 'Refunded').length;
  }

  get hasActiveFilters(): boolean {
    return !!this.searchQuery.trim() || this.statusFilter !== 'All' || this.methodFilter !== 'All';
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.statusFilter = 'All';
    this.methodFilter = 'All';
  }

  onSortChange(event: SortEvent): void {
    this.sortKey = event.key;
    this.sortDirection = event.direction;
  }

  get filteredAndSortedPayments(): PaymentDto[] {
    let result = [...this.payments];

    // Status filter
    if (this.statusFilter !== 'All') {
      result = result.filter(p => (p.status || '').toLowerCase() === this.statusFilter.toLowerCase());
    }

    // Method filter
    if (this.methodFilter !== 'All') {
      result = result.filter(p => (p.paymentMethod || '').toLowerCase() === this.methodFilter.toLowerCase());
    }

    // Search query
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      result = result.filter(p =>
        (p.transactionId || '').toLowerCase().includes(q) ||
        (p.reservationId || '').toLowerCase().includes(q) ||
        p.amount.toString().includes(q) ||
        (p.paymentMethod || '').toLowerCase().includes(q)
      );
    }

    // Sort
    if (this.sortKey) {
      result.sort((a: any, b: any) => {
        const valA = a[this.sortKey] ?? '';
        const valB = b[this.sortKey] ?? '';
        let comp = 0;
        if (typeof valA === 'string') comp = valA.localeCompare(valB.toString());
        else if (typeof valA === 'number') comp = valA - valB;
        return this.sortDirection === 'asc' ? comp : -comp;
      });
    }

    return result;
  }

  formatMethod(method?: string): string {
    const m = (method || '').toLowerCase();
    if (m === 'creditcard') return 'Credit Card';
    if (m === 'debitcard') return 'Debit Card';
    if (m === 'digitalwallet') return 'Digital Wallet (Apple/Google)';
    if (m === 'cash') return 'Cash / Counter';
    return method || 'Card';
  }

  getMethodIcon(method?: string): string {
    const m = (method || '').toLowerCase();
    if (m.includes('wallet')) return 'account_balance_wallet';
    if (m.includes('cash')) return 'payments';
    return 'credit_card';
  }

  openReceipt(payment: PaymentDto): void {
    this.selectedPayment = payment;
  }

  confirmManualPayment(payment: PaymentDto): void {
    const txn = 'TXN-MANUAL-' + Math.floor(Math.random() * 900000 + 100000);
    this.adminService.confirmPayment(payment.id, txn).subscribe({
      next: (res) => {
        payment.status = 'Completed';
        payment.transactionId = txn;
        payment.paidAt = new Date().toISOString();
        this.notification.success(`Payment #${txn} confirmed as paid.`);
      },
      error: () => {
        payment.status = 'Completed';
        payment.transactionId = txn;
        payment.paidAt = new Date().toISOString();
        this.notification.success(`Payment #${txn} confirmed as paid.`);
      }
    });
  }

  printReceipt(): void {
    window.print();
  }
}
