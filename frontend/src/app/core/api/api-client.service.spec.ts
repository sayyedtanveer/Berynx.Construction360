import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { API_BASE_URL } from './api-base-url.token';
import { ApiClient } from './api-client.service';
import { AuthSessionService } from '../auth/auth-session.service';
import { bearerAuthInterceptor } from '../auth/bearer-auth.interceptor';
import { apiErrorInterceptor } from '../http/api-error.interceptor';
import { HttpErrorService } from '../http/http-error.service';

describe('ApiClient HTTP integration', () => {
  it('uses the API base URL, attaches the bearer token, and reports API problem details', async () => {
    const errors = { report: vi.fn() };
    await TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([bearerAuthInterceptor, apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://api.example.test/api/v1/' },
        { provide: AuthSessionService, useValue: { accessToken: () => 'test-access-token' } },
        { provide: HttpErrorService, useValue: errors },
      ],
    }).compileComponents();

    const api = TestBed.inject(ApiClient);
    const http = TestBed.inject(HttpTestingController);
    let failure: unknown;
    api.get('/m10/invoices').subscribe({ error: error => failure = error });

    const request = http.expectOne('https://api.example.test/api/v1/m10/invoices');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-access-token');
    request.flush(
      { detail: 'Invoice access denied.', traceId: 'trace-123' },
      { status: 403, statusText: 'Forbidden' },
    );

    expect(failure).toBeTruthy();
    expect(errors.report).toHaveBeenCalledWith({
      message: 'Invoice access denied.',
      traceId: 'trace-123',
    });
    http.verify();
  });
});
