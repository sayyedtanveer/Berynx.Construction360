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
  requestId = crypto.randomUUID();
  loading = true;
  busy = false;
  error = '';
  notice = '';
  difference: number | null = null;
  previewErrors: string[] = [];
  reason = '';
  status = '';
  selected: Row | null = null;
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
      if (this.page === 'platform' || this.page === 'create') {
        if (!this.session['platform']) throw Error('Platform administrator access is required.');
        if (this.page === 'platform')
          this.tenants = await firstValueFrom(
            this.api.get<Row[]>('platform/tenants?page=' + this.pageNumber),
          );
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
            ...r,
            requestId: this.requestId,
            modules: r['modules']
              .split(',')
              .map((x: string) => x.trim())
              .filter(Boolean),
            limits: {
              users: r['users'],
              storage: r['storage'],
              documents: r['documents'],
              messages: r['messages'],
            },
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
    if (!this.selected) return;
    this.busy = true;
    try {
      await firstValueFrom(
        this.api.post('platform/tenants/' + this.selected['tenantId'] + '/status', {
          version: this.selected['version'],
          data: { status: this.status, reason: this.reason },
        }),
      );
      this.selected = null;
      this.reason = '';
      await this.load();
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
      this.cd.markForCheck();
    }
  }
  get ready() {
    return this.state['checks']?.every((c: Row) => c['passed']);
  }
}
