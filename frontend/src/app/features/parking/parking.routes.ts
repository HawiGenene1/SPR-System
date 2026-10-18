import { Routes } from '@angular/router';

export const PARKING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./parking-overview/parking-overview.component').then(m => m.ParkingOverviewComponent)
  }
];
