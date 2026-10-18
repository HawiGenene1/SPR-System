import { Routes } from '@angular/router';

export const ATTENDANT_ROUTES: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/attendant-dashboard.component').then(m => m.AttendantDashboardComponent)
  },
  {
    path: 'scanner',
    loadComponent: () => import('./scanner/attendant-scanner.component').then(m => m.AttendantScannerComponent)
  },
  {
    path: 'entries',
    loadComponent: () => import('./entries/attendant-entries.component').then(m => m.AttendantEntriesComponent)
  },
  {
    path: 'exits',
    loadComponent: () => import('./exits/attendant-exits.component').then(m => m.AttendantExitsComponent)
  },
  {
    path: 'parking-status',
    loadComponent: () => import('./parking-status/attendant-parking-status.component').then(m => m.AttendantParkingStatusComponent)
  },
  // Legacy / convenience route aliases
  {
    path: 'gate',
    redirectTo: 'scanner',
    pathMatch: 'full'
  },
  {
    path: 'history',
    redirectTo: 'entries',
    pathMatch: 'full'
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  }
];
