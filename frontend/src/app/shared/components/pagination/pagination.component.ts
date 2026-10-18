import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface PageChangeEvent {
  pageIndex: number;
  pageSize: number;
}

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="pagination-container" *ngIf="totalItems > 0">
      <div class="pagination-info">
        <span>Showing <strong>{{ startItem }}</strong> to <strong>{{ endItem }}</strong> of <strong>{{ totalItems }}</strong> entries</span>
      </div>

      <div class="pagination-controls">
        <!-- Page size dropdown -->
        <div class="page-size-selector" *ngIf="showPageSizeSelector">
          <label for="pageSizeSelect">Rows:</label>
          <select 
            id="pageSizeSelect" 
            [ngModel]="pageSize" 
            (ngModelChange)="onPageSizeChange($event)"
            class="size-select">
            <option *ngFor="let size of pageSizeOptions" [value]="size">{{ size }}</option>
          </select>
        </div>

        <!-- Navigation Buttons -->
        <div class="nav-button-group">
          <button 
            type="button"
            class="page-btn nav-btn" 
            [disabled]="pageIndex <= 0" 
            (click)="setPage(0)"
            title="First Page">
            <span class="material-symbols-outlined">first_page</span>
          </button>

          <button 
            type="button"
            class="page-btn nav-btn" 
            [disabled]="pageIndex <= 0" 
            (click)="setPage(pageIndex - 1)"
            title="Previous Page">
            <span class="material-symbols-outlined">chevron_left</span>
          </button>

          <!-- Numbered Pages -->
          <ng-container *ngFor="let page of visiblePages">
            <button 
              *ngIf="page !== -1"
              type="button"
              class="page-btn num-btn" 
              [class.active]="page === pageIndex"
              (click)="setPage(page)">
              {{ page + 1 }}
            </button>
            <span *ngIf="page === -1" class="ellipsis">...</span>
          </ng-container>

          <button 
            type="button"
            class="page-btn nav-btn" 
            [disabled]="pageIndex >= totalPages - 1" 
            (click)="setPage(pageIndex + 1)"
            title="Next Page">
            <span class="material-symbols-outlined">chevron_right</span>
          </button>

          <button 
            type="button"
            class="page-btn nav-btn" 
            [disabled]="pageIndex >= totalPages - 1" 
            (click)="setPage(totalPages - 1)"
            title="Last Page">
            <span class="material-symbols-outlined">last_page</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .pagination-container {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 14px 20px;
      background: #ffffff;
      border-top: 1px solid var(--border-light, #e2e8f0);
      flex-wrap: wrap;
    }

    .pagination-info {
      font-size: 0.875rem;
      color: var(--text-muted, #64748b);
      strong {
        color: var(--text-primary, #0f172a);
      }
    }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .page-size-selector {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 0.875rem;
      color: var(--text-secondary, #475569);

      .size-select {
        padding: 4px 8px;
        border: 1px solid var(--border-medium, #cbd5e1);
        border-radius: var(--radius-sm, 6px);
        background: #ffffff;
        font-size: 0.875rem;
        color: var(--text-primary, #0f172a);
        cursor: pointer;

        &:focus {
          outline: none;
          border-color: var(--primary-600, #2563eb);
        }
      }
    }

    .nav-button-group {
      display: flex;
      align-items: center;
      gap: 4px;

      .page-btn {
        min-width: 34px;
        height: 34px;
        padding: 0 6px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-sm, 6px);
        font-size: 0.875rem;
        font-weight: 600;
        color: var(--text-secondary, #475569);
        cursor: pointer;
        transition: all 0.15s ease;

        &:hover:not(:disabled) {
          border-color: var(--primary-600, #2563eb);
          color: var(--primary-600, #2563eb);
          background: var(--primary-50, #eff6ff);
        }

        &.active {
          background: var(--primary-600, #2563eb);
          border-color: var(--primary-600, #2563eb);
          color: #ffffff;
        }

        &:disabled {
          opacity: 0.4;
          cursor: not-allowed;
        }

        .material-symbols-outlined {
          font-size: 18px;
        }
      }

      .ellipsis {
        padding: 0 4px;
        color: var(--text-muted, #64748b);
        font-size: 0.875rem;
      }
    }
  `]
})
export class PaginationComponent {
  @Input() totalItems: number = 0;
  @Input() pageSize: number = 10;
  @Input() pageIndex: number = 0;
  @Input() pageSizeOptions: number[] = [5, 10, 20, 50];
  @Input() showPageSizeSelector: boolean = true;

  @Output() pageChange = new EventEmitter<PageChangeEvent>();

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize) || 1;
  }

  get startItem(): number {
    if (this.totalItems === 0) return 0;
    return this.pageIndex * this.pageSize + 1;
  }

  get endItem(): number {
    return Math.min((this.pageIndex + 1) * this.pageSize, this.totalItems);
  }

  get visiblePages(): number[] {
    const total = this.totalPages;
    const current = this.pageIndex;
    if (total <= 5) {
      return Array.from({ length: total }, (_, i) => i);
    }
    const pages: number[] = [];
    pages.push(0);
    if (current > 2) pages.push(-1);
    const start = Math.max(1, current - 1);
    const end = Math.min(total - 2, current + 1);
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    if (current < total - 3) pages.push(-1);
    pages.push(total - 1);
    return pages;
  }

  setPage(newIndex: number): void {
    if (newIndex >= 0 && newIndex < this.totalPages && newIndex !== this.pageIndex) {
      this.pageIndex = newIndex;
      this.pageChange.emit({ pageIndex: this.pageIndex, pageSize: this.pageSize });
    }
  }

  onPageSizeChange(newSize: number): void {
    this.pageSize = Number(newSize);
    this.pageIndex = 0;
    this.pageChange.emit({ pageIndex: this.pageIndex, pageSize: this.pageSize });
  }
}
