import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule],
  template: `
    <div class="access-denied-container">
      <div class="denied-card">
        <div class="icon-ring">
          <span class="material-symbols-outlined shield-icon">gpp_maybe</span>
        </div>

        <div class="error-badge">ERROR 403</div>

        <h2>Access Denied</h2>
        <p class="denied-message">
          You do not have permission to access the requested resource or administrative module with your current account privileges.
        </p>

        <!-- Role Comparison Info -->
        <div class="role-details-box" *ngIf="userRole">
          <div class="role-row">
            <span class="role-label">Your Active Role:</span>
            <span class="role-value current">{{ userRole }}</span>
          </div>
          <div class="role-row" *ngIf="requiredRoles">
            <span class="role-label">Required Role(s):</span>
            <span class="role-value required">{{ requiredRoles }}</span>
          </div>
        </div>

        <!-- Action CTAs -->
        <div class="action-buttons">
          <a [routerLink]="dashboardRoute" mat-flat-button color="primary" class="cta-btn">
            <span class="material-symbols-outlined">dashboard</span>
            <span>Return to My Dashboard</span>
          </a>

          <button mat-stroked-button (click)="switchAccount()" class="switch-btn">
            <span class="material-symbols-outlined">switch_account</span>
            <span>Sign In with Different Account</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .access-denied-container {
      min-height: calc(100vh - 180px);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 32px 16px;
    }

    .denied-card {
      width: 100%;
      max-width: 520px;
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: var(--radius-xl, 20px);
      padding: 44px 36px;
      text-align: center;
      box-shadow: 0 20px 25px -5px rgba(15, 23, 42, 0.08), 0 8px 10px -6px rgba(15, 23, 42, 0.04);
      display: flex;
      flex-direction: column;
      align-items: center;
    }

    .icon-ring {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: #fef2f2;
      border: 2px solid #fee2e2;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 20px;

      .shield-icon {
        font-size: 42px;
        color: #dc2626;
      }
    }

    .error-badge {
      display: inline-block;
      font-size: 0.75rem;
      font-weight: 800;
      letter-spacing: 0.1em;
      color: #dc2626;
      background: #fee2e2;
      padding: 4px 12px;
      border-radius: 9999px;
      margin-bottom: 12px;
    }

    h2 {
      margin: 0 0 10px 0;
      font-size: 1.75rem;
      font-weight: 800;
      color: #0f172a;
      letter-spacing: -0.02em;
    }

    .denied-message {
      margin: 0 0 24px 0;
      font-size: 0.9375rem;
      color: #64748b;
      line-height: 1.55;
    }

    .role-details-box {
      width: 100%;
      background: #f8fafc;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: 12px;
      padding: 16px;
      margin-bottom: 28px;
      display: flex;
      flex-direction: column;
      gap: 10px;
      font-size: 0.875rem;

      .role-row {
        display: flex;
        justify-content: space-between;
        align-items: center;

        .role-label {
          color: #64748b;
          font-weight: 600;
        }

        .role-value {
          font-weight: 700;

          &.current {
            color: #2563eb;
            background: #eff6ff;
            padding: 3px 10px;
            border-radius: 6px;
          }

          &.required {
            color: #d97706;
            background: #fffbeb;
            padding: 3px 10px;
            border-radius: 6px;
          }
        }
      }
    }

    .action-buttons {
      display: flex;
      flex-direction: column;
      gap: 12px;
      width: 100%;

      .cta-btn {
        height: 44px;
        font-weight: 700;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
      }

      .switch-btn {
        height: 44px;
        font-weight: 600;
        border-radius: 10px;
        color: #475569;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
      }
    }
  `]
})
export class AccessDeniedComponent {
  auth = inject(AuthService);
  private route = inject(ActivatedRoute);

  get userRole(): string {
    return this.auth.currentUserValue?.role || this.route.snapshot.queryParams['current'] || 'Guest';
  }

  get requiredRoles(): string | null {
    return this.route.snapshot.queryParams['required'] || null;
  }

  get dashboardRoute(): string {
    return this.auth.getDefaultRouteForRole();
  }

  switchAccount(): void {
    this.auth.logout();
  }
}
