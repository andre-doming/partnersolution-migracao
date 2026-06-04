import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../../core/config/api.config';
import {
  ImportClientsCsvResponse,
  ImportPreviewCsvResponse,
  ImportProcessSelectedRequest,
  ImportJobDetail,
  ImportJobListRequest,
  ImportJobListResponse,
  ImportLookupResponse,
  ImportJobErrorsResponse,
  ImportNotification
} from './import.models';

@Injectable({ providedIn: 'root' })
export class ImportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_CONFIG.baseUrl}/import`;

  getLookups(): Observable<ImportLookupResponse> {
    return this.http.get<ImportLookupResponse>(`${this.baseUrl}/lookups`);
  }

  uploadClientsCsv(file: File, companyId: number): Observable<ImportClientsCsvResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('companyId', String(companyId));

    return this.http.post<ImportClientsCsvResponse>(`${this.baseUrl}/clients-csv`, formData);
  }

  previewClientsCsv(file: File, companyId: number): Observable<ImportPreviewCsvResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('companyId', String(companyId));

    return this.http.post<ImportPreviewCsvResponse>(`${this.baseUrl}/clients-csv/preview`, formData);
  }

  processSelectedClients(request: ImportProcessSelectedRequest): Observable<ImportClientsCsvResponse> {
    return this.http.post<ImportClientsCsvResponse>(`${this.baseUrl}/clients-csv/process-selected`, request);
  }

  getJobs(request: ImportJobListRequest): Observable<ImportJobListResponse> {
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (request.status) {
      params = params.set('status', request.status);
    }

    if (request.startDateUtc) {
      params = params.set('startDateUtc', request.startDateUtc);
    }

    if (request.endDateUtc) {
      params = params.set('endDateUtc', request.endDateUtc);
    }

    if (request.userId) {
      params = params.set('userId', request.userId);
    }

    return this.http.get<ImportJobListResponse>(`${this.baseUrl}/jobs`, { params });
  }

  getJobByPublicId(jobPublicId: string): Observable<ImportJobDetail> {
    return this.http.get<ImportJobDetail>(`${this.baseUrl}/jobs/${jobPublicId}`);
  }

  getJobErrors(jobPublicId: string, page: number, pageSize: number): Observable<ImportJobErrorsResponse> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ImportJobErrorsResponse>(`${this.baseUrl}/jobs/${jobPublicId}/errors`, { params });
  }

  getNotifications(): Observable<ImportNotification[]> {
    return this.http.get<ImportNotification[]>(`${this.baseUrl}/notifications`);
  }

  markNotificationRead(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/notifications/${id}/read`, {});
  }

  markAllNotificationsRead(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/notifications/read-all`, {});
  }

  exportJobErrors(jobPublicId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/jobs/${jobPublicId}/errors/export`, {
      responseType: 'blob'
    });
  }
}



