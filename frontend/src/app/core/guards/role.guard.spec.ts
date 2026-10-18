import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { roleGuard } from './role.guard';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';
import { UserProfile } from '../models';

describe('roleGuard', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let notifySpy: jasmine.SpyObj<NotificationService>;

  const mockUser: UserProfile = {
    id: 'u-1',
    fullName: 'Test User',
    email: 'test@example.com',
    role: 'Customer',
    createdAt: '2026-01-01',
    isActive: true
  };

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'hasRole'], {
      currentUserValue: mockUser
    });
    routerSpy = jasmine.createSpyObj('Router', ['createUrlTree']);
    notifySpy = jasmine.createSpyObj('NotificationService', ['warning']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: NotificationService, useValue: notifySpy }
      ]
    });
  });

  it('should allow access if user has required role', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.hasRole.and.returnValue(true);

    const route = { data: { roles: ['Customer'] } } as unknown as ActivatedRouteSnapshot;
    const state = { url: '/customer/dashboard' } as RouterStateSnapshot;

    const result = TestBed.runInInjectionContext(() => roleGuard(route, state));

    expect(result).toBeTrue();
  });

  it('should redirect to access-denied if user role does not match', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.hasRole.and.returnValue(false);
    const mockUrlTree = {} as UrlTree;
    routerSpy.createUrlTree.and.returnValue(mockUrlTree);

    const route = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;
    const state = { url: '/admin/dashboard' } as RouterStateSnapshot;

    const result = TestBed.runInInjectionContext(() => roleGuard(route, state));

    expect(notifySpy.warning).toHaveBeenCalled();
    expect(routerSpy.createUrlTree).toHaveBeenCalledWith(['/access-denied'], {
      queryParams: {
        required: 'Admin',
        current: 'Customer',
        returnUrl: '/admin/dashboard'
      }
    });
    expect(result).toBe(mockUrlTree);
  });
});
