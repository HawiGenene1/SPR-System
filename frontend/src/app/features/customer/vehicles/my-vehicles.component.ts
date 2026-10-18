import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { VehicleService } from '../../../core/services/vehicle.service';
import { NotificationService } from '../../../core/services/notification.service';
import { VehicleDto } from '../../../core/models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-my-vehicles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatProgressSpinnerModule, MatDialogModule],
  template: `
    <div class="vehicles-page">
      <div class="page-header">
        <div>
          <h2>My Registered Vehicles</h2>
          <p>Manage the cars and motor vehicles associated with your parking reservations.</p>
        </div>
        <button (click)="openAddModal()" class="pp-btn primary">
          <span class="material-symbols-outlined">add</span>
          Add New Vehicle
        </button>
      </div>

      <!-- Add Vehicle Card Modal / Section -->
      <div *ngIf="showAddForm" class="add-vehicle-panel pp-card">
        <div class="panel-header">
          <h3>Register Vehicle</h3>
          <button (click)="showAddForm = false" class="btn-close">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>

        <form [formGroup]="vehicleForm" (ngSubmit)="onSaveVehicle()" class="vehicle-form-grid">
          <div class="input-field">
            <label>License Plate</label>
            <input 
              type="text" 
              formControlName="licensePlate" 
              placeholder="e.g. ABC-1234" 
              class="custom-input" />
          </div>

          <div class="input-field">
            <label>Make / Manufacturer</label>
            <input 
              type="text" 
              formControlName="make" 
              placeholder="e.g. Tesla, Toyota, BMW" 
              class="custom-input" />
          </div>

          <div class="input-field">
            <label>Model</label>
            <input 
              type="text" 
              formControlName="model" 
              placeholder="e.g. Model 3, RAV4, 330i" 
              class="custom-input" />
          </div>

          <div class="input-field">
            <label>Vehicle Color</label>
            <input 
              type="text" 
              formControlName="color" 
              placeholder="e.g. Midnight Blue, Pearl White" 
              class="custom-input" />
          </div>

          <div class="input-field">
            <label>Vehicle Classification</label>
            <select formControlName="vehicleType" class="custom-select">
              <option value="Sedan">Sedan</option>
              <option value="SUV">SUV</option>
              <option value="EV">Electric Vehicle (EV)</option>
              <option value="Truck">Truck / Pickup</option>
              <option value="Motorcycle">Motorcycle</option>
            </select>
          </div>

          <div class="form-actions-full">
            <button type="button" (click)="showAddForm = false" class="pp-btn secondary">
              Cancel
            </button>
            <button type="submit" class="pp-btn primary" [disabled]="vehicleForm.invalid || isSaving">
              <mat-spinner *ngIf="isSaving" diameter="20" color="accent"></mat-spinner>
              <span *ngIf="!isSaving">Save Vehicle to Fleet</span>
            </button>
          </div>
        </form>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="loading-state">
        <mat-spinner diameter="36"></mat-spinner>
        <span>Loading registered vehicles...</span>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && vehicles.length === 0 && !showAddForm" class="pp-empty-state">
        <div class="empty-icon">
          <span class="material-symbols-outlined">directions_car</span>
        </div>
        <h4>No vehicles registered yet</h4>
        <p>Register your first vehicle license plate so you can reserve parking slots instantly.</p>
        <button (click)="openAddModal()" class="pp-btn primary">Add Your First Vehicle</button>
      </div>

      <!-- Vehicles Fleet Grid -->
      <div *ngIf="!isLoading && vehicles.length > 0" class="vehicles-grid">
        <div *ngFor="let v of vehicles" class="vehicle-card pp-card interactive">
          <div class="card-top">
            <div class="plate-badge">
              <div class="plate-country">USA</div>
              <div class="plate-digits">{{ v.licensePlate }}</div>
            </div>
            <span class="type-pill" [ngClass]="v.vehicleType.toLowerCase()">
              {{ v.vehicleType }}
            </span>
          </div>

          <div class="car-details">
            <div class="car-title">
              <h3>{{ v.make }} {{ v.model }}</h3>
            </div>
            <div class="car-spec-row">
              <span class="spec-label">Color:</span>
              <span class="spec-value">{{ v.color }}</span>
            </div>
            <div class="car-spec-row">
              <span class="spec-label">Registered:</span>
              <span class="spec-value">{{ v.createdAt | date:'mediumDate' }}</span>
            </div>
          </div>

          <div class="card-footer">
            <button (click)="deleteVehicle(v)" class="pp-btn secondary sm delete-btn">
              <span class="material-symbols-outlined">delete</span>
              Remove
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .vehicles-page {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
      flex-wrap: wrap;

      h2 { margin: 0 0 4px 0; font-size: 1.75rem; }
      p { margin: 0; color: #64748b; font-size: 0.9375rem; }
    }

    .add-vehicle-panel {
      padding: 24px;
      border: 2px solid #bfdbfe;
      background: #f8fafc;

      .panel-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;

        h3 { margin: 0; font-size: 1.2rem; color: #0f172a; }
        .btn-close {
          border: none;
          background: transparent;
          color: #64748b;
          cursor: pointer;
        }
      }

      .vehicle-form-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 16px;

        .input-field {
          display: flex;
          flex-direction: column;
          gap: 6px;

          label {
            font-size: 0.8125rem;
            font-weight: 600;
            color: #334155;
          }

          .custom-input, .custom-select {
            padding: 10px 12px;
            border-radius: var(--radius-md);
            border: 1px solid #cbd5e1;
            font-family: inherit;
            font-size: 0.875rem;
            background: #ffffff;
            &:focus { outline: none; border-color: #2563eb; }
          }
        }

        .form-actions-full {
          grid-column: 1 / -1;
          display: flex;
          justify-content: flex-end;
          gap: 12px;
          margin-top: 10px;
        }
      }
    }

    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 60px 0;
      color: #64748b;
    }

    .vehicles-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 20px;

      .vehicle-card {
        padding: 22px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        gap: 16px;

        .card-top {
          display: flex;
          justify-content: space-between;
          align-items: center;

          .plate-badge {
            display: inline-flex;
            align-items: center;
            border: 2px solid #0f172a;
            border-radius: 6px;
            overflow: hidden;
            background: #ffffff;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.06);

            .plate-country {
              background: #0f172a;
              color: white;
              font-size: 0.5625rem;
              font-weight: 800;
              padding: 4px 6px;
              letter-spacing: 0.05em;
            }

            .plate-digits {
              font-family: 'JetBrains Mono', monospace;
              font-size: 1rem;
              font-weight: 800;
              color: #0f172a;
              padding: 2px 10px;
              letter-spacing: 0.08em;
            }
          }

          .type-pill {
            font-size: 0.6875rem;
            font-weight: 700;
            padding: 3px 8px;
            border-radius: 999px;
            text-transform: uppercase;
            background: #f1f5f9;
            color: #475569;

            &.ev { background: #ecfeff; color: #0891b2; border: 1px solid #a5f3fc; }
            &.suv { background: #eff6ff; color: #2563eb; }
          }
        }

        .car-details {
          .car-title h3 {
            margin: 0 0 10px 0;
            font-size: 1.15rem;
            color: #0f172a;
          }

          .car-spec-row {
            display: flex;
            justify-content: space-between;
            font-size: 0.8125rem;
            padding: 4px 0;
            border-bottom: 1px dashed #f1f5f9;

            .spec-label { color: #64748b; }
            .spec-value { font-weight: 600; color: #334155; }
          }
        }

        .card-footer {
          display: flex;
          justify-content: flex-end;
          border-top: 1px solid var(--border-light);
          padding-top: 14px;

          .delete-btn {
            color: #dc2626;
            &:hover { background: #fef2f2; border-color: #fca5a5; }
          }
        }
      }
    }
  `]
})
export class CustomerVehiclesComponent implements OnInit {
  private fb = inject(FormBuilder);
  private vehicleService = inject(VehicleService);
  private notify = inject(NotificationService);
  private dialog = inject(MatDialog);

