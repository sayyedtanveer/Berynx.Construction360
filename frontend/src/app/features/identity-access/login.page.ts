import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';
import { AuthSessionService, AuthTokenResponse } from '../../core/auth/auth-session.service';

@Component({
  selector: 'app-login-page',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-page">
      <div class="login-container">
        <div class="login-card">
          <!-- Brand Header -->
          <div class="login-brand">
            <div class="brand-icon">BC</div>
            <h1 class="brand-title">Berynx Construction 360</h1>
            <p class="brand-subtitle">Premium Construction Management</p>
          </div>

          <!-- Alert Messages -->
          @if (error()) {
            <div class="alert alert-error" role="alert">
              <span class="alert-icon">⚠</span>
              <span>{{ error() }}</span>
            </div>
          }
          @if (notice()) {
            <div class="alert alert-success" role="status">
              <span class="alert-icon">✓</span>
              <span>{{ notice() }}</span>
            </div>
          }

          <!-- Login Form -->
          @if (view() === 'login') {
            <form class="login-form" (ngSubmit)="login()">
              <h2 class="form-title">Welcome Back</h2>
              <p class="form-subtitle">Sign in to your account</p>

              <div class="form-group">
                <label for="email" class="form-label">Email Address</label>
                <input
                  id="email"
                  type="email"
                  class="form-input"
                  [(ngModel)]="email"
                  name="email"
                  autocomplete="username"
                  placeholder="you@company.com"
                  required
                  [disabled]="loading()"
                />
              </div>

              <div class="form-group">
                <label for="password" class="form-label">Password</label>
                <div class="password-wrapper">
                  <input
                    id="password"
                    [type]="showPassword() ? 'text' : 'password'"
                    class="form-input"
                    [(ngModel)]="password"
                    name="password"
                    autocomplete="current-password"
                    placeholder="Enter your password"
                    required
                    [disabled]="loading()"
                  />
                  <button
                    type="button"
                    class="password-toggle"
                    (click)="showPassword.set(!showPassword())"
                    [attr.aria-label]="showPassword() ? 'Hide password' : 'Show password'"
                  >
                    {{ showPassword() ? '👁️' : '👁️‍🗨️' }}
                  </button>
                </div>
              </div>

              <button
                type="submit"
                class="btn-primary"
                [disabled]="loading()"
              >
                @if (loading()) {
                  <span class="spinner"></span>
                  <span>Signing in...</span>
                } @else {
                  <span>Sign In</span>
                }
              </button>

              <button
                type="button"
                class="btn-link"
                (click)="view.set('forgot')"
                [disabled]="loading()"
              >
                Forgot your password?
              </button>
              <button
                type="button"
                class="btn-link"
                (click)="view.set('activate')"
                [disabled]="loading()"
              >
                Activate account / Have an invitation?
              </button>
              <p class="company-guidance">
                Need a company workspace? Contact your Berynx platform administrator. Company accounts are provisioned by an authorized platform administrator.
              </p>
            </form>
          }

          <!-- Forgot Password -->
          @if (view() === 'forgot') {
            <form class="login-form" (ngSubmit)="forgotPassword()">
              <h2 class="form-title">Reset Password</h2>
              <p class="form-subtitle">Enter your email to reset your password</p>

              <div class="form-group">
                <label for="forgot-email" class="form-label">Email Address</label>
                <input
                  id="forgot-email"
                  type="email"
                  class="form-input"
                  [(ngModel)]="forgotEmail"
                  name="forgotEmail"
                  autocomplete="email"
                  placeholder="you@company.com"
                  required
                  [disabled]="loading()"
                />
              </div>

              <button
                type="submit"
                class="btn-primary"
                [disabled]="loading()"
              >
                @if (loading()) {
                  <span class="spinner"></span>
                  <span>Sending...</span>
                } @else {
                  <span>Send Reset Instructions</span>
                }
              </button>

              <button
                type="button"
                class="btn-link"
                (click)="view.set('login')"
                [disabled]="loading()"
              >
                ← Back to Sign In
              </button>

              @if (developmentResetUrl()) {
                <a class="dev-reset-link" [href]="developmentResetUrl()">
                  🔧 Development: Open reset form
                </a>
              }
            </form>
          }

          <!-- Local Reset (Development Only) -->
          @if (view() === 'local-reset' || view() === 'reset') {
            <form class="login-form" (ngSubmit)="resetPassword()">
              <h2 class="form-title">
                {{ view() === 'local-reset' ? '🔧 Local Password Reset' : 'Reset Password' }}
              </h2>
              <p class="form-subtitle">
                {{ view() === 'local-reset'
                  ? 'Development mode — use the one-time reset link; no email was sent.'
                  : 'Choose a new password using this secure, one-time link.' }}
              </p>

              <div class="form-group">
                <label for="reset-email" class="form-label">Email Address</label>
                <input
                  id="reset-email"
                  type="email"
                  class="form-input"
                  [(ngModel)]="resetEmail"
                  name="resetEmail"
                  autocomplete="email"
                  placeholder="you@company.com"
                  required
                  [readonly]="!!resetToken()"
                  [disabled]="loading()"
                />
              </div>

              <div class="form-group">
                <label for="new-password" class="form-label">New Password</label>
                <input
                  id="new-password"
                  type="password"
                  class="form-input"
                  [(ngModel)]="newPassword"
                  name="newPassword"
                  autocomplete="new-password"
                  placeholder="Enter new password (min 8 characters)"
                  minlength="8"
                  required
                  [disabled]="loading()"
                />
              </div>

              <button
                type="submit"
                class="btn-primary"
                [disabled]="loading() || !resetToken()"
              >
                @if (loading()) {
                  <span class="spinner"></span>
                  <span>Resetting...</span>
                } @else {
                  <span>Reset Password</span>
                }
              </button>

              <button
                type="button"
                class="btn-link"
                (click)="view.set('login')"
                [disabled]="loading()"
              >
                ← Back to Sign In
              </button>
            </form>
          }

          <!-- Invitation Activation -->
          @if (view() === 'activate') {
            <form class="login-form" (ngSubmit)="activateInvitation()">
              <h2 class="form-title">Activate Invitation</h2>
              <p class="form-subtitle">Use the secure invitation link from your administrator.</p>

              @if (!invitationToken()) {
                <div class="form-group">
                  <label for="invitation-token" class="form-label">Invitation link or token</label>
                  <input
                    id="invitation-token"
                    class="form-input"
                    [(ngModel)]="invitationInput"
                    (ngModelChange)="setInvitationToken($event)"
                    name="invitationInput"
                    autocomplete="off"
                    required
                    [disabled]="loading()"
                  />
                </div>
              } @else {
                <div class="alert alert-success" role="status">
                  Invitation link loaded{{ invitedEmail() ? ' for ' + invitedEmail() : '' }}.
                </div>
              }

              @if (invitedEmail()) {
                <div class="form-group">
                  <label for="invited-email" class="form-label">Invited email</label>
                  <input id="invited-email" class="form-input" [value]="invitedEmail()" readonly />
                </div>
              }

              <div class="form-group">
                <label for="activation-password" class="form-label">Set password for a new account</label>
                <input
                  id="activation-password"
                  type="password"
                  class="form-input"
                  [(ngModel)]="activationPassword"
                  name="activationPassword"
                  autocomplete="new-password"
                  minlength="8"
                  [disabled]="loading()"
                />
              </div>
              <p class="form-subtitle">If you already have a Berynx account, leave the password blank and sign in after accepting.</p>

              <button type="submit" class="btn-primary"
                [disabled]="loading() || !invitationToken()">
                @if (loading()) { <span class="spinner"></span><span>Activating...</span> }
                @else { <span>Accept Invitation</span> }
              </button>
              <button type="button" class="btn-link" (click)="view.set('login')" [disabled]="loading()">
                ← Back to Sign In
              </button>
            </form>
          }

          <!-- MFA Challenge -->
          @if (view() === 'mfa') {
            <form class="login-form" (ngSubmit)="completeMfa()">
              <h2 class="form-title">Two-Factor Authentication</h2>
              <p class="form-subtitle">Enter your authenticator code</p>

              <div class="form-group">
                <label for="mfa-code" class="form-label">6-Digit Code</label>
                <input
                  id="mfa-code"
                  type="text"
                  class="form-input text-center"
                  [(ngModel)]="mfaCode"
                  name="mfaCode"
                  autocomplete="one-time-code"
                  inputmode="numeric"
                  placeholder="000000"
                  maxlength="6"
                  required
                  [disabled]="loading()"
                />
              </div>

              <button
                type="submit"
                class="btn-primary"
                [disabled]="loading()"
              >
                @if (loading()) {
                  <span class="spinner"></span>
                  <span>Verifying...</span>
                } @else {
                  <span>Verify & Continue</span>
                }
              </button>
            </form>
          }

          @if (view() === 'mfa-setup') {
            <section class="login-form" aria-labelledby="required-mfa-title">
              <h2 class="form-title" id="required-mfa-title">Set up required MFA</h2>
              <p class="form-subtitle">Your selected workspace requires an authenticator for this role. Business access remains blocked until setup is complete.</p>
              @if (requiredMfaSetup()) {
                <label for="required-mfa-uri" class="form-label">Authenticator provisioning URI</label>
                <textarea id="required-mfa-uri" class="form-input" rows="3" readonly
                  [value]="requiredMfaSetup()?.provisioningUri"></textarea>
                @if (requiredMfaSetup()?.manualKey) {
                  <label for="required-mfa-key" class="form-label">Manual setup key</label>
                  <input id="required-mfa-key" class="form-input" readonly
                    [value]="requiredMfaSetup()?.manualKey" />
                }
                <form class="login-form" (ngSubmit)="completeRequiredMfaSetup()">
                  <div class="form-group">
                    <label for="required-mfa-code" class="form-label">6-Digit Authenticator Code</label>
                    <input id="required-mfa-code" type="text" class="form-input text-center"
                      [(ngModel)]="requiredMfaCode" name="requiredMfaCode" autocomplete="one-time-code"
                      inputmode="numeric" pattern="[0-9]{6}" maxlength="6" required [disabled]="loading()" />
                  </div>
                  <button type="submit" class="btn-primary" [disabled]="loading()">
                    {{ loading() ? 'Verifying…' : 'Verify and Continue' }}
                  </button>
                </form>
              } @else {
                <p class="loading" role="status">Preparing secure authenticator setup…</p>
              }
            </section>
          }

          @if (view() === 'mfa-recovery') {
            <section class="login-form" aria-labelledby="recovery-title">
              <h2 class="form-title" id="recovery-title">Save your recovery codes</h2>
              <p class="form-subtitle">Each code works once. Store them securely before continuing.</p>
              <ul class="recovery-code-list">
                @for (code of requiredRecoveryCodes(); track code) { <li><code>{{ code }}</code></li> }
              </ul>
              <button type="button" class="btn-primary" (click)="continueAfterMfaSetup()">
                I saved these codes
              </button>
            </section>
          }

          <!-- Tenant Selection -->
          @if (view() === 'tenant') {
            <form class="login-form" (ngSubmit)="selectTenant()">
              <h2 class="form-title">Select Workspace</h2>
              <p class="form-subtitle">Choose your organization</p>

              <div class="form-group">
                <label for="tenant-select" class="form-label">Workspace</label>
                <select
                  id="tenant-select"
                  class="form-input"
                  [(ngModel)]="selectedTenantId"
                  name="tenantSelect"
                  required
                  [disabled]="loading()"
                >
                  <option value="">-- Select Workspace --</option>
                  @for (tenant of tenants(); track tenant.tenantId) {
                    <option [value]="tenant.tenantId">{{ tenant.name }}</option>
                  }
                </select>
              </div>

              <button
                type="submit"
                class="btn-primary"
                [disabled]="loading() || !selectedTenantId"
              >
                Continue
              </button>
            </form>
          }

          <!-- Development Quick Access -->
          <div class="dev-tools">
            <button
              type="button"
              class="dev-button"
              (click)="startDevelopmentReset()"
              title="Request a one-time development reset link"
            >
              🔧 Local Reset
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: `
    .login-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #17212B 0%, #2A3A4D 50%, #C46A36 100%);
      padding: var(--space-4);
      position: relative;
      overflow: hidden;
    }

    .login-page::before {
      content: '';
      position: absolute;
      inset: 0;
      background: 
        radial-gradient(circle at 20% 80%, rgba(196, 106, 54, 0.15) 0%, transparent 50%),
        radial-gradient(circle at 80% 20%, rgba(169, 86, 43, 0.1) 0%, transparent 50%);
      pointer-events: none;
    }

    .login-container {
      width: 100%;
      max-width: 440px;
      position: relative;
      z-index: 1;
    }

    .login-card {
      background: rgba(255, 255, 255, 0.98);
      backdrop-filter: blur(20px);
      border-radius: var(--radius-xl);
      padding: var(--space-8);
      box-shadow: 
        0 20px 60px rgba(0, 0, 0, 0.3),
        0 0 1px rgba(255, 255, 255, 0.5) inset;
      border: 1px solid rgba(255, 255, 255, 0.2);
    }

    .login-brand {
      text-align: center;
      margin-bottom: var(--space-8);
    }

    .brand-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 56px;
      height: 56px;
      background: linear-gradient(135deg, #C46A36 0%, #A9562B 100%);
      color: var(--text-inverse);
      font-size: 1.25rem;
      font-weight: var(--font-weight-bold);
      border-radius: var(--radius-lg);
      letter-spacing: 0.05em;
      box-shadow: 
        0 8px 20px rgba(196, 106, 54, 0.4),
        0 0 0 4px rgba(196, 106, 54, 0.1);
      margin-bottom: var(--space-4);
    }

    .brand-title {
      font-size: var(--font-size-xl);
      font-weight: var(--font-weight-bold);
      color: var(--text-heading);
      margin: 0 0 var(--space-1);
      background: linear-gradient(135deg, #1F2933 0%, #C46A36 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .brand-subtitle {
      font-size: var(--font-size-sm);
      color: var(--text-muted);
      margin: 0;
      font-weight: var(--font-weight-medium);
    }

    .alert {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-3) var(--space-4);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      margin-bottom: var(--space-6);
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .alert-error {
      background: var(--danger-light);
      color: var(--danger);
      border: 1px solid var(--danger-border);
    }

    .alert-success {
      background: var(--success-light);
      color: var(--success);
      border: 1px solid var(--success-border);
    }

    .alert-icon {
      font-size: 1.125rem;
      flex-shrink: 0;
    }

    .login-form {
      display: flex;
      flex-direction: column;
      gap: var(--space-5);
    }

    .form-title {
      font-size: var(--font-size-2xl);
      font-weight: var(--font-weight-bold);
      color: var(--text-heading);
      margin: 0;
      text-align: center;
    }

    .form-subtitle {
      font-size: var(--font-size-sm);
      color: var(--text-secondary);
      margin: 0;
      text-align: center;
      margin-top: calc(var(--space-2) * -1);
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
    }

    .form-label {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-semibold);
      color: var(--text-heading);
    }

    .form-input {
      width: 100%;
      height: 44px;
      padding: var(--space-3) var(--space-4);
      font-size: var(--font-size-base);
      font-family: inherit;
      color: var(--text-heading);
      background: var(--surface);
      border: 2px solid var(--border-default);
      border-radius: var(--radius-md);
      transition: all var(--transition-fast);
      outline: none;
    }

    .form-input::placeholder {
      color: var(--text-disabled);
    }

    .form-input:hover:not(:disabled) {
      border-color: var(--border-medium);
    }

    .form-input:focus {
      border-color: var(--primary-copper);
      box-shadow: 0 0 0 4px rgba(196, 106, 54, 0.1);
    }

    .form-input:disabled {
      background: var(--canvas);
      cursor: not-allowed;
      opacity: 0.6;
    }

    .form-input.text-center {
      text-align: center;
      font-size: var(--font-size-2xl);
      font-weight: var(--font-weight-semibold);
      letter-spacing: 0.5em;
      padding-left: calc(var(--space-4) + 0.25em);
    }

    .password-wrapper {
      position: relative;
      display: flex;
      align-items: center;
    }

    .password-wrapper .form-input {
      padding-right: 48px;
    }

    .password-toggle {
      position: absolute;
      right: 2px;
      top: 50%;
      transform: translateY(-50%);
      height: 40px;
      width: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: none;
      border: none;
      color: var(--text-muted);
      cursor: pointer;
      border-radius: var(--radius-sm);
      transition: all var(--transition-fast);
      font-size: 1.125rem;
    }

    .password-toggle:hover {
      background: var(--canvas);
      color: var(--text-heading);
    }

    .btn-primary {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-2);
      height: 44px;
      padding: var(--space-3) var(--space-5);
      background: linear-gradient(135deg, #C46A36 0%, #A9562B 100%);
      color: var(--text-inverse);
      border: none;
      border-radius: var(--radius-md);
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
      cursor: pointer;
      transition: all var(--transition-fast);
      box-shadow: 0 4px 12px rgba(196, 106, 54, 0.3);
    }

    .btn-primary:hover:not(:disabled) {
      transform: translateY(-1px);
      box-shadow: 0 6px 20px rgba(196, 106, 54, 0.4);
    }

    .btn-primary:active:not(:disabled) {
      transform: translateY(0);
    }

    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
      transform: none;
    }

    .btn-link {
      background: none;
      border: none;
      color: var(--primary-copper);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      cursor: pointer;
      padding: var(--space-2);
      text-align: center;
      transition: color var(--transition-fast);
    }

    .btn-link:hover:not(:disabled) {
      color: var(--primary-copper-hover);
      text-decoration: underline;
    }

    .btn-link:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .company-guidance {
      margin: 0;
      color: var(--text-muted);
      font-size: var(--font-size-xs);
      line-height: 1.5;
      text-align: center;
    }

    .form-input:focus-visible,
    .btn-primary:focus-visible,
    .btn-link:focus-visible,
    .password-toggle:focus-visible,
    .dev-button:focus-visible,
    .dev-reset-link:focus-visible {
      outline: 3px solid var(--primary-copper);
      outline-offset: 2px;
    }

    .spinner {
      display: inline-block;
      width: 16px;
      height: 16px;
      border: 2px solid currentColor;
      border-right-color: transparent;
      border-radius: 50%;
      animation: spin 0.6s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .dev-reset-link {
      display: block;
      text-align: center;
      padding: var(--space-2);
      font-size: var(--font-size-xs);
      color: var(--warning);
      text-decoration: none;
      background: var(--warning-light);
      border: 1px solid var(--warning-border);
      border-radius: var(--radius-md);
      margin-top: var(--space-2);
    }

    .dev-reset-link:hover {
      text-decoration: underline;
    }

    .dev-tools {
      margin-top: var(--space-6);
      padding-top: var(--space-4);
      border-top: 1px solid var(--border-light);
      text-align: center;
    }

    .dev-button {
      background: var(--warning-light);
      border: 1px solid var(--warning-border);
      color: var(--warning);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      padding: var(--space-1) var(--space-3);
      border-radius: var(--radius-full);
      cursor: pointer;
      transition: all var(--transition-fast);
    }

    .dev-button:hover {
      background: var(--warning);
      color: var(--text-inverse);
    }

    @media (max-width: 480px) {
      .login-card {
        padding: var(--space-6);
      }

      .brand-title {
        font-size: var(--font-size-lg);
      }

      .form-title {
        font-size: var(--font-size-xl);
      }
    }
  `,
})
export class LoginPage implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly authSession = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);

  view = signal<'login' | 'forgot' | 'local-reset' | 'reset' | 'activate' | 'mfa' | 'mfa-setup' | 'mfa-recovery' | 'tenant'>('login');
  loading = signal(false);
  error = signal<string | null>(null);
  notice = signal<string | null>(null);
  showPassword = signal(false);
  developmentResetUrl = signal<string | null>(null);
  resetToken = signal('');
  invitationToken = signal('');
  invitedEmail = signal('');
  tenants = signal<Array<{ tenantId: string; name: string }>>([]);
  requiredMfaSetup = signal<{ provisioningUri: string; manualKey?: string } | null>(null);
  requiredRecoveryCodes = signal<string[]>([]);

  email = '';
  password = '';
  forgotEmail = '';
  resetEmail = '';
  newPassword = '';
  invitationInput = '';
  activationPassword = '';
  mfaCode = '';
  requiredMfaCode = '';
  private setupAccessToken = '';
  challengeToken = '';
  selectedTenantId = '';

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const returnUrl = params.get('returnUrl');
    if (returnUrl && this.isSafeReturnPath(returnUrl))
      sessionStorage.setItem('m02.returnPath', returnUrl);

    this.email = params.get('email') ?? '';
    this.forgotEmail = this.email;
    this.resetEmail = this.email;
    this.resetToken.set(params.get('token') ?? '');
    this.invitationToken.set(params.get('inviteToken') ?? '');
    this.invitedEmail.set(params.get('inviteToken') ? this.email : '');

    if (this.resetToken()) {
      this.view.set(params.get('view') === 'local-reset' ? 'local-reset' : 'reset');
    } else if (this.invitationToken()) {
      this.view.set('activate');
    } else {
      const requestedView = params.get('view');
      if (requestedView === 'forgot' || requestedView === 'activate')
        this.view.set(requestedView);
    }

    if (this.resetToken() || this.invitationToken()) this.cleanSensitiveQuery();
    this.authSession.isAuthenticated()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((authenticated) => {
        if (authenticated) this.navigateAfterSignIn();
      });
  }

  login(): void {
    this.run(this.api.post<AuthTokenResponse, { email: string; password: string }>(
      'm02/auth/login', { email: this.email, password: this.password },
    ), (result) => this.handleAuthResult(result));
  }

  forgotPassword(): void {
    this.developmentResetUrl.set(null);
    this.run(this.api.post<{
      message: string;
      deliveryStatus?: string;
      developmentResetUrl?: string | null;
    }, { email: string }>(
      'm02/auth/password/forgot', { email: this.forgotEmail || this.email },
    ), (result) => {
      this.notice.set(result.message);
      this.developmentResetUrl.set(result.developmentResetUrl ?? null);
    });
  }

  startDevelopmentReset(): void {
    this.forgotEmail = this.email;
    this.view.set('forgot');
    this.error.set(null);
    this.notice.set(null);
  }

  resetPassword(): void {
    this.run(this.api.post<void, { email: string; token: string; password: string }>(
      'm02/auth/password/reset',
      { email: this.resetEmail || this.email, token: this.resetToken(), password: this.newPassword },
    ), () => {
      this.resetToken.set('');
      this.newPassword = '';
      this.email = this.resetEmail || this.email;
      this.view.set('login');
      this.notice.set('Password reset. You can now sign in.');
      this.cleanSensitiveQuery();
    });
  }

  setInvitationToken(value: string): void {
    this.invitationInput = value;
    const candidate = value.trim();
    try {
      const inviteUrl = new URL(candidate);
      const token = inviteUrl.searchParams.get('inviteToken');
      if (token) {
        this.invitationToken.set(token);
        this.invitedEmail.set(inviteUrl.searchParams.get('email') ?? '');
      } else {
        this.invitationToken.set(candidate);
      }
    } catch {
      this.invitationToken.set(candidate);
    }
    if (this.invitedEmail()) this.email = this.invitedEmail();
  }

  activateInvitation(): void {
    this.run(this.api.post<void, { token: string; password: string | null }>(
      'm02/auth/invitation/activate',
      { token: this.invitationToken(), password: this.activationPassword || null },
    ), () => {
      this.invitationToken.set('');
      this.activationPassword = '';
      this.view.set('login');
      this.notice.set('Invitation accepted. Sign in with your account.');
      this.cleanSensitiveQuery();
    });
  }

  completeMfa(): void {
    this.run(this.api.post<AuthTokenResponse, { challengeToken: string; code: string }>(
      'm02/auth/mfa', { challengeToken: this.challengeToken, code: this.mfaCode },
    ), (result) => {
      this.mfaCode = '';
      this.handleAuthResult(result);
    });
  }

  completeRequiredMfaSetup(): void {
    this.run(this.api.post<AuthTokenResponse, { challengeToken: string; code: string }>(
      'm02/auth/mfa/setup/confirm',
      { challengeToken: this.challengeToken, code: this.requiredMfaCode },
    ), (result) => {
      this.requiredMfaCode = '';
      if (result.accessToken) {
        this.setupAccessToken = result.accessToken;
        this.requiredRecoveryCodes.set(result.recoveryCodes ?? []);
        this.view.set('mfa-recovery');
      } else {
        this.handleAuthResult(result);
      }
    });
  }

  continueAfterMfaSetup(): void {
    if (!this.setupAccessToken) return;
    this.authSession.setAccessToken(this.setupAccessToken);
    this.setupAccessToken = '';
    this.navigateAfterSignIn();
  }

  selectTenant(): void {
    this.run(this.api.post<AuthTokenResponse, { challengeToken: string; tenantId: string }>(
      'm02/auth/tenant-selection',
      { challengeToken: this.challengeToken, tenantId: this.selectedTenantId },
    ), (result) => this.handleAuthResult(result));
  }

  private handleAuthResult(result: AuthTokenResponse): void {
    if (result.requiresMfaSetup) {
      this.challengeToken = result.challengeToken ?? '';
      this.requiredMfaSetup.set(null);
      this.view.set('mfa-setup');
      this.run(this.api.post<{ provisioningUri: string; manualKey?: string },
        { challengeToken: string }>(
        'm02/auth/mfa/setup/begin', { challengeToken: this.challengeToken },
      ), (setup) => this.requiredMfaSetup.set(setup));
      return;
    }
    if (result.requiresMfa) {
      this.challengeToken = result.challengeToken ?? '';
      this.view.set('mfa');
      return;
    }
    if (result.requiresTenantSelection && result.tenants?.length) {
      this.challengeToken = result.challengeToken ?? '';
      this.tenants.set(result.tenants);
      this.selectedTenantId = result.tenants[0].tenantId;
      this.view.set('tenant');
      return;
    }
    if (result.accessToken) {
      this.authSession.setAccessToken(result.accessToken);
      this.navigateAfterSignIn();
    }
  }

  private navigateAfterSignIn(): void {
    const destination = sessionStorage.getItem('m02.returnPath') ?? '/';
    sessionStorage.removeItem('m02.returnPath');
    void this.router.navigateByUrl(this.isSafeReturnPath(destination) ? destination : '/');
  }

  private isSafeReturnPath(path: string): boolean {
    return path.startsWith('/') && !path.startsWith('//') && !path.includes('\\');
  }

  private cleanSensitiveQuery(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { token: null, inviteToken: null, email: null, view: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private run<T>(request: Observable<T>, success: (response: T) => void): void {
    this.loading.set(true);
    this.error.set(null);
    this.notice.set(null);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.loading.set(false);
        success(response);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(error instanceof HttpErrorResponse
          ? error.status === 401
            ? 'Sign-in failed. Check your email and password, or contact your tenant administrator.'
            : error.error?.detail ?? 'The request could not be completed. Check the link and try again.'
          : 'The request could not be completed.');
      },
    });
  }
}
