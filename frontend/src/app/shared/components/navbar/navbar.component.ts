import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, MatMenuModule, MatButtonModule, MatDividerModule],
  template: `
    <header class="app-header">
      <div class="header-container">
        <!-- Left: Sidebar Toggle & Brand -->
        <div class="header-left">
          <button 
            *ngIf="showSidebarToggle && (auth.currentUser$ | async)"
            class="sidebar-hamburger-btn" 
            (click)="onToggleSidebar()"
            title="Toggle Menu">
            <span class="material-symbols-outlined">menu</span>
          </button>

          <a routerLink="/" class="brand-wrap">
            <div class="brand-logo">
              <span class="material-symbols-outlined logo-icon">local_parking</span>
              <div class="beacon-pulse"></div>
            </div>
            <div class="brand-text">
              <span class="brand-title">Park<span>Pulse</span></span>
              <span class="brand-tagline">Smart Parking Platform</span>
            </div>
          </a>
        </div>

        <!-- Role Badges & Navigation Links -->
        <nav class="nav-links" *ngIf="auth.currentUser$ | async as user">
          <!-- Customer Links -->
          <ng-container *ngIf="user.role.toLowerCase() === 'customer'">
            <a routerLink="/customer/dashboard" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">dashboard</span>
              <span>Dashboard</span>
            </a>
            <a routerLink="/customer/parking" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">search_check</span>
              <span>Find & Book</span>
            </a>
            <a routerLink="/customer/reservations" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}" class="nav-link">
              <span class="material-symbols-outlined">confirmation_number</span>
              <span>Reservations</span>
            </a>
            <a routerLink="/customer/vehicles" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">directions_car</span>
              <span>Vehicles</span>
            </a>
            <a routerLink="/customer/history" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">history</span>
              <span>History</span>
            </a>
          </ng-container>

          <!-- Attendant Links -->
          <ng-container *ngIf="user.role.toLowerCase() === 'attendant'">
            <a routerLink="/attendant/dashboard" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">space_dashboard</span>
              <span>Gate Station</span>
            </a>
            <a routerLink="/qr-code/scanner" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">qr_code_scanner</span>
              <span>FastPass Scanner</span>
            </a>
            <a routerLink="/attendant/gate" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">input</span>
              <span>Check-In / Out</span>
            </a>
            <a routerLink="/attendant/history" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">history</span>
              <span>Parking Log</span>
            </a>
          </ng-container>

          <!-- Admin Links -->
          <ng-container *ngIf="user.role.toLowerCase() === 'admin'">
            <a routerLink="/admin/dashboard" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">monitoring</span>
              <span>Executive KPI</span>
            </a>
            <a routerLink="/admin/areas" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">garage_home</span>
              <span>Areas & Slots</span>
            </a>
            <a routerLink="/admin/reservations" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">event_note</span>
              <span>Reservations</span>
            </a>
            <a routerLink="/reports" routerLinkActive="active" class="nav-link">
              <span class="material-symbols-outlined">analytics</span>
              <span>Reports</span>
            </a>
          </ng-container>
        </nav>

        <!-- Right Side: User Profile or Sign In -->
        <div class="header-actions">
          <ng-container *ngIf="auth.currentUser$ | async as user; else guestNav">
            <!-- Role indicator -->
            <span class="role-pill" [ngClass]="user.role.toLowerCase()">
              {{ user.role }}
            </span>

            <!-- Profile Menu Button -->
            <button class="user-menu-btn" [matMenuTriggerFor]="userMenu">
              <div class="avatar-initial">
                {{ user.fullName ? user.fullName.charAt(0).toUpperCase() : 'U' }}
              </div>
              <div class="user-meta">
                <span class="user-name">{{ user.fullName }}</span>
                <span class="user-email">{{ user.email }}</span>
              </div>
              <span class="material-symbols-outlined dropdown-icon">expand_more</span>
            </button>

            <mat-menu #userMenu="matMenu" class="user-mat-menu" xPosition="before">
              <div class="menu-header-card">
                <p class="menu-user-name">{{ user.fullName }}</p>
                <p class="menu-user-email">{{ user.email }}</p>
                <span class="role-subtext">Role: {{ user.role }}</span>
              </div>
              <mat-divider></mat-divider>
              <a mat-menu-item [routerLink]="user.role.toLowerCase() === 'customer' ? '/customer/profile' : '/profile'">
                <span class="material-symbols-outlined">manage_accounts</span>
                <span>Profile Settings</span>
              </a>
              <a mat-menu-item routerLink="/customer/payments" *ngIf="user.role.toLowerCase() === 'customer'">
                <span class="material-symbols-outlined">receipt_long</span>
                <span>Payment History</span>
              </a>
              <a mat-menu-item routerLink="/qr-code" *ngIf="user.role.toLowerCase() === 'customer'">
                <span class="material-symbols-outlined">qr_code</span>
                <span>Digital Pass</span>
              </a>
              <mat-divider></mat-divider>
              <button mat-menu-item (click)="logout()">
                <span class="material-symbols-outlined">logout</span>
                <span>Sign Out</span>
              </button>
            </mat-menu>
          </ng-container>

          <ng-template #guestNav>
            <div class="guest-actions">
              <a routerLink="/auth/login" class="pp-btn secondary sm">Sign In</a>
              <a routerLink="/auth/register" class="pp-btn primary sm">Create Account</a>
            </div>
          </ng-template>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .app-header {
      background: #ffffff;
      border-bottom: 1px solid var(--border-light, #e2e8f0);
      position: sticky;
      top: 0;
      z-index: 1000;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
    }

    .header-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 0 24px;
      height: 70px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 20px;
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: 14px;
    }

    .sidebar-hamburger-btn {
      width: 36px;
      height: 36px;
      border-radius: 8px;
      border: 1px solid var(--border-light, #e2e8f0);
      background: #f8fafc;
      color: #475569;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      transition: all 0.15s ease;

      &:hover {
        background: #eff6ff;
        color: #2563eb;
        border-color: #bfdbfe;
      }

      .material-symbols-outlined {
        font-size: 20px;
      }
    }

    .brand-wrap {
      display: flex;
      align-items: center;
      gap: 12px;
      text-decoration: none;
      color: inherit;

      .brand-logo {
        position: relative;
        width: 40px;
        height: 40px;
        background: linear-gradient(135deg, #1d4ed8, #2563eb);
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        color: white;
        box-shadow: 0 4px 10px rgba(37, 99, 235, 0.3);

        .logo-icon {
          font-size: 26px;
        }

        .beacon-pulse {
          position: absolute;
          top: -2px;
          right: -2px;
          width: 9px;
          height: 9px;
          background: #10b981;
          border: 2px solid #ffffff;
          border-radius: 50%;
          animation: pulse 2s infinite;
        }
      }

      .brand-text {
        display: flex;
        flex-direction: column;

        .brand-title {
          font-size: 1.25rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.02em;

          span {
            color: #2563eb;
          }
        }

        .brand-tagline {
          font-size: 0.6875rem;
          text-transform: uppercase;
          letter-spacing: 0.08em;
          color: #64748b;
          font-weight: 600;
        }
      }
    }

    @keyframes pulse {
      0% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
      70% { box-shadow: 0 0 0 7px rgba(16, 185, 129, 0); }
      100% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
    }

    .nav-links {
      display: flex;
      align-items: center;
      gap: 6px;

      .nav-link {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 8px 14px;
        color: #475569;
        text-decoration: none;
        font-weight: 500;
        font-size: 0.875rem;
        border-radius: 8px;
        transition: all 0.15s ease;

        .material-symbols-outlined {
          font-size: 18px;
          color: #64748b;
        }

        &:hover {
          color: #1e3a8a;
          background: #f1f5f9;

          .material-symbols-outlined {
            color: #2563eb;
          }
        }

        &.active {
          color: #2563eb;
          background: #eff6ff;
          font-weight: 600;

          .material-symbols-outlined {
            color: #2563eb;
          }
        }
      }
    }

    .header-actions {
      display: flex;
      align-items: center;
      gap: 14px;
    }

    .role-pill {
      font-size: 0.6875rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      padding: 4px 10px;
      border-radius: 9999px;
      border: 1px solid transparent;

      &.admin {
        background: #faf5ff;
        color: #6b21a8;
        border-color: #e9d5ff;
      }
      &.attendant {
        background: #f0fdf4;
        color: #166534;
        border-color: #bbf7d0;
      }
      &.customer {
        background: #eff6ff;
        color: #1e40af;
        border-color: #bfdbfe;
      }
    }

    .user-menu-btn {
      display: flex;
      align-items: center;
      gap: 10px;
      background: transparent;
      border: 1px solid transparent;
      padding: 4px 8px;
      border-radius: 10px;
      cursor: pointer;
      transition: all 0.15s ease;

      &:hover {
        background: #f8fafc;
        border-color: #e2e8f0;
      }

      .avatar-initial {
        width: 34px;
        height: 34px;
        border-radius: 50%;
        background: linear-gradient(135deg, #2563eb, #1e40af);
        color: white;
        font-weight: 700;
        font-size: 0.875rem;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .user-meta {
        display: flex;
        flex-direction: column;
        text-align: left;

        .user-name {
          font-size: 0.8125rem;
          font-weight: 600;
          color: #0f172a;
          line-height: 1.2;
        }

        .user-email {
          font-size: 0.6875rem;
          color: #64748b;
        }
      }

      .dropdown-icon {
        font-size: 18px;
        color: #94a3b8;
      }
    }

    .menu-header-card {
      padding: 12px 16px;
      
      .menu-user-name {
        margin: 0;
        font-weight: 700;
        font-size: 0.875rem;
        color: #0f172a;
      }
      .menu-user-email {
        margin: 2px 0 6px 0;
        font-size: 0.75rem;
        color: #64748b;
      }
      .role-subtext {
        font-size: 0.6875rem;
        font-weight: 600;
        color: #2563eb;
      }
    }

    .guest-actions {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    @media (max-width: 1024px) {
      .nav-links {
        display: none;
      }
      .user-meta {
        display: none;
      }
    }
  `]
})
export class NavbarComponent {
  auth = inject(AuthService);
  notification = inject(NotificationService);
  router = inject(Router);

  @Input() showSidebarToggle: boolean = true;
  @Output() toggleSidebar = new EventEmitter<void>();

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  logout(): void {
    this.auth.logout();
    this.notification.info('You have been logged out.');
    this.router.navigate(['/auth/login']);
  }
}
