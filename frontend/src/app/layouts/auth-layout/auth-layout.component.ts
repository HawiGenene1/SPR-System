import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="auth-layout-root">
      <!-- Ambient Glow Accents -->
      <div class="glow-orb glow-orb-1"></div>
      <div class="glow-orb glow-orb-2"></div>

      <!-- Top brand bar -->
      <header class="auth-header">
        <a routerLink="/" class="brand-link">
          <div class="logo-box">
            <span class="material-symbols-outlined logo-icon">local_parking</span>
          </div>
          <span class="brand-title">Park<span>Pulse</span></span>
        </a>
      </header>

      <!-- Center Auth Form Outlet -->
      <main class="auth-viewport">
        <router-outlet></router-outlet>
      </main>

      <!-- Minimal Auth Footer -->
      <footer class="auth-footer">
        <span>© 2026 Smart Parking Reservation System (ParkPulse). Secure Enterprise Access.</span>
      </footer>
    </div>
  `,
  styles: [`
    .auth-layout-root {
      position: relative;
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      background: radial-gradient(circle at 50% 0%, #eff6ff 0%, #f8fafc 100%);
      overflow: hidden;
      padding: 24px;
      box-sizing: border-box;
    }

    .glow-orb {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.35;
      pointer-events: none;
      z-index: 0;

      &.glow-orb-1 {
        width: 380px;
        height: 380px;
        background: #3b82f6;
        top: -100px;
        left: -100px;
      }

      &.glow-orb-2 {
        width: 440px;
        height: 440px;
        background: #60a5fa;
        bottom: -120px;
        right: -120px;
      }
    }

    .auth-header {
      position: relative;
      z-index: 1;
      display: flex;
      justify-content: center;
      padding-top: 12px;

      .brand-link {
        display: inline-flex;
        align-items: center;
        gap: 12px;
        text-decoration: none;

        .logo-box {
          width: 44px;
          height: 44px;
          border-radius: 12px;
          background: linear-gradient(135deg, #1e3a8a, #2563eb);
          display: flex;
          align-items: center;
          justify-content: center;
          color: white;
          box-shadow: 0 4px 14px rgba(37, 99, 235, 0.3);

          .logo-icon {
            font-size: 28px;
          }
        }

        .brand-title {
          font-size: 1.5rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.03em;
          span { color: #2563eb; }
        }
      }
    }

    .auth-viewport {
      position: relative;
      z-index: 1;
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 32px 16px;
    }

    .auth-footer {
      position: relative;
      z-index: 1;
      text-align: center;
      font-size: 0.75rem;
      color: #94a3b8;
      padding: 12px;
    }
  `]
})
export class AuthLayoutComponent {}
