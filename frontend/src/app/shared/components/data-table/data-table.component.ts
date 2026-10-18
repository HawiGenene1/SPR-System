import { Component, ContentChild, EventEmitter, Input, Output, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingSpinnerComponent } from '../loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { PageChangeEvent, PaginationComponent } from '../pagination/pagination.component';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  align?: 'left' | 'center' | 'right';
  width?: string;
}

export interface SortEvent {
  key: string;
  direction: 'asc' | 'desc';
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <div class="data-table-container">
      <!-- Loading Overlay -->
      <app-loading-spinner *ngIf="loading" [overlay]="false" [message]="loadingMessage"></app-loading-spinner>

      <!-- Table Content when not loading -->
      <ng-container *ngIf="!loading">
        <!-- If Empty -->
        <app-empty-state 
          *ngIf="!data || data.length === 0"
          [icon]="emptyIcon"
          [title]="emptyTitle"
          [description]="emptyDescription"
          [actionText]="emptyActionText"
          [actionRoute]="emptyActionRoute"
          (action)="emptyAction.emit()">
        </app-empty-state>

        <!-- Table View when data exists -->
        <div class="table-responsive" *ngIf="data && data.length > 0">
          <table class="custom-data-table">
            <thead>
              <tr>
                <th 
                  *ngFor="let col of columns" 
                  [style.width]="col.width"
                  [style.text-align]="col.align || 'left'"
                  [class.sortable]="col.sortable"
                  (click)="onHeaderClick(col)">
                  <div class="th-content" [style.justify-content]="col.align === 'right' ? 'flex-end' : col.align === 'center' ? 'center' : 'flex-start'">
                    <span>{{ col.label }}</span>
                    <span *ngIf="col.sortable" class="material-symbols-outlined sort-icon">
                      {{ sortKey === col.key ? (sortDir === 'asc' ? 'arrow_upward' : 'arrow_downward') : 'unfold_more' }}
                    </span>
                  </div>
                </th>
              </tr>
            </thead>
            <tbody>
              <tr 
                *ngFor="let row of paginatedData; let i = index" 
                [class.clickable]="rowClickable"
                (click)="onRowClick(row)">
                <td 
                  *ngFor="let col of columns" 
                  [style.text-align]="col.align || 'left'">
                  <!-- Custom cell template if provided -->
                  <ng-container *ngIf="cellTemplate; else defaultCell">
                    <ng-container *ngTemplateOutlet="cellTemplate; context: { $implicit: row, column: col, value: row[col.key], index: i }"></ng-container>
                  </ng-container>

                  <!-- Default cell text -->
                  <ng-template #defaultCell>
                    {{ row[col.key] !== undefined && row[col.key] !== null ? row[col.key] : '-' }}
                  </ng-template>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <app-pagination 
          *ngIf="pagination && (totalItems || data.length) > 0"
          [totalItems]="totalItems || data.length"
          [pageSize]="pageSize"
          [pageIndex]="pageIndex"
          [pageSizeOptions]="pageSizeOptions"
          (pageChange)="onPageChange($event)">
        </app-pagination>
      </ng-container>
    </div>
  `,
  styles: [`
    .data-table-container {
      background: #ffffff;
      border-radius: var(--radius-lg, 16px);
      border: 1px solid var(--border-light, #e2e8f0);
      box-shadow: 0 4px 14px rgba(15, 23, 42, 0.04);
      overflow: hidden;
      width: 100%;
      box-sizing: border-box;
    }

    .table-responsive {
      width: 100%;
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;

      &::-webkit-scrollbar {
        height: 6px;
      }
      &::-webkit-scrollbar-thumb {
        background: #cbd5e1;
        border-radius: 4px;
      }
    }

    .custom-data-table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
      font-size: 0.875rem;
      min-width: 600px;

      thead {
        background: #f8fafc;
        border-bottom: 1px solid var(--border-light, #e2e8f0);

        th {
          padding: 14px 18px;
          font-weight: 700;
          color: var(--text-secondary, #475569);
          letter-spacing: -0.01em;
          user-select: none;
          white-space: nowrap;

          &.sortable {
            cursor: pointer;
            &:hover {
              color: var(--primary-600, #2563eb);
              background: #f1f5f9;
            }
          }

          .th-content {
            display: flex;
            align-items: center;
            gap: 6px;

            .sort-icon {
              font-size: 16px;
              color: var(--text-muted, #64748b);
            }
          }
        }
      }

      tbody {
        tr {
          border-bottom: 1px solid var(--border-light, #f1f5f9);
          transition: background-color 0.15s ease;

          &:last-child {
            border-bottom: none;
          }

          &:hover {
            background-color: #f8faff;
          }

          &.clickable {
            cursor: pointer;
          }

          td {
            padding: 15px 18px;
            color: var(--text-primary, #0f172a);
            vertical-align: middle;
            white-space: normal;
          }
        }
      }
    }

    @media (max-width: 768px) {
      .custom-data-table {
        font-size: 0.8125rem;

        thead th {
          padding: 10px 12px;
        }

        tbody td {
          padding: 11px 12px;
        }
      }
    }
  `]
})
export class DataTableComponent {
  @Input({ required: true }) columns: TableColumn[] = [];
  @Input() data: any[] = [];
  @Input() loading: boolean = false;
  @Input() loadingMessage: string = 'Loading records...';
  
  @Input() emptyTitle: string = 'No entries found';
  @Input() emptyDescription: string = 'Try adjusting your search filters or add a new record.';
  @Input() emptyIcon: string = 'folder_open';
  @Input() emptyActionText?: string;
  @Input() emptyActionRoute?: string;

  @Input() rowClickable: boolean = false;
  @Input() pagination: boolean = true;
  @Input() totalItems?: number;
  @Input() pageIndex: number = 0;
  @Input() pageSize: number = 10;
  @Input() pageSizeOptions: number[] = [5, 10, 20, 50];

  @Output() rowClick = new EventEmitter<any>();
  @Output() sortChange = new EventEmitter<SortEvent>();
  @Output() pageChange = new EventEmitter<PageChangeEvent>();
  @Output() emptyAction = new EventEmitter<void>();

  @ContentChild(TemplateRef) cellTemplate?: TemplateRef<any>;

  sortKey: string = '';
  sortDir: 'asc' | 'desc' = 'asc';

  get paginatedData(): any[] {
    if (this.totalItems === undefined && this.pagination) {
      const start = this.pageIndex * this.pageSize;
      return this.data.slice(start, start + this.pageSize);
    }
    return this.data;
  }

  onHeaderClick(col: TableColumn): void {
    if (!col.sortable) return;
    if (this.sortKey === col.key) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortKey = col.key;
      this.sortDir = 'asc';
    }
    this.sortChange.emit({ key: this.sortKey, direction: this.sortDir });
  }

  onRowClick(row: any): void {
    if (this.rowClickable) {
      this.rowClick.emit(row);
    }
  }

  onPageChange(event: PageChangeEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.pageChange.emit(event);
  }
}
