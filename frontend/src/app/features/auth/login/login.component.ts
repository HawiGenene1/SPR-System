import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="auth-page-container">
      <div class="auth-card-wrapper">
        <!-- Brand Header inside card -->
        <div class="auth-header">
          <div class="brand-badge">
            <span class="material-symbols-outlined logo-icon">local_parking</span>
          </div>
          <h2>Welcome back to ParkPulse</h2>
          <p>Access your smart parking management portal</p>
        </div>

        <!-- Quick Demo Profiles Pills -->
        <div class="demo-profiles-section">
          <span class="demo-title">Quick Demo Login:</span>
          <div class="demo-buttons">
            <button type="button" class="demo-btn admin" (click)="fillDemo('admin')">
              <span class="material-symbols-outlined icon">shield_person</span>
              Admin
            </button>
            <button type="button" class="demo-btn attendant" (click)="fillDemo('attendant')">
              <span class="material-symbols-outlined icon">badge</span>
              Attendant
            </button>
            <button type="button" class="demo-btn customer" (click)="fillDemo('customer')">
              <span class="material-symbols-outlined icon">directions_car</span>
              Customer
            </button>
          </div>
        </div>

        <!-- Login Form -->
        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="auth-form">
          <div class="form-field-group">
            <label class="field-label" for="email">Email Address</label>
            <input 
              id="email" 
              type="email" 
              formControlName="email" 
              class="custom-input" 
              placeholder="name@company.com"
              [class.has-error]="isFieldInvalid('email')" />
            <span class="field-error" *ngIf="isFieldInvalid('email')">
              Please enter a valid email address.
            </span>
          </div>

          <div class="form-field-group">
            <div class="field-label-row">
              <label class="field-label" for="password">Password</label>
              <a href="javascript:void(0)" class="forgot-link">Forgot password?</a>
            </div>
            <input 
              id="password" 
              type="password" 
              formControlName="password" 
              class="custom-input" 
              placeholder="••••••••••••"
              [class.has-error]="isFieldInvalid('password')" />
            <span class="field-error" *ngIf="isFieldInvalid('password')">
              Password is required.
            </span>
          </div>

          <button 
            type="submit" 
            class="pp-btn primary submit-btn" 
            [disabled]="loginForm.invalid || isLoading">
            <mat-spinner *ngIf="isLoading" diameter="20" color="accent"></mat-spinner>
            <span *ngIf="!isLoading">Sign In to Dashboard</span>
          </button>
        </form>

        <div class="auth-footer">
          <p>Don't have an account yet? <a routerLink="/auth/register">Create Customer Account</a></p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page-container {
      min-height: calc(100vh - 160px);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 40px 16px;
    }

    .auth-card-wrapper {
      width: 100%;
      max-width: 460px;
      background: #ffffff;
      border: 1px solid var(--border-light);
      border-radius: var(--radius-xl);
      box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
      padding: 40px 36px;
    }

    .auth-header {
      text-align: center;
      margin-bottom: 24px;

      .brand-badge {
        width: 52px;
        height: 52px;
        border-radius: 14px;
        background: linear-gradient(135deg, #1d4ed8, #2563eb);
        color: white;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 16px;
        box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3);

        .logo-icon {
          font-size: 32px;
        }
      }

      h2 {
        font-size: 1.5rem;
        margin: 0 0 6px 0;
        color: #0f172a;
      }

      p {
        margin: 0;
        font-size: 0.875rem;
        color: #64748b;
      }
    }

    .demo-profiles-section {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: var(--radius-md);
      padding: 12px 14px;
      margin-bottom: 24px;

      .demo-title {
        display: block;
        font-size: 0.75rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: #64748b;
        margin-bottom: 8px;
      }

      .demo-buttons {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 8px;

        .demo-btn {
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 4px;
          padding: 6px 8px;
          border-radius: 6px;
          border: 1px solid #cbd5e1;
          background: #ffffff;
          font-size: 0.75rem;
          font-weight: 600;
          cursor: pointer;
          transition: all 0.15s ease;

          .icon {
            font-size: 14px;
          }

          &.admin:hover { background: #fdf2f8; color: #be185d; border-color: #fbcfe8; }
          &.attendant:hover { background: #fef3c7; color: #b45309; border-color: #fde68a; }
          &.customer:hover { background: #eff6ff; color: #1d4ed8; border-color: #bfdbfe; }
        }
      }
    }

    .auth-form {
      display: flex;
      flex-direction: column;
      gap: 18px;
    }

    .form-field-group {
      display: flex;
      flex-direction: column;
      gap: 6px;

      .field-label-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
      }

      .field-label {
        font-size: 0.8125rem;
        font-weight: 600;
        color: #334155;
      }

      .forgot-link {
        font-size: 0.75rem;
        color: #2563eb;
        text-decoration: none;
        &:hover { text-decoration: underline; }
      }

      .custom-input {
        width: 100%;
        padding: 11px 14px;
        border-radius: var(--radius-md);
        border: 1px solid #cbd5e1;
        font-size: 0.875rem;
        color: #0f172a;
        font-family: inherit;
        background: #ffffff;
        transition: border-color 0.15s ease, box-shadow 0.15s ease;

        &:focus {
          outline: none;
          border-color: #2563eb;
          box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.15);
        }

        &.has-error {
          border-color: #ef4444;
          background: #fef2f2;
        }
      }

      .field-error {
        font-size: 0.75rem;
        color: #dc2626;
      }
    }

    .submit-btn {
      width: 100%;
      padding: 12px;
      font-size: 0.9375rem;
      margin-top: 8px;
    }

    .auth-footer {
      margin-top: 24px;
      text-align: center;
      p {
        margin: 0;
        font-size: 0.8125rem;
        color: #64748b;
        a {
          color: #2563eb;
          font-weight: 600;
          text-decoration: none;
          &:hover { text-decoration: underline; }
        }
      }
    }
  `]
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private notify = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isLoading = false;

  loginForm: FormGroup = this.fb.group({
    email: ['admin@smartparking.com', [Validators.required, Validators.email]],
    password: ['Admin@Password123!', [Validators.required]]
  });

  fillDemo(role: 'admin' | 'attendant' | 'customer'): void {
    if (role === 'admin') {
      this.loginForm.setValue({
        email: 'admin@smartparking.com',
        password: 'Admin@Password123!'
      });
    } else if (role === 'attendant') {
      this.loginForm.setValue({
        email: 'attendant@smartparking.com',
        password: 'Attendant@123456'
      });
    } else {
      this.loginForm.setValue({
        email: 'customer@smartparking.com',
        password: 'Customer@123456'
      });
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const { email, password } = this.loginForm.value;

    this.auth.login({ email, password }).subscribe({
      next: (res) => {
        this.isLoading = false;
        const user = res?.data?.user || res?.user || this.auth.currentUserValue;
        this.notify.success(`Welcome back, ${user?.fullName || 'User'}!`);
        
        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        this.auth.redirectAfterLogin(returnUrl);
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}
