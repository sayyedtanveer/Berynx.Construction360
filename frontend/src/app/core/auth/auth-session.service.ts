import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';
import { API_BASE_URL } from '../api/api-base-url.token';

interface AuthSessionResponse {
  authenticated: boolean;
}

/**
 * Calls the server session contract; no client-side token or tenant state is
 * treated as proof of identity. The corresponding API endpoint is intentionally
 * not implemented during Gate 0.
 */
@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  isAuthenticated(): Observable<boolean> {
    return this.http
      .get<AuthSessionResponse>(`${this.baseUrl}/auth/session`)
      .pipe(
        map((session) => session.authenticated),
        catchError(() => of(false)),
      );
  }
}
