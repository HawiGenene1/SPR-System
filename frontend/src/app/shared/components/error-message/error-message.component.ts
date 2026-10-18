import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-error-message',
  standalone: true,
  imports: [CommonModule, MatButtonModule],
  template: `
    <div class="error-banner anim-slide-up" [class.compact]="compact" role="alert">
      <div class="banner-icon-col">
        <div class="icon-circle">
          <span class="material-symbols-outlined">{{ icon }}</span>
        </div>
      </div>

      <div class="banner-body">
        <h4 *ngIf="title" class="banner-title">{{ title }}</h4>
        
        <p *ngIf="message && !isArray(message)" class="banner-text">
          {{ message }}
        </p>

        <ul *ngIf="isArray(message)" class="banner-list">
          <li *ngFor="let item of asArray(message)">{{ item }}</li>
        </ul>
      </div>

      <div class="banner-actions" *ngIf="retryable || dismissible">
        <button 
          *ngIf="retryable" 
          mat-stroked-button 
          color="warn" 
          class="btn-retry"
          (click)="onRetry()">
          <span class="material-symbols-outlined">refresh</span>
          <span>{{ retryText }}</span>
        </button>

        <button 
          *ngIf="dismissible" 
          class="btn-dismiss" 
          (click)="onDismiss()"
          aria-label="Dismiss error">
          <span class="material-symbols-outlined">close</span>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .error-banner {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      padding: 16px 20px;
      background: #fef2f2;
      border: 1px solid #fecaca;
      border-radius: var(--radius-md, 12px);
      box-shadow: 0 1px 3px rgba(220, 38, 38, 0.05);
      margin: 16px 0;

      &.compact {
        padding: 10px 14px;
        gap: 12px;
      }
    }

    .banner-icon-col {
      flex-shrink: 0;

      .icon-circle {
        width: 36px;
        height: 36px;
        border-radius: 50%;
        background: #fee2e2;
        color: #dc2626;
        display: flex;
        align-items: center;
        justify-content: center;

        .material-symbols-outlined {
          font-size: 22px;
        }
      }
    }

    .banner-body {
      flex: 1;

      .banner-title {
        margin: 0 0 4px 0;
        font-size: 0.9375rem;
        font-weight: 700;
        color: #991b1b;
      }

      .banner-text {
        margin: 0;
        font-size: 0.875rem;
        color: #b91c1c;
        line-height: 1.45;
      }

      .banner-list {
        margin: 4px 0 0 0;
        padding-left: 20px;
        color: #b91c1c;
        font-size: 0.875rem;
        line-height: 1.4;
      }
    }

    .banner-actions {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-shrink: 0;

      .btn-retry {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        font-size: 0.8125rem;
        font-weight: 600;
        border-radius: 8px;
        border-color: #fca5a5;

        .material-symbols-outlined {
          font-size: 16px;
        }
      }

      .btn-dismiss {
        background: transparent;
        border: none;
        color: #991b1b;
        cursor: pointer;
        padding: 6px;
        border-radius: 6px;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: background-color 0.15s ease;

        &:hover {
          background: #fee2e2;
        }

        .material-symbols-outlined {
          font-size: 18px;
        }
      }
    }

    @media (max-width: 600px) {
      .error-banner {
        flex-direction: column;
        gap: 12px;

        .banner-actions {
          width: 100%;
          justify-content: flex-end;
        }
      }
    }
  `]
})
export class ErrorMessageComponent {
  @Input() title?: string = 'Operation Failed';
  @Input() message: string | string[] = 'An unexpected error occurred. Please try again.';
  @Input() icon: string = 'error_outline';
  @Input() retryable: boolean = false;
  @Input() retryText: string = 'Retry';
  @Input() dismissible: boolean = true;
  @Input() compact: boolean = false;

  @Output() retry = new EventEmitter<void>();
  @Output() dismiss = new EventEmitter<void>();

  isArray(val: any): boolean {
    return Array.isArray(val);
  }

  asArray(val: string | string[]): string[] {
    return Array.isArray(val) ? val : [val];
  }

  onRetry(): void {
    this.retry.emit();
  }

  onDismiss(): void {
    this.dismiss.emit();
  }
}
