import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, UserProfile, UserRole, UserDto } from '../models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'spr_token';
  private readonly REFRESH_TOKEN_KEY = 'spr_refresh_token';
  private readonly USER_KEY = 'spr_user';
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  private currentUserSubject = new BehaviorSubject<UserProfile | null>(this.getStoredUser());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  public get currentUserValue(): UserProfile | null {
    return this.currentUserSubject.value;
  }

  public get token(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  public get refreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  public isAuthenticated(): boolean {
    return !!this.token && !!this.currentUserValue && !this.isTokenExpired();
  }

  public isTokenExpired(): boolean {
    const token = this.token;
    if (!token) return true;

    try {
      const payload = this.decodeTokenPayload(token);
      if (!payload || !payload.exp) return false;
      const expirationDate = new Date(0);
      expirationDate.setUTCSeconds(payload.exp);
      return expirationDate.valueOf() <= new Date().valueOf();
    } catch {
      return false;
    }
  }

  public hasRole(role: string | string[]): boolean {
    const user = this.currentUserValue;
    if (!user) return false;

    const userRoleStr = (user.role || '').toLowerCase();
    const roles = Array.isArray(role) ? role : [role];

    return roles.some(r => {
      const target = r.toLowerCase();
      if (target === 'attendant' || target === 'parkingattendant') {
        return userRoleStr.includes('attendant');
      }
      return userRoleStr === target;
    });
  }

  public get isAdmin(): boolean {
    return this.hasRole('Admin');
  }

  public get isAttendant(): boolean {
    return this.hasRole(['ParkingAttendant', 'Attendant']);
  }

  public get isCustomer(): boolean {
    return this.hasRole('Customer');
  }

  login(credentials: LoginRequest): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/login`, credentials).pipe(
      tap(response => {
        const authData = (response && response.data) ? response.data : response;
        if (authData && authData.token) {
          this.setSession(authData);
        }
      })
    );
  }

  register(data: RegisterRequest): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/register`, data).pipe(
      tap(response => {
        const authData = (response && response.data) ? response.data : response;
        if (authData && authData.token) {
          this.setSession(authData);
        }
      })
    );
  }

  refreshTokenCall(): Observable<any> {
    const token = this.token;
    const refreshToken = this.refreshToken;
    if (!token || !refreshToken) {
      return of(null);
    }

    return this.http.post<any>(`${this.baseUrl}/refresh-token`, { token, refreshToken }).pipe(
      tap(response => {
        const authData = (response && response.data) ? response.data : response;
        if (authData && authData.token) {
          this.setSession(authData);
        }
      })
    );
  }

  getCurrentUser(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.baseUrl}/me`).pipe(
      tap(user => {
        if (user && this.currentUserValue) {
          const updated: UserProfile = {
            ...this.currentUserValue,
            fullName: user.fullName || this.currentUserValue.fullName,
            email: user.email || this.currentUserValue.email,
            phoneNumber: user.phoneNumber || this.currentUserValue.phoneNumber
          };
          localStorage.setItem(this.USER_KEY, JSON.stringify(updated));
          this.currentUserSubject.next(updated);
        }
      })
    );
  }

  logout(): void {
    if (this.token) {
      this.http.post(`${this.baseUrl}/logout`, {}).pipe(
        catchError(() => of(null))
      ).subscribe();
    }
    this.clearAuth();
    this.router.navigate(['/auth/login']);
  }

  clearAuth(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
  }

  public getDefaultRouteForRole(): string {
    const user = this.currentUserValue;
    if (!user) return '/auth/login';

    const r = (user.role || '').toLowerCase();
    if (r.includes('admin')) {
      return '/admin/dashboard';
    } else if (r.includes('attendant')) {
      return '/attendant/dashboard';
    } else {
      return '/customer/dashboard';
    }
  }

  public redirectAfterLogin(returnUrl?: string): void {
    if (returnUrl && !returnUrl.includes('/auth/') && !returnUrl.includes('/access-denied')) {
      this.router.navigateByUrl(returnUrl);
    } else {
      this.router.navigateByUrl(this.getDefaultRouteForRole());
    }
  }

  private setSession(authData: any): void {
    const rawUser = authData.user || {};
    let role: UserRole = 'Customer';
    const r = (rawUser.roleName || rawUser.role || '').toLowerCase();
    if (r.includes('admin')) {
      role = 'Admin';
    } else if (r.includes('attendant')) {
      role = 'ParkingAttendant';
    } else {
      role = 'Customer';
    }

    const user: UserProfile = {
      id: rawUser.id || '',
      fullName: rawUser.fullName || '',
      email: rawUser.email || '',
      phoneNumber: rawUser.phoneNumber,
      role,
      createdAt: rawUser.createdAt || new Date().toISOString(),
      isActive: rawUser.isActive ?? true
    };

    localStorage.setItem(this.TOKEN_KEY, authData.token);
    if (authData.refreshToken) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, authData.refreshToken);
    }
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  private getStoredUser(): UserProfile | null {
    try {
      const userStr = localStorage.getItem(this.USER_KEY);
      return userStr ? JSON.parse(userStr) : null;
    } catch {
      return null;
    }
  }

  private decodeTokenPayload(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = parts[1];
      const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch {
      return null;
    }
  }
}
