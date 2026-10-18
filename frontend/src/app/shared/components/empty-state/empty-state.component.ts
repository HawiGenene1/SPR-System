import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule],
  template: `
    <div class="empty-state-card anim-slide-up" [class.compact]="compact">
      <div class="icon-bubble">
        <span class="material-symbols-outlined state-icon">{{ icon }}</span>
      </div>

      <div class="text-group">
        <h3 class="state-title">{{ title }}</h3>
        <p *ngIf="description" class="state-description">{{ description }}</p>
      </div>

      <div class="action-wrap" *ngIf="actionText">
        <a 
          *ngIf="actionRoute" 
          [routerLink]="actionRoute" 
          mat-flat-button 
          color="primary" 
          class="state-btn">
          <span *ngIf="actionIcon" class="material-symbols-outlined btn-icon">{{ actionIcon }}</span>
          <span>{{ actionText }}</span>
        </a>

        <button 
          *ngIf="!actionRoute" 
          mat-flat-button 
          color="primary" 
          class="state-btn"
          (click)="onAction()">
          <span *ngIf="actionIcon" class="material-symbols-outlined btn-icon">{{ actionIcon }}</span>
          <span>{{ actionText }}</span>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .empty-state-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding: 48px 24px;
      background: #ffffff;
      border-radius: var(--radius-lg, 16px);
      border: 1.5px dashed var(--border-medium, #cbd5e1);
      width: 100%;
      box-sizing: border-box;
      transition: all 0.2s ease;

      &.compact {
        padding: 24px 16px;
        .icon-bubble {
          width: 52px;
          height: 52px;
          margin-bottom: 12px;
          .state-icon { font-size: 26px; }
        }
        .state-title { font-size: 1.05rem; }
      }
    }

    .icon-bubble {
      width: 68px;
      height: 68px;
      border-radius: 50%;
      background: var(--primary-50, #eff6ff);
      color: var(--primary-600, #2563eb);
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 18px;
      box-shadow: 0 4px 14px rgba(37, 99, 235, 0.12);
      border: 1px solid #dbeafe;

      .state-icon {
        font-size: 34px;
      }
    }

    .text-group {
      max-width: 440px;
      margin-bottom: 20px;

      .state-title {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--text-primary, #0f172a);
        margin: 0 0 6px 0;
        letter-spacing: -0.02em;
      }

      .state-description {
        font-size: 0.9375rem;
        color: var(--text-muted, #64748b);
        margin: 0;
        line-height: 1.55;
      }
    }

    .action-wrap {
      .state-btn {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        border-radius: var(--radius-md, 10px);
        padding: 0 22px;
        height: 42px;
        box-shadow: 0 2px 6px rgba(37, 99, 235, 0.2);

        .btn-icon {
          font-size: 18px;
        }

        &:hover {
          box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3);
        }
      }
    }

    @media (max-width: 480px) {
      .empty-state-card {
        padding: 32px 16px;
      }
    }
  `]
})
export class EmptyStateComponent {
  @Input() icon: string = 'inbox';
  @Input() title: string = 'No records found';
  @Input() description?: string = 'There is currently no data to display in this view.';
  @Input() actionText?: string;
  @Input() actionRoute?: string;
  @Input() actionIcon?: string = 'add';
  @Input() compact: boolean = false;

  @Output() action = new EventEmitter<void>();

  onAction(): void {
    this.action.emit();
  }
}
