import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, MatProgressSpinnerModule],
  template: `
    <div class="auth-page-container">
      <div class="auth-card-wrapper">
        <div class="auth-header">
          <div class="brand-badge">
            <span class="material-symbols-outlined logo-icon">person_add</span>
          </div>
          <h2>Create Customer Account</h2>
          <p>Book premium parking spaces in seconds</p>
        </div>

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" class="auth-form">
          <div class="form-field-group">
            <label class="field-label" for="fullName">Full Name</label>
            <input 
              id="fullName" 
              type="text" 
              formControlName="fullName" 
              class="custom-input" 
              placeholder="e.g. John Doe"
              [class.has-error]="isFieldInvalid('fullName')" />
            <span class="field-error" *ngIf="isFieldInvalid('fullName')">Full Name is required.</span>
          </div>

          <div class="form-field-group">
            <label class="field-label" for="email">Email Address</label>
            <input 
              id="email" 
              type="email" 
              formControlName="email" 
              class="custom-input" 
              placeholder="john@example.com"
              [class.has-error]="isFieldInvalid('email')" />
            <span class="field-error" *ngIf="isFieldInvalid('email')">Valid email is required.</span>
          </div>

          <div class="form-field-group">
            <label class="field-label" for="phoneNumber">Phone Number (Optional)</label>
            <input 
              id="phoneNumber" 
              type="tel" 
              formControlName="phoneNumber" 
              class="custom-input" 
              placeholder="+1 (555) 000-0000" />
          </div>

          <div class="form-field-group">
            <label class="field-label" for="password">Password</label>
            <input 
              id="password" 
              type="password" 
              formControlName="password" 
              class="custom-input" 
              placeholder="Min. 6 chars (upper, lower, digit, special)"
              [class.has-error]="isFieldInvalid('password')" />
            <span class="field-error" *ngIf="isFieldInvalid('password')">
              Password must be at least 6 characters.
            </span>
          </div>

          <button 
            type="submit" 
            class="pp-btn primary submit-btn" 
            [disabled]="registerForm.invalid || isLoading">
            <mat-spinner *ngIf="isLoading" diameter="20" color="accent"></mat-spinner>
            <span *ngIf="!isLoading">Register & Get Started</span>
          </button>
        </form>

        <div class="auth-footer">
          <p>Already have an account? <a routerLink="/auth/login">Sign In</a></p>
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
      max-width: 480px;
      background: #ffffff;
      border: 1px solid var(--border-light);
      border-radius: var(--radius-xl);
      box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
      padding: 40px 36px;
    }

    .auth-header {
      text-align: center;
      margin-bottom: 28px;

      .brand-badge {
        width: 52px;
        height: 52px;
        border-radius: 14px;
        background: linear-gradient(135deg, #0d9488, #059669);
        color: white;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 16px;
        box-shadow: 0 4px 12px rgba(5, 150, 105, 0.3);

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

    .auth-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .form-field-group {
      display: flex;
      flex-direction: column;
      gap: 6px;

      .field-label {
        font-size: 0.8125rem;
        font-weight: 600;
        color: #334155;
      }

      .custom-input {
        width: 100%;
        padding: 11px 14px;
        border-radius: var(--radius-md);
        border: 1px solid #cbd5e1;
        font-size: 0.875rem;
        color: #0f172a;
        background: #ffffff;
        transition: border-color 0.15s ease, box-shadow 0.15s ease;

        &:focus {
          outline: none;
          border-color: #059669;
          box-shadow: 0 0 0 3px rgba(5, 150, 105, 0.15);
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
      margin-top: 10px;
      background: linear-gradient(135deg, #059669, #047857);
      &:hover:not(:disabled) {
        background: linear-gradient(135deg, #047857, #065f46);
      }
    }

    .auth-footer {
      margin-top: 24px;
      text-align: center;
      p {
        margin: 0;
        font-size: 0.8125rem;
        color: #64748b;
        a {
          color: #059669;
          font-weight: 600;
          text-decoration: none;
          &:hover { text-decoration: underline; }
        }
      }
    }
  `]
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private notify = inject(NotificationService);
  private router = inject(Router);

  isLoading = false;

  registerForm: FormGroup = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const { fullName, email, phoneNumber, password } = this.registerForm.value;

    this.auth.register({ fullName, email, phoneNumber, password }).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.notify.success('Account created successfully! Welcome to ParkPulse.');
        this.router.navigate(['/customer/dashboard']);
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}
