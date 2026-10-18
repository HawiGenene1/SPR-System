import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    localStorage.clear();
    routerSpy = jasmine.createSpyObj('Router', ['navigate', 'navigateByUrl']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created and initial state should be unauthenticated', () => {
    expect(service).toBeTruthy();
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.currentUserValue).toBeNull();
  });

  it('should login user and store session', () => {
    const mockAuthResponse = {
      token: 'fake-jwt-token',
      refreshToken: 'fake-refresh-token',
      user: {
        id: 'u-1',
        fullName: 'Admin User',
        email: 'admin@smartparking.com',
        roleName: 'Admin',
        isActive: true
      }
    };

    service.login({ email: 'admin@smartparking.com', password: 'Password123' }).subscribe((res) => {
      expect(res).toBeTruthy();
      expect(service.token).toBe('fake-jwt-token');
      expect(service.currentUserValue?.fullName).toBe('Admin User');
      expect(service.currentUserValue?.role).toBe('Admin');
      expect(service.isAdmin).toBeTrue();
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush(mockAuthResponse);
  });

  it('should route Admin to Admin Dashboard', () => {
    const mockAuthResponse = {
      token: 'fake-jwt-token',
      user: { id: 'u-1', fullName: 'Admin', email: 'admin@test.com', roleName: 'Admin' }
    };

    service.login({ email: 'admin@test.com', password: 'p' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(mockAuthResponse);

    expect(service.getDefaultRouteForRole()).toBe('/admin/dashboard');
  });

  it('should route Customer to Customer Dashboard', () => {
    const mockAuthResponse = {
      token: 'fake-jwt-token',
      user: { id: 'u-2', fullName: 'Customer', email: 'cust@test.com', roleName: 'Customer' }
    };

    service.login({ email: 'cust@test.com', password: 'p' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(mockAuthResponse);

    expect(service.getDefaultRouteForRole()).toBe('/customer/dashboard');
  });

  it('should route ParkingAttendant to Attendant Dashboard', () => {
    const mockAuthResponse = {
      token: 'fake-jwt-token',
      user: { id: 'u-3', fullName: 'Attendant', email: 'att@test.com', roleName: 'ParkingAttendant' }
    };

    service.login({ email: 'att@test.com', password: 'p' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(mockAuthResponse);

    expect(service.getDefaultRouteForRole()).toBe('/attendant/dashboard');
    expect(service.isAttendant).toBeTrue();
  });

  it('should clear authentication state on logout and redirect to login', () => {
    localStorage.setItem('spr_token', 'token-123');
    localStorage.setItem('spr_user', JSON.stringify({ fullName: 'Test' }));

    service.logout();

    expect(service.token).toBeNull();
    expect(service.currentUserValue).toBeNull();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth/login']);
  });
});
