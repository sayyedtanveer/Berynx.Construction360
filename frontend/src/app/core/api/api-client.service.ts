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

  put<TResponse, TRequest>(path: string, body: TRequest): Observable<TResponse> {
    return this.http.put<TResponse>(this.endpoint(path), body);
  }

  delete<TResponse>(path: string): Observable<TResponse> {
    return this.http.delete<TResponse>(this.endpoint(path));
  }

  getBlob(path: string): Observable<Blob> {
    return this.http.get(this.endpoint(path), { responseType: 'blob' });
  }

  getText(path: string): Observable<string> {
    return this.http.get(this.endpoint(path), { responseType: 'text' });
  }

  private endpoint(path: string): string {
    return `${this.baseUrl.replace(/\/$/, '')}/${path.replace(/^\/+/, '')}`;
  }
}
