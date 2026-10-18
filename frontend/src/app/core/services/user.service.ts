import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserDto, UpdateUserRequest, PagedResultDto } from '../models';

export interface UserFilterParams {
  search?: string;
  roleName?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly baseUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getUsers(filter?: UserFilterParams): Observable<PagedResultDto<UserDto>> {
    let params = new HttpParams();
    if (filter) {
      if (filter.search) params = params.set('search', filter.search);
      if (filter.roleName) params = params.set('roleName', filter.roleName);
      if (filter.isActive !== undefined && filter.isActive !== null) params = params.set('isActive', filter.isActive.toString());
      if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
      if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    }

    return this.http.get<PagedResultDto<UserDto>>(this.baseUrl, { params });
  }

  getUserById(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.baseUrl}/${id}`);
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.baseUrl}/${id}`, request);
  }

  setUserStatus(id: string, isActive: boolean): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.baseUrl}/${id}/status`, isActive);
  }

  deleteUser(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${id}`);
  }
}
