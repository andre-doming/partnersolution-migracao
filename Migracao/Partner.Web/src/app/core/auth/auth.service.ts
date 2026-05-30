import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';

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

interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorageKey = 'partner.token';
  private readonly userNameStorageKey = 'partner.userName';

  login(payload: LoginPayload): Observable<void> {
    return this.http
      .post<LoginResponse>(`${API_CONFIG.baseUrl}/auth/login`, payload)
      .pipe(
        tap((response) => {
          localStorage.setItem(this.tokenStorageKey, response.accessToken);
          localStorage.setItem(this.userNameStorageKey, response.name);
        }),
        map(() => undefined)
      );
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.userNameStorageKey);
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
