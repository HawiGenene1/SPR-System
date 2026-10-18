import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

describe('errorInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let notifySpy: jasmine.SpyObj<NotificationService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['clearAuth']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate'], { url: '/customer/dashboard' });
    notifySpy = jasmine.createSpyObj('NotificationService', ['error', 'warning', 'success', 'info']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: NotificationService, useValue: notifySpy }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should show success notification and normalize response on 201 Created', () => {
    httpClient.post('/api/vehicles', { licensePlate: 'ABC-123' }).subscribe({
      next: (res: any) => {
        expect(res).toBeDefined();
        expect(res.data).toBeDefined();
      }
    });

    const req = httpMock.expectOne('/api/vehicles');
    req.flush({ id: 'veh-1', licensePlate: 'ABC-123' }, { status: 201, statusText: 'Created' });

    expect(notifySpy.success).toHaveBeenCalled();
  });

  it('should handle 400 Bad Request with ModelState errors', () => {
    httpClient.post('/api/reservations', {}).subscribe({
      next: () => fail('should have failed with 400 error'),
      error: (error) => {
        expect(error.status).toBe(400);
      }
    });

    const req = httpMock.expectOne('/api/reservations');
    req.flush({ errors: { SlotId: ['SlotId is required'] } }, { status: 400, statusText: 'Bad Request' });

    expect(notifySpy.error).toHaveBeenCalledWith(jasmine.stringMatching(/SlotId is required/i));
  });

  it('should clear authentication state and redirect to login on 401 Unauthorized', () => {
    httpClient.get('/api/parking-areas').subscribe({
      next: () => fail('should have failed with 401 error'),
      error: (error) => {
        expect(error.status).toBe(401);
      }
    });

    const req = httpMock.expectOne('/api/parking-areas');
    req.flush('Unauthorized token', { status: 401, statusText: 'Unauthorized' });

    expect(authServiceSpy.clearAuth).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth/login'], {
      queryParams: { returnUrl: '/customer/dashboard' }
    });
    expect(notifySpy.error).toHaveBeenCalled();
  });

  it('should redirect to access-denied and show notification on 403 Forbidden', () => {
    httpClient.get('/api/reports/revenue').subscribe({
      next: () => fail('should have failed with 403 error'),
      error: (error) => {
        expect(error.status).toBe(403);
      }
    });

    const req = httpMock.expectOne('/api/reports/revenue');
    req.flush('Forbidden role', { status: 403, statusText: 'Forbidden' });

    expect(notifySpy.warning).toHaveBeenCalledWith(jasmine.stringMatching(/Access Denied/i));
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/access-denied'], {
      queryParams: { returnUrl: '/customer/dashboard' }
    });
  });

  it('should show warning notification on 404 Not Found', () => {
    httpClient.get('/api/parking-slots/non-existent-id').subscribe({
      next: () => fail('should have failed with 404 error'),
      error: (error) => {
        expect(error.status).toBe(404);
      }
    });

    const req = httpMock.expectOne('/api/parking-slots/non-existent-id');
    req.flush({ message: 'Slot not found' }, { status: 404, statusText: 'Not Found' });

    expect(notifySpy.warning).toHaveBeenCalledWith(jasmine.stringMatching(/Slot not found|resource was not found/i));
  });

  it('should show error notification on 409 Conflict', () => {
    httpClient.post('/api/reservations', { slotId: 'slot-1' }).subscribe({
      next: () => fail('should have failed with 409 error'),
      error: (error) => {
        expect(error.status).toBe(409);
      }
    });

    const req = httpMock.expectOne('/api/reservations');
    req.flush({ message: 'Slot is already booked' }, { status: 409, statusText: 'Conflict' });

    expect(notifySpy.error).toHaveBeenCalledWith(jasmine.stringMatching(/already booked|conflict/i));
  });

  it('should show generic error notification on 500 Internal Server Error', () => {
    httpClient.get('/api/dashboard/admin').subscribe({
      next: () => fail('should have failed with 500 error'),
      error: (error) => {
        expect(error.status).toBe(500);
      }
    });

    const req = httpMock.expectOne('/api/dashboard/admin');
    req.flush({ message: 'Database crash' }, { status: 500, statusText: 'Internal Server Error' });

    expect(notifySpy.error).toHaveBeenCalledWith(jasmine.stringMatching(/Internal server error/i));
  });
});
