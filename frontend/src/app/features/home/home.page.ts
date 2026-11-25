import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';
import { CardComponent } from '../../shared/ui/card/card.component';
import { StatusChipComponent } from '../../shared/ui/status-chip/status-chip.component';

interface DashboardMetrics {
  tenantSetup: {
    companyConfigured: boolean;
    branchesCount: number;
    usersCount: number;
    setupProgress: number;
  };
  approvals: {
    pending: number;
    overdue: number;
  };
  masters: {
    customers: number;
    suppliers: number;
    products: number;
    priceLists: number;
    vehicles: number;
  };
  fleet: {
    expiringSoon: number;
    expired: number;
  };
  recentActivity: Array<{
    type: string;
    description: string;
    timestamp: string;
    user: string;
  }>;
}

@Component({
  selector: 'app-home-page',
  imports: [CommonModule, RouterLink, CardComponent, StatusChipComponent],
  template: `
    <div class="dashboard-page">
      <div class="page-container">
        <!-- Page Header -->
        <div class="page-header">
          <div class="page-title">
            <h1>Dashboard</h1>
            <p class="text-secondary text-sm">Construction operations overview</p>
          </div>
          <div class="page-actions">
            <button type="button" class="quick-action-btn">
              <span class="quick-action-icon">+</span>
              Quick Create
            </button>
          </div>
        </div>

        @if (loading) {
          <div class="loading-container">
            <div class="loading-spinner"></div>
            <p>Loading dashboard...</p>
          </div>
        } @else if (error) {
          <div class="notice notice-error">
            <span class="notice-icon">⚠</span>
            <div class="notice-content">{{ error }}</div>
          </div>
        } @else {
          <!-- Metrics Grid -->
          <div class="metrics-grid">
            <!-- Tenant Setup Status -->
            <app-card [elevated]="true">
              <div class="metric-card">
                <div class="metric-header">
                  <span class="metric-icon">🏢</span>
                  <h3 class="metric-title">Tenant Setup</h3>
                </div>
                <div class="metric-body">
                  <div class="metric-primary">
                    <span class="metric-value">{{ metrics.tenantSetup.setupProgress }}%</span>
                    <span class="metric-label">Complete</span>
                  </div>
                  <div class="metric-details">
                    <div class="metric-detail-row">
                      <span class="text-muted text-sm">Company</span>
                      <app-status-chip [status]="metrics.tenantSetup.companyConfigured ? 'completed' : 'pending'">
                        {{ metrics.tenantSetup.companyConfigured ? 'Configured' : 'Pending' }}
                      </app-status-chip>
                    </div>
                    <div class="metric-detail-row">
                      <span class="text-muted text-sm">Branches</span>
                      <span class="font-medium">{{ metrics.tenantSetup.branchesCount }}</span>
                    </div>
                    <div class="metric-detail-row">
                      <span class="text-muted text-sm">Users</span>
                      <span class="font-medium">{{ metrics.tenantSetup.usersCount }}</span>
                    </div>
                  </div>
                </div>
                <div class="metric-footer">
                  <a routerLink="/setup/home" class="metric-link">Complete setup →</a>
                </div>
              </div>
            </app-card>

            <!-- Pending Approvals -->
            <app-card [elevated]="true">
              <div class="metric-card">
                <div class="metric-header">
                  <span class="metric-icon">✓</span>
                  <h3 class="metric-title">Approvals</h3>
                </div>
                <div class="metric-body">
                  <div class="metric-primary">
                    <span class="metric-value">{{ metrics.approvals.pending }}</span>
                    <span class="metric-label">Pending</span>
                  </div>
                  @if (metrics.approvals.overdue > 0) {
                    <div class="metric-alert">
                      <span class="alert-icon">⚠</span>
                      <span class="text-sm">{{ metrics.approvals.overdue }} overdue</span>
                    </div>
                  }
                </div>
                <div class="metric-footer">
                  <a routerLink="/m02/approvals" class="metric-link">Review approvals →</a>
                </div>
              </div>
            </app-card>

            <!-- Customers -->
            <app-card [elevated]="true">
              <div class="metric-card">
                <div class="metric-header">
                  <span class="metric-icon">🏗️</span>
                  <h3 class="metric-title">Customers</h3>
                </div>
                <div class="metric-body">
                  <div class="metric-primary">
                    <span class="metric-value">{{ metrics.masters.customers }}</span>
                    <span class="metric-label">Active</span>
                  </div>
                </div>
                <div class="metric-footer">
                  <a routerLink="/m03/customers" class="metric-link">View customers →</a>
                </div>
              </div>
            </app-card>

            <!-- Suppliers -->
            <app-card [elevated]="true">
              <div class="metric-card">
                <div class="metric-header">
                  <span class="metric-icon">🚚</span>
                  <h3 class="metric-title">Suppliers</h3>
                </div>
                <div class="metric-body">
                  <div class="metric-primary">
                    <span class="metric-value">{{ metrics.masters.suppliers }}</span>
                    <span class="metric-label">Active</span>
                  </div>
                </div>
                <div class="metric-footer">
                  <a routerLink="/m03/suppliers" class="metric-link">View suppliers →</a>
                </div>
              </div>
            </app-card>
          </div>

          <!-- Secondary Metrics -->
          <div class="secondary-metrics">
            <!-- Products & Pricing -->
            <app-card title="Products & Pricing" [elevated]="true">
              <div class="stats-grid">
                <div class="stat-item">
                  <span class="stat-value">{{ metrics.masters.products }}</span>
                  <span class="stat-label">Products</span>
                  <a routerLink="/m03/products" class="stat-link">View</a>
                </div>
                <div class="stat-item">
                  <span class="stat-value">{{ metrics.masters.priceLists }}</span>
                  <span class="stat-label">Price Lists</span>
                  <a routerLink="/m03/price-lists" class="stat-link">View</a>
                </div>
              </div>
            </app-card>

            <!-- Fleet Status -->
            <app-card title="Fleet Compliance" [elevated]="true">
              <div class="stats-grid">
                <div class="stat-item">
                  <span class="stat-value">{{ metrics.masters.vehicles }}</span>
                  <span class="stat-label">Total Vehicles</span>
                  <a routerLink="/m03/vehicles" class="stat-link">View</a>
                </div>
                <div class="stat-item" [class.stat-warning]="metrics.fleet.expiringSoon > 0">
                  <span class="stat-value">{{ metrics.fleet.expiringSoon }}</span>
                  <span class="stat-label">Expiring Soon</span>
                  <a routerLink="/m03/fleet-compliance" class="stat-link">Review</a>
                </div>
                @if (metrics.fleet.expired > 0) {
                  <div class="stat-item stat-danger">
                    <span class="stat-value">{{ metrics.fleet.expired }}</span>
                    <span class="stat-label">Expired</span>
                    <a routerLink="/m03/fleet-compliance" class="stat-link">Fix Now</a>
                  </div>
                }
              </div>
            </app-card>
          </div>

          <!-- Recent Activity -->
          <app-card title="Recent Activity" [elevated]="true">
            @if (metrics.recentActivity.length === 0) {
              <div class="empty-activity">
                <span class="empty-icon">📋</span>
                <p class="text-muted">No recent activity</p>
              </div>
            } @else {
              <div class="activity-list">
                @for (activity of metrics.recentActivity; track activity.timestamp) {
                  <div class="activity-item">
                    <div class="activity-icon">
                      {{ getActivityIcon(activity.type) }}
                    </div>
                    <div class="activity-content">
                      <p class="activity-description">{{ activity.description }}</p>
                      <p class="activity-meta">
                        <span class="activity-user">{{ activity.user }}</span>
                        <span class="activity-time">{{ formatTimestamp(activity.timestamp) }}</span>
                      </p>
                    </div>
                  </div>
                }
              </div>
            }
          </app-card>
        }
      </div>
    </div>
  `,
  styles: `
    .dashboard-page {
      min-height: 100%;
      padding: var(--page-padding);
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      gap: var(--space-5);
      margin-bottom: var(--space-6);
    }

    .metric-card {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
    }

    .metric-header {
      display: flex;
      align-items: center;
      gap: var(--space-3);
    }

    .metric-icon {
      font-size: 1.5rem;
    }

    .metric-title {
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
      color: var(--text-primary);
      margin: 0;
    }

    .metric-body {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
      flex: 1;
    }

    .metric-primary {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
    }

    .metric-value {
      font-size: 2rem;
      font-weight: var(--font-weight-bold);
      color: var(--text-primary);
      line-height: 1;
    }

    .metric-label {
      font-size: var(--font-size-sm);
      color: var(--text-muted);
    }

    .metric-details {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
    }

    .metric-detail-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .metric-alert {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-3);
      background: var(--warning-light);
      border-radius: var(--radius-md);
      color: var(--warning);
    }

    .alert-icon {
      font-size: 1rem;
    }

    .metric-footer {
      padding-top: var(--space-3);
      border-top: 1px solid var(--border-light);
    }

    .metric-link {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--action-blue);
      text-decoration: none;
      transition: color var(--transition-fast);
    }

    .metric-link:hover {
      color: var(--action-blue-dark);
    }

    .secondary-metrics {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: var(--space-5);
      margin-bottom: var(--space-6);
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
      gap: var(--space-4);
    }

    .stat-item {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
      padding: var(--space-3);
      background: var(--canvas);
      border-radius: var(--radius-md);
    }

    .stat-value {
      font-size: var(--font-size-2xl);
      font-weight: var(--font-weight-bold);
      color: var(--text-primary);
    }

    .stat-label {
      font-size: var(--font-size-xs);
      color: var(--text-muted);
    }

    .stat-link {
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      color: var(--action-blue);
      text-decoration: none;
      margin-top: var(--space-1);
    }

    .stat-warning {
      background: var(--warning-light);
    }

    .stat-warning .stat-value {
      color: var(--warning);
    }

    .stat-danger {
      background: var(--danger-light);
    }

    .stat-danger .stat-value {
      color: var(--danger);
    }

    .quick-action-btn {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-4);
      height: var(--height-button);
      background: var(--action-blue);
      color: var(--text-inverse);
      border: none;
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      cursor: pointer;
      transition: background var(--transition-fast);
    }

    .quick-action-btn:hover {
      background: var(--action-blue-dark);
    }

    .quick-action-icon {
      font-size: 1.25rem;
      line-height: 1;
    }

    .activity-list {
      display: flex;
      flex-direction: column;
    }

    .activity-item {
      display: flex;
      gap: var(--space-3);
      padding: var(--space-3);
      border-bottom: 1px solid var(--border-light);
      transition: background var(--transition-fast);
    }

    .activity-item:last-child {
      border-bottom: none;
    }

    .activity-item:hover {
      background: var(--surface-hover);
    }

    .activity-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      background: var(--canvas);
      border-radius: var(--radius-md);
      font-size: 1.125rem;
      flex-shrink: 0;
    }

    .activity-content {
      flex: 1;
      min-width: 0;
    }

    .activity-description {
      font-size: var(--font-size-sm);
      color: var(--text-primary);
      margin: 0 0 var(--space-1);
    }

    .activity-meta {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-xs);
      color: var(--text-muted);
      margin: 0;
    }

    .activity-user {
      font-weight: var(--font-weight-medium);
    }

    .activity-time::before {
      content: '•';
      margin-right: var(--space-2);
    }

    .empty-activity {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-12);
    }

    .empty-icon {
      font-size: 3rem;
      opacity: 0.3;
    }

    @media (max-width: 768px) {
      .metrics-grid,
      .secondary-metrics {
        grid-template-columns: 1fr;
      }

      .stats-grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class HomePage implements OnInit {
  private api = inject(ApiClient);

  loading = true;
  error = '';
  metrics: DashboardMetrics = {
    tenantSetup: {
      companyConfigured: false,
      branchesCount: 0,
      usersCount: 0,
      setupProgress: 0,
    },
    approvals: {
      pending: 0,
      overdue: 0,
    },
    masters: {
      customers: 0,
      suppliers: 0,
      products: 0,
      priceLists: 0,
      vehicles: 0,
    },
    fleet: {
      expiringSoon: 0,
      expired: 0,
    },
    recentActivity: [],
  };

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    // Simulate loading dashboard data
    setTimeout(() => {
      this.metrics = {
        tenantSetup: {
          companyConfigured: true,
          branchesCount: 3,
          usersCount: 12,
          setupProgress: 85,
        },
        approvals: {
          pending: 5,
          overdue: 2,
        },
        masters: {
          customers: 47,
          suppliers: 23,
          products: 156,
          priceLists: 8,
          vehicles: 24,
        },
        fleet: {
          expiringSoon: 3,
          expired: 1,
        },
        recentActivity: [
          {
            type: 'user',
            description: 'New user invitation sent to rajesh@example.com',
            timestamp: new Date(Date.now() - 300000).toISOString(),
            user: 'Admin',
          },
          {
            type: 'customer',
            description: 'Customer "Sharma Builders" created',
            timestamp: new Date(Date.now() - 3600000).toISOString(),
            user: 'Sales Team',
          },
          {
            type: 'product',
            description: 'Price list "RMC Q4 2024" updated',
            timestamp: new Date(Date.now() - 7200000).toISOString(),
            user: 'Pricing Manager',
          },
        ],
      };
      this.loading = false;
    }, 500);
  }

  getActivityIcon(type: string): string {
    const icons: Record<string, string> = {
      user: '👤',
      customer: '🏗️',
      supplier: '🚚',
      product: '📦',
      approval: '✓',
      fleet: '🚛',
    };
    return icons[type] || '📄';
  }

  formatTimestamp(timestamp: string): string {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} min ago`;
    
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
  }
}
