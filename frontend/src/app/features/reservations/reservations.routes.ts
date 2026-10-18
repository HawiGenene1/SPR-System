import { Routes } from '@angular/router';

export const RESERVATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./reservation-list/reservation-list.component').then(m => m.ReservationListComponent)
  },
  {
    path: 'create',
    loadComponent: () => import('./reservation-create/reservation-create.component').then(m => m.ReservationCreateComponent)
  }
];
