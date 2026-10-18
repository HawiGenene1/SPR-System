import { Routes } from '@angular/router';

export const VEHICLE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./vehicles-list/vehicles-list.component').then(m => m.VehiclesListComponent)
  }
];
