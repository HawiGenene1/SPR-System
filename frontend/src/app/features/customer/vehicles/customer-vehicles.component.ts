import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { VehicleService } from '../../../core/services/vehicle.service';
import { NotificationService } from '../../../core/services/notification.service';
import { VehicleDto } from '../../../core/models';
import { DataTableComponent, TableColumn } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmationDialogComponent } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'app-customer-vehicles',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    DataTableComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    StatusBadgeComponent
  ],
  template: `
    <div class="customer-vehicles-container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <h2>My Registered Vehicles</h2>
          <p>Register your personal vehicles for automated gate sensor recognition and quick 1-click bay booking.</p>
        </div>
        <button mat-flat-button color="primary" class="add-vehicle-btn" (click)="openAddModal(addVehicleModal)">
          <span class="material-symbols-outlined">add_circle</span>
          <span>Register Vehicle</span>
        </button>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="loading" message="Loading your vehicle garage..."></app-loading-spinner>

      <!-- Content Table -->
      <div class="content-body" *ngIf="!loading">
        <app-data-table
          [columns]="columns"
          [data]="vehicles"
          [pagination]="true"
          [pageSize]="10"
          emptyTitle="No registered vehicles"
          emptyDescription="Register your primary car, EV, or motorcycle to start booking parking bays."
          emptyIcon="directions_car"
          emptyActionText="Register First Vehicle"
          (emptyAction)="openAddModal(addVehicleModal)">

          <ng-template let-row let-col="column">
            <!-- Plate Column -->
            <ng-container *ngIf="col.key === 'licensePlate'">
              <div class="license-plate-badge">
                <span class="country-code">USA</span>
                <span class="plate-number">{{ row.licensePlate }}</span>
              </div>
            </ng-container>

            <!-- Vehicle Details Column -->
            <ng-container *ngIf="col.key === 'vehicleInfo'">
              <div class="vehicle-details">
                <span class="vehicle-name">{{ row.make }} {{ row.model }}</span>
                <span class="vehicle-color-tag">{{ row.color }}</span>
              </div>
            </ng-container>

            <!-- Vehicle Type Column -->
            <ng-container *ngIf="col.key === 'vehicleType'">
              <app-status-badge [status]="row.vehicleType" [icon]="getTypeIcon(row.vehicleType)"></app-status-badge>
            </ng-container>

            <!-- Registered On -->
            <ng-container *ngIf="col.key === 'createdAt'">
              <span class="date-text">{{ row.createdAt | date:'MMM d, y, h:mm a' }}</span>
            </ng-container>

            <!-- Actions Column -->
            <ng-container *ngIf="col.key === 'actions'">
              <button 
                class="icon-action-btn delete-btn" 
                (click)="confirmDelete(row)" 
                title="Remove vehicle">
                <span class="material-symbols-outlined">delete</span>
              </button>
            </ng-container>
          </ng-template>
        </app-data-table>
      </div>
    </div>

    <!-- Add Vehicle Modal Template -->
    <ng-template #addVehicleModal let-dialogRef="dialogRef">
      <div class="modal-wrapper">
        <div class="modal-header">
          <div class="modal-icon">
            <span class="material-symbols-outlined">directions_car</span>
          </div>
          <div>
            <h3>Register Vehicle</h3>
            <p>Enter your license plate details for automated optical recognition.</p>
          </div>
        </div>

        <form [formGroup]="vehicleForm" (ngSubmit)="saveVehicle(dialogRef)" class="modal-form">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>License Plate</mat-label>
            <input matInput formControlName="licensePlate" placeholder="e.g. 7XYZ999" uppercase style="text-transform: uppercase;">
            <mat-error *ngIf="vehicleForm.get('licensePlate')?.hasError('required')">License plate is required</mat-error>
          </mat-form-field>

          <div class="form-grid-2">
            <mat-form-field appearance="outline">
              <mat-label>Make</mat-label>
              <input matInput formControlName="make" placeholder="e.g. Tesla, Honda">
              <mat-error *ngIf="vehicleForm.get('make')?.hasError('required')">Make is required</mat-error>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Model</mat-label>
              <input matInput formControlName="model" placeholder="e.g. Model Y, Civic">
              <mat-error *ngIf="vehicleForm.get('model')?.hasError('required')">Model is required</mat-error>
            </mat-form-field>
          </div>

          <div class="form-grid-2">
            <mat-form-field appearance="outline">
              <mat-label>Color</mat-label>
              <input matInput formControlName="color" placeholder="e.g. Midnight Silver">
              <mat-error *ngIf="vehicleForm.get('color')?.hasError('required')">Color is required</mat-error>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Vehicle Type</mat-label>
              <mat-select formControlName="vehicleType">
                <mat-option value="Sedan">Sedan</mat-option>
                <mat-option value="SUV">SUV</mat-option>
                <mat-option value="Electric">Electric Vehicle (EV)</mat-option>
                <mat-option value="Truck">Truck</mat-option>
                <mat-option value="Motorcycle">Motorcycle</mat-option>
              </mat-select>
            </mat-form-field>
          </div>

          <div class="modal-actions">
            <button type="button" mat-button (click)="dialogRef.close()">Cancel</button>
            <button type="submit" mat-flat-button color="primary" [disabled]="vehicleForm.invalid || submitting">
              {{ submitting ? 'Registering...' : 'Save Vehicle' }}
            </button>
          </div>
        </form>
      </div>
    </ng-template>
  `,
  styles: [`
    .customer-vehicles-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;

      .header-titles {
        h2 { margin: 0 0 4px 0; font-size: 1.5rem; font-weight: 800; color: #0f172a; letter-spacing: -0.02em; }
        p { margin: 0; color: #64748b; font-size: 0.875rem; }
      }

      .add-vehicle-btn {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        height: 42px;
        border-radius: 10px;
      }
    }

    .license-plate-badge {
      display: inline-flex;
      align-items: center;
      border: 2px solid #0f172a;
      border-radius: 6px;
      background: #f8fafc;
      overflow: hidden;

      .country-code {
        background: #1e3a8a;
        color: #ffffff;
        font-size: 0.625rem;
        font-weight: 800;
        padding: 4px 6px;
      }

      .plate-number {
        font-family: 'Courier New', monospace;
        font-size: 0.9375rem;
        font-weight: 800;
        color: #0f172a;
        padding: 4px 10px;
        letter-spacing: 0.1em;
      }
    }

    .vehicle-details {
      display: flex;
      flex-direction: column;
      .vehicle-name { font-weight: 600; color: #0f172a; }
      .vehicle-color-tag { font-size: 0.75rem; color: #64748b; }
    }

    .date-text { font-size: 0.8125rem; color: #64748b; }

    .icon-action-btn {
      width: 32px;
      height: 32px;
      border-radius: 6px;
      border: none;
      background: transparent;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #94a3b8;
      transition: all 0.15s ease;

      &.delete-btn:hover { background: #fef2f2; color: #dc2626; }
      .material-symbols-outlined { font-size: 18px; }
    }

    .modal-wrapper { padding: 24px; min-width: 420px; }
    .modal-header {
      display: flex;
      align-items: center;
      gap: 14px;
      margin-bottom: 20px;

      .modal-icon {
        width: 44px;
        height: 44px;
        border-radius: 12px;
        background: #eff6ff;
        color: #2563eb;
        display: flex;
        align-items: center;
        justify-content: center;
        .material-symbols-outlined { font-size: 24px; }
      }
      h3 { margin: 0; font-size: 1.25rem; font-weight: 700; color: #0f172a; }
      p { margin: 2px 0 0 0; font-size: 0.8125rem; color: #64748b; }
    }

    .modal-form {
      display: flex;
      flex-direction: column;
      gap: 14px;
      .w-full { width: 100%; }
      .form-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
      .modal-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 10px; }
    }
  `]
})
export class CustomerVehiclesComponent implements OnInit {
  private vehicleService = inject(VehicleService);
  private notification = inject(NotificationService);
  private dialog = inject(MatDialog);
  private fb = inject(FormBuilder);

