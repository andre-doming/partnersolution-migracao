import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../../core/config/api.config';
import {
  ClientDetail,
  ClientListRequest,
  ClientListResponse,
  ClientLookupResponse,
  ClientUpsertRequest
} from './clients.models';

@Injectable({ providedIn: 'root' })
export class ClientsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_CONFIG.baseUrl}/clients`;

  getList(request: ClientListRequest): Observable<ClientListResponse> {
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

    return this.http.get<ClientListResponse>(this.baseUrl, { params });
  }

  getById(id: number): Observable<ClientDetail> {
    return this.http.get<ClientDetail>(`${this.baseUrl}/${id}`);
  }

  getLookups(): Observable<ClientLookupResponse> {
    return this.http.get<ClientLookupResponse>(`${this.baseUrl}/lookups`);
  }

  create(payload: ClientUpsertRequest): Observable<ClientDetail> {
    return this.http.post<ClientDetail>(this.baseUrl, payload);
  }

  update(id: number, payload: ClientUpsertRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

