import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';
import { forms, Group, steps, titles } from './setup-fields';

type Row = Record<string, any>;
@Component({
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './setup.page.html',
  styleUrl: './setup.page.scss',
})
export class SetupPage implements OnInit {
  private api = inject(ApiClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private cd = inject(ChangeDetectorRef);
  page = this.route.snapshot.data['page'] as string;
  titles = titles;
  steps = steps;
  groups = forms[this.page] ?? [];
  model: Record<string, Row[]> = {};
  state: Row = {};
  session: Row = {};
  tenants: Row[] = [];
  plans: Row[] = [];
  modules: Row[] = [];
  addonModules: Row[] = [];
  tenantDetail: Row | null = null;
  tenantAudit: Row[] = [];
  selectedTenant: Row | null = null;
  assignment: Row = {};
  actionReason = '';
  actionEndsAt = '';
  planForm: Row = {
    planCode: '',
    name: '',
    description: '',
    maxUsers: null,
    maxStorageBytes: null,
    maxDocumentsPerMonth: null,
    maxMessagesPerMonth: null,
    maxApiRequestsPerMonth: null,
    trialDays: null,
    gracePeriodDays: 0,
    priceAmount: null,
    currency: 'INR',
    includedModules: [],
    version: 0,
  };
  requestId = crypto.randomUUID();
  loading = true;
  busy = false;
  error = '';
  notice = '';
  difference: number | null = null;
  previewErrors: string[] = [];
  pageNumber = 1;
  generation = 0;
  get locked() {
    return (
      ['Suspended', 'Cancelled'].includes(this.state['status']) ||
      (this.page === 'openings' && !!this.state['liveAt'])
    );
  }
  async ngOnInit() {
    await this.load();
  }
  async load() {
    this.loading = true;
    this.error = '';
    try {
      this.session = await firstValueFrom(this.api.get<Row>('auth/session'));
      if (this.page === 'home') {
        const destination = this.session['platform']
          ? 'platform'
          : this.session['admin']
            ? 'wizard'
            : this.session['accounts']
              ? 'openings'
              : null;
        if (destination) await this.router.navigateByUrl('/setup/' + destination);
        else throw Error('Your identity has no onboarding role.');
        return;
      }
      if (this.page === 'platform' || this.page === 'create' || this.page === 'plans') {
        if (!this.session['platform']) throw Error('Platform administrator access is required.');
        [this.plans, this.modules] = await Promise.all([
          firstValueFrom(this.api.get<Row[]>('platform/plans')),
          firstValueFrom(this.api.get<Row[]>('platform/modules')),
        ]);
        this.addonModules = this.modules.filter((module) => !module['isCore'] && module['isActive']);
        if (
          this.page === 'plans' &&
          !this.planForm['version'] &&
          !(this.planForm['includedModules'] as string[]).length
        ) {
          this.resetPlanForm();
        }
        if (this.page === 'create') {
          this.groups = (forms['create'] ?? []).map((group) => ({
            ...group,
            fields: group.fields.map((field) => ({
              ...field,
              options:
                field.key === 'plan'
                  ? this.plans.filter((plan) => plan['isActive']).map((plan) => plan['planCode'])
                  : field.key === 'addOnModules'
                    ? this.addonModules.map((module) => module['moduleCode'])
                    : field.options,
            })),
          }));
        }
        if (this.page === 'platform') {
          this.tenants = await firstValueFrom(
            this.api.get<Row[]>('platform/tenants?page=' + this.pageNumber),
          );
        }
      } else {
        if (!this.session['admin'] && !(this.page === 'openings' && this.session['accounts']))
          throw Error('You do not have permission to open this screen.');
        this.state = await firstValueFrom(
          this.api.get<Row>(
            this.page === 'usage'
              ? 'onboarding/usage'
              : this.page === 'openings'
                ? 'openings'
                : 'onboarding',
          ),
        );
      }
      const s = this.state['setup'] ?? {};
      for (const g of this.groups) {
        const data = g.key === 'rows' ? this.state['rows'] : s[g.key];
        this.model[g.key] = g.repeat
          ? g.key === 'registrations'
            ? (data ?? []).map((gstin: string) => ({ gstin }))
            : structuredClone(data ?? [])
          : [
              g.key === 'fiscal'
                ? { startMonth: s.startMonth ?? 4 }
                : g.key === 'invite'
                  ? { skip: s.skipInvite }
                  : structuredClone(data ?? {}),
            ];
      }
      if (this.page === 'create' && this.model['tenant']?.[0]) {
        this.model['tenant'][0]['addOnModules'] ??= [];
      }
      if (this.page === 'openings' || this.page === 'numbering') await this.preview();
    } catch (e) {
      this.fail(e);
    } finally {
      this.loading = false;
      this.cd.markForCheck();
    }
  }
  fail(e: any) {
    this.error =
      e?.error?.detail ??
      e?.error?.title ??
      (e?.status === 401
        ? 'Sign in through your organization to continue.'
        : (e?.message ?? 'Request failed.'));
  }
  add(g: Group) {
    const row: Row = {};
    for (const f of g.fields)
      row[f.key] = f.type === 'number' ? 0 : f.type === 'checkbox' ? false : (f.options?.[0] ?? '');
    if (['branches', 'plants'].includes(g.key)) row['id'] = crypto.randomUUID();
    this.model[g.key].push(row);
    this.changed();
  }
  changed() {
    if (this.page === 'openings' || this.page === 'numbering') void this.preview();
  }
  async preview() {
    const generation = ++this.generation;
    try {
      if (this.page === 'openings') {
        const result = await firstValueFrom(
          this.api.post<Row, Row[]>('openings/preview', this.payload() as Row[]),
        );
        if (generation === this.generation) {
          this.difference = result['difference'];
          this.previewErrors = result['errors'];
        }
      } else {
        await Promise.all(
          this.model['series'].map(async (row) => {
            try {
              const result = await firstValueFrom(
                this.api.post<Row, Row>('onboarding/number-preview', {
                  series: row,
                  startMonth: this.model['fiscal'][0]['startMonth'],
                }),
              );
              if (generation === this.generation) row['preview'] = result['number'];
            } catch {
              if (generation === this.generation)
                row['preview'] = 'Check pattern and financial year';
            }
          }),
        );
      }
    } catch (e) {
      if (generation === this.generation) this.fail(e);
    } finally {
      this.cd.markForCheck();
    }
  }
  payload(): unknown {
    const m = this.model;
    switch (this.page) {
      case 'company':
        return m['company'][0];
      case 'locations':
        return {
          registrations: m['registrations'].map((r) => r['gstin']),
          branches: m['branches'],
          plants: m['plants'],
        };
      case 'numbering':
        return { ...m['fiscal'][0], series: m['series'] };
      case 'invites':
        return m['invite'][0];
      case 'openings':
        return m['rows'].map((r) => ({
          ...r,
          debit: r['debit'] || '0',
          credit: r['credit'] || '0',
        }));
      default:
        return m[this.page];
    }
  }
  async save() {
    this.busy = true;
    this.error = '';
    this.notice = '';
    try {
      if (this.page === 'create') {
        const r = this.model['tenant'][0];
        await firstValueFrom(
          this.api.post('platform/tenants', {
            requestId: this.requestId,
            name: r['name'],
            plan: r['plan'],
            adminEmail: r['adminEmail'],
            addOnModules: r['addOnModules'] ?? [],
          }),
        );
        await this.router.navigateByUrl('/setup/platform');
        return;
      }
      await firstValueFrom(
        this.api.post(
          this.page === 'checklist'
            ? 'onboarding/go-live'
            : this.page === 'openings'
              ? 'openings'
              : 'onboarding/steps/' + (steps.indexOf(this.page) + 1),
          { version: this.state['version'], data: this.payload() },
        ),
      );
      this.notice = this.page === 'checklist' ? 'Company is live.' : 'Changes saved.';
      await this.load();
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
      this.cd.markForCheck();
    }
  }
  async excel(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    try {
      const result = await firstValueFrom(this.api.post<Row, File>('openings/excel', file));
      this.model['rows'] = result['rows'];
      this.previewErrors = result['errors'];
      this.difference = result['difference'];
      this.notice = 'Review the imported entries and save to confirm.';
    } catch (e) {
      this.fail(e);
    }
    this.cd.markForCheck();
  }
  async importMasters(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    try {
      if (file.size > 1000000) throw Error('Maximum import size is 1 MB.');
      const lines = (await file.text()).trim().split(/\r?\n/);
      if (lines.shift()?.trim() !== 'Kind\tReference\tName')
        throw Error('Use tab-separated columns Kind, Reference and Name.');
      const rows = lines.map((line, i) => {
        const [kind, reference, name, ...extra] = line.split('\t');
        if (!name || extra.length) throw Error('Check row ' + (i + 2));
        return { kind, reference, name };
      });
      this.masterPreview = rows;
    } catch (e) {
      this.fail(e);
    }
    this.cd.markForCheck();
  }
  masterPreview: Row[] = [];
  async confirmMasters() {
    this.busy = true;
    try {
      await firstValueFrom(
        this.api.post('onboarding/steps/7', {
          version: this.state['version'],
          data: this.masterPreview,
        }),
      );
      this.masterPreview = [];
      this.notice = 'Master import saved.';
      await this.load();
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
      this.cd.markForCheck();
    }
  }
  async lifecycle() {
    await this.subscriptionAction('suspend');
  }
  async openTenant(tenant: Row) {
    this.selectedTenant = tenant;
    this.busy = true;
    this.error = '';
    try {
      const id = tenant['tenantId'];
      [this.tenantDetail, this.tenantAudit] = await Promise.all([
        firstValueFrom(this.api.get<Row>('platform/tenants/' + id)),
        firstValueFrom(this.api.get<Row[]>('platform/tenants/' + id + '/audit')),
      ]);
      const subscription = this.tenantDetail['subscription'];
      const addons = (this.tenantDetail['entitlements'] ?? [])
        .filter((entitlement: Row) => entitlement['source'] === 'AddOn' && entitlement['isEnabled'])
        .map((entitlement: Row) => entitlement['moduleCode']);
      this.assignment = {
        planCode: subscription?.planCode ?? '',
        addOnModules: addons,
        startsAt: this.toLocalInput(subscription?.startsAt ?? new Date()),
        endsAt: subscription?.endsAt ? this.toLocalInput(subscription.endsAt) : '',
        status: subscription?.status === 'Trial' ? 'Trial' : 'Active',
        version: subscription?.version ?? 0,
        reason: '',
      };
      this.actionEndsAt = this.assignment['endsAt'];
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
      this.cd.markForCheck();
    }
  }
  usageRows(): { label: string; used: number; limit: number }[] {
    const usage = this.tenantDetail?.['usage'];
    if (!usage) return [];
    return [
      { label: 'Users', used: usage['users'], limit: usage['quotas']['users'] },
      { label: 'Storage bytes', used: usage['storageBytes'], limit: usage['quotas']['storageBytes'] },
      { label: 'Documents this month', used: usage['documents'], limit: usage['quotas']['documents'] },
      { label: 'Messages this month', used: usage['messages'], limit: usage['quotas']['messages'] },
      { label: 'API requests this month', used: usage['apiRequests'], limit: usage['quotas']['apiRequests'] },
    ];
  }
  async assignSubscription() {
    if (!this.selectedTenant) return;
    this.busy = true;
    this.error = '';
    try {
      const id = this.selectedTenant['tenantId'];
      await firstValueFrom(
        this.api.put('platform/tenants/' + id + '/subscription', {
          ...this.assignment,
          startsAt: new Date(this.assignment['startsAt']).toISOString(),
          endsAt: new Date(this.assignment['endsAt']).toISOString(),
        }),
      );
      this.notice = 'Plan and module entitlements updated.';
      await this.loadTenantAfterMutation();
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
      this.cd.markForCheck();
    }
  }
  async subscriptionAction(action: string) {
    if (!this.selectedTenant) return;
    this.busy = true;
    this.error = '';
    try {
      const id = this.selectedTenant['tenantId'];
      const subscription = this.tenantDetail?.['subscription'];
      await firstValueFrom(
        this.api.post('platform/tenants/' + id + '/subscription/' + action, {
          version: subscription?.version ?? 0,
          reason: this.actionReason,
          endsAt: this.actionEndsAt ? new Date(this.actionEndsAt).toISOString() : null,
        }),
      );
      this.actionReason = '';
      this.notice = 'Subscription ' + action + ' applied.';
      await this.loadTenantAfterMutation();
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
      this.cd.markForCheck();
    }
  }
  async runSubscriptionAction() {
    await this.subscriptionAction('renew');
  }
  async savePlan() {
    this.busy = true;
    this.error = '';
    try {
      const isEdit = !!this.planForm['version'];
      const code = this.planForm['planCode'];
      const payload = { ...this.planForm };
      await firstValueFrom(
        isEdit
          ? this.api.put('platform/plans/' + code, payload)
          : this.api.post('platform/plans', payload),
      );
      this.notice = isEdit ? 'Plan updated.' : 'Plan created.';
      this.resetPlanForm();
      await this.load();
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
      this.cd.markForCheck();
    }
  }
  editPlan(plan: Row) {
    this.planForm = structuredClone({
      ...plan,
      includedModules: plan['includedModules'] ?? [],
    });
  }
  togglePlanModule(moduleCode: string, checked: boolean) {
    const included = new Set<string>(this.planForm['includedModules'] ?? []);
    if (checked) included.add(moduleCode);
    else included.delete(moduleCode);
    this.planForm['includedModules'] = [...included];
  }
  resetPlanForm() {
    this.planForm = {
      planCode: '',
      name: '',
      description: '',
      maxUsers: null,
      maxStorageBytes: null,
      maxDocumentsPerMonth: null,
      maxMessagesPerMonth: null,
      maxApiRequestsPerMonth: null,
      trialDays: null,
      gracePeriodDays: 0,
      priceAmount: null,
      currency: 'INR',
      includedModules: this.modules.filter((module) => module['isCore']).map((module) => module['moduleCode']),
      version: 0,
    };
  }
  private async loadTenantAfterMutation() {
    if (!this.selectedTenant) return;
    const selected = this.selectedTenant;
    await this.load();
    await this.openTenant(selected);
  }
  private toLocalInput(value: string | Date): string {
    const date = value instanceof Date ? value : new Date(value);
    const pad = (part: number) => String(part).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
  get ready() {
    return this.state['checks']?.every((c: Row) => c['passed']);
  }
}