  vehicles: VehicleDto[] = [];
  loading: boolean = false;
  submitting: boolean = false;

  columns: TableColumn[] = [
    { key: 'licensePlate', label: 'License Plate', sortable: true, width: '180px' },
    { key: 'vehicleInfo', label: 'Vehicle Details', sortable: true },
    { key: 'vehicleType', label: 'Type', sortable: true, width: '140px' },
    { key: 'createdAt', label: 'Registered On', sortable: true, width: '180px' },
    { key: 'actions', label: 'Actions', align: 'center', width: '80px' }
  ];

  vehicleForm: FormGroup = this.fb.group({
    licensePlate: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(15)]],
    make: ['', Validators.required],
    model: ['', Validators.required],
    color: ['', Validators.required],
    vehicleType: ['Sedan', Validators.required]
  });

  ngOnInit(): void {
    this.loadVehicles();
  }

  loadVehicles(): void {
    this.loading = true;
    this.vehicleService.getMyVehicles().subscribe({
      next: (res: any) => {
        this.loading = false;
        this.vehicles = res.data || [];
      },
      error: (err: any) => {
        this.loading = false;
        this.notification.error('Failed to load vehicles: ' + (err.error?.message || err.message));
      }
    });
  }

  getTypeIcon(type: string): string {
    const t = (type || '').toLowerCase();
    if (t.includes('electric') || t.includes('ev')) return 'bolt';
    if (t.includes('motorcycle')) return 'two_wheeler';
    if (t.includes('truck')) return 'local_shipping';
    return 'directions_car';
  }

  openAddModal(templateRef: any): void {
    this.vehicleForm.reset({ vehicleType: 'Sedan' });
    this.dialog.open(templateRef, { width: '480px' });
  }

  saveVehicle(dialogRef: any): void {
    if (this.vehicleForm.invalid) return;

    this.submitting = true;
    const req = {
      ...this.vehicleForm.value,
      licensePlate: this.vehicleForm.value.licensePlate.trim().toUpperCase()
    };

    this.vehicleService.createVehicle(req).subscribe({
      next: () => {
        this.submitting = false;
        this.notification.success('Vehicle registered successfully!');
        dialogRef.close();
        this.loadVehicles();
      },
      error: (err: any) => {
        this.submitting = false;
        this.notification.error('Failed to register vehicle: ' + (err.error?.message || err.message));
      }
    });
  }

  confirmDelete(vehicle: VehicleDto): void {
    const ref = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Vehicle',
        message: `Remove vehicle ${vehicle.licensePlate} (${vehicle.make} ${vehicle.model}) from your registered fleet?`,
        confirmText: 'Remove Vehicle',
        cancelText: 'Cancel',
        type: 'danger'
      }
    });

    ref.afterClosed().subscribe((confirmed) => {
      if (confirmed) {
        this.vehicleService.deleteVehicle(vehicle.id).subscribe({
          next: () => {
            this.notification.success('Vehicle removed successfully.');
            this.loadVehicles();
          },
          error: (err: any) => {
            this.notification.error('Failed to delete vehicle: ' + (err.error?.message || err.message));
          }
        });
      }
    });
  }
}
