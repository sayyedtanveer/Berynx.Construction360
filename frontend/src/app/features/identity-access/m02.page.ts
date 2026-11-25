import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';

type Screen =
  | 'mfa'
  | 'security'
  | 'users'
  | 'roles'
  | 'approval-rules'
  | 'approvals'
  | 'profile'
  | 'audit';

interface UserRow {
  userId: string;
  email: string | null;
  mobile: string | null;
  displayName: string;
  status: string;
  inviteExpiresAt: string | null;
  invitationDeliveredAt: string | null;
  version: number;
  roles: Array<{ roleKey: string; name: string; scopeType: string; scopeId: string | null }>;
}

interface ScopeOption {
  id: string;
  name: string;
}

interface RoleRow {
  roleId: string;
  roleKey: string;
  name: string;
  description: string;
  isSystem: boolean;
  version: number;
  permissionCodes: string[];
}

interface PermissionRow {
  permissionCode: string;
  module: string;
  action: string;
  description: string;
}

interface ApprovalLevel {
  level: number;
  approverRoleId: string | null;
  approverUserId: string | null;
  slaMinutes: number;
  escalationRoleId: string | null;
  escalationUserId: string | null;
}

interface ApprovalRuleRow {
  ruleId: string;
  name: string;
  entityType: string;
  amountGreaterThan: number | null;
  discountPercentGreaterThan: number | null;
  marginPercentBelow: number | null;
  creditStatus: string | null;
  enabled: boolean;
  version: number;
  levels: ApprovalLevel[];
}

interface ApprovalRequestRow {
  requestId: string;
  sourceType: string;
  sourceId: string;
  requesterUserId: string;
  currentLevel: number;
  amount: number;
  discountPercent: number | null;
  marginPercent: number | null;
  dueAt: string;
  version: number;
  snapshot: string;
  history: Array<{ decision: string; comment: string | null; at: string }>;
}

interface OpenTask {
  provider: string;
  taskId: string;
  summary: string;
}

interface ProviderSession {
  id: string;
  device: string;
  ipAddress: string | null;
  createdAt: string;
  lastSeenAt: string;
  expiresAt: string | null;
  current: boolean;
}

interface M02Notification {
  notificationId: string;
  kind: string;
  payload: string;
  createdAt: string;
  readAt: string | null;
}

interface AuditEvent {
  eventId: string;
  at: string;
  actorSubject: string;
  action: string;
  entityType: string;
  entityId: string | null;
  before: string;
  after: string;
}

