import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    <!-- SKELETON MODE -->
    <div *ngIf="mode === 'skeleton'" class="skeleton-container" [style.min-height.px]="minHeight">
      <div class="skeleton-card-item" *ngFor="let i of skeletonItems">
        <div class="skeleton-box skeleton-icon-placeholder"></div>
        <div class="skeleton-lines">
          <div class="skeleton-box skeleton-text w-half"></div>
          <div class="skeleton-box skeleton-text w-3-4"></div>
        </div>
      </div>
    </div>

    <!-- SPINNER MODE -->
    <div *ngIf="mode !== 'skeleton'" [class.spinner-overlay]="overlay" [class.spinner-inline]="!overlay" class="spinner-container" [style.min-height.px]="minHeight">
      <div class="spinner-content">
        <div class="radar-pulse">
          <div class="pulse-ring"></div>
          <mat-spinner [diameter]="diameter" [strokeWidth]="strokeWidth" color="primary"></mat-spinner>
        </div>
        <p *ngIf="message" class="spinner-message">{{ message }}</p>
      </div>
    </div>
  `,
  styles: [`
    .spinner-container {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 24px;
      width: 100%;
      box-sizing: border-box;
    }

    .spinner-inline {
      width: 100%;
      min-height: 180px;
    }

    .spinner-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(15, 23, 42, 0.45);
      backdrop-filter: blur(4px);
      z-index: 9999;
    }

    .spinner-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 16px;
      background: #ffffff;
      padding: 24px 32px;
      border-radius: var(--radius-lg, 16px);
      box-shadow: 0 20px 25px -5px rgba(15, 23, 42, 0.1), 0 8px 10px -6px rgba(15, 23, 42, 0.06);
      border: 1px solid var(--border-light, #e2e8f0);
    }

    .radar-pulse {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;

      .pulse-ring {
        position: absolute;
        width: 100%;
        height: 100%;
        border-radius: 50%;
        border: 2px solid var(--primary-500, #3b82f6);
        animation: radar-ping 1.8s cubic-bezier(0, 0, 0.2, 1) infinite;
        pointer-events: none;
      }
    }

    @keyframes radar-ping {
      0% {
        transform: scale(0.9);
        opacity: 0.8;
      }
      80%, 100% {
        transform: scale(1.6);
        opacity: 0;
      }
    }

    .spinner-message {
      margin: 0;
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--text-secondary, #475569);
      letter-spacing: -0.01em;
    }

    /* Skeleton layout */
    .skeleton-container {
      display: flex;
      flex-direction: column;
      gap: 16px;
      width: 100%;
      padding: 16px;
      box-sizing: border-box;

      .skeleton-card-item {
        display: flex;
        align-items: center;
        gap: 16px;
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: var(--radius-md, 12px);
        padding: 16px 20px;

        .skeleton-icon-placeholder {
          width: 44px;
          height: 44px;
          border-radius: 10px;
          flex-shrink: 0;
        }

        .skeleton-lines {
          flex: 1;
          display: flex;
          flex-direction: column;
          gap: 6px;
        }
      }
    }
  `]
})
export class LoadingSpinnerComponent {
  @Input() message: string = 'Loading data...';
  @Input() overlay: boolean = false;
  @Input() diameter: number = 44;
  @Input() strokeWidth: number = 4;
  @Input() mode: 'spinner' | 'skeleton' = 'spinner';
  @Input() skeletonCount: number = 3;
  @Input() minHeight: number = 180;

  get skeletonItems(): number[] {
    return Array.from({ length: this.skeletonCount }, (_, i) => i);
  }
}
