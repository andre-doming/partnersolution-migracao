import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../../core/config/api.config';
import {
  UserDetail,
  UserListRequest,
  UserListResponse,
  UserLookupResponse,
  UserUpsertRequest
} from './users.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_CONFIG.baseUrl}/users`;

  getList(request: UserListRequest): Observable<UserListResponse> {
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (request.field) {
      params = params.set('field', request.field);
    }
    if (request.value) {
      params = params.set('value', request.value);
    }
    if (typeof request.companyId === 'number') {
      params = params.set('companyId', request.companyId);
    }
    if (request.active) {
      params = params.set('active', request.active);
    }

    return this.http.get<UserListResponse>(this.baseUrl, { params });
  }

  getById(id: number): Observable<UserDetail> {
    return this.http.get<UserDetail>(`${this.baseUrl}/${id}`);
  }

  getLookups(): Observable<UserLookupResponse> {
    return this.http.get<UserLookupResponse>(`${this.baseUrl}/lookups`);
  }

  create(payload: UserUpsertRequest): Observable<UserDetail> {
    return this.http.post<UserDetail>(this.baseUrl, payload);
  }

  update(id: number, payload: UserUpsertRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  resetMfa(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/mfa/reset`, {});
  }

  unlockUser(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/mfa/unlock`, {});
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}