  isLoading = true;
  isSaving = false;
  showAddForm = false;
  vehicles: VehicleDto[] = [];

  vehicleForm: FormGroup = this.fb.group({
    licensePlate: ['', [Validators.required, Validators.minLength(3)]],
    make: ['', [Validators.required]],
    model: ['', [Validators.required]],
    color: ['Silver', [Validators.required]],
    vehicleType: ['Sedan', [Validators.required]]
  });

  ngOnInit(): void {
    this.loadVehicles();
  }

  loadVehicles(): void {
    this.isLoading = true;
    this.vehicleService.getMyVehicles().subscribe({
      next: (res) => {
        this.vehicles = res.data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  openAddModal(): void {
    this.vehicleForm.reset({
      color: 'Silver',
      vehicleType: 'Sedan'
    });
    this.showAddForm = true;
  }

  onSaveVehicle(): void {
    if (this.vehicleForm.invalid) return;

    this.isSaving = true;
    const formVal = this.vehicleForm.value;
    formVal.licensePlate = formVal.licensePlate.trim().toUpperCase();

    this.vehicleService.createVehicle(formVal).subscribe({
      next: () => {
        this.isSaving = false;
        this.showAddForm = false;
        this.notify.success('Vehicle registered successfully!');
        this.loadVehicles();
      },
      error: () => {
        this.isSaving = false;
      }
    });
  }

  deleteVehicle(vehicle: VehicleDto): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Vehicle',
        message: `Are you sure you want to delete ${vehicle.make} ${vehicle.model} (${vehicle.licensePlate})?`,
        confirmText: 'Yes, Delete',
        isDanger: true
      }
    });

    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.vehicleService.deleteVehicle(vehicle.id).subscribe({
          next: () => {
            this.notify.success('Vehicle removed from your account.');
            this.loadVehicles();
          }
        });
      }
    });
  }
}
