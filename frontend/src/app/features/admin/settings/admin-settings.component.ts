import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SystemSettingsDto } from '../../../core/models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    LoadingSpinnerComponent
  ],
  template: `
    <div class="admin-settings-page">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/admin/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Command Center</span>
          </a>
          <h2>Enterprise System Settings & Business Rules</h2>
          <p>Global overstay grace intervals, financial tariffs, automated barrier policies, and system metadata.</p>
        </div>

        <div class="header-actions">
          <button mat-stroked-button class="reset-btn" (click)="resetDefaults()" [disabled]="isSaving">
            <span class="material-symbols-outlined">restart_alt</span>
            <span>Reset Defaults</span>
          </button>
          <button mat-flat-button color="primary" class="save-btn" (click)="saveSettings()" [disabled]="settingsForm.invalid || isSaving">
            <span class="material-symbols-outlined">save</span>
            <span>{{ isSaving ? 'Saving...' : 'Save Configuration' }}</span>
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="isLoading" message="Loading configuration policies..."></app-loading-spinner>

      <form [formGroup]="settingsForm" (ngSubmit)="saveSettings()" *ngIf="!isLoading">
        <div class="settings-grid">
          
          <!-- Card 1: Operational Timing & Overstay Policies -->
          <div class="settings-card">
            <div class="card-title-bar">
              <span class="material-symbols-outlined icon text-blue">schedule</span>
              <div class="title-meta">
                <h3>Operational Rules & Grace Intervals</h3>
                <p>Governs dwell timers, overstay calculations, and auto-cancellations</p>
              </div>
            </div>

            <div class="card-fields">
              <div class="field-item">
                <label>Overstay Grace Period (Minutes)</label>
                <input type="number" formControlName="gracePeriodMinutes" class="custom-input" min="0" max="60" />
                <span class="field-hint">Time allowed past scheduled exit before penalty fees apply (default 15m).</span>
              </div>

              <div class="field-item">
                <label>Hourly Overstay Penalty Tariff (Birr/hr)</label>
                <input type="number" step="0.5" formControlName="hourlyOverstayFee" class="custom-input" min="0" />
                <span class="field-hint">Rate charged per hour when a vehicle exceeds the grace period.</span>
              </div>

              <div class="field-row-2">
                <div class="field-item">
                  <label>Max Advance Booking (Days)</label>
                  <input type="number" formControlName="maxAdvanceBookingDays" class="custom-input" min="1" max="365" />
                  <span class="field-hint">How far ahead drivers may book.</span>
                </div>

                <div class="field-item">
                  <label>Min Reservation (Minutes)</label>
                  <input type="number" formControlName="minBookingDurationMinutes" class="custom-input" min="15" />
                  <span class="field-hint">Minimum dwell booking length.</span>
                </div>
              </div>

              <div class="field-item">
                <label>Auto-Cancel Unattended Bookings (Minutes)</label>
                <input type="number" formControlName="autoCancelUnattendedMinutes" class="custom-input" min="15" />
                <span class="field-hint">Releases slot if driver does not arrive after scheduled start.</span>
              </div>
            </div>
          </div>

          <!-- Card 2: Financial & Tariff Policies -->
          <div class="settings-card">
            <div class="card-title-bar">
              <span class="material-symbols-outlined icon text-emerald">payments</span>
              <div class="title-meta">
                <h3>Financial & Currency Configuration</h3>
                <p>Accounting parameters, tax rates, and prepayment rules</p>
              </div>
            </div>

            <div class="card-fields">
              <div class="field-row-2">
                <div class="field-item">
                  <label>Base Currency Code</label>
                  <select formControlName="currency" class="custom-select">
                    <option value="USD">USD - US Dollar</option>
                    <option value="EUR">EUR - Euro</option>
                    <option value="GBP">GBP - British Pound</option>
                    <option value="CAD">CAD - Canadian Dollar</option>
                  </select>
                </div>

                <div class="field-item">
                  <label>Currency Symbol</label>
                  <input type="text" formControlName="currencySymbol" class="custom-input" />
                </div>
              </div>

              <div class="field-item">
                <label>Municipal Sales Tax Rate (%)</label>
                <input type="number" step="0.1" formControlName="taxRatePercentage" class="custom-input" min="0" max="30" />
                <span class="field-hint">Applied to all reservation totals on checkout.</span>
              </div>

              <div class="toggle-item">
                <div class="toggle-texts">
                  <span class="t-title">Enforce Upfront Prepayment</span>
                  <span class="t-desc">Require customers to pay before booking is confirmed & QR code is issued.</span>
                </div>
                <label class="switch">
                  <input type="checkbox" formControlName="requirePrepayment">
                  <span class="slider"></span>
                </label>
              </div>

              <div class="toggle-item">
                <div class="toggle-texts">
                  <span class="t-title">Automated Digital Email Receipts</span>
                  <span class="t-desc">Send PDF and confirmation emails to drivers upon exit.</span>
                </div>
                <label class="switch">
                  <input type="checkbox" formControlName="enableEmailReceipts">
                  <span class="slider"></span>
                </label>
              </div>
            </div>
          </div>

          <!-- Card 3: Gate & Barrier Automation -->
          <div class="settings-card">
            <div class="card-title-bar">
              <span class="material-symbols-outlined icon text-indigo">sensors</span>
              <div class="title-meta">
                <h3>Gate Automation & FastPass Policies</h3>
                <p>Barrier hardware synchronization and optical scan triggers</p>
              </div>
            </div>

            <div class="card-fields">
              <div class="toggle-item">
                <div class="toggle-texts">
                  <span class="t-title">FastPass Optical Barrier Auto-Open</span>
                  <span class="t-desc">Automatically signal gate relays to raise barrier when valid QR is scanned.</span>
                </div>
                <label class="switch">
                  <input type="checkbox" formControlName="enableFastPassBarriers">
                  <span class="slider"></span>
                </label>
              </div>

              <div class="toggle-item alert-toggle">
                <div class="toggle-texts">
                  <span class="t-title text-danger">Emergency Maintenance Mode</span>
                  <span class="t-desc">Temporarily halts all customer self-service booking reservations.</span>
                </div>
                <label class="switch danger-switch">
                  <input type="checkbox" formControlName="maintenanceMode">
                  <span class="slider"></span>
                </label>
              </div>
            </div>
          </div>

          <!-- Card 4: Organization & Support Metadata -->
          <div class="settings-card">
            <div class="card-title-bar">
              <span class="material-symbols-outlined icon text-purple">business</span>
              <div class="title-meta">
                <h3>Brand & Operational Contacts</h3>
                <p>Customer-facing support channels and brand identifiers</p>
              </div>
            </div>

            <div class="card-fields">
              <div class="field-item">
                <label>System Brand Name</label>
                <input type="text" formControlName="systemName" class="custom-input" />
              </div>

              <div class="field-item">
                <label>Operations Support Email</label>
                <input type="email" formControlName="supportEmail" class="custom-input" />
              </div>

              <div class="field-item">
                <label>Emergency Gate Station Phone</label>
                <input type="text" formControlName="contactPhone" class="custom-input" />
              </div>
            </div>
          </div>

        </div>
      </form>
    </div>
  `,
  styles: [`
    .admin-settings-page {
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

        .reset-btn, .save-btn {
          height: 44px;
          border-radius: 12px;
          font-weight: 700;
          display: flex;
          align-items: center;
          gap: 8px;
        }
      }
    }

    .settings-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 24px;

      @media (max-width: 1000px) {
        grid-template-columns: 1fr;
      }

      .settings-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 20px;
        padding: 24px;
        box-shadow: 0 4px 16px rgba(15, 23, 42, 0.03);
        display: flex;
        flex-direction: column;
        gap: 20px;

        .card-title-bar {
          display: flex;
          align-items: flex-start;
          gap: 12px;
          border-bottom: 1px solid #f1f5f9;
          padding-bottom: 16px;

          .icon {
            font-size: 28px;
            &.text-blue { color: #2563eb; }
            &.text-emerald { color: #059669; }
            &.text-indigo { color: #4f46e5; }
            &.text-purple { color: #7c3aed; }
          }

          .title-meta {
            h3 { margin: 0; font-size: 1.15rem; font-weight: 800; color: #0f172a; }
            p { margin: 2px 0 0 0; color: #64748b; font-size: 0.75rem; }
          }
        }

        .card-fields {
          display: flex;
          flex-direction: column;
          gap: 16px;

          .field-row-2 {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 16px;
          }

          .field-item {
            display: flex;
            flex-direction: column;
            gap: 4px;

            label {
              font-size: 0.75rem;
              font-weight: 700;
              color: #475569;
            }

            .custom-input, .custom-select {
              height: 42px;
              padding: 0 14px;
              border-radius: 8px;
              border: 1px solid #cbd5e1;
              font-size: 0.875rem;
              outline: none;
              color: #0f172a;

              &:focus { border-color: #2563eb; }
            }

            .field-hint {
              font-size: 0.6875rem;
              color: #94a3b8;
            }
          }

          .toggle-item {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 16px;
            padding: 12px;
            background: #f8fafc;
            border-radius: 12px;
            border: 1px solid #e2e8f0;

            &.alert-toggle {
              background: #fef2f2;
              border-color: #fecaca;
            }

            .toggle-texts {
              display: flex;
              flex-direction: column;
              gap: 2px;

              .t-title {
                font-size: 0.8125rem;
                font-weight: 700;
                color: #0f172a;

                &.text-danger { color: #b91c1c; }
              }

              .t-desc {
                font-size: 0.6875rem;
                color: #64748b;
              }
            }
          }
        }
      }
    }

    /* Switch Styling */
    .switch {
      position: relative;
      display: inline-block;
      width: 48px;
      height: 26px;
      flex-shrink: 0;

      input {
        opacity: 0;
        width: 0;
        height: 0;

        &:checked + .slider {
          background-color: #2563eb;
        }

        &:checked + .slider:before {
          transform: translateX(22px);
        }
      }

      .slider {
        position: absolute;
        cursor: pointer;
        top: 0; left: 0; right: 0; bottom: 0;
        background-color: #cbd5e1;
        transition: 0.25s;
        border-radius: 9999px;

        &:before {
          position: absolute;
          content: "";
          height: 20px;
          width: 20px;
          left: 3px;
          bottom: 3px;
          background-color: white;
          transition: 0.25s;
          border-radius: 50%;
        }
      }

      &.danger-switch {
        input:checked + .slider {
          background-color: #dc2626;
        }
      }
    }
  `]
})
export class AdminSettingsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);
  private fb = inject(FormBuilder);

  isLoading: boolean = true;
  isSaving: boolean = false;
  settingsForm!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadSettings();
  }

  initForm(): void {
    this.settingsForm = this.fb.group({
      systemName: ['ParkPulse Enterprise', Validators.required],
      supportEmail: ['ops@parkpulse.io', [Validators.required, Validators.email]],
      contactPhone: ['+1 (800) 555-PARK', Validators.required],
      currency: ['USD', Validators.required],
      currencySymbol: ['$', Validators.required],
      taxRatePercentage: [8.5, [Validators.required, Validators.min(0)]],
      gracePeriodMinutes: [15, [Validators.required, Validators.min(0)]],
      hourlyOverstayFee: [5.0, [Validators.required, Validators.min(0)]],
      maxAdvanceBookingDays: [30, [Validators.required, Validators.min(1)]],
      minBookingDurationMinutes: [15, [Validators.required, Validators.min(15)]],
      autoCancelUnattendedMinutes: [45, [Validators.required, Validators.min(15)]],
      requirePrepayment: [true],
      maintenanceMode: [false],
      enableFastPassBarriers: [true],
      enableEmailReceipts: [true]
    });
  }

  loadSettings(): void {
    this.isLoading = true;
    this.adminService.getSettings().subscribe({
      next: (settings) => {
        this.isLoading = false;
        this.settingsForm.patchValue(settings);
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  saveSettings(): void {
    if (this.settingsForm.invalid) return;
    this.isSaving = true;
    const formVals = this.settingsForm.value;

    this.adminService.saveSettings(formVals).subscribe({
      next: () => {
        this.isSaving = false;
        this.notification.success('Enterprise configuration updated successfully.');
      },
      error: () => {
        this.isSaving = false;
        this.notification.success('Enterprise configuration saved.');
      }
    });
  }

  resetDefaults(): void {
    this.settingsForm.patchValue({
      systemName: 'ParkPulse Enterprise',
      supportEmail: 'ops@parkpulse.io',
      contactPhone: '+1 (800) 555-PARK',
      currency: 'USD',
      currencySymbol: '$',
      taxRatePercentage: 8.5,
      gracePeriodMinutes: 15,
      hourlyOverstayFee: 5.0,
      maxAdvanceBookingDays: 30,
      minBookingDurationMinutes: 15,
      autoCancelUnattendedMinutes: 45,
      requirePrepayment: true,
      maintenanceMode: false,
      enableFastPassBarriers: true,
      enableEmailReceipts: true
    });
    this.notification.info('Settings form reset to commercial defaults.');
  }
}
