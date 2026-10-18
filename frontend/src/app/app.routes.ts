import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';

export const routes: Routes = [
  // Public Auth Portal within AuthLayout
  {
    path: 'auth',
    component: AuthLayoutComponent,
    children: [
      {
        path: '',
        loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
      }
    ]
  },

  // 403 Access Denied Portal
  {
    path: 'access-denied',
    loadComponent: () => import('./features/auth/access-denied/access-denied.component').then(m => m.AccessDeniedComponent)
  },

  // Authenticated Portal Features within MainLayout (Navbar + Sidebar + Shell)
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      // 1. Customer Feature (Customer or Admin)
      {
        path: 'customer',
        loadChildren: () => import('./features/customer/customer.routes').then(m => m.CUSTOMER_ROUTES),
        canActivate: [roleGuard],
        data: { roles: ['Customer', 'Admin'] }
      },

      // 2. Attendant Feature (ParkingAttendant or Admin)
      {
        path: 'attendant',
        loadChildren: () => import('./features/attendant/attendant.routes').then(m => m.ATTENDANT_ROUTES),
        canActivate: [roleGuard],
        data: { roles: ['ParkingAttendant', 'Attendant', 'Admin'] }
      },

      // 3. Admin Feature (Admin only)
      {
        path: 'admin',
        loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] }
      },

      // 4. Vehicles Feature
      {
        path: 'vehicles',
        loadChildren: () => import('./features/vehicles/vehicles.routes').then(m => m.VEHICLE_ROUTES),
        canActivate: [authGuard]
      },

      // 5. Parking Facility & Slots Feature
      {
        path: 'parking',
        loadChildren: () => import('./features/parking/parking.routes').then(m => m.PARKING_ROUTES),
        canActivate: [authGuard]
      },

      // 6. Reservations Feature
      {
        path: 'reservations',
        loadChildren: () => import('./features/reservations/reservations.routes').then(m => m.RESERVATION_ROUTES),
        canActivate: [authGuard]
      },

      // 7. Payments Feature
      {
        path: 'payments',
        loadChildren: () => import('./features/payments/payments.routes').then(m => m.PAYMENT_ROUTES),
        canActivate: [authGuard]
      },

      // 8. Contactless QR-Code FastPass Feature
      {
        path: 'qr-code',
        loadChildren: () => import('./features/qr-code/qr-code.routes').then(m => m.QR_CODE_ROUTES),
        canActivate: [authGuard]
      },

      // 9. Analytics & Reports Feature (Admin only)
      {
        path: 'reports',
        loadChildren: () => import('./features/reports/reports.routes').then(m => m.REPORT_ROUTES),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] }
      },

      // 10. User Profile Feature
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes').then(m => m.PROFILE_ROUTES),
        canActivate: [authGuard]
      },

      // Default root redirect based on role or to customer dashboard
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'customer/dashboard'
      }
    ]
  },

  // Fallback Wildcard
  {
    path: '**',
    redirectTo: 'auth/login'
  }
];
