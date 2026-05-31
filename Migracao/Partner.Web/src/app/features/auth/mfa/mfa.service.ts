import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../../../core/config/api.config';
import { MfaActivateResponse, MfaSetupResponse, MfaVerifyRequest, MfaVerifyResponse } from './mfa.models';

@Injectable({ providedIn: 'root' })
export class MfaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_CONFIG.baseUrl}/auth/mfa`;

  setup(): Observable<MfaSetupResponse> {
    return this.http.post<MfaSetupResponse>(`${this.baseUrl}/setup`, {});
  }

  activate(totpCode: string): Observable<MfaActivateResponse> {
    return this.http.post<MfaActivateResponse>(`${this.baseUrl}/activate`, { totpCode });
  }

  verify(payload: MfaVerifyRequest): Observable<MfaVerifyResponse> {
    return this.http.post<MfaVerifyResponse>(`${this.baseUrl}/verify`, payload);
  }
}
