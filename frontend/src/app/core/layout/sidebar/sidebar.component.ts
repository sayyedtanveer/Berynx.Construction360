import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ApiClient } from '../../api/api-client.service';

interface NavItem {
  label: string;
  icon?: string;
  path?: string;
  children?: NavItem[];
  permission?: string;
  module?: string;
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <aside [class.sidebar-collapsed]="collapsed()" class="sidebar">
      <div class="sidebar-header">
        <button
          type="button"
          class="sidebar-toggle"
          (click)="toggleSidebar()"
          [attr.aria-label]="collapsed() ? 'Expand sidebar' : 'Collapse sidebar'"
        >
          @if (collapsed()) {
            <span class="brand-icon">BC</span>
          } @else {
            <div class="brand-full">
              <span class="brand-icon">BC</span>
              <span class="brand-text">Berynx C360</span>
            </div>
          }
        </button>
      </div>

      <nav class="sidebar-nav" aria-label="Main navigation">
        @for (group of navGroups; track group.label) {
          @if (visibleItems(group).length) {
            @if (!collapsed() && group.label) {
              <div class="nav-group-label">{{ group.label }}</div>
            }

            @for (item of visibleItems(group); track item.label) {
              @if (item.children) {
              <!-- Parent with children -->
              <div class="nav-parent">
                <button
                  type="button"
                  class="nav-link"
                  [class.nav-link-active]="isGroupActive(item.children)"
                  (click)="toggleGroup(item.label)"
                  [attr.aria-expanded]="expandedGroups.has(item.label)"
                >
                  <span class="nav-icon" [attr.aria-hidden]="true">{{ item.icon }}</span>
                  @if (!collapsed()) {
                    <span class="nav-label">{{ item.label }}</span>
                    <span class="nav-chevron" [class.nav-chevron-open]="expandedGroups.has(item.label)">›</span>
                  }
                </button>
                
                @if (expandedGroups.has(item.label) && !collapsed()) {
                  <div class="nav-children">
                    @for (child of visibleChildren(item); track child.label) {
                      <a
                        [routerLink]="child.path"
                        routerLinkActive="nav-link-active"
                        class="nav-link nav-link-child"
                        [attr.title]="child.label"
                      >
                        <span class="nav-child-dot"></span>
                        <span class="nav-label">{{ child.label }}</span>
                      </a>
                    }
                  </div>
                }
              </div>
              } @else {
              <!-- Simple link -->
              <a
                [routerLink]="item.path"
                routerLinkActive="nav-link-active"
                class="nav-link"
                [attr.title]="item.label"
              >
                <span class="nav-icon" [attr.aria-hidden]="true">{{ item.icon }}</span>
                @if (!collapsed()) {
                  <span class="nav-label">{{ item.label }}</span>
                }
              </a>
              }
            }
          }
        }
      </nav>

      @if (!collapsed()) {
        <div class="sidebar-footer">
          <div class="sidebar-footer-content">
            <span class="footer-text">v1.0.0</span>
          </div>
        </div>
      }
    </aside>
  `,
  styles: `
    .sidebar {
      position: fixed;
      left: 0;
      top: 0;
      bottom: 0;
      width: var(--sidebar-width);
      background: var(--sidebar-bg);
      border-right: 1px solid var(--sidebar-border);
      display: flex;
      flex-direction: column;
      transition: width var(--transition-base);
      z-index: var(--z-fixed);
      box-shadow: 2px 0 8px rgba(0, 0, 0, 0.08);
    }

    .sidebar-collapsed {
      width: var(--sidebar-collapsed);
    }

    .sidebar-header {
      position: relative;
      height: var(--height-header);
      display: flex;
      align-items: center;
      padding: 0 var(--space-4);
      border-bottom: 1px solid var(--sidebar-border);
      flex-shrink: 0;
      background: rgba(0, 0, 0, 0.1);
    }

