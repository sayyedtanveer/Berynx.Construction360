import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';

type EnquiryResponse = { rows: any[]; total: number };

@Component({
  selector: 'app-m04-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head">
        <div><p class="eyebrow">SELL · M04 CRM</p><h1>Enquiries</h1><p class="muted">Capture, qualify and follow up customer demand.</p></div>
        <button class="primary" type="button" (click)="openCreate()">＋ New enquiry</button>
      </header>
      <div class="toolbar">
        <div class="tabs">
          <button [class.active]="view() === 'list'" (click)="view.set('list')">List</button>
          <button [class.active]="view() === 'kanban'" (click)="view.set('kanban')">Pipeline</button>
          <button [class.active]="view() === 'followups'" (click)="loadFollowUps()">Follow-ups</button>
        </div>
        <div class="filters" *ngIf="view() !== 'followups'">
          <input aria-label="Search enquiries" placeholder="Search number or notes" [(ngModel)]="search" (keyup.enter)="load()">
          <select aria-label="Filter by status" [(ngModel)]="statusFilter" (change)="load()">
            <option value="">All statuses</option><option *ngFor="let status of statuses" [value]="status">{{ status }}</option>
          </select>
          <button class="secondary" (click)="load()">Apply</button>
        </div>
      </div>
      <p class="error" *ngIf="error()">{{ error() }}</p>
      <p class="loading" *ngIf="loading()">Loading enquiries…</p>
      <section class="table-wrap" *ngIf="view() === 'list' && !loading()">
        <table>
          <thead><tr><th>Enquiry</th><th>Customer / contact</th><th>Owner</th><th>Status</th><th>Next action</th><th>Value</th></tr></thead>
          <tbody>
            <tr *ngFor="let row of rows()" (click)="openDetail(row)">
              <td><strong>{{ row.number }}</strong><small>{{ row.source }} · {{ row.createdAt | date:'mediumDate' }}</small></td>
              <td>{{ customerName(row.customerPartyId) }}<small>{{ contactName(row.contactId) }}</small></td>
              <td>{{ row.ownerUserId | slice:0:8 }}</td>
              <td><span class="chip" [attr.data-status]="row.status">{{ row.status }}</span></td>
              <td>{{ row.nextActionAt | date:'medium' }}</td>
              <td>{{ row.expectedValue == null ? '—' : (row.expectedValue | currency:'INR':'symbol':'1.0-2') }}</td>
            </tr>
          </tbody>
        </table>
        <div class="empty" *ngIf="!rows().length">No enquiries match these filters.</div>
        <footer class="pager" *ngIf="total() > pageSize"><span>{{ total() }} enquiries</span><button class="secondary" [disabled]="page === 1" (click)="changePage(-1)">Previous</button><button class="secondary" [disabled]="page * pageSize >= total()" (click)="changePage(1)">Next</button></footer>
      </section>
      <section class="kanban" *ngIf="view() === 'kanban'">
        <article class="lane" *ngFor="let status of statuses"><header><span>{{ status }}</span><b>{{ rowsFor(status).length }}</b></header>
          <button class="card" *ngFor="let row of rowsFor(status)" (click)="openDetail(row)">
            <strong>{{ row.number }}</strong><span>{{ customerName(row.customerPartyId) }}</span>
            <small>{{ row.nextActionAt | date:'mediumDate' }} · {{ row.source }}</small>
          </button>
        </article>
      </section>
      <section class="table-wrap" *ngIf="view() === 'followups'">
        <div class="followup-filters">
          <button class="secondary" *ngFor="let bucket of buckets" [class.selected]="followupBucket === bucket" (click)="followupBucket=bucket; loadFollowUps()">{{ bucket }}</button>
        </div>
        <table><thead><tr><th>Enquiry</th><th>Customer</th><th>Action</th><th>Due</th><th>Status</th></tr></thead>
          <tbody><tr *ngFor="let item of followups()"><td><a href="#" (click)="$event.preventDefault(); openDetail({enquiryId:item.enquiryId})">{{ item.number }}</a></td><td>{{ customerName(item.customerPartyId) }}</td><td>{{ item.action }}</td><td>{{ item.dueAt | date:'medium' }}</td><td>{{ item.status }} <button *ngIf="item.status !== 'Lost' && item.status !== 'Won'" class="secondary compact" (click)="completeFollowUp(item)">Complete</button></td></tr></tbody>
        </table>
        <div class="empty" *ngIf="!followups().length">No {{ followupBucket.toLowerCase() }} follow-ups.</div>
      </section>
      <div class="backdrop" *ngIf="creating()" (click)="creating.set(false)"></div>
      <aside class="drawer" *ngIf="creating()" aria-label="New enquiry form">
        <header><div><p class="eyebrow">QUICK CAPTURE</p><h2>New enquiry</h2></div><button class="icon" (click)="creating.set(false)" aria-label="Close">×</button></header>
        <form (ngSubmit)="create()">
          <label>Source<select name="source" [(ngModel)]="draft.source"><option *ngFor="let source of sources" [value]="source">{{ source }}</option></select></label>
          <label>Customer<select name="customer" [(ngModel)]="draft.customerPartyId" (change)="customerChanged()"><option value="">Select customer</option><option *ngFor="let customer of customers()" [value]="customer.partyId">{{ customer.tradeName || customer.legalName }}</option></select></label>
          <label>Contact<select name="contact" [(ngModel)]="draft.contactId"><option value="">Select contact (optional)</option><option *ngFor="let contact of customerContacts()" [value]="contact.contactId">{{ contact.name }}</option></select></label>
          <label>Site<select name="site" [(ngModel)]="draft.siteId" (change)="siteChanged()"><option value="">Select site (optional)</option><option *ngFor="let site of customerSites()" [value]="site.siteId">{{ site.name }}</option></select></label>
          <label>Product<select name="product" [(ngModel)]="draft.productId" (change)="productChanged()"><option value="">Select product</option><option *ngFor="let product of products()" [value]="product.productId">{{ product.name }} · {{ product.baseUnitCode }}</option></select></label>
          <ng-container *ngIf="selectedProduct()?.category === 'RMC'">
            <p class="notice">Specify RMC mix requirements from an approved M03 rate card.</p>
            <label>RMC grade<select name="rmcGrade" [(ngModel)]="draft.rmcGrade" (change)="rmcGradeChanged()"><option value="">Select grade</option><option *ngFor="let grade of rmcGrades()" [value]="grade">{{ grade }}</option></select></label>
            <label>Slump band<select name="rmcSlump" [(ngModel)]="draft.rmcSlumpBand"><option value="">Select slump band</option><option *ngFor="let value of rmcValues('slumpBand')" [value]="value">{{ value }}</option></select></label>
            <label>Admixture<select name="rmcAdmixture" [(ngModel)]="draft.rmcAdmixtureCode"><option value="">Select admixture</option><option *ngFor="let value of rmcValues('admixtureCode')" [value]="value">{{ value }}</option></select></label>
            <label>Pump type<select name="rmcPump" [(ngModel)]="draft.rmcPumpType"><option value="">Select pump type</option><option *ngFor="let value of rmcValues('pumpType')" [value]="value">{{ value }}</option></select></label>
          </ng-container>
          <div class="two"><label>Quantity<input name="quantity" type="number" min="0.001" step="0.001" [(ngModel)]="draft.quantity" required></label><label>Unit<input name="unit" [value]="draft.unitCode" readonly></label></div>
          <div class="two"><label>Priority<select name="priority" [(ngModel)]="draft.priority"><option>Normal</option><option>Low</option><option>High</option><option>Urgent</option></select></label><label>Expected value<input name="value" type="number" min="0" step="0.01" [(ngModel)]="draft.expectedValue"></label></div>
          <label>Required by<input name="requiredBy" type="date" [(ngModel)]="draft.requiredBy"></label>
          <label>Next follow-up<input name="nextActionAt" type="datetime-local" [(ngModel)]="draft.nextActionAt" required></label>
          <label>Enquiry notes<textarea name="notes" [(ngModel)]="draft.notes" maxlength="4000" required></textarea></label>
          <p class="error" *ngIf="error()">{{ error() }}</p>
          <footer><button type="button" class="secondary" (click)="creating.set(false)">Cancel</button><button class="primary" type="submit" [disabled]="saving()">Save enquiry</button></footer>
        </form>
      </aside>
      <div class="backdrop" *ngIf="selected()" (click)="selected.set(null)"></div>
      <aside class="drawer detail" *ngIf="selected()" aria-label="Enquiry details">
        <header><div><p class="eyebrow">{{ selected()?.enquiry?.number }}</p><h2>{{ customerName(selected()?.enquiry?.customerPartyId) }}</h2></div><button class="icon" (click)="selected.set(null)" aria-label="Close">×</button></header>
        <p *ngIf="detailLoading()">Loading details…</p>
        <ng-container *ngIf="selected()?.enquiry as enquiry">
          <div class="status-line"><span class="chip" [attr.data-status]="enquiry.status">{{ enquiry.status }}</span><span>{{ enquiry.source }}</span></div>
          <p>{{ enquiry.notes }}</p>
          <h3>Products</h3><p *ngFor="let line of selected()?.lines">{{ productName(line.productId) }} · {{ line.quantity }} {{ line.unitCode }}</p>
          <section class="actions" *ngIf="enquiry.status !== 'Lost' && enquiry.status !== 'Won'">
            <label>Change status<select #nextStatus><option *ngFor="let status of statuses" [value]="status" [selected]="status === enquiry.status">{{ status }}</option></select></label>
            <label *ngIf="nextStatus.value === 'Lost'">Loss reason<select name="lossReason" [(ngModel)]="draftLostReason"><option value="">Select reason</option><option *ngFor="let reason of lostReasons" [value]="reason">{{ reason }}</option></select></label>
            <button class="secondary" (click)="changeStatus(enquiry, nextStatus.value, draftLostReason || null)">Update status</button>
            <button class="primary" (click)="createQuote(enquiry)">Create quotation</button>
          </section>
          <section class="actions" *ngIf="enquiry.status === 'Lost'">
            <h3>Reopen enquiry</h3>
            <label>Open stage<select name="reopenStatus" [(ngModel)]="reopenStatus"><option>New</option><option>Contacted</option><option>Follow-up</option><option>Negotiation</option><option>On hold</option></select></label>
            <label>Next action date<input name="reopenNextAction" type="datetime-local" [(ngModel)]="reopenNextActionAt" required></label>
            <button class="secondary" (click)="reopen(enquiry)">Reopen</button>
          </section>
          <h3>Log activity</h3>
          <form (ngSubmit)="logActivity(enquiry)">
            <label>Type<select name="activityKind" [(ngModel)]="activity.kind"><option>Call</option><option>Message</option><option>Email</option><option>Meeting</option><option>Note</option><option>SiteVisit</option></select></label>
            <label>Channel<select name="activityChannel" [(ngModel)]="activity.channel"><option>Phone</option><option>WhatsApp</option><option>SMS</option><option>Email</option><option>InPerson</option><option>Internal</option></select></label>
            <label>Outcome<input name="outcome" [(ngModel)]="activity.outcome" required></label>
            <label>Notes<textarea name="activityNotes" [(ngModel)]="activity.notes"></textarea></label>
            <label>Next action date<input name="activityNext" type="datetime-local" [(ngModel)]="activity.nextActionAt"></label>
            <label>Next action<input name="activityAction" [(ngModel)]="activity.nextAction"></label>
            <button class="secondary" type="submit">Log activity</button>
          </form>
          <h3>Timeline</h3><div class="timeline" *ngFor="let item of selected()?.activities"><b>{{ item.kind }}</b><small>{{ item.at | date:'medium' }}</small><p>{{ item.outcome }} {{ item.notes }}</p></div>
        </ng-container>
      </aside>
    </main>
  `,
  styles: [`
    :host{display:block;color:#252422}.workspace{padding:28px 32px 48px;background:#f6f2ed;min-height:100%;font-family:Inter,Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:center;gap:20px;margin-bottom:24px}.eyebrow{font-size:11px;letter-spacing:.14em;font-weight:700;color:#9d573b;margin:0 0 7px}.page-head h1,.drawer h2{margin:0;font-size:28px;letter-spacing:-.03em}.muted,small{color:#77736e}.toolbar{display:flex;justify-content:space-between;gap:16px;align-items:center;margin-bottom:16px}.tabs,.filters,.followup-filters{display:flex;gap:8px;align-items:center}.tabs button{border:0;background:transparent;color:#68635f;padding:10px 14px;border-bottom:2px solid transparent}.tabs button.active{color:#292622;border-color:#9d573b;font-weight:700}.filters input,.filters select,.drawer input,.drawer select,.drawer textarea{border:1px solid #d9d1c9;border-radius:7px;background:#fff;padding:10px 12px;color:#252422;min-width:145px}.filters input{width:230px}.primary,.secondary,.icon{border-radius:7px;padding:10px 14px;font-weight:650;cursor:pointer}.primary{border:1px solid #89452f;background:#914b34;color:white}.secondary{border:1px solid #d6ccc3;background:#fff;color:#393531}.primary:disabled{opacity:.55}.table-wrap{background:#fff;border:1px solid #e3ddd7;border-radius:10px;overflow:auto}table{width:100%;border-collapse:collapse;min-width:760px}th{text-align:left;font-size:11px;text-transform:uppercase;letter-spacing:.08em;color:#77736e;background:#faf8f6;padding:13px 16px;border-bottom:1px solid #e8e2dc}td{padding:14px 16px;border-bottom:1px solid #eee9e4;font-size:13px}tbody tr{cursor:pointer}tbody tr:hover{background:#fbf7f2}td small{display:block;margin-top:5px}.chip{display:inline-block;border-radius:999px;background:#f1e8df;padding:5px 9px;font-size:11px;font-weight:700}.chip[data-status="Lost"]{background:#f5e5e3;color:#8d3e37}.chip[data-status="Won"]{background:#e2efe5;color:#28603a}.pager{display:flex;gap:12px;align-items:center;justify-content:flex-end;padding:12px 16px}.pager span{margin-right:auto;color:#777}.empty,.loading{padding:28px;text-align:center;color:#777}.error{color:#9c3028}.kanban{display:grid;grid-template-columns:repeat(6,minmax(190px,1fr));gap:12px;overflow:auto}.lane{background:#eee9e4;border-radius:10px;padding:12px;min-height:280px}.lane header{display:flex;justify-content:space-between;font-size:12px;font-weight:700;padding:4px 3px 12px}.card{width:100%;display:grid;text-align:left;gap:8px;border:1px solid #e3ddd7;background:#fff;border-radius:8px;padding:12px;margin-bottom:9px;cursor:pointer}.card small{color:#777}.followup-filters{padding:12px}.followup-filters .selected{border-color:#914b34;color:#914b34}.backdrop{position:fixed;inset:0;background:#1f1b1880;z-index:20}.drawer{position:fixed;right:0;top:0;bottom:0;z-index:21;width:min(500px,100vw);background:#f9f6f2;box-shadow:-10px 0 35px #0002;padding:24px;overflow:auto}.drawer header{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:18px}.drawer form{display:grid;gap:13px}.drawer label{display:grid;gap:6px;font-size:12px;font-weight:650;color:#605b55}.drawer input,.drawer select,.drawer textarea{width:100%;box-sizing:border-box;font:inherit;font-weight:400}.drawer textarea{min-height:70px;resize:vertical}.drawer footer{display:flex;justify-content:flex-end;gap:8px;margin:8px 0}.icon{background:transparent;border:0;font-size:24px;padding:0 5px}.two{display:grid;grid-template-columns:1fr 1fr;gap:10px}.detail{width:min(560px,100vw)}.status-line{display:flex;gap:12px;align-items:center;margin:15px 0}.detail h3{font-size:14px;margin:24px 0 10px;border-bottom:1px solid #e2d9d0;padding-bottom:7px}.actions{display:grid;gap:10px;margin:20px 0}.timeline{padding:12px 0;border-bottom:1px solid #e3ddd7}.timeline small{float:right}.timeline p{margin:8px 0;color:#615d58}@media(max-width:800px){.workspace{padding:20px 14px}.page-head{align-items:flex-start}.toolbar{align-items:flex-start;flex-direction:column}.filters{width:100%;flex-wrap:wrap}.filters input{flex:1;min-width:190px}.kanban{grid-template-columns:repeat(6,200px)}}
  `],
})
export class M04Page implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);
  readonly rows = signal<any[]>([]);
  readonly customers = signal<any[]>([]);
  readonly contacts = signal<any[]>([]);
  readonly sites = signal<any[]>([]);
  readonly products = signal<any[]>([]);
  readonly rmcCards = signal<any[]>([]);
  readonly followups = signal<any[]>([]);
  readonly selected = signal<any | null>(null);
  readonly loading = signal(false);
  readonly detailLoading = signal(false);
  readonly saving = signal(false);
  readonly creating = signal(false);
  readonly error = signal('');
  readonly total = signal(0);
  readonly view = signal<'list' | 'kanban' | 'followups'>('list');
  readonly statuses = ['New', 'Contacted', 'Follow-up', 'Quotation Sent', 'Negotiation', 'On hold', 'Lost'];
  readonly sources = ['Phone', 'WalkIn', 'WhatsApp', 'Referral', 'SiteVisit', 'Website', 'Campaign', 'Repeat'];
  readonly lostReasons = ['Price', 'Competitor', 'Credit', 'Timing', 'Quality', 'NoResponse', 'Other'];
  readonly buckets = ['Overdue', 'Today', 'Upcoming'];
  readonly pageSize = 50;
  search = '';
  statusFilter = '';
  draftLostReason = '';
  reopenStatus = 'Follow-up';
  reopenNextActionAt = '';
  followupBucket = 'Overdue';
  page = 1;
  draft: any = this.emptyDraft();
  activity: any = { kind: 'Call', channel: 'Phone', outcome: '', notes: '', nextActionAt: '', nextAction: 'Call' };

  ngOnInit(): void {
    this.load();
    this.api.get<any[]>('/m03/parties?role=Customer').subscribe({ next: (rows) => this.customers.set(rows), error: () => this.error.set('Customer lookup is unavailable.') });
    this.api.get<any[]>('/m03/contacts').subscribe({ next: (rows) => this.contacts.set(rows), error: () => undefined });
    this.api.get<any[]>('/m03/sites').subscribe({ next: (rows) => this.sites.set(rows), error: () => undefined });
    this.api.get<any[]>('/m03/products').subscribe({ next: (rows) => this.products.set(rows), error: () => undefined });
  }

  load(): void {
    this.view.set(this.view() === 'followups' ? 'list' : this.view());
    this.loading.set(true); this.error.set('');
    const params = new URLSearchParams({ page: String(this.page), pageSize: String(this.pageSize) });
    if (this.search.trim()) params.set('q', this.search.trim());
    if (this.statusFilter) params.set('status', this.statusFilter);
    this.api.get<EnquiryResponse>(`/m04/enquiries?${params}`).subscribe({
      next: (response) => { this.rows.set(response.rows ?? []); this.total.set(response.total ?? 0); this.loading.set(false); },
      error: (error) => { this.error.set(error?.error?.detail ?? 'Could not load enquiries. Check your M04 access.'); this.loading.set(false); },
    });
  }

  loadFollowUps(): void {
    this.view.set('followups');
    this.api.get<any[]>(`/m04/follow-ups?bucket=${encodeURIComponent(this.followupBucket)}`).subscribe({
      next: (rows) => this.followups.set(rows),
      error: (error) => this.error.set(error?.error?.detail ?? 'Could not load follow-ups.'),
    });
  }

  rowsFor(status: string): any[] { return this.rows().filter((row) => row.status === status); }
  customerName(id: string | null | undefined): string { const row = this.customers().find((x) => x.partyId === id); return row?.tradeName || row?.legalName || 'Prospect'; }
  contactName(id: string | null | undefined): string { return this.contacts().find((x) => x.contactId === id)?.name ?? ''; }
  productName(id: string): string { return this.products().find((x) => x.productId === id)?.name ?? id; }
  customerContacts(): any[] { return this.contacts().filter((x) => x.partyId === this.draft.customerPartyId); }
  customerSites(): any[] { return this.sites().filter((x) => x.customerPartyId === this.draft.customerPartyId); }
  customerChanged(): void { this.draft.contactId = ''; this.draft.siteId = ''; this.draft.branchId = ''; }
  siteChanged(): void { const site = this.customerSites().find((x) => x.siteId === this.draft.siteId); this.draft.branchId = site?.branchId ?? ''; }
  productChanged(): void {
    this.draft.unitCode = this.selectedProduct()?.baseUnitCode ?? '';
    this.rmcCards.set([]);
    this.draft.rmcGrade = ''; this.draft.rmcSlumpBand = '';
    this.draft.rmcAdmixtureCode = ''; this.draft.rmcPumpType = '';
    if (this.selectedProduct()?.category === 'RMC')
      this.api.get<any[]>(`/m03/rmc-rate-cards?productId=${encodeURIComponent(this.draft.productId)}`)
        .subscribe({ next: (rows) => this.rmcCards.set(rows), error: () => this.error.set('RMC rate-card lookup is unavailable.') });
  }
  selectedProduct(): any { return this.products().find((x) => x.productId === this.draft.productId); }
  rmcGrades(): string[] { return [...new Set(this.rmcCards().map((x) => x.grade))].sort(); }
  rmcValues(field: 'slumpBand' | 'admixtureCode' | 'pumpType'): string[] {
    return [...new Set(this.rmcCards().filter((x) => !this.draft.rmcGrade || x.grade === this.draft.rmcGrade).map((x) => x[field]))].filter(Boolean).sort();
  }
  rmcGradeChanged(): void {
    const match = this.rmcCards().find((x) => x.grade === this.draft.rmcGrade);
    this.draft.rmcSlumpBand = match?.slumpBand ?? '';
    this.draft.rmcAdmixtureCode = match?.admixtureCode ?? '';
    this.draft.rmcPumpType = match?.pumpType ?? '';
  }
  openCreate(): void { this.error.set(''); this.draft = this.emptyDraft(); this.creating.set(true); }
  openDetail(row: any): void {
    if (!row?.enquiryId) return;
    this.selected.set({ enquiry: row }); this.detailLoading.set(true);
    this.api.get<any>(`/m04/enquiries/${row.enquiryId}`).subscribe({
      next: (detail) => { this.selected.set(detail); this.detailLoading.set(false); },
      error: (error) => { this.error.set(error?.error?.detail ?? 'Could not load enquiry details.'); this.detailLoading.set(false); },
    });
  }

  create(): void {
    if (!this.draft.productId || !this.draft.unitCode || !this.draft.nextActionAt) { this.error.set('Select a product, unit and follow-up time.'); return; }
    this.saving.set(true); this.error.set('');
    const body = {
      source: this.draft.source, contactId: this.draft.contactId || null,
      customerPartyId: this.draft.customerPartyId || null, siteId: this.draft.siteId || null,
      branchId: this.draft.branchId || null,
      lines: [{ productId: this.draft.productId, quantity: Number(this.draft.quantity), unitCode: this.draft.unitCode,
        rmcGrade: this.draft.rmcGrade || null, rmcSlumpBand: this.draft.rmcSlumpBand || null,
        rmcAdmixtureCode: this.draft.rmcAdmixtureCode || null, rmcPumpType: this.draft.rmcPumpType || null }],
      requiredBy: this.draft.requiredBy || null, expectedValue: this.draft.expectedValue === '' ? null : Number(this.draft.expectedValue),
      priority: this.draft.priority, nextActionAt: new Date(this.draft.nextActionAt).toISOString(),
      notes: this.draft.notes, idempotencyKey: crypto.randomUUID(),
    };
    this.api.post<any, any>('/m04/enquiries', body).subscribe({
      next: () => { this.saving.set(false); this.creating.set(false); this.load(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Enquiry could not be saved.'); },
    });
  }

  changeStatus(enquiry: any, status: string, lostReason: string | null): void {
    if (status === 'Lost' && !lostReason) { this.error.set('Select a loss reason.'); return; }
    this.api.post<any, any>(`/m04/enquiries/${enquiry.enquiryId}/status`, { status, lostReason: status === 'Lost' ? lostReason : null, version: enquiry.version }).subscribe({
      next: () => { this.error.set(''); this.openDetail(enquiry); this.load(); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Status change was rejected.'),
    });
  }

  logActivity(enquiry: any): void {
    const nextActionAt = this.activity.nextActionAt ? new Date(this.activity.nextActionAt).toISOString() : null;
    this.api.post<any, any>(`/m04/enquiries/${enquiry.enquiryId}/activities`, {
      ...this.activity, nextActionAt,
    }).subscribe({
      next: () => { this.activity = { kind: 'Call', channel: 'Phone', outcome: '', notes: '', nextActionAt: '', nextAction: 'Call' }; this.openDetail(enquiry); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Activity could not be saved.'),
    });
  }

  reopen(enquiry: any): void {
    if (!this.reopenNextActionAt) { this.error.set('Choose a future next-action date before reopening.'); return; }
    this.api.post<any, any>(`/m04/enquiries/${enquiry.enquiryId}/reopen`, {
      status: this.reopenStatus, lostReason: null, version: enquiry.version,
      nextActionAt: new Date(this.reopenNextActionAt).toISOString(),
    }).subscribe({
      next: () => { this.error.set(''); this.reopenNextActionAt = ''; this.load(); this.openDetail(enquiry); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Enquiry could not be reopened.'),
    });
  }

  completeFollowUp(item: any): void {
    const outcome = window.prompt('Record the follow-up outcome:');
    if (!outcome?.trim()) return;
    const nextActionAt = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
    this.api.post<any, any>(`/m04/follow-ups/${item.followUpId}/complete`, {
      nextActionAt, nextAction: 'Call', outcome: outcome.trim(), version: item.version,
    }).subscribe({
      next: () => this.loadFollowUps(),
      error: (error) => this.error.set(error?.error?.detail ?? 'Follow-up could not be completed.'),
    });
  }

  createQuote(enquiry: any): void {
    this.router.navigate(['/m05/quotations'], { queryParams: { fromEnquiry: enquiry.enquiryId } });
  }

  changePage(delta: number): void { this.page = Math.max(1, this.page + delta); this.load(); }

  private emptyDraft(): any {
    return { source: 'Phone', customerPartyId: '', contactId: '', siteId: '', branchId: '', productId: '', quantity: 1, unitCode: '', priority: 'Normal', expectedValue: '', requiredBy: '', nextActionAt: '', notes: '' };
  }
}
