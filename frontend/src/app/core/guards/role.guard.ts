import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';
import { UserRole } from '../models';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const notify = inject(NotificationService);

  const allowedRoles = route.data?.['roles'] as (UserRole | string)[] | undefined;
  const currentUser = authService.currentUserValue;

  // 1. Must be authenticated first
  if (!authService.isAuthenticated() || !currentUser) {
    return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
  }

  // 2. If no role restriction, allow
  if (!allowedRoles || allowedRoles.length === 0) {
    return true;
  }

  // 3. Check role authorization
  if (authService.hasRole(allowedRoles as string[])) {
    return true;
  }

  // 4. Unauthorized role: redirect to access-denied page & alert
  notify.warning(`Access Denied: Page requires role [${allowedRoles.join(' or ')}]. Your current role is [${currentUser.role}].`);
  
  return router.createUrlTree(['/access-denied'], {
    queryParams: {
      required: allowedRoles.join(','),
      current: currentUser.role,
      returnUrl: state.url
    }
  });
};
