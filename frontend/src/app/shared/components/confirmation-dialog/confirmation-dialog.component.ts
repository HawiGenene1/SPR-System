import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface ConfirmationDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'danger' | 'warning' | 'info' | 'primary';
  isDanger?: boolean;
}

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <div class="confirmation-dialog-root" [ngClass]="dialogType">
      <div class="dialog-icon-badge">
        <span class="material-symbols-outlined">{{ iconName }}</span>
      </div>

      <div class="dialog-text-wrap">
        <h3 mat-dialog-title class="dialog-title">{{ data.title }}</h3>
        <div mat-dialog-content class="dialog-body">
          <p>{{ data.message }}</p>
        </div>
      </div>

      <div mat-dialog-actions align="end" class="dialog-actions">
        <button mat-button (click)="onCancel()" class="btn-cancel">
          {{ data.cancelText || 'Cancel' }}
        </button>
        <button 
          mat-flat-button 
          [color]="dialogType === 'danger' ? 'warn' : 'primary'"
          (click)="onConfirm()"
          class="btn-confirm">
          {{ data.confirmText || 'Confirm' }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .confirmation-dialog-root {
      padding: 16px;
      min-width: 360px;
      max-width: 440px;

      &.danger {
        .dialog-icon-badge {
          background: #fef2f2;
          color: #dc2626;
          border-color: #fecaca;
        }
      }
      &.warning {
        .dialog-icon-badge {
          background: #fffbeb;
          color: #d97706;
          border-color: #fde68a;
        }
      }
      &.info, &.primary {
        .dialog-icon-badge {
          background: #eff6ff;
          color: #2563eb;
          border-color: #bfdbfe;
        }
      }
    }

    .dialog-icon-badge {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      border: 1px solid transparent;
      margin-bottom: 16px;

      .material-symbols-outlined {
        font-size: 26px;
      }
    }

    .dialog-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0 0 8px 0;
      padding: 0;
      line-height: 1.3;
    }

    .dialog-body {
      padding: 0;
      p {
        margin: 0;
        color: #475569;
        font-size: 0.9375rem;
        line-height: 1.5;
      }
    }

    .dialog-actions {
      margin-top: 24px;
      padding: 0;
      gap: 10px;

      .btn-cancel {
        color: #64748b;
        font-weight: 600;
      }

      .btn-confirm {
        font-weight: 600;
        border-radius: 8px;
        padding: 0 20px;
      }
    }
  `]
})
export class ConfirmationDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConfirmationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmationDialogData
  ) {}

  get dialogType(): string {
    if (this.data.isDanger) return 'danger';
    return this.data.type || 'primary';
  }

  get iconName(): string {
    switch (this.dialogType) {
      case 'danger':
        return 'warning';
      case 'warning':
        return 'report_problem';
      case 'info':
        return 'info';
      default:
        return 'help_outline';
    }
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
