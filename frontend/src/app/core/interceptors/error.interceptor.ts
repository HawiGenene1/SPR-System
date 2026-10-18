import { HttpInterceptorFn, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

/**
 * Normalizes API response bodies so components expecting:
 * - Direct array/object (from ASP.NET Core controllers)
 * - Wrapped ApiResponse<T> ({ success: true, data: T })
 * - PagedResultDto<T> ({ items: T[], totalCount: number })
 * can seamlessly access both `.data`, `.items`, and array/object indexes.
 */
function normalizeResponseBody(body: any): any {
  if (body === null || body === undefined || typeof body !== 'object') {
    return body;
  }

  // If already an array, attach .data and .items to the array instance
  if (Array.isArray(body)) {
    try {
      (body as any).data = body;
      (body as any).items = body;
      (body as any).success = true;
    } catch {
      // In case body is sealed/frozen
    }
    return body;
  }

  // If ASP.NET Core PagedResultDto { items: [...], totalCount: ... }
  if ('items' in body && Array.isArray(body.items)) {
    try {
      if (!('data' in body)) {
        (body as any).data = body.items;
      }
      (body as any).success = true;
    } catch {}
    return body;
  }

  // If standard object without .data, attach .data reference to itself
  if (!('data' in body)) {
    try {
      (body as any).data = body;
      (body as any).success = true;
    } catch {}
  }

  return body;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const notify = inject(NotificationService);

  return next(req).pipe(
    map(event => {
      if (event instanceof HttpResponse) {
        const body = event.body as any;
        // Handle 201 Created
        if (event.status === 201) {
          const createdMsg = body?.message || 'Resource created successfully.';
          if (typeof notify.success === 'function') {
            notify.success(createdMsg);
          }
        }
        // Handle 200 OK for mutations (POST, PUT, DELETE) that explicitly return a message
        else if (event.status === 200 && (req.method === 'POST' || req.method === 'PUT' || req.method === 'DELETE')) {
          if (body?.message && !req.url.includes('/auth/login') && !req.url.includes('/auth/refresh-token')) {
            if (typeof notify.success === 'function') {
              notify.success(body.message);
            }
          }
        }

        // Normalize response body
        if (event.body !== null && typeof event.body === 'object') {
          const normalized = normalizeResponseBody(event.body);
          return event.clone({ body: normalized });
        }
      }
      return event;
    }),
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unexpected server error occurred. Please try again.';

      // Parse ASP.NET Core ValidationProblemDetails or error objects
      if (error.error) {
        if (typeof error.error === 'string') {
          errorMessage = error.error;
        } else if (error.error.errors) {
          if (Array.isArray(error.error.errors)) {
            errorMessage = error.error.errors.join(' | ');
          } else if (typeof error.error.errors === 'object') {
            // ASP.NET Core ModelState dictionary: { [field: string]: string[] }
            const errorList: string[] = [];
            for (const key of Object.keys(error.error.errors)) {
              const fieldErrors = error.error.errors[key];
              if (Array.isArray(fieldErrors)) {
                errorList.push(...fieldErrors);
              } else if (typeof fieldErrors === 'string') {
                errorList.push(fieldErrors);
              }
            }
            if (errorList.length > 0) {
              errorMessage = errorList.join(' | ');
            }
          }
        } else if (error.error.message) {
          errorMessage = error.error.message;
        } else if (error.error.title) {
          errorMessage = error.error.title;
        }
      }

      // Handle specific HTTP status codes
      switch (error.status) {
        case 400: // Bad Request
          notify.error(errorMessage || 'Bad Request: Please check the submitted information and try again.');
          break;

        case 401: // Unauthorized
          if (!req.url.includes('/auth/login') && !req.url.includes('/auth/register')) {
            notify.error('Your session has expired. Please sign in again.');
            authService.clearAuth();
            router.navigate(['/auth/login'], {
              queryParams: { returnUrl: router.url }
            });
          } else {
            notify.error(errorMessage || 'Invalid email or password.');
          }
          break;

        case 403: // Forbidden
          notify.warning('Access Denied: You do not have permission to perform this action.');
          router.navigate(['/access-denied'], {
            queryParams: { returnUrl: router.url }
          });
          break;

        case 404: // Not Found
          notify.warning(errorMessage || 'Requested resource was not found.');
          break;

        case 409: // Conflict
          notify.error(errorMessage || 'Conflict detected: The parking slot or record is already reserved or modified. Please refresh and try again.');
          break;

        case 500: // Internal Server Error
          notify.error('Internal server error: A temporary server failure occurred. Please try again later.');
          break;

        default:
          if (error.status >= 500) {
            notify.error('Server error: A server failure occurred. Please try again later.');
          } else {
            notify.error(errorMessage || 'An error occurred while processing your request.');
          }
          break;
      }

      return throwError(() => error);
    })
  );
};
