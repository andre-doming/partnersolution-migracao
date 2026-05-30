import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../../core/config/api.config';
import {
  CompanyDetail,
  CompanyListRequest,
  CompanyListResponse,
  CompanyUpsertRequest
} from './companies.models';

@Injectable({ providedIn: 'root' })
export class CompaniesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_CONFIG.baseUrl}/companies`;

  getList(request: CompanyListRequest): Observable<CompanyListResponse> {
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (request.field) {
      params = params.set('field', request.field);
    }

    if (request.value) {
      params = params.set('value', request.value);
    }

    if (request.active) {
      params = params.set('active', request.active);
    }

    return this.http.get<CompanyListResponse>(this.baseUrl, { params });
  }

  getById(id: number): Observable<CompanyDetail> {
    return this.http.get<CompanyDetail>(`${this.baseUrl}/${id}`);
  }

  create(payload: CompanyUpsertRequest): Observable<CompanyDetail> {
    return this.http.post<CompanyDetail>(this.baseUrl, payload);
  }

  update(id: number, payload: CompanyUpsertRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

