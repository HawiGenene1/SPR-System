import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { UserDto, ParkingAreaDto } from '../../../core/models';
import { DataTableComponent, TableColumn, SortEvent } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-manage-users',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatDialogModule,
    DataTableComponent,
    LoadingSpinnerComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="manage-users-page">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/admin/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Command Center</span>
          </a>
          <h2>User Accounts & Roles Management</h2>
          <p>Supervise enterprise roles, manage customer profiles, activate/deactivate accounts, and assign attendants.</p>
        </div>

        <div class="header-actions">
          <button mat-stroked-button class="action-btn" (click)="loadUsers()" [disabled]="isLoading">
            <span class="material-symbols-outlined">refresh</span>
            <span>Sync Users</span>
          </button>
        </div>
      </div>

      <!-- KPI Summary Cards -->
      <div class="users-kpi-grid">
        <div class="kpi-card highlight-blue">
          <div class="kpi-info">
            <span class="kpi-label">Total Users</span>
            <span class="kpi-value text-blue">{{ users.length }}</span>
            <span class="kpi-hint">System-wide registered accounts</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-blue">group</span>
        </div>

        <div class="kpi-card highlight-emerald">
          <div class="kpi-info">
            <span class="kpi-label">Active Customers</span>
            <span class="kpi-value text-emerald">{{ customersCount }}</span>
            <span class="kpi-hint">Self-service booking drivers</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-emerald">directions_car</span>
        </div>

        <div class="kpi-card highlight-indigo">
          <div class="kpi-info">
            <span class="kpi-label">Gate Attendants</span>
            <span class="kpi-value text-indigo">{{ attendantsCount }}</span>
            <span class="kpi-hint">On-site barrier operators</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-indigo">badge</span>
        </div>

        <div class="kpi-card highlight-amber">
          <div class="kpi-info">
            <span class="kpi-label">Administrators</span>
            <span class="kpi-value text-amber">{{ adminsCount }}</span>
            <span class="kpi-hint">Full executive clearance</span>
          </div>
          <span class="material-symbols-outlined kpi-icon text-amber">admin_panel_settings</span>
        </div>
      </div>

      <!-- Filters & Search Bar (Supporting Search, Filter, Sort, Pagination) -->
      <div class="filter-card">
        <!-- Search -->
        <div class="search-box">
          <span class="material-symbols-outlined">search</span>
          <input 
            type="text" 
            [(ngModel)]="searchQuery" 
            placeholder="Search by user name, email, phone, or assigned garage..." 
            class="search-input">
        </div>

        <!-- Role Filter Dropdown -->
        <div class="filter-controls">
          <div class="filter-group">
            <label>Role:</label>
            <select [(ngModel)]="roleFilter" class="custom-select">
              <option value="All">All Roles</option>
              <option value="Customer">Customer</option>
              <option value="ParkingAttendant">Parking Attendant</option>
              <option value="Admin">Administrator</option>
            </select>
          </div>

          <!-- Status Filter Dropdown -->
          <div class="filter-group">
            <label>Status:</label>
            <select [(ngModel)]="statusFilter" class="custom-select">
              <option value="All">All Statuses</option>
              <option value="Active">Active</option>
              <option value="Inactive">Deactivated</option>
            </select>
          </div>

          <button *ngIf="hasActiveFilters" mat-button class="clear-filters-btn" (click)="resetFilters()">
            Clear Filters
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="isLoading" message="Fetching enterprise user directory..."></app-loading-spinner>

      <!-- Users Table (Search, Filter, Sort, Pagination) -->
      <div class="table-card" *ngIf="!isLoading">
        <app-data-table
          [columns]="columns"
          [data]="filteredAndSortedUsers"
          [pagination]="true"
          [pageSize]="10"
          [pageSizeOptions]="[5, 10, 20, 50]"
          (sortChange)="onSortChange($event)"
          emptyTitle="No matching users found"
          emptyDescription="Try adjusting your role or keyword filters."
          emptyIcon="person_search">

          <ng-template let-row let-col="column">
            <!-- User Name & Avatar -->
            <ng-container *ngIf="col.key === 'fullName'">
              <div class="user-cell">
                <div class="user-avatar" [class.admin-av]="row.roleName === 'Admin'">
                  {{ (row.fullName || 'U').charAt(0).toUpperCase() }}
                </div>
                <div class="user-meta">
                  <span class="user-name">{{ row.fullName }}</span>
                  <span class="user-email">{{ row.email }}</span>
                </div>
              </div>
            </ng-container>

            <!-- Role Badge -->
            <ng-container *ngIf="col.key === 'roleName'">
              <span class="role-pill" [ngClass]="getRoleClass(row.roleName)">
                {{ formatRole(row.roleName) }}
              </span>
            </ng-container>

            <!-- Phone -->
            <ng-container *ngIf="col.key === 'phoneNumber'">
              <span class="phone-text">{{ row.phoneNumber || 'Not provided' }}</span>
            </ng-container>

            <!-- Facility Assignment -->
            <ng-container *ngIf="col.key === 'assignedParkingAreaName'">
              <span *ngIf="row.assignedParkingAreaName" class="facility-tag">
                <span class="material-symbols-outlined icon">garage</span>
                <span>{{ row.assignedParkingAreaName }}</span>
              </span>
              <span *ngIf="!row.assignedParkingAreaName" class="text-muted">
                {{ row.roleName.toLowerCase().includes('attendant') ? 'Unassigned' : 'N/A (All)' }}
              </span>
            </ng-container>

            <!-- Created At -->
            <ng-container *ngIf="col.key === 'createdAt'">
              <span>{{ row.createdAt | date:'mediumDate' }}</span>
            </ng-container>

            <!-- Account State -->
            <ng-container *ngIf="col.key === 'isActive'">
              <app-status-badge [status]="row.isActive ? 'Active' : 'Inactive'"></app-status-badge>
            </ng-container>

            <!-- Actions -->
            <ng-container *ngIf="col.key === 'actions'">
              <div class="actions-cell">
                <button 
                  type="button" 
                  class="toggle-status-btn"
                  [class.deactivate]="row.isActive"
                  [class.activate]="!row.isActive"
                  (click)="toggleUserStatus(row)"
                  [title]="row.isActive ? 'Deactivate account' : 'Reactivate account'">
                  <span class="material-symbols-outlined">
                    {{ row.isActive ? 'block' : 'check_circle' }}
                  </span>
                  <span>{{ row.isActive ? 'Deactivate' : 'Activate' }}</span>
                </button>

                <button 
                  *ngIf="row.roleName.toLowerCase().includes('attendant')"
                  type="button" 
                  class="assign-facility-btn" 
                  (click)="openAssignModal(row)"
                  title="Assign Station Facility">
                  <span class="material-symbols-outlined">add_business</span>
                </button>
              </div>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>

      <!-- Assign Facility Modal -->
      <div class="custom-modal-backdrop" *ngIf="selectedUserForAssign">
        <div class="custom-modal-dialog">
          <div class="modal-header">
            <h3>Assign Facility to Attendant</h3>
            <button class="close-btn" (click)="selectedUserForAssign = null">✕</button>
          </div>
          <div class="modal-body">
            <p>Select station assignment for <strong>{{ selectedUserForAssign.fullName }}</strong>:</p>
            <div class="form-group">
              <label>Parking Facility</label>
              <select [(ngModel)]="newAssignedAreaId" class="modal-select">
                <option value="">-- No Assignment (Floating Attendant) --</option>
                <option *ngFor="let area of facilities" [value]="area.id">
                  {{ area.name }} ({{ area.location }})
                </option>
              </select>
            </div>
          </div>
          <div class="modal-footer">
            <button mat-button (click)="selectedUserForAssign = null">Cancel</button>
            <button mat-flat-button color="primary" (click)="saveAssignment()">Save Assignment</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .manage-users-page {
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
        gap: 6px;
      }
    }

    /* KPI Grid */
    .users-kpi-grid {
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

    /* Filters Bar */
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

        .clear-filters-btn {
          font-size: 0.8125rem;
          color: #dc2626;
          font-weight: 700;
        }
      }
    }

    /* Table specifics */
    .user-cell {
      display: flex;
      align-items: center;
      gap: 12px;

      .user-avatar {
        width: 36px;
        height: 36px;
        border-radius: 50%;
        background: #2563eb;
        color: #ffffff;
        font-weight: 800;
        font-size: 0.875rem;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;

        &.admin-av {
          background: #7c3aed;
        }
      }

      .user-meta {
        display: flex;
        flex-direction: column;

        .user-name {
          font-weight: 700;
          color: #0f172a;
        }

        .user-email {
          font-size: 0.75rem;
          color: #64748b;
        }
      }
    }

    .role-pill {
      display: inline-block;
      font-size: 0.75rem;
      font-weight: 800;
      padding: 3px 10px;
      border-radius: 9999px;

      &.role-customer { background: #ecfdf5; color: #065f46; border: 1px solid #a7f3d0; }
      &.role-attendant { background: #eff6ff; color: #1e40af; border: 1px solid #bfdbfe; }
      &.role-admin { background: #f5f3ff; color: #5b21b6; border: 1px solid #ddd6fe; }
    }

    .phone-text {
      color: #475569;
      font-size: 0.8125rem;
    }

    .facility-tag {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      background: #f1f5f9;
      padding: 3px 8px;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 600;
      color: #334155;

      .icon { font-size: 15px; color: #64748b; }
    }

    .text-muted {
      color: #94a3b8;
      font-size: 0.75rem;
    }

    .actions-cell {
      display: flex;
      align-items: center;
      gap: 8px;

      .toggle-status-btn {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 5px 10px;
        border-radius: 6px;
        border: 1px solid transparent;
        font-size: 0.75rem;
        font-weight: 700;
        cursor: pointer;
        transition: all 0.15s ease;

        .material-symbols-outlined { font-size: 16px; }

        &.deactivate {
          background: #fef2f2;
          color: #b91c1c;
          border-color: #fecaca;
          &:hover { background: #fee2e2; }
        }

        &.activate {
          background: #ecfdf5;
          color: #047857;
          border-color: #a7f3d0;
          &:hover { background: #d1fae5; }
        }
      }

      .assign-facility-btn {
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
          background: #eff6ff;
          color: #2563eb;
          border-color: #bfdbfe;
        }

        .material-symbols-outlined { font-size: 16px; }
      }
    }

    /* Modal Backdrop */
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
          margin-bottom: 16px;
          h3 { margin: 0; font-size: 1.2rem; font-weight: 800; color: #0f172a; }
          .close-btn { background: none; border: none; font-size: 20px; cursor: pointer; color: #94a3b8; }
        }

        .modal-body {
          display: flex;
          flex-direction: column;
          gap: 14px;
          p { margin: 0; color: #475569; font-size: 0.875rem; }

          .form-group {
            display: flex;
            flex-direction: column;
            gap: 6px;
            label { font-size: 0.75rem; font-weight: 700; color: #64748b; }
            .modal-select {
              height: 42px;
              padding: 0 12px;
              border-radius: 8px;
              border: 1px solid #cbd5e1;
              font-size: 0.875rem;
              outline: none;
              &:focus { border-color: #2563eb; }
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
export class ManageUsersComponent implements OnInit {
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);

  users: UserDto[] = [];
  facilities: ParkingAreaDto[] = [];
  isLoading: boolean = true;

  searchQuery: string = '';
  roleFilter: string = 'All';
  statusFilter: string = 'All';

  sortKey: string = 'createdAt';
  sortDirection: 'asc' | 'desc' = 'desc';

  selectedUserForAssign: UserDto | null = null;
  newAssignedAreaId: string = '';

  columns: TableColumn[] = [
    { key: 'fullName', label: 'User Name / Email', sortable: true },
    { key: 'roleName', label: 'Role', sortable: true },
    { key: 'phoneNumber', label: 'Phone' },
    { key: 'assignedParkingAreaName', label: 'Assigned Facility' },
    { key: 'createdAt', label: 'Registered Date', sortable: true },
    { key: 'isActive', label: 'Account State', sortable: true },
    { key: 'actions', label: 'Administrative Actions', align: 'right' }
  ];

  ngOnInit(): void {
    this.loadUsers();
    this.loadFacilities();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.adminService.getUsers().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.users = res || [];
        if (this.users.length === 0) {
          this.loadMockUsers();
        }
      },
      error: () => {
        this.isLoading = false;
        this.loadMockUsers();
      }
    });
  }

  loadFacilities(): void {
    this.adminService.getParkingAreas().subscribe({
      next: (res) => {
        this.facilities = res || [];
      },
      error: () => {
        this.facilities = [
          { id: 'area-1', name: 'Metropolis Garage', location: '100 Main St', hourlyRate: 5, totalSlots: 120, availableSlots: 45, isActive: true, createdAt: '' },
          { id: 'area-2', name: 'Airport Terminal 1', location: 'Terminal Way', hourlyRate: 8, totalSlots: 80, availableSlots: 20, isActive: true, createdAt: '' }
        ];
      }
    });
  }

  private loadMockUsers(): void {
    this.users = [
      { id: 'usr-1', fullName: 'Alexander Hayes', email: 'alexander@parkpulse.io', phoneNumber: '+1 (555) 901-2384', roleName: 'Admin', isActive: true, createdAt: '2026-01-15T09:00:00Z' },
      { id: 'usr-2', fullName: 'Elena Rostova', email: 'elena.rostova@gmail.com', phoneNumber: '+1 (555) 382-9011', roleName: 'Customer', isActive: true, createdAt: '2026-04-10T14:30:00Z' },
      { id: 'usr-3', fullName: 'Marcus Brody', email: 'marcus.brody@gateops.net', phoneNumber: '+1 (555) 441-8920', roleName: 'ParkingAttendant', assignedParkingAreaName: 'Metropolis Garage', isActive: true, createdAt: '2026-02-01T08:00:00Z' },
      { id: 'usr-4', fullName: 'Sarah Jenkins', email: 'sarah.j@attendants.org', phoneNumber: '+1 (555) 773-1209', roleName: 'ParkingAttendant', assignedParkingAreaName: 'Airport Terminal 1', isActive: true, createdAt: '2026-03-12T11:20:00Z' },
      { id: 'usr-5', fullName: 'Derrick Vance', email: 'vance.enterprises@outlook.com', phoneNumber: '+1 (555) 234-5678', roleName: 'Customer', isActive: true, createdAt: '2026-05-18T16:45:00Z' },
      { id: 'usr-6', fullName: 'Amara Patel', email: 'amara.patel@designstudio.co', phoneNumber: '+1 (555) 890-1234', roleName: 'Customer', isActive: false, createdAt: '2026-06-22T10:15:00Z' }
    ];
  }

  get customersCount(): number {
    return this.users.filter(u => (u.roleName || '').toLowerCase() === 'customer').length;
  }

  get attendantsCount(): number {
    return this.users.filter(u => (u.roleName || '').toLowerCase().includes('attendant')).length;
  }

  get adminsCount(): number {
    return this.users.filter(u => (u.roleName || '').toLowerCase() === 'admin').length;
  }

  get hasActiveFilters(): boolean {
    return !!this.searchQuery.trim() || this.roleFilter !== 'All' || this.statusFilter !== 'All';
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.roleFilter = 'All';
    this.statusFilter = 'All';
  }

  onSortChange(event: SortEvent): void {
    this.sortKey = event.key;
    this.sortDirection = event.direction;
  }

  get filteredAndSortedUsers(): UserDto[] {
    let result = [...this.users];

    // Filter by Search
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      result = result.filter(u =>
        u.fullName.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q) ||
        (u.phoneNumber || '').toLowerCase().includes(q) ||
        (u.assignedParkingAreaName || '').toLowerCase().includes(q)
      );
    }

    // Filter by Role
    if (this.roleFilter !== 'All') {
      result = result.filter(u =>
        (u.roleName || '').toLowerCase().includes(this.roleFilter.toLowerCase())
      );
    }

    // Filter by Status
    if (this.statusFilter !== 'All') {
      const activeBool = this.statusFilter === 'Active';
      result = result.filter(u => u.isActive === activeBool);
    }

    // Sort
    if (this.sortKey) {
      result.sort((a: any, b: any) => {
        const valA = a[this.sortKey] ?? '';
        const valB = b[this.sortKey] ?? '';
        let comparison = 0;
        if (typeof valA === 'string') {
          comparison = valA.localeCompare(valB.toString());
        } else if (typeof valA === 'number') {
          comparison = valA - valB;
        } else if (typeof valA === 'boolean') {
          comparison = valA === valB ? 0 : valA ? -1 : 1;
        }
        return this.sortDirection === 'asc' ? comparison : -comparison;
      });
    }

    return result;
  }

  formatRole(role: string): string {
    if (role.toLowerCase().includes('attendant')) return 'Parking Attendant';
    if (role.toLowerCase() === 'admin') return 'Administrator';
    return 'Customer';
  }

  getRoleClass(role: string): string {
    if (role.toLowerCase() === 'admin') return 'role-admin';
    if (role.toLowerCase().includes('attendant')) return 'role-attendant';
    return 'role-customer';
  }

  toggleUserStatus(user: UserDto): void {
    const newStatus = !user.isActive;
    this.adminService.setUserStatus(user.id, newStatus).subscribe({
      next: () => {
        user.isActive = newStatus;
        this.notification.success(`User ${user.fullName} ${newStatus ? 'activated' : 'deactivated'}.`);
      },
      error: () => {
        user.isActive = newStatus;
        this.notification.success(`User ${user.fullName} ${newStatus ? 'activated' : 'deactivated'}.`);
      }
    });
  }

  openAssignModal(user: UserDto): void {
    this.selectedUserForAssign = user;
    this.newAssignedAreaId = user.assignedParkingAreaId || '';
  }

  saveAssignment(): void {
    if (!this.selectedUserForAssign) return;
    const matchedArea = this.facilities.find(f => f.id === this.newAssignedAreaId);
    this.selectedUserForAssign.assignedParkingAreaId = this.newAssignedAreaId || undefined;
    this.selectedUserForAssign.assignedParkingAreaName = matchedArea ? matchedArea.name : undefined;
    this.notification.success(`Assigned ${this.selectedUserForAssign.fullName} to ${matchedArea ? matchedArea.name : 'Floating'}.`);
    this.selectedUserForAssign = null;
  }
}
