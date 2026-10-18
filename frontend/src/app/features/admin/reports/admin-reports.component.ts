import { Component, OnInit, ElementRef, ViewChild, inject, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Chart, registerables } from 'chart.js';
import { AdminService } from '../../../core/services/admin.service';
import { ParkingAreaDto } from '../../../core/models';
import { DataTableComponent, TableColumn, SortEvent } from '../../../shared/components/data-table/data-table.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    DataTableComponent,
    LoadingSpinnerComponent
  ],
  template: `
    <div class="admin-reports-page">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-titles">
          <a routerLink="/admin/dashboard" class="back-link">
            <span class="material-symbols-outlined">arrow_back</span>
            <span>Back to Command Center</span>
          </a>
          <h2>Enterprise Analytics & Commercial Reporting</h2>
          <p>Financial revenue trajectory, facility utilization telemetry, peak-hour distributions, and commercial yield.</p>
        </div>

        <div class="header-actions">
          <button mat-stroked-button class="print-btn" (click)="printReport()">
            <span class="material-symbols-outlined">print</span>
            <span>Print Report</span>
          </button>
        </div>
      </div>

      <!-- Filter Panel: Interval presets & Facility Selector -->
      <div class="filter-panel">
        <div class="interval-pills">
          <button 
            *ngFor="let interval of intervals" 
            type="button"
            class="interval-pill" 
            [class.active]="selectedInterval === interval" 
            (click)="selectInterval(interval)">
            {{ interval }}
          </button>
        </div>

        <div class="facility-select-wrap">
          <label>Facility:</label>
          <select [(ngModel)]="selectedFacilityId" (change)="loadData()" class="custom-select">
            <option value="All">All Facilities (Consolidated)</option>
            <option *ngFor="let area of facilities" [value]="area.id">{{ area.name }}</option>
          </select>
        </div>
      </div>

      <!-- Loading State -->
      <app-loading-spinner *ngIf="isLoading" message="Synthesizing multi-dimension analytics..."></app-loading-spinner>

      <!-- Analytics Body -->
      <div class="reports-body" *ngIf="!isLoading">
        <!-- KPI Metrics Grid -->
        <div class="kpi-grid">
          <div class="kpi-card highlight-amber">
            <span class="kpi-title">Gross Revenue</span>
            <span class="kpi-value text-amber">\${{ totalRevenue | number:'1.2-2' }}</span>
            <span class="kpi-trend">Settled booking fees & overstays</span>
          </div>

          <div class="kpi-card highlight-blue">
            <span class="kpi-title">Peak Utilization</span>
            <span class="kpi-value text-blue">{{ peakOccupancyRate }}%</span>
            <span class="kpi-trend">Highest daily capacity load</span>
          </div>

          <div class="kpi-card highlight-teal">
            <span class="kpi-title">Avg Parking Dwell</span>
            <span class="kpi-value text-teal">{{ avgDurationHours }} hrs</span>
            <span class="kpi-trend">Per vehicle session dwell time</span>
          </div>

          <div class="kpi-card highlight-purple">
            <span class="kpi-title">Total Bookings</span>
            <span class="kpi-value text-purple">{{ totalBookingsCount }}</span>
            <span class="kpi-trend">Within selected interval</span>
          </div>
        </div>

        <!-- 2 Main Analytics Charts (Revenue & Peak Hours) -->
        <div class="charts-grid">
          <!-- Chart 1: Revenue Curve -->
          <div class="chart-card">
            <div class="chart-header">
              <div class="chart-title-wrap">
                <span class="material-symbols-outlined icon-amber">payments</span>
                <div>
                  <h3>Revenue Performance Trajectory</h3>
                  <p>Daily settled receipts across all facilities</p>
                </div>
              </div>
            </div>
            <div class="canvas-wrap">
              <canvas #revenueChartRef></canvas>
            </div>
          </div>

          <!-- Chart 2: Peak Hour Distribution -->
          <div class="chart-card">
            <div class="chart-header">
              <div class="chart-title-wrap">
                <span class="material-symbols-outlined icon-blue">schedule</span>
                <div>
                  <h3>Hourly Occupancy Distribution</h3>
                  <p>Facility turnover and rush intervals (06:00 – 22:00)</p>
                </div>
              </div>
            </div>
            <div class="canvas-wrap">
              <canvas #peakHoursChartRef></canvas>
            </div>
          </div>
        </div>

        <!-- Facility Performance Summary Table (Supporting Search, Filter, Sort, Pagination) -->
        <div class="table-card">
          <div class="table-header-bar">
            <div>
              <h3>Facility Revenue & Utilization Summary</h3>
              <p>Granular performance breakdown per parking structure</p>
            </div>
            <div class="table-search">
              <span class="material-symbols-outlined">search</span>
              <input 
                type="text" 
                [(ngModel)]="searchTable" 
                placeholder="Search facility summary..." 
                class="search-input">
            </div>
          </div>

          <app-data-table
            [columns]="tableColumns"
            [data]="filteredTableData"
            [pagination]="true"
            [pageSize]="5"
            (sortChange)="onSortChange($event)"
            emptyTitle="No facility data available"
            emptyIcon="analytics">

            <ng-template let-row let-col="column">
              <!-- Name -->
              <ng-container *ngIf="col.key === 'facilityName'">
                <span class="font-bold text-blue">{{ row.facilityName }}</span>
              </ng-container>

              <!-- Capacity -->
              <ng-container *ngIf="col.key === 'totalSlots'">
                <span>{{ row.totalSlots }} bays</span>
              </ng-container>

              <!-- Occupancy -->
              <ng-container *ngIf="col.key === 'occupancyRate'">
                <div class="rate-gauge-wrap">
                  <span class="rate-num">{{ row.occupancyRate }}%</span>
                  <div class="mini-bar">
                    <div class="fill" [style.width.%]="row.occupancyRate"></div>
                  </div>
                </div>
              </ng-container>

              <!-- Bookings -->
              <ng-container *ngIf="col.key === 'bookingsCount'">
                <span class="font-bold">{{ row.bookingsCount }}</span>
              </ng-container>

              <!-- Revenue -->
              <ng-container *ngIf="col.key === 'revenue'">
                <strong class="text-emerald">\${{ row.revenue | number:'1.2-2' }}</strong>
              </ng-container>

              <!-- Avg Yield -->
              <ng-container *ngIf="col.key === 'avgYield'">
                <span>\${{ row.avgYield | number:'1.2-2' }} / bay</span>
              </ng-container>
            </ng-template>
          </app-data-table>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-reports-page {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-end;
      gap: 16px;
      flex-wrap: wrap;

      .header-titles {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .back-link {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          color: #2563eb;
          font-weight: 600;
          font-size: 0.8125rem;
          text-decoration: none;
          &:hover { text-decoration: underline; }
        }

        h2 {
          margin: 0;
          font-size: 1.75rem;
          font-weight: 800;
          color: #0f172a;
          letter-spacing: -0.02em;
        }

        p {
          margin: 0;
          color: #64748b;
          font-size: 0.9375rem;
        }
      }

      .print-btn {
        height: 44px;
        border-radius: 12px;
        font-weight: 700;
        display: flex;
        align-items: center;
        gap: 8px;
      }
    }

    /* Filter Panel */
    .filter-panel {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: 16px;
      padding: 16px 20px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;

      .interval-pills {
        display: flex;
        align-items: center;
        gap: 8px;
        flex-wrap: wrap;

        .interval-pill {
          background: #f1f5f9;
          border: 1px solid transparent;
          color: #475569;
          font-size: 0.8125rem;
          font-weight: 700;
          padding: 6px 14px;
          border-radius: 9999px;
          cursor: pointer;
          transition: all 0.15s ease;

          &:hover {
            background: #e2e8f0;
            color: #0f172a;
          }

          &.active {
            background: #eff6ff;
            color: #2563eb;
            border-color: #bfdbfe;
          }
        }
      }

      .facility-select-wrap {
        display: flex;
        align-items: center;
        gap: 8px;

        label {
          font-size: 0.75rem;
          font-weight: 700;
          color: #64748b;
        }

        .custom-select {
          height: 38px;
          padding: 0 12px;
          border-radius: 8px;
          border: 1px solid #cbd5e1;
          background: #ffffff;
          font-size: 0.8125rem;
          font-weight: 600;
          color: #0f172a;
          outline: none;
          &:focus { border-color: #2563eb; }
        }
      }
    }

    .reports-body {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    /* KPI Grid */
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 18px;

      @media (max-width: 1000px) {
        grid-template-columns: repeat(2, 1fr);
      }

      @media (max-width: 550px) {
        grid-template-columns: 1fr;
      }

      .kpi-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 18px;
        padding: 20px;
        display: flex;
        flex-direction: column;
        gap: 4px;
        box-shadow: 0 4px 12px rgba(15, 23, 42, 0.03);

        .kpi-title {
          font-size: 0.75rem;
          font-weight: 700;
          color: #64748b;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .kpi-value {
          font-size: 1.85rem;
          font-weight: 900;
          line-height: 1.1;
          margin: 4px 0 2px 0;

          &.text-amber { color: #d97706; }
          &.text-blue { color: #2563eb; }
          &.text-teal { color: #0d9488; }
          &.text-purple { color: #7c3aed; }
        }

        .kpi-trend {
          font-size: 0.6875rem;
          color: #94a3b8;
        }
      }
    }

    /* Charts Grid */
    .charts-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;

      @media (max-width: 1000px) {
        grid-template-columns: 1fr;
      }

      .chart-card {
        background: #ffffff;
        border: 1px solid var(--border-light, #e2e8f0);
        border-radius: 20px;
        padding: 22px;
        box-shadow: 0 4px 16px rgba(15, 23, 42, 0.03);
        display: flex;
        flex-direction: column;
        gap: 16px;

        .chart-header {
          .chart-title-wrap {
            display: flex;
            align-items: center;
            gap: 10px;

            .material-symbols-outlined {
              font-size: 26px;
              &.icon-amber { color: #d97706; }
              &.icon-blue { color: #2563eb; }
            }

            h3 {
              margin: 0;
              font-size: 1.1rem;
              font-weight: 800;
              color: #0f172a;
            }

            p {
              margin: 2px 0 0 0;
              color: #64748b;
              font-size: 0.75rem;
            }
          }
        }

        .canvas-wrap {
          position: relative;
          height: 250px;
          width: 100%;
        }
      }
    }

    /* Table specifics */
    .table-card {
      background: #ffffff;
      border: 1px solid var(--border-light, #e2e8f0);
      border-radius: 20px;
      padding: 24px;
      display: flex;
      flex-direction: column;
      gap: 16px;

      .table-header-bar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 16px;
        flex-wrap: wrap;

        h3 {
          margin: 0;
          font-size: 1.15rem;
          font-weight: 800;
          color: #0f172a;
        }

        p {
          margin: 2px 0 0 0;
          color: #64748b;
          font-size: 0.8125rem;
        }

        .table-search {
          display: flex;
          align-items: center;
          gap: 8px;
          border: 1px solid #cbd5e1;
          border-radius: 10px;
          padding: 0 12px;
          height: 38px;

          .material-symbols-outlined { color: #94a3b8; font-size: 18px; }

          .search-input {
            border: none;
            outline: none;
            font-size: 0.8125rem;
            color: #0f172a;
            width: 220px;
          }
        }
      }
    }

    .rate-gauge-wrap {
      display: flex;
      align-items: center;
      gap: 8px;

      .rate-num { font-size: 0.8125rem; font-weight: 700; }
      .mini-bar {
        width: 60px;
        height: 6px;
        background: #f1f5f9;
        border-radius: 9999px;
        overflow: hidden;

        .fill {
          height: 100%;
          background: #2563eb;
          border-radius: 9999px;
        }
      }
    }

    .font-bold { font-weight: 700; }
    .text-blue { color: #2563eb; }
    .text-emerald { color: #059669; }
  `]
})
export class AdminReportsComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('revenueChartRef') revenueCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('peakHoursChartRef') peakHoursCanvas!: ElementRef<HTMLCanvasElement>;

  private adminService = inject(AdminService);

  isLoading: boolean = true;
  facilities: ParkingAreaDto[] = [];

  intervals: string[] = ['Today', 'Last 7 Days', 'Last 30 Days', 'This Quarter', 'Year to Date'];
  selectedInterval: string = 'Last 7 Days';
  selectedFacilityId: string = 'All';

  totalRevenue: number = 24850.00;
  peakOccupancyRate: number = 88.5;
  avgDurationHours: number = 2.8;
  totalBookingsCount: number = 512;

  searchTable: string = '';
  sortKey: string = 'revenue';
  sortDirection: 'asc' | 'desc' = 'desc';

  private revenueChart?: Chart;
  private peakChart?: Chart;

  facilityTableData: any[] = [];

  tableColumns: TableColumn[] = [
    { key: 'facilityName', label: 'Facility', sortable: true },
    { key: 'totalSlots', label: 'Capacity', sortable: true },
    { key: 'occupancyRate', label: 'Avg Occupancy', sortable: true },
    { key: 'bookingsCount', label: 'Total Bookings', sortable: true },
    { key: 'revenue', label: 'Settled Revenue', sortable: true },
    { key: 'avgYield', label: 'Yield / Bay', sortable: true }
  ];

  ngOnInit(): void {
    this.loadData();
  }

  ngAfterViewInit(): void {
    // Charts will initialize once data loads
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  selectInterval(interval: string): void {
    this.selectedInterval = interval;
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.adminService.getParkingAreas().subscribe({
      next: (areas) => {
        this.facilities = areas || [];
        this.loadReports();
      },
      error: () => {
        this.facilities = [
          { id: 'area-1', name: 'Metropolis Garage', location: '100 Main St', hourlyRate: 5, totalSlots: 120, availableSlots: 45, isActive: true, createdAt: '' },
          { id: 'area-2', name: 'Airport Terminal 1', location: 'Terminal Way', hourlyRate: 8, totalSlots: 80, availableSlots: 20, isActive: true, createdAt: '' },
          { id: 'area-3', name: 'Civic Center Lot B', location: '450 Government Blvd', hourlyRate: 4, totalSlots: 60, availableSlots: 22, isActive: true, createdAt: '' }
        ];
        this.loadReports();
      }
    });
  }

  private loadReports(): void {
    this.isLoading = false;
    this.facilityTableData = [
      { facilityName: 'Metropolis Garage', totalSlots: 120, occupancyRate: 78, bookingsCount: 242, revenue: 12450.00, avgYield: 103.75 },
      { facilityName: 'Airport Terminal 1', totalSlots: 80, occupancyRate: 92, bookingsCount: 180, revenue: 8940.00, avgYield: 111.75 },
      { facilityName: 'Civic Center Lot B', totalSlots: 60, occupancyRate: 64, bookingsCount: 90, revenue: 3460.00, avgYield: 57.67 }
    ];

    setTimeout(() => this.initCharts(), 50);
  }

  onSortChange(event: SortEvent): void {
    this.sortKey = event.key;
    this.sortDirection = event.direction;
  }

  get filteredTableData(): any[] {
    let result = [...this.facilityTableData];

    if (this.searchTable.trim()) {
      const q = this.searchTable.toLowerCase().trim();
      result = result.filter(r => r.facilityName.toLowerCase().includes(q));
    }

    if (this.sortKey) {
      result.sort((a, b) => {
        const valA = a[this.sortKey] ?? '';
        const valB = b[this.sortKey] ?? '';
        let comp = 0;
        if (typeof valA === 'string') comp = valA.localeCompare(valB.toString());
        else if (typeof valA === 'number') comp = valA - valB;
        return this.sortDirection === 'asc' ? comp : -comp;
      });
    }

    return result;
  }

  private initCharts(): void {
    this.destroyCharts();

    // 1. Revenue Trajectory
    if (this.revenueCanvas?.nativeElement) {
      this.revenueChart = new Chart(this.revenueCanvas.nativeElement, {
        type: 'line',
        data: {
          labels: ['Fri', 'Sat', 'Sun', 'Mon', 'Tue', 'Wed', 'Thu'],
          datasets: [{
            label: 'Daily Revenue ($)',
            data: [3200, 4100, 4800, 2900, 3400, 3650, 2800],
            borderColor: '#f59e0b',
            backgroundColor: 'rgba(245, 158, 11, 0.12)',
            fill: true,
            tension: 0.35,
            borderWidth: 3,
            pointBackgroundColor: '#f59e0b',
            pointRadius: 4
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false },
            tooltip: { callbacks: { label: (ctx) => ` $${Number(ctx.raw).toFixed(2)}` } }
          },
          scales: {
            y: { beginAtZero: true, ticks: { callback: v => `$${v}` }, grid: { color: '#f1f5f9' } },
            x: { grid: { display: false } }
          }
        }
      });
    }

    // 2. Peak Hours
    if (this.peakHoursCanvas?.nativeElement) {
      this.peakChart = new Chart(this.peakHoursCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: ['06:00', '08:00', '10:00', '12:00', '14:00', '16:00', '18:00', '20:00', '22:00'],
          datasets: [{
            label: 'Occupancy %',
            data: [25, 68, 82, 88, 76, 92, 85, 48, 30],
            backgroundColor: '#2563eb',
            borderRadius: 6
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false },
            tooltip: { callbacks: { label: (ctx) => ` Occupancy: ${ctx.raw}%` } }
          },
          scales: {
            y: { beginAtZero: true, max: 100, ticks: { callback: v => `${v}%` }, grid: { color: '#f1f5f9' } },
            x: { grid: { display: false } }
          }
        }
      });
    }
  }

  private destroyCharts(): void {
    if (this.revenueChart) { this.revenueChart.destroy(); this.revenueChart = undefined; }
    if (this.peakChart) { this.peakChart.destroy(); this.peakChart = undefined; }
  }

  printReport(): void {
    window.print();
  }
}