@Component({
  selector: 'app-m02-page',
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink],
  templateUrl: './m02.page.html',
  styleUrl: './m02.page.scss',
})
export class M02Page implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authSession = inject(AuthSessionService);
  readonly screen = this.route.snapshot.data['screen'] as Screen;
  readonly screenTitles: Record<Screen, string> = {
    mfa: 'Multi-factor authentication',
    security: 'Tenant security policy',
    users: 'Users',
    roles: 'Roles and permissions',
    'approval-rules': 'Approval rules',
    approvals: 'My approvals',
    profile: 'Profile and sessions',
    audit: 'Audit log',
  };

  loading = false;
  error = '';
  notice = '';
  users: UserRow[] = [];
  private readonly userPermissions = signal<ReadonlySet<string>>(new Set());
  branches: ScopeOption[] = [];
  plants: ScopeOption[] = [];
  roles: RoleRow[] = [];
  permissions: PermissionRow[] = [];
  rules: ApprovalRuleRow[] = [];
  approvals: ApprovalRequestRow[] = [];
  auditEvents: AuditEvent[] = [];
  sessions: ProviderSession[] = [];
  notifications: M02Notification[] = [];
  openTasks: OpenTask[] = [];
  mfaSetup: { provisioningUri: string; manualKey?: string } | null = null;
  currentTaskUser = '';
  selectedRole: RoleRow | null = null;
  selectedRule: ApprovalRuleRow | null = null;
  selectedPermissions = new Set<string>();
  userForm = { displayName: '', email: '', mobile: '' };
  roleAssignments: Array<{ roleKey: string; scopeType: string; scopeId: string }> = [];
  editingUser: UserRow | null = null;
  roleForm = { name: '', description: '' };
  ruleForm = {
    name: '',
    entityType: '',
    amountGreaterThan: null as number | null,
    discountPercentGreaterThan: null as number | null,
    marginPercentBelow: null as number | null,
    creditStatus: '',
    enabled: true,
  };
  ruleLevels: ApprovalLevel[] = [this.newLevel(1)];
  auditFilter = { userId: '', entity: '', action: '', from: '', to: '' };
  profile = { userId: '', email: '', mobile: '', displayName: '' };
  mfaCode = '';
  currentMfaCode = '';
  securityPolicy = {
    requireMfaForTenantAdmins: false,
    requireMfaForAccounts: false,
    version: 0,
  };
  recoveryCodes: string[] = [];
  delegateUserId = '';
  delegationStart = '';
  delegationEnd = '';
  rejectionComments = new Map<string, string>();
  taskOwners = new Map<string, string>();
  deactivatingUser: UserRow | null = null;

  ngOnInit(): void {
    if (this.screen === 'users') this.loadUsers();
    if (this.screen === 'roles') this.loadRoles();
    if (this.screen === 'approval-rules') this.loadRules();
    if (this.screen === 'approvals') this.loadApprovals();
    if (this.screen === 'profile') this.loadProfile();
    if (this.screen === 'audit') this.searchAudit();
    if (this.screen === 'security') this.loadSecurityPolicy();
  }

  get permissionModules(): string[] {
    return [...new Set(this.permissions.map((permission) => permission.module))].sort();
  }

  permissionsFor(module: string): PermissionRow[] {
    return this.permissions.filter((permission) => permission.module === module);
  }

  roleSummary(user: UserRow): string {
    return user.roles.map((role) => `${role.name} · ${role.scopeType}`).join(', ');
  }

  can(permissionCode: string): boolean {
    return this.userPermissions().has(permissionCode);
  }

  beginMfa(): void {
    this.run(this.api.post<{ provisioningUri: string; manualKey?: string }, Record<string, never>>(
      'm02/mfa/setup', {},
    ), (result) => { this.mfaSetup = result; });
  }

  confirmMfa(): void {
    this.run(this.api.post<{ recoveryCodes: string[] }, { code: string; currentCode: string | null }>(
      'm02/mfa/confirm', { code: this.mfaCode, currentCode: this.currentMfaCode || null },
    ), (result) => {
      this.recoveryCodes = result.recoveryCodes;
      this.notice = 'Authenticator verification completed. Save these recovery codes securely.';
      this.mfaCode = '';
      this.currentMfaCode = '';
      this.authSession.setAccessToken(null);
    });
  }

  loadSecurityPolicy(): void {
    this.loading = true;
    this.api.get<{ permissions: string[] }>('m02/context').subscribe({
      next: (context) => {
        this.userPermissions.set(new Set(context.permissions));
        this.api.get<typeof this.securityPolicy>('m02/security-policy').subscribe({
          next: (policy) => { this.securityPolicy = policy; this.loading = false; },
          error: (error) => this.fail(error),
        });
      },
      error: (error) => this.fail(error),
    });
  }

  saveSecurityPolicy(): void {
    this.run(this.api.put<typeof this.securityPolicy, typeof this.securityPolicy>(
      'm02/security-policy', this.securityPolicy,
    ), (policy) => {
      this.securityPolicy = policy;
      this.notice = 'Tenant MFA policy saved. New requirements are enforced on the next request.';
    });
  }

  loadUsers(): void {
    this.loading = true;
    this.error = '';
    this.api.get<{ permissions: string[] }>('m02/context').subscribe({
      next: (context) => { this.userPermissions.set(new Set(context.permissions)); },
      error: () => { this.userPermissions.set(new Set()); },
    });
    this.api.get<UserRow[]>('m02/users').subscribe({
      next: (users) => { this.users = users; this.loading = false; },
      error: (error) => this.fail(error),
    });
    this.api.get<{ roles: RoleRow[] }>('m02/roles').subscribe({
      next: (data) => {
        this.roles = data.roles;
        if (!this.roleAssignments.length && this.roles.length)
          this.roleAssignments = [{ roleKey: this.roles[0].roleKey, scopeType: 'All', scopeId: '' }];
      },
      error: () => undefined,
    });
    this.api.get<{ branches: ScopeOption[]; plants: ScopeOption[] }>('m02/scopes').subscribe({
      next: (scopes) => { this.branches = scopes.branches; this.plants = scopes.plants; },
      error: () => undefined,
    });
  }

  inviteUser(): void {
    if (!this.roleAssignments.length || this.roleAssignments.some((item) => !item.roleKey)) {
      this.error = 'Assign at least one role.';
      return;
    }
    const request = {
      email: this.userForm.email || null,
      mobile: this.userForm.mobile || null,
      displayName: this.userForm.displayName,
      roles: this.roleAssignments.map((item) => ({ ...item, scopeId: item.scopeId || null })),
    };
    const save = this.editingUser
      ? this.api.put(`m02/users/${this.editingUser.userId}/roles`, {
          roles: request.roles, version: this.editingUser.version,
        })
      : this.api.post('m02/users', request);
    this.run(save, () => {
      this.notice = 'Invitation queued. The link expires in 72 hours.';
      this.userForm = { displayName: '', email: '', mobile: '' };
      this.editingUser = null;
      this.loadUsers();
    });
  }

  editUser(user: UserRow): void {
    this.editingUser = user;
    this.userForm = { displayName: user.displayName, email: user.email ?? '', mobile: user.mobile ?? '' };
    this.roleAssignments = user.roles.map((role) => ({
      roleKey: role.roleKey, scopeType: role.scopeType, scopeId: role.scopeId ?? '',
    }));
  }

  addRoleAssignment(): void {
    this.roleAssignments = [...this.roleAssignments, {
      roleKey: this.roles[0]?.roleKey ?? '', scopeType: 'All', scopeId: '',
    }];
  }

  removeRoleAssignment(index: number): void {
    this.roleAssignments = this.roleAssignments.filter((_, itemIndex) => itemIndex !== index);
  }

  changeScope(assignment: { roleKey: string; scopeType: string; scopeId: string }, scopeType: string): void {
    assignment.scopeType = scopeType;
    assignment.scopeId = '';
  }

  resendInvite(user: UserRow): void {
    this.run(this.api.post<void, Record<string, never>>(`m02/users/${user.userId}/resend`, {}),
      () => { this.notice = 'A new invitation was queued.'; this.loadUsers(); });
  }

  revokeInvite(user: UserRow): void {
    this.run(this.api.post<void, Record<string, never>>(`m02/users/${user.userId}/revoke-invitation`, {}),
      () => { this.notice = 'Invitation revoked.'; this.loadUsers(); });
  }

  activateUser(user: UserRow): void {
    this.run(this.api.post<void, Record<string, never>>(`m02/users/${user.userId}/activate`, {}),
      () => { this.notice = 'User activated.'; this.loadUsers(); });
  }

  beginDeactivation(user: UserRow): void {
    this.deactivatingUser = user;
    this.currentTaskUser = user.userId;
    this.taskOwners.clear();
    this.run(this.api.get<OpenTask[]>(`m02/users/${user.userId}/open-tasks`),
      (tasks) => { this.openTasks = tasks; });
  }

  setTaskOwner(task: OpenTask, userId: string): void {
    this.taskOwners.set(`${task.provider}|${task.taskId}`, userId);
  }

  confirmDeactivation(): void {
    if (!this.deactivatingUser) return;
    const reassignments = this.openTasks.map((task) => ({
      provider: task.provider,
      taskId: task.taskId,
      newOwnerUserId: this.taskOwners.get(`${task.provider}|${task.taskId}`),
    }));
    if (reassignments.some((item) => !item.newOwnerUserId)) {
      this.error = 'Assign every open task before continuing.';
      return;
    }
    this.run(this.api.post<void, unknown>(`m02/users/${this.deactivatingUser.userId}/deactivate`, {
      version: this.deactivatingUser.version,
      reassignments,
    }), () => {
      this.notice = 'User deactivated and open tasks reassigned.';
      this.deactivatingUser = null;
      this.openTasks = [];
      this.loadUsers();
    });
  }

  loadRoles(): void {
    this.run(this.api.get<{ roles: RoleRow[]; permissions: PermissionRow[] }>('m02/roles'), (data) => {
      this.roles = data.roles;
      this.permissions = data.permissions;
      if (this.roles.length) this.selectRole(this.roles[0]);
    });
  }

  selectRole(role: RoleRow): void {
    this.selectedRole = role;
    this.roleForm = { name: role.name, description: role.description };
    this.selectedPermissions = new Set(role.permissionCodes);
  }

  startNewRole(): void {
    this.selectedRole = null;
    this.roleForm = { name: '', description: '' };
    this.selectedPermissions.clear();
  }

  togglePermission(code: string, checked: boolean): void {
    checked ? this.selectedPermissions.add(code) : this.selectedPermissions.delete(code);
  }

  saveRole(): void {
    const permissions = [...this.selectedPermissions];
    if (this.selectedRole) {
      this.run(this.api.put(`m02/roles/${this.selectedRole.roleId}`, {
        ...this.roleForm, permissions, version: this.selectedRole.version,
      }), () => { this.notice = 'Role permissions saved.'; this.loadRoles(); });
    } else {
      this.run(this.api.post('m02/roles', { ...this.roleForm, permissions, version: 0 }),
        () => { this.notice = 'Role created.'; this.loadRoles(); });
    }
  }

  cloneRole(role: RoleRow): void {
    this.run(this.api.post(`m02/roles/${role.roleId}/clone`, {
      name: `${role.name} copy`, description: role.description, permissions: role.permissionCodes,
      version: role.version,
    }), () => { this.notice = 'Role cloned. Edit the clone to tailor permissions.'; this.loadRoles(); });
  }

  deleteRole(role: RoleRow): void {
    this.run(this.api.delete(`m02/roles/${role.roleId}?version=${role.version}`),
      () => { this.notice = 'Role deleted.'; this.loadRoles(); });
  }

  loadRules(): void {
    this.loading = true;
    this.api.get<ApprovalRuleRow[]>('m02/approval-rules').subscribe({
      next: (rules) => { this.rules = rules; this.loading = false; },
      error: (error) => this.fail(error),
    });
    this.api.get<{ roles: RoleRow[] }>('m02/roles').subscribe({
      next: (data) => { this.roles = data.roles; },
      error: () => undefined,
    });
  }

  selectRule(rule: ApprovalRuleRow): void {
    this.selectedRule = rule;
    this.ruleForm = {
      name: rule.name,
      entityType: rule.entityType,
      amountGreaterThan: rule.amountGreaterThan,
      discountPercentGreaterThan: rule.discountPercentGreaterThan,
      marginPercentBelow: rule.marginPercentBelow,
      creditStatus: rule.creditStatus ?? '',
      enabled: rule.enabled,
    };
    this.ruleLevels = rule.levels.map((level) => ({ ...level }));
  }

  newRule(): void {
    this.selectedRule = null;
    this.ruleForm = {
      name: '', entityType: '', amountGreaterThan: null,
      discountPercentGreaterThan: null, marginPercentBelow: null, creditStatus: '', enabled: true,
    };
    this.ruleLevels = [this.newLevel(1)];
  }

  addApprovalLevel(): void {
    this.ruleLevels = [...this.ruleLevels, this.newLevel(this.ruleLevels.length + 1)];
  }

  removeApprovalLevel(index: number): void {
    if (this.ruleLevels.length <= 1) return;
    this.ruleLevels = this.ruleLevels.filter((_, itemIndex) => itemIndex !== index)
      .map((level, itemIndex) => ({ ...level, level: itemIndex + 1 }));
  }

  saveRule(): void {
    const request = {
      ...this.ruleForm,
      creditStatus: this.ruleForm.creditStatus || null,
      levels: this.ruleLevels,
      version: this.selectedRule?.version ?? 0,
    };
    if (this.selectedRule) {
      this.run(this.api.put(`m02/approval-rules/${this.selectedRule.ruleId}`, request),
        () => { this.notice = 'Approval rule saved.'; this.loadRules(); });
    } else {
      this.run(this.api.post('m02/approval-rules', request),
        () => { this.notice = 'Approval rule created.'; this.loadRules(); });
    }
  }

  disableRule(rule: ApprovalRuleRow): void {
    this.run(this.api.put(`m02/approval-rules/${rule.ruleId}`, {
      name: rule.name, entityType: rule.entityType, amountGreaterThan: rule.amountGreaterThan,
      discountPercentGreaterThan: rule.discountPercentGreaterThan,
      marginPercentBelow: rule.marginPercentBelow, creditStatus: rule.creditStatus,
      enabled: false, levels: rule.levels, version: rule.version,
    }), () => { this.notice = 'Approval rule disabled.'; this.loadRules(); });
  }

  loadApprovals(): void {
    this.loading = true;
    this.api.get<ApprovalRequestRow[]>('m02/approvals/inbox').subscribe({
      next: (rows) => { this.approvals = rows; this.loading = false; },
      error: (error) => this.fail(error),
    });
    this.api.get<UserRow[]>('m02/users').subscribe({
      next: (users) => { this.users = users.filter((user) => user.status === 'Active'); },
      error: () => undefined,
    });
    this.api.get<M02Notification[]>('m02/notifications').subscribe({
      next: (notifications) => { this.notifications = notifications; },
      error: () => undefined,
    });
  }

  markNotificationRead(notification: M02Notification): void {
    this.run(this.api.post<void, Record<string, never>>(
      `m02/notifications/${notification.notificationId}/read`, {},
    ), () => {
      notification.readAt = new Date().toISOString();
    });
  }

  decide(request: ApprovalRequestRow, decision: 'Approve' | 'Reject'): void {
    const comment = this.rejectionComments.get(request.requestId) ?? '';
    if (decision === 'Reject' && !comment.trim()) {
      this.error = 'A rejection reason is required.';
      return;
    }
    const decisionLabel = decision === 'Approve' ? 'approved' : 'rejected';
    this.run(this.api.post(`m02/approvals/${request.requestId}/decision`, {
      decision, comment: comment || null, version: request.version,
    }), () => { this.notice = `Request ${decisionLabel}.`; this.loadApprovals(); });
  }

  setRejectionComment(requestId: string, comment: string): void {
    this.rejectionComments.set(requestId, comment);
  }

  createDelegation(): void {
    if (!this.delegateUserId || !this.delegationStart || !this.delegationEnd) {
      this.error = 'Choose a delegate and both delegation dates.';
      return;
    }
    const startsAt = new Date(this.delegationStart);
    const endsAt = new Date(this.delegationEnd);
    if (Number.isNaN(startsAt.valueOf()) || Number.isNaN(endsAt.valueOf())) {
      this.error = 'Enter valid delegation dates.';
      return;
    }
    this.run(this.api.post('m02/delegations', {
      delegateUserId: this.delegateUserId,
      startsAt: startsAt.toISOString(),
      endsAt: endsAt.toISOString(),
    }), () => { this.notice = 'Delegation saved.'; });
  }

  loadProfile(): void {
    this.run(this.api.get<{ userId: string; email: string; mobile: string; displayName: string; sessions: ProviderSession[] }>(
      'm02/profile',
    ), (result) => {
      this.profile = {
        userId: result.userId, email: result.email, mobile: result.mobile, displayName: result.displayName,
      };
      this.sessions = result.sessions;
    });
  }

  saveProfile(): void {
    this.run(this.api.put('m02/profile', { displayName: this.profile.displayName }),
      () => { this.notice = 'Profile updated.'; this.loadProfile(); });
  }

  revokeSession(session: ProviderSession): void {
    this.run(this.api.delete(`m02/sessions/${encodeURIComponent(session.id)}`),
      () => { this.notice = 'Session revoked.'; this.loadProfile(); });
  }

  logout(): void {
    this.run(this.api.post<void, Record<string, never>>('m02/logout', {}),
      () => {
        this.authSession.clear();
        window.location.assign('/login');
      });
  }

  logoutAll(): void {
    this.run(this.api.post<void, Record<string, never>>('m02/logout-all', {}),
      () => {
        this.authSession.clear();
        window.location.assign('/login');
      });
  }

  searchAudit(): void {
    const params = new URLSearchParams();
    if (this.auditFilter.userId) params.set('userId', this.auditFilter.userId);
    if (this.auditFilter.entity) params.set('entity', this.auditFilter.entity);
    if (this.auditFilter.action) params.set('action', this.auditFilter.action);
    if (this.auditFilter.from) params.set('from', new Date(this.auditFilter.from).toISOString());
    if (this.auditFilter.to) params.set('to', new Date(this.auditFilter.to).toISOString());
    this.run(this.api.get<AuditEvent[]>(`m02/audit?${params.toString()}`),
      (events) => { this.auditEvents = events; });
  }

  exportAudit(): void {
    const params = new URLSearchParams();
    if (this.auditFilter.userId) params.set('userId', this.auditFilter.userId);
    if (this.auditFilter.entity) params.set('entity', this.auditFilter.entity);
    if (this.auditFilter.action) params.set('action', this.auditFilter.action);
    if (this.auditFilter.from) params.set('from', new Date(this.auditFilter.from).toISOString());
    if (this.auditFilter.to) params.set('to', new Date(this.auditFilter.to).toISOString());
    this.run(this.api.getBlob(`m02/audit/export?${params.toString()}`), (blob) => {
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'm02-audit.csv';
      anchor.click();
      URL.revokeObjectURL(url);
    });
  }

  private newLevel(level: number): ApprovalLevel {
    return {
      level, approverRoleId: this.roles[0]?.roleId ?? null, approverUserId: null,
      slaMinutes: 60, escalationRoleId: null, escalationUserId: null,
    };
  }

  private run<T>(request: import('rxjs').Observable<T>, success: (result: T) => void): void {
    this.loading = true;
    this.error = '';
    this.notice = '';
    request.subscribe({
      next: (result) => { this.loading = false; success(result); },
      error: (error) => this.fail(error),
    });
  }

  private fail(error: unknown): void {
    this.loading = false;
    this.error = error instanceof HttpErrorResponse
      ? error.status === 401
        ? 'Sign-in failed. Check your email and password, or contact your tenant administrator.'
        : error.error?.detail ?? 'The request could not be completed. Check access and try again.'
      : 'The request could not be completed.';
  }
}
