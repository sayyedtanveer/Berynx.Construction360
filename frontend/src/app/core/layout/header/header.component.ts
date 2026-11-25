import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthSessionService } from '../../auth/auth-session.service';
import { ApiClient } from '../../api/api-client.service';

interface HeaderContext {
  tenantName: string;
  displayName: string;
  email: string;
  scopeTypes: string[];
}

interface HeaderNotification {
  notificationId: string;
  kind: string;
  payload: string;
  createdAt: string;
  readAt: string | null;
}

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterLink],
  template: `
    <header class="header">
      <div class="header-left">
        <button
          type="button"
          class="mobile-menu-toggle"
          (click)="toggleMobileMenu()"
          aria-label="Toggle menu"
        >
          ☰
        </button>

        <nav class="breadcrumbs" aria-label="Breadcrumb">
          <ol>
            <li><a routerLink="/">Home</a></li>
            @if (currentPage()) {
              <li aria-current="page">{{ currentPage() }}</li>
            }
          </ol>
        </nav>
      </div>

      <div class="header-right">
        <!-- Tenant/Branch Selector -->
        <div class="tenant-selector">
          <div class="selector-button" aria-label="Current workspace and role-defined access scope">
            <div class="selector-content">
              <span class="selector-icon">🏢</span>
              <div class="selector-text">
                <span class="selector-label">{{ currentTenant() }}</span>
                @if (currentBranch()) {
                  <span class="selector-sublabel">{{ currentBranch() }}</span>
                }
              </div>
            </div>
          </div>
        </div>

        <!-- Quick Actions -->
        <button
          type="button"
          class="header-icon-button"
          title="Quick create"
          (click)="showQuickCreate.set(!showQuickCreate())"
        >
          <span class="icon">+</span>
        </button>

        <!-- Notifications -->
        <button
          type="button"
          class="header-icon-button"
          title="Notifications"
          (click)="showNotifications.set(!showNotifications())"
        >
          <span class="icon">🔔</span>
          @if (unreadCount() > 0) {
            <span class="badge">{{ unreadCount() }}</span>
          }
        </button>

        @if (showNotifications()) {
          <div class="dropdown-menu dropdown-menu-wide">
            <div class="dropdown-header">
              <h3>Notifications</h3>
              @if (unreadCount() > 0) {
                <button type="button" class="text-link" (click)="markAllRead()">Mark all read</button>
              }
            </div>
            <div class="dropdown-section">
              @if (notifications().length === 0) {
                <div class="empty-notifications">
                  <span class="empty-icon">✓</span>
                  <p>No notifications.</p>
                </div>
              } @else {
                @for (notification of notifications(); track notification.notificationId) {
                  <button type="button" class="notification-item"
                    [class.notification-unread]="!notification.readAt"
                    [disabled]="!!notification.readAt"
                    [attr.aria-label]="notification.readAt ? notification.kind : 'Mark ' + notification.kind + ' as read'"
                    (click)="markNotificationRead(notification)">
                    <span class="notification-dot" aria-hidden="true"></span>
                    <span class="notification-content">
                      <span class="notification-title">{{ notification.kind }}</span>
                      <span class="notification-time">{{ notification.createdAt | date:'short' }}</span>
                    </span>
                  </button>
                }
              }
            </div>
          </div>
        }

        <!-- User Menu -->
        <button
          type="button"
          class="user-menu-button"
          (click)="showUserMenu.set(!showUserMenu())"
          [attr.aria-expanded]="showUserMenu()"
        >
          <div class="user-avatar">{{ userInitials() }}</div>
          <span class="user-name">{{ userName() }}</span>
        </button>

        @if (showUserMenu()) {
          <div class="dropdown-menu dropdown-menu-right">
            <div class="dropdown-section">
              <div class="user-info">
                <div class="user-info-avatar">{{ userInitials() }}</div>
                <div class="user-info-text">
                  <div class="user-info-name">{{ userName() }}</div>
                  <div class="user-info-email">{{ userEmail() }}</div>
                </div>
              </div>
            </div>
            <div class="dropdown-divider"></div>
            <div class="dropdown-section">
              <a routerLink="/m02/profile" class="dropdown-item">
                <span class="dropdown-icon">👤</span>
                Profile & Sessions
              </a>
              <a routerLink="/m02/mfa" class="dropdown-item">
                <span class="dropdown-icon">🔒</span>
                MFA Setup
              </a>
            </div>
            <div class="dropdown-divider"></div>
            <div class="dropdown-section">
              <button type="button" class="dropdown-item" (click)="logout()">
                <span class="dropdown-icon">🚪</span>
                Sign Out
              </button>
            </div>
          </div>
        }
      </div>
    </header>
  `,
  styles: `
    .header {
      position: fixed;
      top: 0;
      left: var(--sidebar-width);
      right: 0;
      height: var(--height-header);
      background: var(--surface);
      border-bottom: 1px solid var(--border-default);
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 var(--space-5);
      z-index: var(--z-sticky);
      transition: left var(--transition-base);
      box-shadow: 0 1px 3px rgba(31, 41, 51, 0.04);
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: var(--space-4);
      flex: 1;
    }

    .mobile-menu-toggle {
      display: none;
      background: none;
      border: none;
      font-size: 1.25rem;
      color: var(--text-primary);
      cursor: pointer;
      padding: var(--space-2);
    }

    .breadcrumbs ol {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      list-style: none;
      margin: 0;
      padding: 0;
      font-size: var(--font-size-sm);
    }

    .breadcrumbs li {
      display: flex;
      align-items: center;
      color: var(--text-secondary);
    }

    .breadcrumbs li:not(:last-child)::after {
      content: '/';
      margin-left: var(--space-2);
      color: var(--text-disabled);
    }

    .breadcrumbs a {
      color: var(--text-secondary);
      text-decoration: none;
      transition: color var(--transition-fast);
    }

    .breadcrumbs a:hover {
      color: var(--primary-copper);
    }

    .breadcrumbs li[aria-current="page"] {
      color: var(--text-heading);
      font-weight: var(--font-weight-medium);
    }

    .header-right {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      position: relative;
    }

    .tenant-selector {
      position: relative;
    }

    .selector-button {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-3);
      background: var(--canvas);
      border: 1px solid var(--border-default);
      border-radius: var(--radius-md);
      cursor: pointer;
      transition: all var(--transition-fast);
      font-size: var(--font-size-sm);
      min-width: 160px;
    }

    .selector-button:hover {
      border-color: var(--border-medium);
      background: var(--surface-hover);
    }

    .selector-content {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      flex: 1;
    }

    .selector-icon {
      font-size: 1rem;
    }

    .selector-text {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      text-align: left;
    }

    .selector-label {
      font-weight: var(--font-weight-medium);
      color: var(--text-heading);
      font-size: var(--font-size-sm);
      line-height: 1.3;
    }

    .selector-sublabel {
      font-size: var(--font-size-xs);
      color: var(--text-muted);
      line-height: 1.3;
    }

    .selector-chevron {
      font-size: var(--font-size-xs);
      color: var(--text-muted);
    }

    .header-icon-button {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      background: none;
      border: none;
      border-radius: var(--radius-md);
      cursor: pointer;
      transition: background var(--transition-fast);
      font-size: 1.125rem;
    }

    .header-icon-button:hover {
      background: var(--canvas);
    }

    .badge {
      position: absolute;
      top: 2px;
      right: 2px;
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 16px;
      height: 16px;
      padding: 0 var(--space-1);
      background: var(--danger);
      color: var(--text-inverse);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      border-radius: var(--radius-full);
      border: 2px solid var(--surface);
    }

    .user-menu-button {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-1);
      background: none;
      border: none;
      border-radius: var(--radius-md);
      cursor: pointer;
      transition: background var(--transition-fast);
    }

    .user-menu-button:hover {
      background: var(--canvas);
    }

    .user-avatar {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      background: var(--primary-copper);
      color: var(--text-inverse);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      border-radius: 50%;
    }

    .user-name {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--text-heading);
    }

    .dropdown-menu {
      position: absolute;
      top: calc(100% + var(--space-2));
      right: 0;
      min-width: 220px;
      background: var(--surface);
      border: 1px solid var(--border-default);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-lg);
      z-index: var(--z-dropdown);
      overflow: hidden;
    }

    .dropdown-menu-wide {
      min-width: 300px;
    }

    .dropdown-menu-right {
      right: 0;
    }

    .dropdown-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: var(--space-3) var(--space-4);
      border-bottom: 1px solid var(--border-light);
    }

    .dropdown-header h3 {
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
      margin: 0;
    }

    .dropdown-section {
      padding: var(--space-2);
    }

    .dropdown-label {
      padding: var(--space-2) var(--space-3);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      color: var(--text-muted);
      text-transform: uppercase;
    }

    .dropdown-item {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      width: 100%;
      padding: var(--space-2) var(--space-3);
      background: none;
      border: none;
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      color: var(--text-primary);
      text-align: left;
      text-decoration: none;
      cursor: pointer;
      transition: background var(--transition-fast);
    }

    .dropdown-item:hover {
      background: var(--canvas);
    }

    .dropdown-icon {
      font-size: 1rem;
    }

    .dropdown-divider {
      height: 1px;
      background: var(--border-light);
      margin: var(--space-2) 0;
    }

    .user-info {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-2) var(--space-3);
    }

    .user-info-avatar {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      background: var(--primary-copper);
      color: var(--text-inverse);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-semibold);
      border-radius: 50%;
      flex-shrink: 0;
    }

    .user-info-text {
      flex: 1;
      min-width: 0;
    }

    .user-info-name {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-semibold);
      color: var(--text-heading);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .user-info-email {
      font-size: var(--font-size-xs);
      color: var(--text-muted);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .empty-notifications {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-6);
      color: var(--text-muted);
    }

    .empty-icon {
      font-size: 1.75rem;
      opacity: 0.5;
    }

    .notification-item {
      display: flex;
      width: 100%;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-3);
      border-radius: var(--radius-md);
      border: 0;
      background: transparent;
      color: inherit;
      font: inherit;
      text-align: left;
      cursor: pointer;
      transition: background var(--transition-fast);
    }

    .notification-item:disabled {
      cursor: default;
    }

    .notification-item:hover {
      background: var(--canvas);
    }

    .notification-item:disabled:hover {
      background: transparent;
    }

    .notification-dot {
      display: none;
      width: 6px;
      height: 6px;
      background: var(--primary-copper);
      border-radius: 50%;
      margin-top: var(--space-1);
      flex-shrink: 0;
    }

    .notification-unread .notification-dot {
      display: block;
    }

    .notification-content {
      flex: 1;
      display: flex;
      flex-direction: column;
    }

    .notification-title {
      font-size: var(--font-size-sm);
      color: var(--text-heading);
      margin: 0 0 var(--space-1);
      line-height: 1.4;
    }

    .notification-time {
      font-size: var(--font-size-xs);
      color: var(--text-muted);
      margin: 0;
    }

    .text-link {
      background: none;
      border: none;
      color: var(--primary-copper);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      cursor: pointer;
      padding: 0;
    }

    .text-link:hover {
      color: var(--primary-copper-hover);
      text-decoration: underline;
    }

    @media (max-width: 768px) {
      .header {
        left: 0;
      }

      .mobile-menu-toggle {
        display: block;
      }

      .breadcrumbs {
        display: none;
      }

      .user-name {
        display: none;
      }

      .selector-button {
        min-width: auto;
      }

      .selector-text {
        display: none;
      }
    }
  `,
})
export class HeaderComponent implements OnInit {
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly api = inject(ApiClient);

