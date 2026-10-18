import { Routes } from '@angular/router';

export const CUSTOMER_ROUTES: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/customer-dashboard.component').then(m => m.CustomerDashboardComponent)
  },
  {
    path: 'parking',
    loadComponent: () => import('./parking/customer-parking.component').then(m => m.CustomerParkingComponent)
  },
  {
    path: 'reservations',
    loadComponent: () => import('./reservations/customer-reservations.component').then(m => m.CustomerReservationsComponent)
  },
  {
    path: 'vehicles',
    loadComponent: () => import('./vehicles/customer-vehicles.component').then(m => m.CustomerVehiclesComponent)
  },
  {
    path: 'payments',
    loadComponent: () => import('./payments/customer-payments.component').then(m => m.CustomerPaymentsComponent)
  },
  {
    path: 'payments/checkout',
    loadComponent: () => import('../payments/payment-checkout/payment-checkout.component').then(m => m.PaymentCheckoutComponent)
  },
  {
    path: 'payments/:reservationId',
    loadComponent: () => import('../payments/payment-checkout/payment-checkout.component').then(m => m.PaymentCheckoutComponent)
  },
  {
    path: 'history',
    loadComponent: () => import('./history/customer-history.component').then(m => m.CustomerHistoryComponent)
  },
  {
    path: 'profile',
    loadComponent: () => import('./profile/customer-profile.component').then(m => m.CustomerProfileComponent)
  },
  // Convenience alias:
  {
    path: 'book',
    redirectTo: 'parking',
    pathMatch: 'full'
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  }
];
