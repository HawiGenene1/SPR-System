import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: string;
}

interface NavGroup {
  title: string;
  items: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <aside class="sidebar-aside" [class.collapsed]="collapsed">
      <!-- Sidebar Header / Collapse Action -->
      <div class="sidebar-header">
        <div class="brand-preview" *ngIf="!collapsed">
          <div class="brand-badge">
            <span class="material-symbols-outlined logo-icon">local_parking</span>
          </div>
          <div class="brand-info">
            <span class="brand-title">Park<span>Pulse</span></span>
            <span class="brand-edition">Enterprise</span>
          </div>
        </div>

        <button 
          class="collapse-toggle-btn" 
          (click)="toggleCollapse()" 
          [title]="collapsed ? 'Expand sidebar' : 'Collapse sidebar'">
          <span class="material-symbols-outlined">
            {{ collapsed ? 'chevron_right' : 'chevron_left' }}
          </span>
        </button>
      </div>

      <!-- Navigation Links -->
      <div class="nav-groups-container" *ngIf="auth.currentUser$ | async as user">
        <!-- Customer Links -->
        <div class="nav-group" *ngIf="user.role.toLowerCase() === 'customer' || user.role.toLowerCase() === 'admin'">
          <div class="group-title" *ngIf="!collapsed">
            <span>Customer Portal</span>
          </div>
          <ul class="nav-list">
            <li>
              <a routerLink="/customer/dashboard" routerLinkActive="active" class="nav-item-link" title="Customer Dashboard">
                <span class="material-symbols-outlined nav-icon">dashboard</span>
                <span class="nav-label" *ngIf="!collapsed">Dashboard</span>
              </a>
            </li>
            <li>
              <a routerLink="/customer/parking" routerLinkActive="active" class="nav-item-link" title="Find & Reserve Parking">
                <span class="material-symbols-outlined nav-icon">search_check</span>
                <span class="nav-label" *ngIf="!collapsed">Find & Book</span>
              </a>
            </li>
            <li>
              <a routerLink="/customer/reservations" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}" class="nav-item-link" title="My Reservations">
                <span class="material-symbols-outlined nav-icon">confirmation_number</span>
                <span class="nav-label" *ngIf="!collapsed">Reservations</span>
              </a>
            </li>
            <li>
              <a routerLink="/customer/vehicles" routerLinkActive="active" class="nav-item-link" title="My Registered Vehicles">
                <span class="material-symbols-outlined nav-icon">directions_car</span>
                <span class="nav-label" *ngIf="!collapsed">Vehicles</span>
              </a>
            </li>
            <li>
              <a routerLink="/customer/payments" routerLinkActive="active" class="nav-item-link" title="Payments & Ledger">
                <span class="material-symbols-outlined nav-icon">receipt_long</span>
                <span class="nav-label" *ngIf="!collapsed">Payments</span>
              </a>
            </li>
            <li>
              <a routerLink="/customer/history" routerLinkActive="active" class="nav-item-link" title="Parking History & Visits">
                <span class="material-symbols-outlined nav-icon">history</span>
                <span class="nav-label" *ngIf="!collapsed">History</span>
              </a>
            </li>
            <li>
              <a routerLink="/customer/profile" routerLinkActive="active" class="nav-item-link" title="Customer Profile">
                <span class="material-symbols-outlined nav-icon">person</span>
                <span class="nav-label" *ngIf="!collapsed">Profile</span>
              </a>
            </li>
            <li>
              <a routerLink="/qr-code" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}" class="nav-item-link" title="Digital FastPass QR">
                <span class="material-symbols-outlined nav-icon">qr_code</span>
                <span class="nav-label" *ngIf="!collapsed">FastPass QR</span>
              </a>
            </li>
          </ul>
        </div>

        <!-- Attendant Links -->
        <div class="nav-group" *ngIf="user.role.toLowerCase().includes('attendant') || user.role.toLowerCase() === 'admin'">
          <div class="group-title" *ngIf="!collapsed">
            <span>Gate Operations</span>
          </div>
          <ul class="nav-list">
            <li>
              <a routerLink="/attendant/dashboard" routerLinkActive="active" class="nav-item-link" title="Gate Station Dashboard">
                <span class="material-symbols-outlined nav-icon">space_dashboard</span>
                <span class="nav-label" *ngIf="!collapsed">Gate Dashboard</span>
              </a>
            </li>
            <li>
              <a routerLink="/attendant/scanner" routerLinkActive="active" class="nav-item-link" title="FastPass Barrier Scanner">
                <span class="material-symbols-outlined nav-icon">qr_code_scanner</span>
                <span class="nav-label" *ngIf="!collapsed">Barrier Scanner</span>
              </a>
            </li>
            <li>
              <a routerLink="/attendant/entries" routerLinkActive="active" class="nav-item-link" title="Live Vehicle Entries">
                <span class="material-symbols-outlined nav-icon">login</span>
                <span class="nav-label" *ngIf="!collapsed">Gate Entries</span>
              </a>
            </li>
            <li>
              <a routerLink="/attendant/exits" routerLinkActive="active" class="nav-item-link" title="Vehicle Exits & Ledger">
                <span class="material-symbols-outlined nav-icon">logout</span>
                <span class="nav-label" *ngIf="!collapsed">Gate Exits</span>
              </a>
            </li>
            <li>
              <a routerLink="/attendant/parking-status" routerLinkActive="active" class="nav-item-link" title="Live Bay Status & Floor Map">
                <span class="material-symbols-outlined nav-icon">grid_view</span>
                <span class="nav-label" *ngIf="!collapsed">Bay Status Map</span>
              </a>
            </li>
          </ul>
        </div>

        <!-- Admin Links -->
        <div class="nav-group" *ngIf="user.role.toLowerCase() === 'admin'">
          <div class="group-title" *ngIf="!collapsed">
            <span>Administration</span>
          </div>
          <ul class="nav-list">
            <li>
              <a routerLink="/admin/dashboard" routerLinkActive="active" class="nav-item-link" title="Executive Dashboard">
                <span class="material-symbols-outlined nav-icon">monitoring</span>
                <span class="nav-label" *ngIf="!collapsed">Executive KPIs</span>
              </a>
            </li>
            <li>
              <a routerLink="/admin/users" routerLinkActive="active" class="nav-item-link" title="User Management">
                <span class="material-symbols-outlined nav-icon">group</span>
                <span class="nav-label" *ngIf="!collapsed">Users & Roles</span>
              </a>
            </li>
            <li>
              <a routerLink="/admin/parking-areas" routerLinkActive="active" class="nav-item-link" title="Facilities Management">
                <span class="material-symbols-outlined nav-icon">garage_home</span>
                <span class="nav-label" *ngIf="!collapsed">Parking Areas</span>
              </a>
            </li>
            <li>
              <a routerLink="/admin/parking-slots" routerLinkActive="active" class="nav-item-link" title="Bay Inventory">
                <span class="material-symbols-outlined nav-icon">grid_view</span>
                <span class="nav-label" *ngIf="!collapsed">Parking Slots</span>
              </a>
            </li>
            <li>
              <a routerLink="/admin/reservations" routerLinkActive="active" class="nav-item-link" title="All System Reservations">
                <span class="material-symbols-outlined nav-icon">event_note</span>
                <span class="nav-label" *ngIf="!collapsed">Reservations</span>
              </a>
            </li>
            <li>
              <a routerLink="/admin/payments" routerLinkActive="active" class="nav-item-link" title="Financial Transactions Ledger">
                <span class="material-symbols-outlined nav-icon">receipt_long</span>
                <span class="nav-label" *ngIf="!collapsed">Payments</span>
              </a>
            </li>
            <li>
              <a routerLink="/admin/reports" routerLinkActive="active" class="nav-item-link" title="Commercial Analytics & Telemetry">
                <span class="material-symbols-outlined nav-icon">analytics</span>
                <span class="nav-label" *ngIf="!collapsed">Reports</span>
              </a>
            </li>
            <li>
              <a routerLink="/admin/settings" routerLinkActive="active" class="nav-item-link" title="System Settings & Policies">
                <span class="material-symbols-outlined nav-icon">settings</span>
                <span class="nav-label" *ngIf="!collapsed">Settings</span>
              </a>
            </li>
          </ul>
        </div>

        <!-- Settings Group -->
        <div class="nav-group">
          <div class="group-title" *ngIf="!collapsed">
            <span>Account</span>
          </div>
          <ul class="nav-list">
            <li>
              <a routerLink="/profile" routerLinkActive="active" class="nav-item-link" title="My Profile Settings">
                <span class="material-symbols-outlined nav-icon">manage_accounts</span>
                <span class="nav-label" *ngIf="!collapsed">Profile Settings</span>
              </a>
            </li>
          </ul>
        </div>
      </div>

      <!-- User Card Footer in Sidebar -->
      <div class="sidebar-footer" *ngIf="auth.currentUser$ | async as user">
        <div class="user-strip" [class.collapsed-strip]="collapsed">
          <div class="user-avatar" [title]="user.fullName">
            {{ user.fullName ? user.fullName.charAt(0).toUpperCase() : 'U' }}
          </div>
          <div class="user-meta" *ngIf="!collapsed">
            <span class="user-name">{{ user.fullName }}</span>
            <span class="user-role-tag">{{ user.role }}</span>
          </div>
          <button 
            *ngIf="!collapsed" 
            class="logout-icon-btn" 
            (click)="logout()" 
            title="Log Out">
            <span class="material-symbols-outlined">logout</span>
          </button>
        </div>
      </div>
    </aside>
  `,
  styles: [`
    .sidebar-aside {
      width: 260px;
      height: 100%;
      background: #ffffff;
      border-right: 1px solid var(--border-light, #e2e8f0);
      display: flex;
      flex-direction: column;
      flex-shrink: 0;
      transition: width 0.25s cubic-bezier(0.16, 1, 0.3, 1);
      overflow: hidden;

      &.collapsed {
        width: 76px;
      }
    }

    .sidebar-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      height: 68px;
      padding: 0 16px;
      border-bottom: 1px solid var(--border-light, #e2e8f0);
      flex-shrink: 0;

      .brand-preview {
        display: flex;
        align-items: center;
        gap: 12px;

        .brand-badge {
          width: 38px;
          height: 38px;
          border-radius: 10px;
          background: linear-gradient(135deg, #1e3a8a 0%, #2563eb 100%);
          display: flex;
          align-items: center;
          justify-content: center;
          color: #ffffff;
          box-shadow: 0 4px 10px rgba(37, 99, 235, 0.25);

          .logo-icon {
            font-size: 22px;
          }
        }

        .brand-info {
          display: flex;
          flex-direction: column;

          .brand-title {
            font-size: 1.125rem;
            font-weight: 800;
            color: #0f172a;
            letter-spacing: -0.03em;
            span { color: #2563eb; }
          }

          .brand-edition {
            font-size: 0.6875rem;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.08em;
            color: #64748b;
          }
        }
      }

      .collapse-toggle-btn {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        border: 1px solid var(--border-light, #e2e8f0);
        background: #f8fafc;
        color: #64748b;
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
    }

    .nav-groups-container {
      flex: 1;
      overflow-y: auto;
      padding: 16px 12px;
      display: flex;
      flex-direction: column;
      gap: 20px;

      &::-webkit-scrollbar {
        width: 4px;
      }
      &::-webkit-scrollbar-thumb {
        background: #e2e8f0;
        border-radius: 4px;
      }
    }

    .nav-group {
      .group-title {
        font-size: 0.6875rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        color: #94a3b8;
        padding: 0 12px 6px 12px;
      }

      .nav-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 4px;

        .nav-item-link {
          display: flex;
          align-items: center;
          gap: 12px;
          height: 40px;
          padding: 0 12px;
          border-radius: 10px;
          color: #475569;
          text-decoration: none;
          font-size: 0.875rem;
          font-weight: 600;
          transition: all 0.15s ease;

          .nav-icon {
            font-size: 20px;
            color: #64748b;
            flex-shrink: 0;
            transition: color 0.15s ease;
          }

          .nav-label {
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
          }

          &:hover {
            background: #f1f5f9;
            color: #0f172a;
            .nav-icon { color: #2563eb; }
          }

          &.active {
            background: #eff6ff;
            color: #2563eb;
            font-weight: 700;
            .nav-icon { color: #2563eb; }
          }
        }
      }
    }

    .sidebar-footer {
      padding: 14px;
      border-top: 1px solid var(--border-light, #e2e8f0);
      background: #fafafa;
      flex-shrink: 0;

      .user-strip {
        display: flex;
        align-items: center;
        gap: 10px;

        &.collapsed-strip {
          justify-content: center;
        }

        .user-avatar {
          width: 36px;
          height: 36px;
          border-radius: 50%;
          background: #2563eb;
          color: #ffffff;
          font-weight: 700;
          font-size: 0.875rem;
          display: flex;
          align-items: center;
          justify-content: center;
          flex-shrink: 0;
        }

        .user-meta {
          flex: 1;
          display: flex;
          flex-direction: column;
          min-width: 0;

          .user-name {
            font-size: 0.8125rem;
            font-weight: 700;
            color: #0f172a;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
          }

          .user-role-tag {
            font-size: 0.6875rem;
            color: #64748b;
          }
        }

        .logout-icon-btn {
          background: transparent;
          border: none;
          color: #94a3b8;
          cursor: pointer;
          padding: 6px;
          border-radius: 6px;
          display: flex;
          align-items: center;
          justify-content: center;
          transition: all 0.15s ease;

          &:hover {
            color: #dc2626;
            background: #fef2f2;
          }

          .material-symbols-outlined {
            font-size: 18px;
          }
        }
      }
    }
  `]
})
export class SidebarComponent {
  auth = inject(AuthService);
  notification = inject(NotificationService);
  router = inject(Router);

  @Input() collapsed: boolean = false;
  @Output() collapsedChange = new EventEmitter<boolean>();

  toggleCollapse(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }

  logout(): void {
    this.auth.logout();
    this.notification.info('You have been signed out.');
    this.router.navigate(['/auth/login']);
  }
}
