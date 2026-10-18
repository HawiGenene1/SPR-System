import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="kpi-card">
      <div class="kpi-icon-wrap" [ngClass]="theme">
        <span class="material-symbols-outlined">{{ icon }}</span>
      </div>
      <div class="kpi-content">
        <div class="kpi-label">{{ label }}</div>
        <div class="kpi-value">{{ value }}</div>
        <div class="kpi-trend" *ngIf="trend" [ngClass]="trendType">
          <span class="material-symbols-outlined" style="font-size: 14px;">
            {{ trendType === 'positive' ? 'trending_up' : (trendType === 'negative' ? 'trending_down' : 'info') }}
          </span>
          <span>{{ trend }}</span>
        </div>
      </div>
    </div>
  `
})
export class StatCardComponent {
  @Input() label: string = '';
  @Input() value: string | number = '';
  @Input() icon: string = 'analytics';
  @Input() theme: 'blue' | 'emerald' | 'amber' | 'rose' | 'purple' | 'cyan' = 'blue';
  @Input() trend?: string;
  @Input() trendType: 'positive' | 'negative' | 'neutral' = 'neutral';
}