  currentTenant = signal('Workspace');
  currentBranch = signal('');
  currentPage = signal('Dashboard');
  userName = signal('Account');
  userEmail = signal('');
  userInitials = signal('A');
  notifications = signal<HeaderNotification[]>([]);
  unreadCount = computed(() => this.notifications().filter((item) => !item.readAt).length);

  showQuickCreate = signal(false);
  showNotifications = signal(false);
  showUserMenu = signal(false);

  ngOnInit(): void {
    this.api.get<HeaderContext>('m02/context').subscribe({
      next: (context) => {
        this.currentTenant.set(context.tenantName);
        this.currentBranch.set(context.scopeTypes.length
          ? `Role-defined scope: ${context.scopeTypes.join(', ')}`
          : 'Access scope is enforced by your membership');
        const displayName = context.displayName?.trim() || context.email;
        this.userName.set(displayName || 'Account');
        this.userEmail.set(context.email || '');
        this.userInitials.set(displayName
          .split(/\s+/)
          .filter(Boolean)
          .slice(0, 2)
          .map((part) => part[0].toUpperCase())
          .join('') || 'A');
      },
      error: () => undefined,
    });
    this.loadNotifications();
  }

  toggleMobileMenu(): void {
    // Emit event for parent to handle mobile menu
  }

  logout(): void {
    this.api.post<void, Record<string, never>>('m02/logout', {}).subscribe({
      next: () => this.finishLogout(),
      error: () => this.finishLogout(),
    });
  }

  markNotificationRead(notification: HeaderNotification): void {
    if (notification.readAt) return;
    this.api.post<void, Record<string, never>>(
      `m02/notifications/${notification.notificationId}/read`, {},
    ).subscribe({
      next: () => this.notifications.update((items) => items.map((item) =>
        item.notificationId === notification.notificationId
          ? { ...item, readAt: new Date().toISOString() }
          : item)),
    });
  }

  markAllRead(): void {
    this.notifications().filter((item) => !item.readAt)
      .forEach((notification) => this.markNotificationRead(notification));
  }

  private loadNotifications(): void {
    this.api.get<HeaderNotification[]>('m02/notifications').subscribe({
      next: (items) => this.notifications.set(items),
      error: () => this.notifications.set([]),
    });
  }

  private finishLogout(): void {
    this.authSession.clear();
    void this.router.navigate(['/login']);
  }
}
