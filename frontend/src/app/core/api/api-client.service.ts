import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from './api-base-url.token';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  get<TResponse>(path: string): Observable<TResponse> {
    return this.http.get<TResponse>(this.endpoint(path));
  }

  post<TResponse, TRequest>(path: string, body: TRequest): Observable<TResponse> {
    return this.http.post<TResponse>(this.endpoint(path), body);
  }

  private endpoint(path: string): string {
    return `${this.baseUrl.replace(/\/$/, '')}/${path.replace(/^\/+/, '')}`;
  }
}
