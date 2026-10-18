import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  isDanger?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <div class="confirm-modal-root" [class.is-danger]="data.isDanger">
      <!-- Icon Pill -->
      <div class="modal-icon-badge">
        <span class="material-symbols-outlined icon">
          {{ data.isDanger ? 'warning' : 'help' }}
        </span>
      </div>

      <!-- Text Header & Content -->
      <div class="modal-text-content">
        <h3 mat-dialog-title class="modal-title">{{ data.title }}</h3>
        <div mat-dialog-content class="modal-body">
          <p>{{ data.message }}</p>
        </div>
      </div>

      <!-- Action Buttons -->
      <div mat-dialog-actions align="end" class="modal-actions">
        <button 
          type="button" 
          mat-stroked-button 
          (click)="onCancel()" 
          class="btn-cancel">
          {{ data.cancelText || 'Cancel' }}
        </button>
        <button 
          type="button" 
          mat-flat-button 
          [color]="data.isDanger ? 'warn' : 'primary'"
          (click)="onConfirm()"
          class="btn-confirm"
          [class.btn-danger]="data.isDanger">
          <span class="material-symbols-outlined btn-icon" *ngIf="data.isDanger">delete_forever</span>
          <span>{{ data.confirmText || 'Confirm' }}</span>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .confirm-modal-root {
      padding: 24px;
      min-width: 380px;
      max-width: 460px;
      box-sizing: border-box;

      .modal-icon-badge {
        width: 52px;
        height: 52px;
        border-radius: 14px;
        background: #eff6ff;
        color: #2563eb;
        display: flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 18px;
        border: 1px solid #dbeafe;

        .icon {
          font-size: 28px;
        }
      }

      &.is-danger {
        .modal-icon-badge {
          background: #fef2f2;
          color: #dc2626;
          border-color: #fecaca;
        }
      }

      .modal-title {
        margin: 0 0 8px 0;
        font-size: 1.25rem;
        font-weight: 700;
        color: #0f172a;
        line-height: 1.3;
        padding: 0;
      }

      .modal-body {
        padding: 0;
        p {
          margin: 0;
          color: #475569;
          font-size: 0.9375rem;
          line-height: 1.55;
        }
      }

      .modal-actions {
        margin-top: 24px;
        padding: 0;
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: 12px;

        .btn-cancel {
          border-radius: 8px;
          font-weight: 600;
          color: #64748b;
          border-color: #cbd5e1;
        }

        .btn-confirm {
          border-radius: 8px;
          font-weight: 600;
          padding: 0 20px;
          height: 40px;
          display: inline-flex;
          align-items: center;
          gap: 6px;

          &.btn-danger {
            background-color: #dc2626 !important;
            color: #ffffff !important;
            &:hover {
              background-color: #b91c1c !important;
            }
          }

          .btn-icon {
            font-size: 18px;
          }
        }
      }
    }

    @media (max-width: 480px) {
      .confirm-modal-root {
        min-width: 100%;
        padding: 18px;

        .modal-actions {
          flex-direction: column-reverse;
          width: 100%;
          gap: 8px;

          button {
            width: 100%;
            justify-content: center;
          }
        }
      }
    }
  `]
})
export class ConfirmDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConfirmDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogData
  ) {}

  onConfirm(): void {
    this.dialogRef.close(true);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
