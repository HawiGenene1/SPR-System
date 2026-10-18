import { Routes } from '@angular/router';

export const QR_CODE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./qr-view/qr-view.component').then(m => m.QrViewComponent)
  },
  {
    path: 'scanner',
    loadComponent: () => import('./qr-scanner/qr-scanner.component').then(m => m.QrScannerComponent)
  }
];