    .sidebar-toggle {
      width: 100%;
      background: none;
      border: none;
      padding: 0;
      cursor: pointer;
      transition: opacity var(--transition-fast);
    }

    .sidebar-toggle:hover {
      opacity: 0.85;
    }

    .brand-full {
      display: flex;
      align-items: center;
      gap: var(--space-3);
    }

    .brand-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      background: var(--primary-copper);
      color: var(--text-inverse);
      font-size: 0.6875rem;
      font-weight: var(--font-weight-bold);
      border-radius: var(--radius-md);
      letter-spacing: 0.05em;
      flex-shrink: 0;
      box-shadow: 0 2px 8px rgba(196, 106, 54, 0.3);
    }

    .brand-text {
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
      color: var(--text-inverse);
    }

    .sidebar-nav {
      position: relative;
      flex: 1;
      overflow-y: auto;
      overflow-x: hidden;
      padding: var(--space-2) 0;
    }

    .nav-group-label {
      padding: var(--space-3) var(--space-4) var(--space-2);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      color: rgba(255, 255, 255, 0.4);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .nav-link {
      position: relative;
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-2) var(--space-4);
      margin: 0 var(--space-2);
      color: rgba(255, 255, 255, 0.7);
      text-decoration: none;
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      border-radius: var(--radius-md);
      transition: all var(--transition-fast);
      cursor: pointer;
      border: none;
      background: none;
      width: calc(100% - var(--space-4));
      text-align: left;
    }

    .sidebar-collapsed .nav-link {
      justify-content: center;
      padding: var(--space-2);
    }

    .nav-link:hover {
      background: var(--sidebar-hover);
      color: rgba(255, 255, 255, 0.95);
    }

    .nav-link-active {
      background: var(--primary-copper);
      color: var(--text-inverse);
      box-shadow: 0 2px 6px rgba(196, 106, 54, 0.3);
    }

    .nav-link-active:hover {
      background: var(--primary-copper-hover);
      color: var(--text-inverse);
    }

    .nav-icon {
      position: relative;
      z-index: 1;
      font-size: 1.125rem;
      flex-shrink: 0;
      width: 18px;
      text-align: center;
    }

    .nav-label {
      position: relative;
      z-index: 1;
      flex: 1;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .nav-chevron {
      position: relative;
      z-index: 1;
      font-size: var(--font-size-base);
      transition: transform var(--transition-fast);
      flex-shrink: 0;
    }

    .nav-chevron-open {
      transform: rotate(90deg);
    }

    .nav-children {
      display: flex;
      flex-direction: column;
      margin-left: var(--space-3);
    }

    .nav-link-child {
      padding-left: var(--space-8);
      font-size: var(--font-size-sm);
    }

    .nav-child-dot {
      position: relative;
      z-index: 1;
      width: 4px;
      height: 4px;
      border-radius: 50%;
      background: currentColor;
      flex-shrink: 0;
    }

    .sidebar-footer {
      position: relative;
      padding: var(--space-3);
      border-top: 1px solid var(--sidebar-border);
      flex-shrink: 0;
      background: rgba(0, 0, 0, 0.15);
    }

    .sidebar-footer-content {
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .footer-text {
      font-size: var(--font-size-xs);
      color: rgba(255, 255, 255, 0.35);
    }

    /* Scrollbar styling for sidebar */
    .sidebar-nav::-webkit-scrollbar {
      width: 6px;
    }

    .sidebar-nav::-webkit-scrollbar-track {
      background: rgba(255, 255, 255, 0.03);
    }

    .sidebar-nav::-webkit-scrollbar-thumb {
      background: rgba(255, 255, 255, 0.15);
      border-radius: 3px;
    }

    .sidebar-nav::-webkit-scrollbar-thumb:hover {
      background: rgba(255, 255, 255, 0.25);
    }

    /* Mobile */
    @media (max-width: 768px) {
      .sidebar {
        transform: translateX(-100%);
      }

      .sidebar.sidebar-open {
        transform: translateX(0);
      }
    }
  `,
})
export class SidebarComponent implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly allowedPermissions = signal<ReadonlySet<string> | null>(null);
  private readonly enabledModules = signal<ReadonlySet<string> | null>(null);
  
  collapsed = signal(false);
  expandedGroups = new Set<string>(['Sell', 'Masters']);

  navGroups: NavGroup[] = [
    {
      label: '',
      items: [
        { label: 'Dashboard', icon: '📊', path: '/' },
        { label: 'Operations dashboard', icon: '▦', path: '/m15/dashboard', permission: 'm15.dashboard.view', module: 'M15' },
        { label: 'Reports', icon: '▤', path: '/m16/reports', permission: 'm16.reports.view', module: 'M16' },
        { label: 'Promotion & Catalogue', icon: '▧', path: '/m17/promotion', permission: 'm17.catalogue.manage', module: 'M17' },
        { label: 'Quality & disputes', icon: 'Q', path: '/m19/quality', permission: 'm19.samples.view', module: 'M19' },
      ],
    },
    {
      label: 'Sell',
      items: [
        { label: 'Enquiries', icon: '◉', path: '/m04/enquiries', permission: 'm04.enquiries.view', module: 'M04' },
        { label: 'Quotations', icon: '▤', path: '/m05/quotations', permission: 'm05.quotations.view', module: 'M05' },
        { label: 'Sales orders', icon: '▣', path: '/m06/orders', permission: 'm06.orders.view', module: 'M06' },
        { label: 'Procurement', icon: '▱', path: '/m07/procurement', permission: 'm07.purchase_orders.view', module: 'M07' },
        { label: 'Dispatch board', icon: '🚚', path: '/m08/dispatch', permission: 'm08.dispatch.view', module: 'M08' },
        { label: 'Delivery challans', icon: '▤', path: '/m09/challans', permission: 'm09.challans.view', module: 'M09' },
        { label: 'Sales invoices', icon: '▣', path: '/m10/invoices', permission: 'm10.invoices.view', module: 'M10' },
        { label: 'Receipts', icon: '₹', path: '/m11/receipts', permission: 'm11.receipts.view', module: 'M11' },
        { label: 'Accounting', icon: '≋', path: '/m12/accounting', permission: 'm12.reports.view', module: 'M12' },
        { label: 'GST workspace', icon: '%', path: '/m13/gst', permission: 'm13.gst.view', module: 'M13' },
        { label: 'Expenses', icon: '−', path: '/m14/expenses', permission: 'm14.expenses.view', module: 'M14' },
      ],
    },
    {
      label: 'Platform',
      items: [
        { label: 'Tenants', icon: '🏢', path: '/setup/platform', module: 'PLATFORM' },
        { label: 'Plans & modules', icon: '⚙️', path: '/setup/plans', module: 'PLATFORM' },
      ],
    },
    {
      label: 'Organization',
      items: [
        { label: 'Company Setup', icon: '🏢', path: '/setup/home', module: 'M01' },
        { label: 'Branches & Plants', icon: '🏭', path: '/setup/branches', module: 'M01' },
      ],
    },
    {
      label: 'Access Control',
      items: [
        { label: 'Users', icon: '👥', path: '/m02/users', permission: 'users.view', module: 'M02' },
        { label: 'Roles & Permissions', icon: '🔐', path: '/m02/roles', permission: 'roles.view', module: 'M02' },
        { label: 'Approval Rules', icon: '✓', path: '/m02/approval-rules', permission: 'approvals.view', module: 'M02' },
        { label: 'My Approvals', icon: '📋', path: '/m02/approvals', permission: 'approvals.view', module: 'M02' },
        { label: 'Audit Log', icon: '📝', path: '/m02/audit', permission: 'audit.view', module: 'M02' },
      ],
    },
    {
      label: 'Masters',
      items: [
        {
          label: 'Customers',
          icon: '🏗️',
          module: 'M03',
          permission: 'masters.view',
          children: [
            { label: 'All Customers', path: '/m03/customers' },
            { label: 'Customer Sites', path: '/m03/sites' },
            { label: 'Contacts', path: '/m03/contacts' },
          ],
        },
        {
          label: 'Suppliers',
          icon: '🚚',
          module: 'M03',
          permission: 'masters.view',
          children: [
            { label: 'All Suppliers', path: '/m03/suppliers' },
            { label: 'Supplier Plants', path: '/m03/plants' },
            { label: 'Supplier Banks', path: '/m03/supplier-bank-accounts' },
          ],
        },
        { label: 'Products', icon: '📦', path: '/m03/products', module: 'M03', permission: 'masters.view' },
        {
          label: 'Pricing',
          icon: '💰',
          module: 'M03',
          permission: 'masters.view',
          children: [
            { label: 'Price Lists', path: '/m03/price-lists' },
            { label: 'Price Lines', path: '/m03/price-lines' },
            { label: 'RMC Rate Cards', path: '/m03/rmc-rate-cards' },
          ],
        },
        {
          label: 'Fleet',
          icon: '🚛',
          module: 'M03',
          permission: 'masters.view',
          children: [
            { label: 'Vehicles & Mixers', path: '/m03/vehicles' },
            { label: 'Drivers', path: '/m03/drivers' },
            { label: 'Pumps & Equipment', path: '/m03/equipment' },
            { label: 'Compliance', path: '/m03/fleet-compliance' },
          ],
        },
        {
          label: 'Configuration',
          icon: '⚙️',
          module: 'M03',
          permission: 'masters.view',
          children: [
            { label: 'Tax & HSN', path: '/m03/tax' },
            { label: 'Category Tax Overrides', path: '/m03/category-tax-overrides' },
            { label: 'Units & Conversions', path: '/m03/units' },
            { label: 'Payment Terms', path: '/m03/payment-terms' },
            { label: 'Charge Types', path: '/m03/charge-types' },
            { label: 'Bank Accounts', path: '/m03/bank-accounts' },
          ],
        },
      ],
    },
    {
      label: 'Account',
      items: [
        { label: 'Profile & Sessions', icon: '👤', path: '/m02/profile', module: 'M02' },
        { label: 'MFA Setup', icon: '🔒', path: '/m02/mfa', module: 'M02' },
      ],
    },
  ];

  ngOnInit(): void {
    this.api.get<{ permissions: string[]; modules: string[] }>('m02/context').subscribe({
      next: (navigation) => {
        this.allowedPermissions.set(new Set(navigation.permissions));
        this.enabledModules.set(new Set(navigation.modules));
      },
      error: () => {
        this.allowedPermissions.set(new Set());
        this.enabledModules.set(new Set());
      },
    });
  }

  visibleItems(group: NavGroup): NavItem[] {
    return group.items.filter((item) =>
      this.isVisible(item) && (!item.children || this.visibleChildren(item).length > 0));
  }

  visibleChildren(parent: NavItem): NavItem[] {
    return (parent.children ?? []).filter((child) => this.isVisible(child, parent));
  }

  private isVisible(item: NavItem, parent?: NavItem): boolean {
    const permission = item.permission ?? parent?.permission;
    const module = item.module ?? parent?.module;
    const permissions = this.allowedPermissions();
    const modules = this.enabledModules();
    return (!permission || permissions?.has(permission) === true) &&
      (!module || modules?.has(module) === true);
  }

  toggleSidebar(): void {
    this.collapsed.set(!this.collapsed());
  }

  toggleGroup(label: string): void {
    if (this.expandedGroups.has(label)) {
      this.expandedGroups.delete(label);
    } else {
      this.expandedGroups.add(label);
    }
  }

  isGroupActive(children: NavItem[]): boolean {
    // Check if any child is active based on current route
    return false; // Can be enhanced with Router
  }
}
