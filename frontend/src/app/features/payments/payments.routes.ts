import { Routes } from '@angular/router';

export const PAYMENT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./payment-history/payment-history.component').then(m => m.PaymentHistoryComponent)
  },
  {
    path: 'checkout',
    loadComponent: () => import('./payment-checkout/payment-checkout.component').then(m => m.PaymentCheckoutComponent)
  },
  {
    path: ':reservationId',
    loadComponent: () => import('./payment-checkout/payment-checkout.component').then(m => m.PaymentCheckoutComponent)
  }
];
