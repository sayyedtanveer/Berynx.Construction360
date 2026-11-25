import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { LoginPage } from './login.page';
import { AuthSessionService } from '../../core/auth/auth-session.service';

@Component({ standalone: true, template: '' })
class HomeStub {}

describe('LoginPage', () => {
  let routeParams: BehaviorSubject<ParamMap>;

  beforeEach(async () => {
    localStorage.clear();
    routeParams = new BehaviorSubject(convertToParamMap({}));
    await TestBed.configureTestingModule({
      imports: [LoginPage, HomeStub],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '', component: HomeStub }]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              get queryParamMap() { return routeParams.value; },
            },
          },
        },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
  });

  function createPage() {
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/v1/m02/auth/refresh-state').flush({ hasRefreshSession: false });
    fixture.detectChanges();
    return { fixture, http, page: fixture.componentInstance };
  }

  it('keeps the new login UX and shows no authenticated shell on first visit', () => {
    const { fixture, http } = createPage();
    const page = fixture.nativeElement as HTMLElement;
    expect(page.querySelector('.login-card')).not.toBeNull();
    expect(page.querySelector('h1')?.textContent).toContain('Berynx Construction 360');
    expect(page.querySelector('#email')).not.toBeNull();
    expect(page.querySelector('#password')).not.toBeNull();
    expect(page.textContent).toContain('Forgot your password?');
    expect(page.textContent).toContain('Activate account / Have an invitation?');
    expect(page.textContent).toContain('Contact your Berynx platform administrator');
    expect(page.querySelector('app-sidebar')).toBeNull();
    expect(http.match(() => true)).toHaveLength(0);
    expect(TestBed.inject(AuthSessionService).accessToken()).toBeNull();
  });

  it('opens the styled forgot-password form from the login UX', () => {
    const { fixture, page } = createPage();
    page.view.set('forgot');
    fixture.detectChanges();
    expect(page.view()).toBe('forgot');
    expect((fixture.nativeElement as HTMLElement).querySelector('#forgot-email')).not.toBeNull();
  });

  it('shows the explicit unavailable state when password email is not configured', () => {
    const { fixture, http, page } = createPage();
    page.view.set('forgot');
    page.forgotEmail = 'person@example.invalid';
    page.forgotPassword();
    http.expectOne('/api/v1/m02/auth/password/forgot').flush({
      message: 'Password reset email delivery is not configured. Contact your administrator.',
      deliveryStatus: 'unavailable',
    });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('Password reset email delivery is not configured');
  });

  it('shows the Development reset link and never calls a tokenless reset endpoint', () => {
    const { fixture, http, page } = createPage();
    page.view.set('forgot');
    page.forgotEmail = 'platform.admin@berynx.local';
    page.forgotPassword();
    http.expectOne('/api/v1/m02/auth/password/forgot').flush({
      message: 'A local development reset link is ready. No email was sent.',
      deliveryStatus: 'local-link',
      developmentResetUrl: 'http://localhost:4200/login?view=local-reset&email=platform.admin%40berynx.local&token=one-time',
    });
    fixture.detectChanges();

    const resetLink = (fixture.nativeElement as HTMLElement)
      .querySelector('a[href*="token=one-time"]');
    expect(resetLink?.textContent).toContain('Development: Open reset form');
    expect(http.match((request) => request.url.includes('reset-local'))).toHaveLength(0);
  });

  it('sends credentials to the M02 API and stores the issued access token', () => {
    const { http, page } = createPage();
    page.email = 'user@example.invalid';
    page.password = 'correct-password';
    page.login();
    http.expectOne('/api/v1/m02/auth/login').flush({
      accessToken: 'signed-access-token',
      expiresIn: 300,
      challengeToken: null,
      requiresTenantSelection: false,
      requiresMfa: false,
      tenants: [],
    });
    expect(TestBed.inject(AuthSessionService).accessToken()).toBe('signed-access-token');
  });

  it('shows a generic message for incorrect credentials', () => {
    const { fixture, http, page } = createPage();
    page.email = 'user@example.invalid';
    page.password = 'incorrect';
    page.login();
    http.expectOne('/api/v1/m02/auth/login')
      .flush({ title: 'Authentication required.' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('Sign-in failed. Check your email and password');
  });

  it('completes workspace selection through the M02 challenge API', () => {
    const { http, page } = createPage();
    page.email = 'user@example.invalid';
    page.password = 'correct-password';
    page.login();
    http.expectOne('/api/v1/m02/auth/login').flush({
      accessToken: null,
      expiresIn: 0,
      challengeToken: 'tenant-challenge',
      requiresTenantSelection: true,
      requiresMfa: false,
      tenants: [{ tenantId: 'tenant-1', name: 'Example workspace' }],
    });
    expect(page.view()).toBe('tenant');
    page.selectTenant();
    const selection = http.expectOne('/api/v1/m02/auth/tenant-selection');
    expect(selection.request.body).toEqual({
      challengeToken: 'tenant-challenge',
      tenantId: 'tenant-1',
    });
    selection.flush({
      accessToken: 'tenant-access-token',
      expiresIn: 300,
      challengeToken: null,
      requiresTenantSelection: false,
      requiresMfa: false,
      tenants: [],
    });
    expect(TestBed.inject(AuthSessionService).accessToken()).toBe('tenant-access-token');
  });

  it('accepts the secure invitation link through the activation API', () => {
    routeParams.next(convertToParamMap({
      inviteToken: 'single-use-invitation-token',
      email: 'invited@example.invalid',
    }));
    const { fixture, http, page } = createPage();
    expect(page.view()).toBe('activate');
    expect(page.invitationToken()).toBe('single-use-invitation-token');
    expect(page.invitedEmail()).toBe('invited@example.invalid');
    page.activationPassword = 'Initial-password-123!';
    page.activateInvitation();
    const activation = http.expectOne('/api/v1/m02/auth/invitation/activate');
    expect(activation.request.body).toEqual({
      token: 'single-use-invitation-token',
      password: 'Initial-password-123!',
    });
    activation.flush({});
    fixture.detectChanges();
    expect(page.view()).toBe('login');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Invitation accepted');
  });

  it('resets a password using the one-time Identity token, including Development reset', () => {
    routeParams.next(convertToParamMap({
      view: 'local-reset',
      email: 'platform.admin@berynx.local',
      token: 'single-use-reset-token',
    }));
    const { fixture, http, page } = createPage();
    expect(page.view()).toBe('local-reset');
    expect(page.resetEmail).toBe('platform.admin@berynx.local');
    page.newPassword = 'Replacement-password-123!';
    page.resetPassword();

    const reset = http.expectOne('/api/v1/m02/auth/password/reset');
    expect(reset.request.body).toEqual({
      email: 'platform.admin@berynx.local',
      token: 'single-use-reset-token',
      password: 'Replacement-password-123!',
    });
    reset.flush({});
    fixture.detectChanges();
    expect(page.view()).toBe('login');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Password reset');
  });

  it('supports keyboard-accessible password visibility and the secure reset entry point', () => {
    const { fixture, page } = createPage();
    const view = fixture.nativeElement as HTMLElement;
    const password = view.querySelector('#password') as HTMLInputElement;
    const toggle = view.querySelector('.password-toggle') as HTMLButtonElement;
    expect(password.type).toBe('password');
    expect(toggle.getAttribute('aria-label')).toBe('Show password');
    toggle.click();
    fixture.detectChanges();
    expect(password.type).toBe('text');
    expect(toggle.getAttribute('aria-label')).toBe('Hide password');
    const localResetButton = view.querySelector('.dev-button') as HTMLButtonElement;
    localResetButton.click();
    expect(page.view()).toBe('forgot');
  });
});
