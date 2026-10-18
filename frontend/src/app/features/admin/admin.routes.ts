import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
  },
  {
    path: 'users',
    loadComponent: () => import('./users/manage-users.component').then(m => m.ManageUsersComponent)
  },
  {
    path: 'parking-areas',
    loadComponent: () => import('./areas/manage-areas.component').then(m => m.AdminAreasComponent)
  },
  {
    path: 'parking-slots',
    loadComponent: () => import('./slots/manage-slots.component').then(m => m.ManageSlotsComponent)
  },
  {
    path: 'reservations',
    loadComponent: () => import('./reservations/manage-reservations.component').then(m => m.AdminReservationsComponent)
  },
  {
    path: 'payments',
    loadComponent: () => import('./payments/manage-payments.component').then(m => m.ManagePaymentsComponent)
  },
  {
    path: 'reports',
    loadComponent: () => import('./reports/admin-reports.component').then(m => m.AdminReportsComponent)
  },
  {
    path: 'settings',
    loadComponent: () => import('./settings/admin-settings.component').then(m => m.AdminSettingsComponent)
  },
  // Legacy / convenience route aliases
  {
    path: 'areas',
    redirectTo: 'parking-areas',
    pathMatch: 'full'
  },
  {
    path: 'slots',
    redirectTo: 'parking-slots',
    pathMatch: 'full'
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  }
];
