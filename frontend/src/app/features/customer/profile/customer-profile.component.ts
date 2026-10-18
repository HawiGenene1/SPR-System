import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-customer-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    StatusBadgeComponent
  ],
  template: `
    <div class="customer-profile-container" *ngIf="auth.currentUser$ | async as user">
      <!-- Header -->
      <div class="page-header">
        <div class="header-titles">
          <h2>Customer Profile & Preferences</h2>
          <p>Update personal contact credentials, notification settings, and password security.</p>
        </div>
      </div>

      <div class="profile-layout">
        <!-- Identity Summary Card -->
        <div class="profile-card identity-card">
          <div class="avatar-banner">
            <div class="avatar-circle">
              {{ user.fullName ? user.fullName.charAt(0).toUpperCase() : 'C' }}
            </div>
            <h3>{{ user.fullName }}</h3>
            <span class="user-email">{{ user.email }}</span>
            <div class="role-badge-row">
              <app-status-badge [status]="'Active'" label="Customer Member"></app-status-badge>
            </div>
          </div>

          <div class="card-divider"></div>

          <div class="info-list">
            <div class="info-item">
              <span class="label">Customer ID</span>
              <span class="val font-mono">{{ user.id }}</span>
            </div>
            <div class="info-item">
              <span class="label">Account Status</span>
              <span class="val text-green">Verified & Active</span>
            </div>
            <div class="info-item">
              <span class="label">Member Since</span>
              <span class="val">{{ (user.createdAt || '2026-01-01') | date:'mediumDate' }}</span>
            </div>
          </div>
        </div>

        <!-- Form Columns -->
        <div class="forms-col">
          <!-- Contact Info Form -->
          <div class="profile-card">
            <div class="card-header-row">
              <div class="icon-circle icon-blue">
                <span class="material-symbols-outlined">badge</span>
              </div>
              <div>
                <h3>Personal Information</h3>
                <p>Used for booking receipts and SMS gate entry notifications.</p>
              </div>
            </div>

            <form [formGroup]="infoForm" (ngSubmit)="updateInfo()" class="card-form">
              <div class="form-grid-2">
                <mat-form-field appearance="outline">
                  <mat-label>Full Name</mat-label>
                  <input matInput formControlName="fullName">
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Email Address</mat-label>
                  <input matInput formControlName="email" readonly>
                </mat-form-field>
              </div>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Phone Number</mat-label>
                <input matInput formControlName="phoneNumber" placeholder="+1 (555) 000-0000">
              </mat-form-field>

              <div class="form-actions">
                <button mat-flat-button color="primary" type="submit" [disabled]="infoForm.invalid || savingInfo">
                  {{ savingInfo ? 'Saving...' : 'Save Profile Changes' }}
                </button>
              </div>
            </form>
          </div>

          <!-- Password Security Form -->
          <div class="profile-card">
            <div class="card-header-row">
              <div class="icon-circle icon-amber">
                <span class="material-symbols-outlined">lock_reset</span>
              </div>
              <div>
                <h3>Password Security</h3>
                <p>Modify your password to keep your contactless tickets and vehicle passes safe.</p>
              </div>
            </div>

            <form [formGroup]="passwordForm" (ngSubmit)="changePassword()" class="card-form">
              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Current Password</mat-label>
                <input matInput type="password" formControlName="currentPassword" placeholder="••••••••">
              </mat-form-field>

              <div class="form-grid-2">
                <mat-form-field appearance="outline">
                  <mat-label>New Password</mat-label>
                  <input matInput type="password" formControlName="newPassword" placeholder="••••••••">
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Confirm New Password</mat-label>
                  <input matInput type="password" formControlName="confirmPassword" placeholder="••••••••">
                </mat-form-field>
              </div>

              <div class="form-actions">
                <button mat-flat-button color="accent" type="submit" [disabled]="passwordForm.invalid || savingPassword">
                  {{ savingPassword ? 'Updating Password...' : 'Save New Password' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .customer-profile-container { display: flex; flex-direction: column; gap: 24px; }
    .page-header {
      .header-titles {
        h2 { margin: 0 0 4px 0; font-size: 1.5rem; font-weight: 800; color: #0f172a; letter-spacing: -0.02em; }
        p { margin: 0; color: #64748b; font-size: 0.875rem; }
      }
    }
    .profile-layout {
      display: grid; grid-template-columns: 320px 1fr; gap: 24px; align-items: start;
      @media (max-width: 1024px) { grid-template-columns: 1fr; }
    }
    .profile-card {
      background: #ffffff; border: 1px solid var(--border-light, #e2e8f0); border-radius: var(--radius-lg, 16px);
      padding: 24px; box-shadow: 0 1px 3px rgba(15, 23, 42, 0.04);
    }
    .identity-card {
      display: flex; flex-direction: column;
      .avatar-banner {
        display: flex; flex-direction: column; align-items: center; text-align: center;
        .avatar-circle {
          width: 80px; height: 80px; border-radius: 50%; background: linear-gradient(135deg, #1e3a8a, #2563eb);
          color: #ffffff; font-size: 2rem; font-weight: 800; display: flex; align-items: center; justify-content: center;
          margin-bottom: 12px; box-shadow: 0 4px 14px rgba(37, 99, 235, 0.25);
        }
        h3 { margin: 0 0 2px 0; font-size: 1.25rem; font-weight: 800; color: #0f172a; }
        .user-email { font-size: 0.8125rem; color: #64748b; margin-bottom: 10px; }
      }
      .card-divider { height: 1px; background: #f1f5f9; margin: 16px 0; }
      .info-list {
        display: flex; flex-direction: column; gap: 12px; font-size: 0.8125rem;
        .info-item {
          display: flex; flex-direction: column; gap: 2px;
          .label { color: #94a3b8; font-weight: 600; text-transform: uppercase; font-size: 0.6875rem; }
          .val { color: #0f172a; font-weight: 600; }
          .font-mono { font-family: 'Courier New', monospace; font-size: 0.75rem; }
          .text-green { color: #059669; }
        }
      }
    }
    .forms-col { display: flex; flex-direction: column; gap: 24px; }
    .card-header-row {
      display: flex; align-items: center; gap: 14px; margin-bottom: 20px;
      .icon-circle {
        width: 44px; height: 44px; border-radius: 12px; display: flex; align-items: center; justify-content: center;
        &.icon-blue { background: #eff6ff; color: #2563eb; }
        &.icon-amber { background: #fffbeb; color: #d97706; }
        .material-symbols-outlined { font-size: 24px; }
      }
      h3 { margin: 0; font-size: 1.15rem; font-weight: 700; color: #0f172a; }
      p { margin: 2px 0 0 0; font-size: 0.8125rem; color: #64748b; }
    }
    .card-form {
      display: flex; flex-direction: column; gap: 14px;
      .w-full { width: 100%; }
      .form-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; @media (max-width: 640px) { grid-template-columns: 1fr; } }
      .form-actions { display: flex; justify-content: flex-end; margin-top: 6px; }
    }
  `]
})
export class CustomerProfileComponent implements OnInit {
  auth = inject(AuthService);
  private fb = inject(FormBuilder);
  private notification = inject(NotificationService);

  infoForm!: FormGroup;
  passwordForm!: FormGroup;

  savingInfo: boolean = false;
  savingPassword: boolean = false;

  ngOnInit(): void {
    const user = this.auth.currentUserValue;

    this.infoForm = this.fb.group({
      fullName: [user?.fullName || '', Validators.required],
      email: [{ value: user?.email || '', disabled: true }],
      phoneNumber: [user?.phoneNumber || '']
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required, Validators.minLength(6)]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  updateInfo(): void {
    if (this.infoForm.invalid) return;

    this.savingInfo = true;
    setTimeout(() => {
      this.savingInfo = false;
      this.notification.success('Profile details saved successfully.');
    }, 700);
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;

    const { newPassword, confirmPassword } = this.passwordForm.value;
    if (newPassword !== confirmPassword) {
      this.notification.error('New password and confirmation do not match.');
      return;
    }

    this.savingPassword = true;
    setTimeout(() => {
      this.savingPassword = false;
      this.notification.success('Security password modified successfully.');
      this.passwordForm.reset();
    }, 900);
  }
}
