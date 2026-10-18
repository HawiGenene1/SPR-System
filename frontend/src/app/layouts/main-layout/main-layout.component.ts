import { Component, HostListener, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { SidebarComponent } from '../../shared/components/sidebar/sidebar.component';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarComponent, SidebarComponent],
  template: `
    <div class="main-layout-root">
      <!-- Top Fixed Header -->
      <app-navbar 
        [showSidebarToggle]="true" 
        (toggleSidebar)="toggleSidebar()">
      </app-navbar>

      <div class="layout-body-wrapper">
        <!-- Backdrop Overlay for Mobile Drawer -->
        <div 
          class="sidebar-backdrop" 
          *ngIf="isMobile && !sidebarCollapsed && (auth.currentUser$ | async)"
          (click)="closeSidebar()">
        </div>

        <!-- Collapsible Sidebar -->
        <div class="sidebar-container" [class.mobile-open]="isMobile && !sidebarCollapsed">
          <app-sidebar 
            *ngIf="auth.currentUser$ | async" 
            [collapsed]="sidebarCollapsed"
            (collapsedChange)="onSidebarCollapsedChange($event)">
          </app-sidebar>
        </div>

        <!-- Dynamic Main Workspace -->
        <div class="content-viewport-column">
          <main class="main-content-area" [class.with-bottom-bar]="isMobile && (auth.currentUser$ | async)">
            <router-outlet></router-outlet>
          </main>

          <!-- Commercial Footer -->
          <footer class="app-footer" *ngIf="!isMobile">
            <div class="footer-container">
              <div class="footer-left">
                <span class="footer-brand">ParkPulse Enterprise</span>
                <span class="footer-copy">© 2026 Smart Parking Reservation System. High-availability facility control.</span>
              </div>
              <div class="footer-right">
                <span class="status-indicator">
                  <span class="indicator-dot"></span>
                  <span>Backend API Live (Port 5057)</span>
                </span>
              </div>
            </div>
          </footer>
        </div>
      </div>

      <!-- Mobile Bottom Navigation Dock for Instant Touch Access -->
      <nav class="mobile-bottom-dock" *ngIf="isMobile && (auth.currentUser$ | async) as user">
        <!-- Customer Dock -->
        <ng-container *ngIf="user.role.toLowerCase() === 'customer'">
          <a routerLink="/customer/dashboard" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">dashboard</span>
            <span>Home</span>
          </a>
          <a routerLink="/customer/parking" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">search_check</span>
            <span>Book</span>
          </a>
          <a routerLink="/customer/reservations" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">confirmation_number</span>
            <span>Passes</span>
          </a>
          <a routerLink="/customer/vehicles" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">directions_car</span>
            <span>Vehicles</span>
          </a>
          <button class="dock-item btn-dock-more" (click)="toggleSidebar()">
            <span class="material-symbols-outlined">menu</span>
            <span>Menu</span>
          </button>
        </ng-container>

        <!-- Attendant Dock -->
        <ng-container *ngIf="user.role.toLowerCase().includes('attendant')">
          <a routerLink="/attendant/dashboard" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">space_dashboard</span>
            <span>Station</span>
          </a>
          <a routerLink="/attendant/scanner" routerLinkActive="active" class="dock-item highlight-dock">
            <span class="material-symbols-outlined">qr_code_scanner</span>
            <span>Scan QR</span>
          </a>
          <a routerLink="/attendant/entries" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">login</span>
            <span>Entries</span>
          </a>
          <a routerLink="/attendant/exits" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">logout</span>
            <span>Exits</span>
          </a>
          <button class="dock-item btn-dock-more" (click)="toggleSidebar()">
            <span class="material-symbols-outlined">menu</span>
            <span>More</span>
          </button>
        </ng-container>

        <!-- Admin Dock -->
        <ng-container *ngIf="user.role.toLowerCase() === 'admin'">
          <a routerLink="/admin/dashboard" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">monitoring</span>
            <span>KPI</span>
          </a>
          <a routerLink="/admin/parking-areas" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">garage_home</span>
            <span>Areas</span>
          </a>
          <a routerLink="/admin/parking-slots" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">local_parking</span>
            <span>Slots</span>
          </a>
          <a routerLink="/admin/reservations" routerLinkActive="active" class="dock-item">
            <span class="material-symbols-outlined">event_note</span>
            <span>Bookings</span>
          </a>
          <button class="dock-item btn-dock-more" (click)="toggleSidebar()">
            <span class="material-symbols-outlined">menu</span>
            <span>More</span>
          </button>
        </ng-container>
      </nav>
    </div>
  `,
  styles: [`
    .main-layout-root {
      display: flex;
      flex-direction: column;
      height: 100vh;
      width: 100vw;
      overflow: hidden;
      background-color: var(--bg-main, #f8fafc);
    }

    .layout-body-wrapper {
      display: flex;
      flex: 1;
      overflow: hidden;
      position: relative;
    }

    .sidebar-container {
      height: 100%;
      flex-shrink: 0;
      transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1);
    }

    .sidebar-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(15, 23, 42, 0.5);
      backdrop-filter: blur(4px);
      z-index: 998;
      animation: fadeIn 0.2s ease-out forwards;
    }

    .content-viewport-column {
      display: flex;
      flex-direction: column;
      flex: 1;
      height: 100%;
      overflow-y: auto;
      overflow-x: hidden;
      background-color: var(--bg-main, #f8fafc);

      &::-webkit-scrollbar {
        width: 6px;
      }
      &::-webkit-scrollbar-thumb {
        background: #cbd5e1;
        border-radius: 4px;
      }
    }

    .main-content-area {
      flex: 1;
      max-width: 1400px;
      width: 100%;
      margin: 0 auto;
      padding: 32px 24px 64px 24px;
      box-sizing: border-box;

      &.with-bottom-bar {
        padding-bottom: 96px;
      }
    }

    .app-footer {
      background: #ffffff;
      border-top: 1px solid var(--border-light, #e2e8f0);
      padding: 16px 24px;
      flex-shrink: 0;

      .footer-container {
        max-width: 1400px;
        margin: 0 auto;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
      }

      .footer-left {
        display: flex;
        flex-direction: column;
        gap: 2px;

        .footer-brand {
          font-weight: 700;
          color: #0f172a;
          font-size: 0.8125rem;
        }

        .footer-copy {
          color: #64748b;
          font-size: 0.75rem;
        }
      }

      .footer-right {
        .status-indicator {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          font-size: 0.75rem;
          color: #059669;
          font-weight: 600;
          background: #ecfdf5;
          padding: 4px 10px;
          border-radius: 9999px;
          border: 1px solid #a7f3d0;

          .indicator-dot {
            width: 6px;
            height: 6px;
            border-radius: 50%;
            background: #10b981;
          }
        }
      }
    }

    /* Mobile Bottom Navigation Dock */
    .mobile-bottom-dock {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      height: 64px;
      background: #ffffff;
      border-top: 1px solid var(--border-light, #e2e8f0);
      box-shadow: 0 -4px 16px rgba(15, 23, 42, 0.06);
      display: flex;
      align-items: center;
      justify-content: space-around;
      z-index: 990;
      padding: 0 8px;

      .dock-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 3px;
        flex: 1;
        height: 100%;
        color: #64748b;
        text-decoration: none;
        background: transparent;
        border: none;
        cursor: pointer;
        font-family: inherit;
        font-size: 0.6875rem;
        font-weight: 600;
        transition: color 0.15s ease, transform 0.15s ease;
        padding: 4px;

        .material-symbols-outlined {
          font-size: 22px;
          transition: transform 0.15s ease;
        }

        &.active {
          color: #2563eb;
          .material-symbols-outlined {
            transform: scale(1.1);
          }
        }

        &.highlight-dock {
          color: #2563eb;
          .material-symbols-outlined {
            font-size: 26px;
            background: #eff6ff;
            border-radius: 50%;
            padding: 4px;
          }
        }

        &:active {
          transform: scale(0.94);
        }
      }
    }

    /* Responsive Breakdown: Tablet & Mobile */
    @media (max-width: 1024px) {
      .sidebar-container {
        position: fixed;
        top: 64px;
        left: 0;
        bottom: 0;
        z-index: 999;
        transform: translateX(-100%);
        box-shadow: 8px 0 24px rgba(15, 23, 42, 0.12);

        &.mobile-open {
          transform: translateX(0);
        }
      }

      .main-content-area {
        padding: 24px 18px 48px 18px;
      }
    }

    @media (max-width: 640px) {
      .main-content-area {
        padding: 16px 14px 32px 14px;
      }
    }
  `]
})
export class MainLayoutComponent implements OnInit {
  auth = inject(AuthService);
  router = inject(Router);

  sidebarCollapsed: boolean = false;
  isMobile: boolean = false;

  ngOnInit(): void {
    this.checkScreenSize();

    // Close mobile drawer on route change
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      if (this.isMobile) {
        this.sidebarCollapsed = true;
      }
    });
  }

  @HostListener('window:resize')
  onResize(): void {
    this.checkScreenSize();
  }

  private checkScreenSize(): void {
    this.isMobile = window.innerWidth <= 1024;
    if (this.isMobile) {
      // Default closed on tablet/mobile
      this.sidebarCollapsed = true;
    }
  }

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  closeSidebar(): void {
    this.sidebarCollapsed = true;
  }

  onSidebarCollapsedChange(collapsed: boolean): void {
    this.sidebarCollapsed = collapsed;
  }
}
