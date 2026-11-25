import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, finalize, map, Observable, of, shareReplay, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from '../api/api-base-url.token';

interface AuthSessionResponse {
  authenticated: boolean;
}

interface RefreshIndicatorResponse {
  hasRefreshSession: boolean;
}

export interface AuthTokenResponse {
  accessToken: string | null;
  expiresIn: number | null;
  challengeToken: string | null;
  requiresTenantSelection: boolean;
  requiresMfa: boolean;
  tenants: Array<{ tenantId: string; name: string }>;
  provisioningUri?: string | null;
  manualKey?: string | null;
  recoveryCodes?: string[] | null;
}

/**
 * Holds the short-lived locally issued access token in memory only. The API
 * validates its tenant membership and revocable server-side session on use.
 */
@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private token: string | null = null;
  private refreshRequest: Observable<string> | null = null;
  private refreshIndicatorRequest: Observable<boolean> | null = null;
  private sessionCheckRequest: Observable<boolean> | null = null;
  private noRefreshSession = false;
  private readonly refreshHintKey = 'm02.refresh-session-available';

  setAccessToken(token: string | null): void {
    this.token = token;
    if (token) {
      this.noRefreshSession = false;
      this.setRefreshHint(true);
    }
  }

  accessToken(): string | null {
    return this.token;
  }

  hasAccessToken(): boolean {
    return this.token !== null;
  }

  clear(): void {
    this.token = null;
    this.noRefreshSession = true;
    this.setRefreshHint(false);
  }

  refresh(): Observable<string> {
    if (!this.refreshRequest) {
      this.refreshRequest = this.http
        .post<AuthTokenResponse>(`${this.baseUrl}/m02/auth/refresh`, {}, { withCredentials: true })
        .pipe(
          map((response) => {
            if (!response.accessToken) throw new Error('No access token was returned.');
            this.setAccessToken(response.accessToken);
            return response.accessToken;
          }),
          catchError((error: unknown) => {
            this.clear();
            return throwError(() => error);
          }),
          finalize(() => { this.refreshRequest = null; }),
          shareReplay({ bufferSize: 1, refCount: false }),
        );
    }
    return this.refreshRequest;
  }

  isAuthenticated(): Observable<boolean> {
    if (this.sessionCheckRequest) return this.sessionCheckRequest;

    const sessionIndicator = this.token ? of(true) : this.refreshIndicator();
    this.sessionCheckRequest = sessionIndicator.pipe(
      switchMap((hasSession) =>
        hasSession ? (this.token ? of(this.token) : this.refresh()) : of(null),
      ),
      switchMap((token) => token
        ? this.http.get<AuthSessionResponse>(`${this.baseUrl}/auth/session`)
        : of({ authenticated: false }),
      ),
      map((session) => {
        if (!session.authenticated) this.clear();
        return session.authenticated;
      }),
      catchError(() => {
        this.clear();
        return of(false);
      }),
      finalize(() => { this.sessionCheckRequest = null; }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.sessionCheckRequest;
  }

  private refreshIndicator(): Observable<boolean> {
    if (this.hasRefreshHint()) return of(true);
    if (this.noRefreshSession) return of(false);
    if (!this.refreshIndicatorRequest) {
      this.refreshIndicatorRequest = this.http
        .get<RefreshIndicatorResponse>(`${this.baseUrl}/m02/auth/refresh-state`, {
          withCredentials: true,
        })
        .pipe(
          map((response) => {
            this.noRefreshSession = !response.hasRefreshSession;
            if (response.hasRefreshSession) this.setRefreshHint(true);
            return response.hasRefreshSession;
          }),
          catchError(() => {
            this.noRefreshSession = true;
            return of(false);
          }),
          finalize(() => { this.refreshIndicatorRequest = null; }),
          shareReplay({ bufferSize: 1, refCount: false }),
        );
    }
    return this.refreshIndicatorRequest;
  }

  private hasRefreshHint(): boolean {
    try {
      return localStorage.getItem(this.refreshHintKey) === '1';
    } catch {
      return false;
    }
  }

  private setRefreshHint(available: boolean): void {
    try {
      if (available) localStorage.setItem(this.refreshHintKey, '1');
      else localStorage.removeItem(this.refreshHintKey);
    } catch {
      // The hint is an optimization only; the HttpOnly cookie remains authoritative.
    }
  }
}
