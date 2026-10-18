import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="status-pill" [ngClass]="badgeClass" [class.with-icon]="!!icon">
      <span *ngIf="icon" class="material-symbols-outlined badge-icon">{{ icon }}</span>
      <span *ngIf="!icon" class="dot"></span>
      <span class="label">{{ label || status }}</span>
    </span>
  `,
  styles: [`
    .status-pill {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 3px 10px;
      border-radius: var(--radius-full, 9999px);
      font-size: 0.75rem;
      font-weight: 600;
      letter-spacing: 0.01em;
      white-space: nowrap;
      transition: all 0.2s ease;
      border: 1px solid transparent;

      .dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
      }

      .badge-icon {
        font-size: 14px;
        line-height: 1;
      }

      /* State variants */
      &.available {
        background: #ecfdf5;
        color: #065f46;
        border-color: #a7f3d0;
        .dot { background: #10b981; }
      }

      &.reserved {
        background: #fffbeb;
        color: #92400e;
        border-color: #fde68a;
        .dot { background: #f59e0b; }
      }

      &.occupied {
        background: #fef2f2;
        color: #991b1b;
        border-color: #fecaca;
        .dot { background: #ef4444; }
      }

      &.maintenance {
        background: #f1f5f9;
        color: #475569;
        border-color: #cbd5e1;
        .dot { background: #64748b; }
      }

      &.info {
        background: #f0f9ff;
        color: #075985;
        border-color: #bae6fd;
        .dot { background: #0284c7; }
      }

      &.purple {
        background: #faf5ff;
        color: #6b21a8;
        border-color: #e9d5ff;
        .dot { background: #a855f7; }
      }
    }
  `]
})
export class StatusBadgeComponent {
  @Input() status: string = 'Unknown';
  @Input() label?: string;
  @Input() icon?: string;

  get badgeClass(): string {
    const s = (this.status || '').toLowerCase().trim();
    if (['available', 'active', 'completed', 'paid', 'confirmed', 'success', 'parked'].includes(s)) {
      return 'available';
    }
    if (['reserved', 'pending', 'in-progress', 'warning'].includes(s)) {
      return 'reserved';
    }
    if (['occupied', 'failed', 'cancelled', 'danger', 'overstay', 'expired'].includes(s)) {
      return 'occupied';
    }
    if (['maintenance', 'inactive', 'disabled', 'neutral', 'closed'].includes(s)) {
      return 'maintenance';
    }
    if (['ev', 'info', 'standard'].includes(s)) {
      return 'info';
    }
    if (['vip'].includes(s)) {
      return 'purple';
    }
    return 'maintenance';
  }
}
