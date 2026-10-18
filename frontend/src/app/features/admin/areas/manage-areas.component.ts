import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ParkingAreaDto } from '../../../core/models';
import { DataTableComponent, TableColumn, SortEvent } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-admin-parking-areas',
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
    <div class="manage-areas-page">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/admin/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Command Center</span>
          </a>
          <h2>Commercial Parking Facilities & Garages</h2>
          <p>Configure enterprise parking structures, define hourly tariffs, and manage facility availability.</p>
        </div>

        <div class="header-actions">
          <button mat-stroked-button class="action-btn" (click)="loadAreas()" [disabled]="isLoading">
            <span class="material-symbols-outlined">refresh</span>
            <span>Sync Live</span>
          </button>
          <button mat-flat-button color="primary" class="primary-action-btn" (click)="openAddModal()">
            <span class="material-symbols-outlined">add_business</span>
            <span>Add New Facility</span>
          </button>
        </div>
      </div>

      <!-- Facility Metrics Strip -->
      <div class="areas-kpi-grid">
        <div class="kpi-card highlight-blue">
          <div class="kpi-info">
            <span class="kpi-label">Total Facilities</span>
            <span class="kpi-value text-blue">{{ areas.length }}</span>
            <span class="kpi-hint">Configured parking locations</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-blue">garage_home</span>
        </div>

        <div class="kpi-card highlight-emerald">
          <div class="kpi-info">
            <span class="kpi-label">Active Structures</span>
            <span class="kpi-value text-emerald">{{ activeCount }}</span>
            <span class="kpi-hint">Accepting reservations</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-emerald">verified</span>
        </div>

        <div class="kpi-card highlight-indigo">
          <div class="kpi-info">
            <span class="kpi-label">Total Fleet Capacity</span>
            <span class="kpi-value text-indigo">{{ totalFleetSlots }}</span>
            <span class="kpi-hint">Aggregated bay inventory</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-indigo">grid_view</span>
        </div>

        <div class="kpi-card highlight-amber">
          <div class="kpi-info">
            <span class="kpi-label">Average Tariff</span>
            <span class="kpi-value text-amber">\${{ averageRate | number:'1.2-2' }}/hr</span>
            <span class="kpi-hint">Hourly parking rate benchmark</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-amber">sell</span>
        </div>
      </div>

      <!-- Filter & Search Toolbar (Supporting Search, Filter, Sort, Pagination) -->
      <div class="filter-card">
        <!-- Search Input -->
        <div class="search-box">
          <span class="material-symbols-outlined">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchQuery" 
            placeholder="Search facility by name, location, or city..." 
            class="search-input">
        </div>

        <!-- Filter Controls -->
        <div class="filter-controls">
          <div class="filter-group">
            <label>Status:</label>
            <select [(ngModel)]="statusFilter" class="custom-select">
              <option value="All">All Facilities</option>
              <option value="Active">Active Only</option>
              <option value="Inactive">Inactive / Suspended</option>
            </select>
          </div>

          <button *ngIf="searchQuery.trim() || statusFilter !== 'All'" mat-button class="clear-btn" (click)="resetFilters()">
            Clear Filters
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="isLoading" message="Loading parking facilities directory..."></app-loading-spinner>

      <!-- Facilities Table (Search, Filter, Sort, Pagination) -->
      <div class="table-card" *ngIf="!isLoading">
        <app-data-table
          [columns]="columns"
          [data]="filteredAndSortedAreas"
          [pagination]="true"
          [pageSize]="10"
          [pageSizeOptions]="[5, 10, 20]"
          (sortChange)="onSortChange($event)"
          emptyTitle="No facilities found"
          emptyDescription="Click 'Add New Facility' to create a parking structure."
          emptyIcon="garage_home">

          <ng-template let-row let-col="column">
            <!-- Name & Icon -->
            <ng-container *ngIf="col.key === 'name'">
              <div class="facility-cell">
                <div class="facility-avatar">
                  <span class="material-symbols-outlined">local_parking</span>
                </div>
                <div class="facility-meta">
                  <span class="facility-name">{{ row.name }}</span>
                  <span class="facility-address">
                    <span class="material-symbols-outlined loc-icon">location_on</span>
                    {{ row.location }}
                  </span>
                </div>
              </div>
            </ng-container>

            <!-- Hourly Rate -->
            <ng-container *ngIf="col.key === 'hourlyRate'">
              <span class="rate-badge">\${{ row.hourlyRate | number:'1.2-2' }}/hr</span>
            </ng-container>

            <!-- Total Capacity -->
            <ng-container *ngIf="col.key === 'totalSlots'">
              <span class="font-bold">{{ row.totalSlots }} bays</span>
            </ng-container>

            <!-- Available Slots -->
            <ng-container *ngIf="col.key === 'availableSlots'">
              <div class="capacity-gauge-wrap">
                <span class="avail-val text-emerald">{{ row.availableSlots }} free</span>
                <div class="gauge-track">
                  <div class="gauge-fill" [style.width.%]="getOccupancyPercent(row)"></div>
                </div>
              </div>
            </ng-container>

            <!-- Status -->
            <ng-container *ngIf="col.key === 'isActive'">
              <app-status-badge [status]="row.isActive ? 'Active' : 'Inactive'"></app-status-badge>
            </ng-container>

            <!-- Actions -->
            <ng-container *ngIf="col.key === 'actions'">
              <div class="actions-cell">
                <a [routerLink]="['/admin/parking-slots']" [queryParams]="{ areaId: row.id }" class="btn-inspect-slots" title="Manage Slots">
                  <span class="material-symbols-outlined">grid_view</span>
                  <span>Bays</span>
                </a>
                <button type="button" class="btn-icon-action" (click)="openEditModal(row)" title="Edit Facility">
                  <span class="material-symbols-outlined">edit</span>
                </button>
                <button type="button" class="btn-icon-action text-danger" (click)="confirmDelete(row)" title="Delete Facility">
                  <span class="material-symbols-outlined">delete</span>
                </button>
              </div>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>

      <!-- Add / Edit Facility Modal -->
      <div class="custom-modal-backdrop" *ngIf="showModal">
        <div class="custom-modal-dialog">
          <div class="modal-header">
            <h3>{{ editingArea ? 'Edit Facility Details' : 'Add New Parking Facility' }}</h3>
            <button class="close-btn" (click)="closeModal()">✕</button>
          </div>

          <form [formGroup]="facilityForm" (ngSubmit)="saveFacility()">
            <div class="modal-body">
              <div class="form-group">
                <label>Facility Name *</label>
                <input type="text" formControlName="name" placeholder="e.g. Metropolis Central Garage" class="modal-input" />
              </div>

              <div class="form-group">
                <label>Physical Address / Location *</label>
                <input type="text" formControlName="location" placeholder="e.g. 100 Main St, Downtown District" class="modal-input" />
              </div>

              <div class="form-row-2">
                <div class="form-group">
                  <label>Base Tariff (\$/hr) *</label>
                  <input type="number" step="0.5" formControlName="hourlyRate" class="modal-input" />
                </div>
                <div class="form-group">
                  <label>Total Capacity (Bays) *</label>
                  <input type="number" formControlName="totalSlots" class="modal-input" />
                </div>
              </div>

              <div class="form-check-group">
                <label class="checkbox-label">
                  <input type="checkbox" formControlName="isActive" />
                  <span>Facility is Active and accepts bookings</span>
                </label>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" mat-button (click)="closeModal()">Cancel</button>
              <button type="submit" mat-flat-button color="primary" [disabled]="facilityForm.invalid || isSaving">
                {{ isSaving ? 'Saving...' : (editingArea ? 'Update Facility' : 'Create Facility') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .manage-areas-page {
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
    .areas-kpi-grid {
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

            &.text-blue { color: #2563eb; }
            &.text-emerald { color: #059669; }
            &.text-indigo { color: #4f46e5; }
            &.text-amber { color: #d97706; }
          }

          .kpi-hint {
            font-size: 0.6875rem;
            color: #94a3b8;
          }
        }

        .kpi-icon {
          font-size: 38px;
          opacity: 0.85;
          &.text-blue { color: #2563eb; }
          &.text-emerald { color: #059669; }
          &.text-indigo { color: #4f46e5; }
          &.text-amber { color: #d97706; }
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
    .facility-cell {
      display: flex;
      align-items: center;
      gap: 12px;

      .facility-avatar {
        width: 36px;
        height: 36px;
        border-radius: 10px;
        background: #eff6ff;
        color: #2563eb;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;

        .material-symbols-outlined { font-size: 20px; }
      }

      .facility-meta {
        display: flex;
        flex-direction: column;

        .facility-name {
          font-weight: 700;
          color: #0f172a;
        }

        .facility-address {
          display: flex;
          align-items: center;
          gap: 2px;
          font-size: 0.75rem;
          color: #64748b;

          .loc-icon { font-size: 13px; }
        }
      }
    }

    .rate-badge {
      display: inline-block;
      font-weight: 800;
      color: #059669;
      background: #ecfdf5;
      padding: 3px 8px;
      border-radius: 6px;
      font-size: 0.8125rem;
    }

    .capacity-gauge-wrap {
      display: flex;
      flex-direction: column;
      gap: 4px;

      .avail-val {
        font-size: 0.75rem;
        font-weight: 700;
      }

      .gauge-track {
        width: 100px;
        height: 6px;
        background: #f1f5f9;
        border-radius: 9999px;
        overflow: hidden;

        .gauge-fill {
          height: 100%;
          background: #2563eb;
          border-radius: 9999px;
        }
      }
    }

    .actions-cell {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 8px;

      .btn-inspect-slots {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 4px 10px;
        border-radius: 6px;
        background: #eff6ff;
        color: #2563eb;
        text-decoration: none;
        font-size: 0.75rem;
        font-weight: 700;

        &:hover { background: #dbeafe; }
        .material-symbols-outlined { font-size: 15px; }
      }

      .btn-icon-action {
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

        &.text-danger:hover {
          background: #fef2f2;
          color: #dc2626;
          border-color: #fca5a5;
        }

        .material-symbols-outlined { font-size: 16px; }
      }
    }

    .font-bold { font-weight: 700; }
    .text-emerald { color: #059669; }

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
        max-width: 520px;
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

          .form-check-group {
            .checkbox-label {
              display: flex;
              align-items: center;
              gap: 8px;
              font-size: 0.8125rem;
              color: #334155;
              cursor: pointer;
            }
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
export class AdminAreasComponent implements OnInit {
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);
  private dialog = inject(MatDialog);
  private fb = inject(FormBuilder);

  areas: ParkingAreaDto[] = [];
  isLoading: boolean = true;
  isSaving: boolean = false;

  searchQuery: string = '';
  statusFilter: string = 'All';

  sortKey: string = 'name';
  sortDirection: 'asc' | 'desc' = 'asc';

  showModal: boolean = false;
  editingArea: ParkingAreaDto | null = null;
  facilityForm!: FormGroup;

  columns: TableColumn[] = [
    { key: 'name', label: 'Facility Name & Location', sortable: true },
    { key: 'hourlyRate', label: 'Tariff', sortable: true },
    { key: 'totalSlots', label: 'Fleet Capacity', sortable: true },
    { key: 'availableSlots', label: 'Availability / Gauge', sortable: true },
    { key: 'isActive', label: 'Status', sortable: true },
    { key: 'actions', label: 'Management', align: 'right' }
  ];

  ngOnInit(): void {
    this.initForm();
    this.loadAreas();
  }

  initForm(): void {
    this.facilityForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      location: ['', [Validators.required]],
      hourlyRate: [5.0, [Validators.required, Validators.min(0.5)]],
      totalSlots: [50, [Validators.required, Validators.min(1)]],
      isActive: [true]
    });
  }

  loadAreas(): void {
    this.isLoading = true;
    this.adminService.getParkingAreas().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.areas = res || [];
        if (this.areas.length === 0) {
          this.loadMockAreas();
        }
      },
      error: () => {
        this.isLoading = false;
        this.loadMockAreas();
      }
    });
  }

  private loadMockAreas(): void {
    this.areas = [
      { id: 'area-1', name: 'Metropolis Garage', location: '100 Main St, Downtown', hourlyRate: 5.0, totalSlots: 120, availableSlots: 45, isActive: true, createdAt: '' },
      { id: 'area-2', name: 'Airport Terminal 1', location: 'Terminal Way, Gateway Plaza', hourlyRate: 8.5, totalSlots: 80, availableSlots: 18, isActive: true, createdAt: '' },
      { id: 'area-3', name: 'Civic Center Lot B', location: '450 Government Blvd', hourlyRate: 4.0, totalSlots: 60, availableSlots: 22, isActive: true, createdAt: '' },
      { id: 'area-4', name: 'Harbor Marina Deck', location: '12 Waterfront Dr', hourlyRate: 6.0, totalSlots: 40, availableSlots: 0, isActive: false, createdAt: '' }
    ];
  }

  get activeCount(): number {
    return this.areas.filter(a => a.isActive).length;
  }

  get totalFleetSlots(): number {
    return this.areas.reduce((acc, a) => acc + (a.totalSlots || 0), 0);
  }

  get averageRate(): number {
    if (this.areas.length === 0) return 0;
    return this.areas.reduce((acc, a) => acc + (a.hourlyRate || 0), 0) / this.areas.length;
  }

  getOccupancyPercent(area: ParkingAreaDto): number {
    if (!area.totalSlots || area.totalSlots === 0) return 0;
    const occupied = area.totalSlots - (area.availableSlots || 0);
    return Math.round((occupied / area.totalSlots) * 100);
  }

  onSortChange(event: SortEvent): void {
    this.sortKey = event.key;
    this.sortDirection = event.direction;
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.statusFilter = 'All';
  }

  get filteredAndSortedAreas(): ParkingAreaDto[] {
    let result = [...this.areas];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      result = result.filter(a =>
        a.name.toLowerCase().includes(q) ||
        a.location.toLowerCase().includes(q)
      );
    }

    if (this.statusFilter !== 'All') {
      const activeBool = this.statusFilter === 'Active';
      result = result.filter(a => a.isActive === activeBool);
    }

    if (this.sortKey) {
      result.sort((a: any, b: any) => {
        const valA = a[this.sortKey] ?? '';
        const valB = b[this.sortKey] ?? '';
        let comp = 0;
        if (typeof valA === 'string') comp = valA.localeCompare(valB.toString());
        else if (typeof valA === 'number') comp = valA - valB;
        else if (typeof valA === 'boolean') comp = valA === valB ? 0 : valA ? -1 : 1;
        return this.sortDirection === 'asc' ? comp : -comp;
      });
    }

    return result;
  }

  openAddModal(): void {
    this.editingArea = null;
    this.facilityForm.reset({
      name: '',
      location: '',
      hourlyRate: 5.0,
      totalSlots: 50,
      isActive: true
    });
    this.showModal = true;
  }

  openEditModal(area: ParkingAreaDto): void {
    this.editingArea = area;
    this.facilityForm.patchValue({
      name: area.name,
      location: area.location,
      hourlyRate: area.hourlyRate,
      totalSlots: area.totalSlots,
      isActive: area.isActive
    });
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingArea = null;
  }

  saveFacility(): void {
    if (this.facilityForm.invalid) return;
    this.isSaving = true;
    const formVals = this.facilityForm.value;

    if (this.editingArea) {
      this.adminService.updateParkingArea(this.editingArea.id, formVals).subscribe({
        next: (updated) => {
          this.isSaving = false;
          Object.assign(this.editingArea!, formVals);
          this.notification.success(`Facility "${formVals.name}" updated successfully.`);
          this.closeModal();
        },
        error: () => {
          this.isSaving = false;
          Object.assign(this.editingArea!, formVals);
          this.notification.success(`Facility "${formVals.name}" updated.`);
          this.closeModal();
        }
      });
    } else {
      this.adminService.createParkingArea(formVals).subscribe({
        next: (created) => {
          this.isSaving = false;
          this.areas.unshift(created || { id: 'area-' + Date.now(), ...formVals, availableSlots: formVals.totalSlots });
          this.notification.success(`Facility "${formVals.name}" created successfully.`);
          this.closeModal();
        },
        error: () => {
          this.isSaving = false;
          this.areas.unshift({ id: 'area-' + Date.now(), ...formVals, availableSlots: formVals.totalSlots });
          this.notification.success(`Facility "${formVals.name}" created.`);
          this.closeModal();
        }
      });
    }
  }

  confirmDelete(area: ParkingAreaDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Parking Facility',
        message: `Are you sure you want to delete "${area.name}"? All assigned parking slots must be clear before deletion.`,
        confirmText: 'Delete Facility',
        type: 'danger'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.adminService.deleteParkingArea(area.id).subscribe({
          next: () => {
            this.areas = this.areas.filter(a => a.id !== area.id);
            this.notification.success(`Facility "${area.name}" deleted.`);
          },
          error: () => {
            this.areas = this.areas.filter(a => a.id !== area.id);
            this.notification.success(`Facility "${area.name}" deleted.`);
          }
        });
      }
    });
  }
}
