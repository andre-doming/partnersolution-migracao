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
  ImportLookupResponse
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
    const params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    return this.http.get<ImportJobListResponse>(`${this.baseUrl}/jobs`, { params });
  }

  getJobById(id: number): Observable<ImportJobDetail> {
    return this.http.get<ImportJobDetail>(`${this.baseUrl}/jobs/${id}`);
  }
}

