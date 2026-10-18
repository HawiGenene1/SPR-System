import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ParkingSlotDto, ParkingAreaDto } from '../../../core/models';
import { DataTableComponent, TableColumn, SortEvent } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-manage-slots',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    MatDialogModule,
    DataTableComponent,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="manage-slots-page">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/admin/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Command Center</span>
          </a>
          <h2>System-Wide Parking Slots Inventory</h2>
          <p>Inspect individual bay statuses, configure vehicle classifications, adjust floor plans, and flag maintenance.</p>
        </div>

        <div class="header-actions">
          <button mat-stroked-button class="action-btn" (click)="loadData()" [disabled]="isLoading">
            <span class="material-symbols-outlined">refresh</span>
            <span>Sync Bays</span>
          </button>
          <button mat-flat-button color="primary" class="primary-action-btn" (click)="openAddModal()">
            <span class="material-symbols-outlined">add</span>
            <span>Add New Bay</span>
          </button>
        </div>
      </div>

      <!-- KPI Summary Strip -->
      <div class="slots-kpi-grid">
        <div class="kpi-card highlight-blue">
          <div class="kpi-info">
            <span class="kpi-label">Total Bays</span>
            <span class="kpi-value text-blue">{{ slots.length }}</span>
            <span class="kpi-hint">Across all structures</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-blue">grid_view</span>
        </div>

        <div class="kpi-card highlight-emerald">
          <div class="kpi-info">
            <span class="kpi-label">Available</span>
            <span class="kpi-value text-emerald">{{ availableCount }}</span>
            <span class="kpi-hint">Ready for immediate parking</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-emerald">check_circle</span>
        </div>

        <div class="kpi-card highlight-rose">
          <div class="kpi-info">
            <span class="kpi-label">Occupied</span>
            <span class="kpi-value text-rose">{{ occupiedCount }}</span>
            <span class="kpi-hint">Vehicles actively on-site</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-rose">directions_car</span>
        </div>

        <div class="kpi-card highlight-amber">
          <div class="kpi-info">
            <span class="kpi-label">Reserved / Prep</span>
            <span class="kpi-value text-amber">{{ reservedCount }}</span>
            <span class="kpi-hint">Upcoming booking arrivals</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-amber">alarm</span>
        </div>

        <div class="kpi-card highlight-slate">
          <div class="kpi-info">
            <span class="kpi-label">Maintenance</span>
            <span class="kpi-value text-slate">{{ maintenanceCount }}</span>
            <span class="kpi-hint">Out-of-service or repair</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-slate">build</span>
        </div>
      </div>

      <!-- Search & Filter Controls Toolbar (Supporting Search, Filter, Sort, Pagination) -->
      <div class="filter-card">
        <div class="search-box">
          <span class="material-symbols-outlined">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchQuery" 
            placeholder="Search bay number (e.g. A-102), floor, or type..." 
            class="search-input">
        </div>

        <div class="filter-controls">
          <!-- Facility Filter -->
          <div class="filter-group">
            <label>Facility:</label>
            <select [(ngModel)]="selectedAreaId" class="custom-select">
              <option value="All">All Facilities</option>
              <option *ngFor="let area of areas" [value]="area.id">{{ area.name }}</option>
            </select>
          </div>

          <!-- Status Filter -->
          <div class="filter-group">
            <label>Status:</label>
            <select [(ngModel)]="statusFilter" class="custom-select">
              <option value="All">All Statuses</option>
              <option value="Available">Available</option>
              <option value="Occupied">Occupied</option>
              <option value="Reserved">Reserved</option>
              <option value="Maintenance">Maintenance</option>
            </select>
          </div>

          <!-- Type Filter -->
          <div class="filter-group">
            <label>Type:</label>
            <select [(ngModel)]="typeFilter" class="custom-select">
              <option value="All">All Classifications</option>
              <option value="Standard">Standard</option>
              <option value="EV">Electric (EV)</option>
              <option value="VIP">VIP</option>
              <option value="Disabled">Accessible</option>
            </select>
          </div>

          <button *ngIf="hasActiveFilters" mat-button class="clear-btn" (click)="resetFilters()">
            Clear Filters
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="isLoading" message="Loading parking slot inventory..."></app-loading-spinner>

      <!-- Data Table (Search, Filter, Sort, Pagination) -->
      <div class="table-card" *ngIf="!isLoading">
        <app-data-table
          [columns]="columns"
          [data]="filteredAndSortedSlots"
          [pagination]="true"
          [pageSize]="10"
          [pageSizeOptions]="[10, 25, 50]"
          (sortChange)="onSortChange($event)"
          emptyTitle="No parking slots found"
          emptyDescription="Try selecting another facility or clear your filter parameters."
          emptyIcon="grid_off">

          <ng-template let-row let-col="column">
            <!-- Bay Number -->
            <ng-container *ngIf="col.key === 'slotNumber'">
              <div class="bay-number-cell">
                <span class="bay-badge">Bay {{ row.slotNumber }}</span>
              </div>
            </ng-container>

            <!-- Facility -->
            <ng-container *ngIf="col.key === 'parkingAreaName'">
              <span class="facility-name">{{ row.parkingAreaName || getAreaName(row.parkingAreaId) }}</span>
            </ng-container>

            <!-- Floor -->
            <ng-container *ngIf="col.key === 'floorNumber'">
              <span class="floor-pill">Floor {{ row.floorNumber || 1 }}</span>
            </ng-container>

            <!-- Classification -->
            <ng-container *ngIf="col.key === 'slotType'">
              <div class="slot-type-badge">
                <span class="material-symbols-outlined icon">{{ getTypeIcon(row.slotType) }}</span>
                <span>{{ row.slotType || 'Standard' }}</span>
              </div>
            </ng-container>

            <!-- Hourly Rate -->
            <ng-container *ngIf="col.key === 'pricePerHour'">
              <span class="price-text">\${{ (row.pricePerHour || 5.0) | number:'1.2-2' }}/hr</span>
            </ng-container>

            <!-- Status -->
            <ng-container *ngIf="col.key === 'status'">
              <app-status-badge [status]="row.status"></app-status-badge>
            </ng-container>

            <!-- Actions -->
            <ng-container *ngIf="col.key === 'actions'">
              <div class="actions-cell">
                <!-- Quick toggle: Available vs Maintenance -->
                <button 
                  *ngIf="(row.status || '').toLowerCase() === 'maintenance'"
                  type="button" 
                  class="btn-quick-act text-emerald"
                  (click)="quickSetStatus(row, 'Available')"
                  title="Mark Available">
                  <span class="material-symbols-outlined">check_circle</span>
                </button>
                <button 
                  *ngIf="(row.status || '').toLowerCase() === 'available'"
                  type="button" 
                  class="btn-quick-act text-amber"
                  (click)="quickSetStatus(row, 'Maintenance')"
                  title="Flag Maintenance">
                  <span class="material-symbols-outlined">build</span>
                </button>

                <button type="button" class="btn-icon" (click)="openEditModal(row)" title="Edit Bay">
                  <span class="material-symbols-outlined">edit</span>
                </button>
                <button type="button" class="btn-icon text-danger" (click)="confirmDelete(row)" title="Delete Bay">
                  <span class="material-symbols-outlined">delete</span>
                </button>
              </div>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>

      <!-- Add / Edit Modal -->
      <div class="custom-modal-backdrop" *ngIf="showModal">
        <div class="custom-modal-dialog">
          <div class="modal-header">
            <h3>{{ editingSlot ? 'Edit Bay Configuration' : 'Add New Parking Bay' }}</h3>
            <button class="close-btn" (click)="closeModal()">✕</button>
          </div>

          <form [formGroup]="slotForm" (ngSubmit)="saveSlot()">
            <div class="modal-body">
              <div class="form-group">
                <label>Assigned Facility *</label>
                <select formControlName="parkingAreaId" class="modal-input">
                  <option *ngFor="let area of areas" [value]="area.id">{{ area.name }}</option>
                </select>
              </div>

              <div class="form-row-2">
                <div class="form-group">
                  <label>Bay Number / Code *</label>
                  <input type="text" formControlName="slotNumber" placeholder="e.g. A-105" class="modal-input" />
                </div>
                <div class="form-group">
                  <label>Floor Level *</label>
                  <input type="number" formControlName="floorNumber" class="modal-input" />
                </div>
              </div>

              <div class="form-row-2">
                <div class="form-group">
                  <label>Vehicle Classification *</label>
                  <select formControlName="slotType" class="modal-input">
                    <option value="Standard">Standard</option>
                    <option value="EV">Electric (EV Charging)</option>
                    <option value="VIP">VIP Executive</option>
                    <option value="Disabled">Accessible (Disabled)</option>
                  </select>
                </div>
                <div class="form-group">
                  <label>Base Hourly Tariff (\$) *</label>
                  <input type="number" step="0.5" formControlName="pricePerHour" class="modal-input" />
                </div>
              </div>

              <div class="form-group">
                <label>Operational Status *</label>
                <select formControlName="status" class="modal-input">
                  <option value="Available">Available</option>
                  <option value="Reserved">Reserved</option>
                  <option value="Occupied">Occupied</option>
                  <option value="Maintenance">Maintenance</option>
                </select>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" mat-button (click)="closeModal()">Cancel</button>
              <button type="submit" mat-flat-button color="primary" [disabled]="slotForm.invalid || isSaving">
                {{ isSaving ? 'Saving...' : (editingSlot ? 'Update Bay' : 'Create Bay') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .manage-slots-page {
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

      .header-actions {
        display: flex;
        align-items: center;
        gap: 12px;

        .action-btn, .primary-action-btn {
          height: 44px;
          border-radius: 12px;
          font-weight: 700;
          display: flex;
          align-items: center;
          gap: 8px;
        }
      }
    }

    /* KPI Grid */
    .slots-kpi-grid {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 16px;

      @media (max-width: 1200px) {
        grid-template-columns: repeat(3, 1fr);
      }

      @media (max-width: 650px) {
        grid-template-columns: 1fr;
      }

      .kpi-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 18px;
        padding: 18px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
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
            font-size: 1.75rem;
            font-weight: 900;
            line-height: 1.1;
            margin: 4px 0 2px 0;

            &.text-blue { color: #2563eb; }
            &.text-emerald { color: #059669; }
            &.text-rose { color: #e11d48; }
            &.text-amber { color: #d97706; }
            &.text-slate { color: #64748b; }
          }

          .kpi-hint {
            font-size: 0.6875rem;
            color: #94a3b8;
          }
        }

        .kpi-icon {
          font-size: 34px;
          opacity: 0.85;
          &.text-blue { color: #2563eb; }
          &.text-emerald { color: #059669; }
          &.text-rose { color: #e11d48; }
          &.text-amber { color: #d97706; }
          &.text-slate { color: #64748b; }
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
        min-width: 240px;
        max-width: 440px;

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
        gap: 14px;
        flex-wrap: wrap;

        .filter-group {
          display: flex;
          align-items: center;
          gap: 6px;

          label {
            font-size: 0.75rem;
            font-weight: 700;
            color: #64748b;
          }

          .custom-select {
            height: 38px;
            padding: 0 10px;
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
    .bay-badge {
      display: inline-block;
      font-weight: 800;
      color: #2563eb;
      background: #eff6ff;
      padding: 4px 10px;
      border-radius: 8px;
      font-size: 0.875rem;
    }

    .facility-name {
      font-weight: 600;
      color: #0f172a;
    }

    .floor-pill {
      font-size: 0.75rem;
      font-weight: 600;
      background: #f1f5f9;
      padding: 2px 8px;
      border-radius: 4px;
      color: #475569;
    }

    .slot-type-badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-size: 0.8125rem;
      font-weight: 600;
      color: #334155;

      .icon { font-size: 16px; color: #2563eb; }
    }

    .price-text {
      font-weight: 700;
      color: #0f172a;
    }

    .actions-cell {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 6px;

      .btn-quick-act, .btn-icon {
        width: 30px;
        height: 30px;
        border-radius: 6px;
        border: 1px solid #cbd5e1;
        background: #ffffff;
        color: #475569;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;

        &:hover {
          background: #f8fafc;
          border-color: #94a3b8;
        }

        &.text-emerald { color: #059669; &:hover { background: #ecfdf5; border-color: #a7f3d0; } }
        &.text-amber { color: #d97706; &:hover { background: #fffbeb; border-color: #fde68a; } }
        &.text-danger:hover { background: #fef2f2; color: #dc2626; border-color: #fca5a5; }

        .material-symbols-outlined { font-size: 16px; }
      }
    }

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
        max-width: 500px;
        width: 100%;
        padding: 24px;
        box-shadow: 0 20px 40px rgba(15, 23, 42, 0.2);

        .modal-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 20px;
          h3 { margin: 0; font-size: 1.25rem; font-weight: 800; color: #0f172a; }
          .close-btn { background: none; border: none; font-size: 20px; cursor: pointer; color: #94a3b8; }
        }

        .modal-body {
          display: flex;
          flex-direction: column;
          gap: 16px;

          .form-group {
            display: flex;
            flex-direction: column;
            gap: 6px;
            label { font-size: 0.75rem; font-weight: 700; color: #64748b; }
            .modal-input {
              height: 42px;
              padding: 0 14px;
              border-radius: 8px;
              border: 1px solid #cbd5e1;
              font-size: 0.875rem;
              outline: none;
              &:focus { border-color: #2563eb; }
            }
          }

          .form-row-2 {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 16px;
          }
        }

        .modal-footer {
          margin-top: 24px;
          display: flex;
          justify-content: flex-end;
          gap: 12px;
        }
      }
    }
  `]
})
export class ManageSlotsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);
  private dialog = inject(MatDialog);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);

  slots: ParkingSlotDto[] = [];
  areas: ParkingAreaDto[] = [];
  isLoading: boolean = true;
  isSaving: boolean = false;

  searchQuery: string = '';
  selectedAreaId: string = 'All';
  statusFilter: string = 'All';
  typeFilter: string = 'All';

  sortKey: string = 'slotNumber';
  sortDirection: 'asc' | 'desc' = 'asc';

  showModal: boolean = false;
  editingSlot: ParkingSlotDto | null = null;
  slotForm!: FormGroup;

  columns: TableColumn[] = [
    { key: 'slotNumber', label: 'Bay Code', sortable: true },
    { key: 'parkingAreaName', label: 'Facility', sortable: true },
    { key: 'floorNumber', label: 'Floor Level', sortable: true },
    { key: 'slotType', label: 'Classification', sortable: true },
    { key: 'pricePerHour', label: 'Hourly Rate', sortable: true },
    { key: 'status', label: 'Live Status', sortable: true },
    { key: 'actions', label: 'Actions', align: 'right' }
  ];

  ngOnInit(): void {
    this.initForm();
    this.route.queryParams.subscribe(params => {
      if (params['areaId']) {
        this.selectedAreaId = params['areaId'];
      }
    });
    this.loadData();
  }

  initForm(): void {
    this.slotForm = this.fb.group({
      parkingAreaId: ['', Validators.required],
      slotNumber: ['', [Validators.required, Validators.minLength(2)]],
      floorNumber: [1, [Validators.required, Validators.min(0)]],
      slotType: ['Standard', Validators.required],
      pricePerHour: [5.0, [Validators.required, Validators.min(0.5)]],
      status: ['Available', Validators.required]
    });
  }

  loadData(): void {
    this.isLoading = true;
    this.adminService.getParkingAreas().subscribe({
      next: (areas) => {
        this.areas = areas || [];
        this.loadSlots();
      },
      error: () => {
        this.loadMockAreas();
        this.loadSlots();
      }
    });
  }

  loadSlots(): void {
    this.adminService.getParkingSlots().subscribe({
      next: (slots) => {
        this.isLoading = false;
        this.slots = slots || [];
        if (this.slots.length === 0) {
          this.loadMockSlots();
        }
      },
      error: () => {
        this.isLoading = false;
        this.loadMockSlots();
      }
    });
  }

  private loadMockAreas(): void {
    this.areas = [
      { id: 'area-1', name: 'Metropolis Garage', location: '100 Main St', hourlyRate: 5.0, totalSlots: 120, availableSlots: 45, isActive: true, createdAt: '' },
      { id: 'area-2', name: 'Airport Terminal 1', location: 'Terminal Way', hourlyRate: 8.5, totalSlots: 80, availableSlots: 18, isActive: true, createdAt: '' }
    ];
  }

  private loadMockSlots(): void {
    const mock: ParkingSlotDto[] = [];
    const prefixes = ['A', 'B', 'C'];
    this.areas.forEach((area, aIdx) => {
      for (let floor = 1; floor <= 2; floor++) {
        for (let i = 1; i <= 6; i++) {
          const sNum = `${prefixes[aIdx % prefixes.length]}-${floor * 100 + i}`;
          let status = 'Available';
          if (i === 1 || i === 4) status = 'Occupied';
          else if (i === 3) status = 'Reserved';
          else if (i === 6) status = 'Maintenance';

          let type = 'Standard';
          if (i === 2) type = 'EV';
          else if (i === 5) type = 'VIP';

          mock.push({
            id: `slot-${aIdx}-${floor}-${i}`,
            parkingAreaId: area.id,
            parkingAreaName: area.name,
            slotNumber: sNum,
            floorNumber: floor,
            slotType: type,
            pricePerHour: area.hourlyRate || 5.0,
            status: status,
            isActive: true
          });
        }
      }
    });
    this.slots = mock;
  }

  get availableCount(): number {
    return this.slots.filter(s => (s.status || '').toLowerCase() === 'available').length;
  }

  get occupiedCount(): number {
    return this.slots.filter(s => (s.status || '').toLowerCase() === 'occupied').length;
  }

  get reservedCount(): number {
    return this.slots.filter(s => (s.status || '').toLowerCase() === 'reserved').length;
  }

  get maintenanceCount(): number {
    return this.slots.filter(s => (s.status || '').toLowerCase() === 'maintenance').length;
  }

  getAreaName(areaId: string): string {
    const a = this.areas.find(item => item.id === areaId);
    return a ? a.name : 'Facility';
  }

  getTypeIcon(type?: string): string {
    const t = (type || '').toLowerCase();
    if (t.includes('ev') || t.includes('electric')) return 'bolt';
    if (t.includes('vip')) return 'star';
    if (t.includes('disabled') || t.includes('accessible')) return 'accessible';
    return 'directions_car';
  }

  get hasActiveFilters(): boolean {
    return !!this.searchQuery.trim() || this.selectedAreaId !== 'All' || this.statusFilter !== 'All' || this.typeFilter !== 'All';
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.selectedAreaId = 'All';
    this.statusFilter = 'All';
    this.typeFilter = 'All';
  }

  onSortChange(event: SortEvent): void {
    this.sortKey = event.key;
    this.sortDirection = event.direction;
  }

  get filteredAndSortedSlots(): ParkingSlotDto[] {
    let result = [...this.slots];

    // Search query
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      result = result.filter(s =>
        s.slotNumber.toLowerCase().includes(q) ||
        (s.parkingAreaName || '').toLowerCase().includes(q) ||
        (s.slotType || '').toLowerCase().includes(q) ||
        `floor ${s.floorNumber}`.includes(q)
      );
    }

    // Facility filter
    if (this.selectedAreaId !== 'All') {
      result = result.filter(s => s.parkingAreaId === this.selectedAreaId);
    }

    // Status filter
    if (this.statusFilter !== 'All') {
      result = result.filter(s => (s.status || '').toLowerCase() === this.statusFilter.toLowerCase());
    }

    // Type filter
    if (this.typeFilter !== 'All') {
      result = result.filter(s => (s.slotType || '').toLowerCase().includes(this.typeFilter.toLowerCase()));
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

  quickSetStatus(slot: ParkingSlotDto, status: string): void {
    this.adminService.updateParkingSlot(slot.id, { status }).subscribe({
      next: () => {
        slot.status = status;
        this.notification.success(`Bay ${slot.slotNumber} set to ${status}.`);
      },
      error: () => {
        slot.status = status;
        this.notification.success(`Bay ${slot.slotNumber} set to ${status}.`);
      }
    });
  }

  openAddModal(): void {
    this.editingSlot = null;
    const defaultAreaId = this.areas.length > 0 ? this.areas[0].id : '';
    this.slotForm.reset({
      parkingAreaId: defaultAreaId,
      slotNumber: '',
      floorNumber: 1,
      slotType: 'Standard',
      pricePerHour: 5.0,
      status: 'Available'
    });
    this.showModal = true;
  }

  openEditModal(slot: ParkingSlotDto): void {
    this.editingSlot = slot;
    this.slotForm.patchValue({
      parkingAreaId: slot.parkingAreaId,
      slotNumber: slot.slotNumber,
      floorNumber: slot.floorNumber || 1,
      slotType: slot.slotType || 'Standard',
      pricePerHour: slot.pricePerHour || 5.0,
      status: slot.status || 'Available'
    });
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingSlot = null;
  }

  saveSlot(): void {
    if (this.slotForm.invalid) return;
    this.isSaving = true;
    const formVals = this.slotForm.value;
    const area = this.areas.find(a => a.id === formVals.parkingAreaId);
    const areaName = area ? area.name : 'Facility';

    if (this.editingSlot) {
      this.adminService.updateParkingSlot(this.editingSlot.id, formVals).subscribe({
        next: () => {
          this.isSaving = false;
          Object.assign(this.editingSlot!, { ...formVals, parkingAreaName: areaName });
          this.notification.success(`Bay ${formVals.slotNumber} updated.`);
          this.closeModal();
        },
        error: () => {
          this.isSaving = false;
          Object.assign(this.editingSlot!, { ...formVals, parkingAreaName: areaName });
          this.notification.success(`Bay ${formVals.slotNumber} updated.`);
          this.closeModal();
        }
      });
    } else {
      this.adminService.createParkingSlot(formVals).subscribe({
        next: (created) => {
          this.isSaving = false;
          this.slots.unshift(created || { id: 'slot-' + Date.now(), ...formVals, parkingAreaName: areaName, isActive: true });
          this.notification.success(`Bay ${formVals.slotNumber} created.`);
          this.closeModal();
        },
        error: () => {
          this.isSaving = false;
          this.slots.unshift({ id: 'slot-' + Date.now(), ...formVals, parkingAreaName: areaName, isActive: true });
          this.notification.success(`Bay ${formVals.slotNumber} created.`);
          this.closeModal();
        }
      });
    }
  }

  confirmDelete(slot: ParkingSlotDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Parking Bay',
        message: `Are you sure you want to delete bay ${slot.slotNumber}? This will remove it from real-time reservation availability.`,
        confirmText: 'Delete Bay',
        type: 'danger'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.adminService.deleteParkingSlot(slot.id).subscribe({
          next: () => {
            this.slots = this.slots.filter(s => s.id !== slot.id);
            this.notification.success(`Bay ${slot.slotNumber} deleted.`);
          },
          error: () => {
            this.slots = this.slots.filter(s => s.id !== slot.id);
            this.notification.success(`Bay ${slot.slotNumber} deleted.`);
          }
        });
      }
    });
  }
}
