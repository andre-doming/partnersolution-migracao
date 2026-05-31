import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import { LoginResponse, MfaVerifyResponse } from '../../features/auth/mfa/mfa.models';

export interface JwtClaims {
  userId?: string;
  login?: string;
  name?: string;
  admin?: string;
  permissions?: string[] | string;
  companies?: string[] | string;
  exp?: number;
}

export interface LoginPayload {
  login: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorageKey = 'partner.token';
  private readonly userNameStorageKey = 'partner.userName';
  private readonly pendingTokenStorageKey = 'partner.mfa.pendingToken';

  login(payload: LoginPayload): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_CONFIG.baseUrl}/auth/login`, payload)
      .pipe(tap((response) => this.handleLoginResponse(response)));
  }

  completeLogin(response: MfaVerifyResponse): void {
    localStorage.setItem(this.tokenStorageKey, response.accessToken);
    if (response.name) {
      localStorage.setItem(this.userNameStorageKey, response.name);
    }
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.userNameStorageKey);
    localStorage.removeItem(this.pendingTokenStorageKey);
  }

  setPendingToken(token: string): void {
    localStorage.setItem(this.pendingTokenStorageKey, token);
  }

  getPendingToken(): string | null {
    return localStorage.getItem(this.pendingTokenStorageKey);
  }

  clearPendingToken(): void {
    localStorage.removeItem(this.pendingTokenStorageKey);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    const claims = this.getClaims();
    if (!claims) {
      this.logout();
      return false;
    }

    const nowInSeconds = Math.floor(Date.now() / 1000);
    if (typeof claims.exp === 'number' && claims.exp <= nowInSeconds) {
      this.logout();
      return false;
    }

    return true;
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenStorageKey);
  }

  getUserName(): string {
    const fromStorage = localStorage.getItem(this.userNameStorageKey);
    if (fromStorage) {
      return fromStorage;
    }

    const fromToken = this.getClaims()?.name;
    return fromToken && fromToken.trim().length > 0 ? fromToken : 'Administrador';
  }

  private handleLoginResponse(response: LoginResponse): void {
    if (response.status === 'LOGIN_SUCCESS' && response.accessToken) {
      localStorage.setItem(this.tokenStorageKey, response.accessToken);
      if (response.name) {
        localStorage.setItem(this.userNameStorageKey, response.name);
      }
      return;
    }

    if (response.pendingToken) {
      this.setPendingToken(response.pendingToken);
    }
  }

  getClaims(): JwtClaims | null {
    const token = this.getToken();
    if (!token) {
      return null;
    }

    try {
      const payload = token.split('.')[1];
      if (!payload) {
        return null;
      }

      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const pad = normalized.length % 4;
      const padded = pad === 0 ? normalized : normalized + '='.repeat(4 - pad);
      const decoded = atob(padded);
      return JSON.parse(decoded) as JwtClaims;
    } catch {
      return null;
    }
  }

  isAdmin(): boolean {
    const admin = this.getClaims()?.admin;
    return String(admin).toLowerCase() === 'true';
  }

  hasPermission(permission: string): boolean {
    if (this.isAdmin()) {
      return true;
    }

    const permissions = this.getClaims()?.permissions;
    if (!permissions) {
      return false;
    }

    if (Array.isArray(permissions)) {
      return permissions.some((p) => p === permission);
    }

    return permissions === permission;
  }
}
