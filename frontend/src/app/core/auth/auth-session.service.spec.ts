import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';

describe('AuthSessionService', () => {
  let http: HttpTestingController;
  let session: AuthSessionService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('checks for the HttpOnly cookie server-side and does not refresh when it is absent', () => {
    let authenticated: boolean | undefined;
    session.isAuthenticated().subscribe((value) => authenticated = value);
    http.expectOne('/api/v1/m02/auth/refresh-state')
      .flush({ hasRefreshSession: false });
    expect(authenticated).toBe(false);
    http.expectNone('/api/v1/m02/auth/refresh');
  });

  it('refreshes once when a prior login left a session hint, then verifies the server session', () => {
    localStorage.setItem('m02.refresh-session-available', '1');
    const first: boolean[] = [];
    const second: boolean[] = [];

    session.isAuthenticated().subscribe((value) => first.push(value));
    session.isAuthenticated().subscribe((value) => second.push(value));

    const refresh = http.expectOne('/api/v1/m02/auth/refresh');
    refresh.flush({
      accessToken: 'short-lived-access-token',
      expiresIn: 300,
      challengeToken: null,
      requiresTenantSelection: false,
      requiresMfa: false,
      tenants: [],
    });
    const sessionCheck = http.expectOne('/api/v1/auth/session');
    sessionCheck.flush({ authenticated: true });

    expect(first).toEqual([true]);
    expect(second).toEqual([true]);
  });

  it('allows one silent refresh when the server reports an existing HttpOnly cookie', () => {
    session.isAuthenticated().subscribe();
    http.expectOne('/api/v1/m02/auth/refresh-state')
      .flush({ hasRefreshSession: true });
    http.expectOne('/api/v1/m02/auth/refresh').flush({
      accessToken: 'restored-access-token',
      expiresIn: 300,
      challengeToken: null,
      requiresTenantSelection: false,
      requiresMfa: false,
      tenants: [],
    });
    http.expectOne('/api/v1/auth/session').flush({ authenticated: true });
  });

  it('clears the local hint after a rejected refresh cookie', () => {
    localStorage.setItem('m02.refresh-session-available', '1');
    session.isAuthenticated().subscribe();
    http.expectOne('/api/v1/m02/auth/refresh')
      .flush({ title: 'Authentication required' }, { status: 401, statusText: 'Unauthorized' });

    expect(localStorage.getItem('m02.refresh-session-available')).toBeNull();
  });
});
