import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m10-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head"><div><p class="eyebrow">FINANCE · M10 BILLING</p><h1>Sales invoices</h1>
        <p class="muted">Create draft invoices from receiver-verified delivery challans.</p></div>
        <button class="primary" type="button" (click)="openDraft()">Create draft</button></header>
      <p class="notice">Posting rechecks verified M08/M18 evidence, branch invoice treatment, numbering, ledger mappings and period controls.</p>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p>
      <p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <nav class="tabs">
        <button *ngFor="let status of statuses" type="button" [class.active]="filter === status" (click)="setFilter(status)">{{ status }}</button>
      </nav>
      <section class="panel">
        <p *ngIf="loading()">Loading invoices…</p>
        <p class="empty" *ngIf="!loading() && !items().length">No invoices match this filter.</p>
        <div class="table-wrap" *ngIf="items().length">
          <table><thead><tr><th>Number</th><th>Date</th><th>Customer</th><th>Tax type</th><th>Taxable</th><th>Total</th><th>Status</th><th></th></tr></thead>
          <tbody><tr *ngFor="let row of items()">
            <td>{{ row.number || 'Draft — not numbered' }}</td><td>{{ row.invoiceDate | date:'mediumDate' }}</td>
            <td>{{ row.customerNameSnapshot }}</td><td>{{ row.taxTreatment }}</td>
            <td>{{ row.taxableAmount | currency:'INR':'symbol':'1.2-2' }}</td>
            <td>{{ row.totalAmount | currency:'INR':'symbol':'1.2-2' }}</td>
            <td><span class="chip">{{ row.collectionStatus || row.status }}</span></td>
            <td><button class="secondary compact" type="button" (click)="openDetail(row.invoiceId)">Review</button></td>
          </tr></tbody></table>
        </div>
      </section>
      <div class="backdrop" *ngIf="draftOpen()" (click)="draftOpen.set(false)"></div>
      <aside class="drawer" *ngIf="draftOpen()" aria-label="Create invoice draft">
        <header><div><p class="eyebrow">SCR-M10-02 · INVOICE FORM</p><h2>Choose delivered challans</h2></div>
          <button class="icon" type="button" (click)="draftOpen.set(false)" aria-label="Close">×</button></header>
        <p class="notice">Only M09 challans with <code>Delivered</code> status and verified receiver evidence are eligible. Only completed trips with persisted supervisor verification and an eligible M09 challan are shown. Refresh this list after an M08 evidence decision.</p>
        <button class="secondary compact" type="button" (click)="refreshEligible()" [disabled]="eligibilityLoading()">{{ eligibilityLoading() ? 'Refreshing…' : 'Refresh eligible challans' }}</button>
        <p class="empty" *ngIf="!eligible().length">No receiver-verified challans are available. Do not override; receiver verification is pending in M08.</p>
        <div class="check-row" *ngFor="let row of eligible()">
          <input type="checkbox" [checked]="selectedIds.has(row.challanId)" (change)="toggle(row.challanId, $event)">
          <div><strong>{{ row.number }} · {{ row.customerNameSnapshot }}</strong>
            <small>{{ row.siteAddressSnapshot }} · {{ row.productNameSnapshot }} · {{ row.deliveredQuantity }}</small>
            <small>{{ row.deliveredValueSnapshot | currency:'INR':'symbol':'1.2-2' }} · {{ row.challanDate | date:'mediumDate' }}</small></div>
        </div>
        <label>Invoice date<input type="date" [(ngModel)]="invoiceDate"></label>
        <label>Notes<textarea [(ngModel)]="notes"></textarea></label>
        <p class="notice" *ngIf="selectedIds.size">Customer billing mode: <strong>{{ selectedBillingMode() || 'Not configured or inconsistent' }}</strong>. The server validates grouping against delivery dates and sales-order references.</p>
        <label *ngIf="selectedIds.size">Billing-mode override
          <select [(ngModel)]="billingModeOverride">
            <option value="">Use customer setting</option>
            <option *ngFor="let mode of billingModes" [value]="mode">{{ mode }}</option>
          </select>
        </label>
        <label *ngIf="requiresBillingModeReason()">Override reason<textarea [(ngModel)]="billingModeOverrideReason" maxlength="1000" required></textarea></label>
        <button class="primary" type="button" [disabled]="saving() || !canCreateDraft()" (click)="createDraft()">Save draft</button>
      </aside>
      <div class="backdrop" *ngIf="detail()" (click)="detail.set(null)"></div>
      <aside class="drawer" *ngIf="detail()" aria-label="Invoice detail and preview">
        <header><div><p class="eyebrow">SCR-M10-03 · DRAFT REVIEW</p><h2>{{ detail().invoice.number || 'Invoice draft' }}</h2></div>
          <button class="icon" type="button" (click)="detail.set(null)" aria-label="Close">×</button></header>
        <p>{{ detail().invoice.customerNameSnapshot }} · {{ detail().invoice.customerGstinSnapshot || 'No GSTIN' }}</p>
        <p><span class="chip">{{ detail().invoice.taxTreatment }}</span> · Place of supply {{ detail().invoice.placeOfSupplyStateCode }}</p>
        <div class="facts"><div><small>Taxable value</small><strong>{{ detail().invoice.taxableAmount | currency:'INR':'symbol':'1.2-2' }}</strong></div>
          <div><small>CGST / SGST / IGST</small><strong>{{ detail().invoice.cgstAmount | currency:'INR':'symbol':'1.2-2' }} / {{ detail().invoice.sgstAmount | currency:'INR':'symbol':'1.2-2' }} / {{ detail().invoice.igstAmount | currency:'INR':'symbol':'1.2-2' }}</strong></div>
          <div><small>Round-off</small><strong>{{ detail().invoice.roundOffAmount | currency:'INR':'symbol':'1.2-2' }}</strong></div>
          <div><small>Draft total</small><strong>{{ detail().invoice.totalAmount | currency:'INR':'symbol':'1.2-2' }}</strong></div>
        </div>
        <h3>Lines</h3><div class="line" *ngFor="let line of detail().lines">
          <strong>{{ line.productNameSnapshot }} · {{ line.quantity }} {{ line.unitCodeSnapshot }}</strong>
          <small>HSN {{ line.hsnCodeSnapshot }} · rate {{ line.rate }} · taxable {{ line.taxableAmount | currency:'INR':'symbol':'1.2-2' }}</small>
          <small>Challans: {{ line.challanReferencesJson }}</small>
        </div>
        <h3>Selected challans</h3><div class="line" *ngFor="let challan of detail().challans">
          <strong>{{ challan.number }} · {{ challan.status }}</strong><small>Quantity {{ challan.quantity }}</small>
        </div>
        <p class="notice">Consequence preview is draft-only. No voucher, receivable, GST liability, invoice number or due date has been created.</p>
        <p class="notice" *ngIf="detail().invoice.status === 'Draft' && detail().invoice.invoiceType === 'Unresolved'">Posting is blocked: configure the branch GST registration treatment so the server can choose tax invoice versus bill of supply.</p>
        <button *ngIf="detail().invoice.status === 'Draft'" class="primary" type="button" [disabled]="saving()" (click)="postInvoice()">Post invoice</button>
        <button *ngIf="detail().invoice.status === 'Draft'" class="danger" type="button" [disabled]="saving()" (click)="cancelDraft()">Cancel draft and release challans</button>
        <section *ngIf="detail().invoice.status === 'Posted'" class="cancellation">
          <h3>Cancel posted invoice</h3>
          <p class="notice">Cancellation posts a linked M12 reversal and releases delivery quantities. Receipts must be unallocated first. Invoices with an IRN require recorded portal cancellation before this action can proceed.</p>
          <label>Reason for cancellation<textarea [(ngModel)]="cancelReason" maxlength="1000" required></textarea></label>
          <button class="danger" type="button" [disabled]="saving() || !cancelReason.trim()" (click)="cancelPostedInvoice()">Cancel invoice and reverse voucher</button>
        </section>
      </aside>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1450px;margin:auto;color:var(--ink,#292723)}
    .page-head{display:flex;justify-content:space-between;align-items:flex-start;gap:20px}
    .eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}
    h1{font-size:30px;margin:4px 0}.muted,small{color:#77736c}small{display:block;margin-top:4px}
    .primary,.secondary,.danger{border:0;border-radius:6px;padding:10px 14px;cursor:pointer}
    .primary{background:var(--copper,#a65d3b);color:#fff}.secondary{background:#eee9e1;color:#39352f}.danger{background:#9d392e;color:#fff}
    .notice{background:#f0e8de;padding:12px;border-radius:6px;font-size:13px;line-height:1.5}
    .error{color:#a3342a}.success{color:#24734f}.tabs{display:flex;gap:6px;border-bottom:1px solid #d9d3ca;margin-top:20px}
    .tabs button{border:0;background:#eee9e1;padding:9px 12px;border-radius:6px}.tabs .active{background:#a65d3b;color:#fff}
    .panel{margin-top:16px;background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:16px}
    .table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}td,th{text-align:left;padding:11px;border-bottom:1px solid #eee9e1;font-size:13px}
    th{color:#77736c;white-space:nowrap}.chip{background:#eee9e1;border-radius:20px;padding:4px 9px;font-size:11px}
    .compact{padding:7px 10px;font-size:12px}.empty{padding:16px;color:#77736c}
    .backdrop{position:fixed;inset:0;background:#19171477;z-index:10}.drawer{position:fixed;right:0;top:0;bottom:0;width:min(680px,95vw);overflow:auto;background:#f7f4ee;padding:24px;z-index:11}
    .drawer header{display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid #ddd6cc;padding-bottom:16px}
    .drawer h2{margin:4px 0}.icon{border:0;background:none;font-size:24px;cursor:pointer}.check-row,.line{display:flex;align-items:center;gap:12px;padding:12px;margin:9px 0;background:#fff;border:1px solid #e3ded6;border-radius:7px}
    .check-row>div{flex:1}.drawer label{display:grid;gap:6px;margin:12px 0}.drawer input,.drawer textarea{padding:10px;border:1px solid #d7d1c8;border-radius:6px}
    .facts{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin:18px 0}.facts div{padding:12px;background:#fff;border-radius:6px}
    .cancellation{margin-top:20px;padding-top:12px;border-top:1px solid #ddd6cc}
    @media(max-width:740px){.workspace{padding:18px 14px}.page-head{align-items:center}.facts{grid-template-columns:1fr}}
  `],
})
export class M10Page implements OnInit, OnDestroy {
  private readonly api = inject(ApiClient);
  readonly items = signal<any[]>([]);
  readonly eligible = signal<any[]>([]);
  readonly eligibilityLoading = signal(false);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly draftOpen = signal(false);
  readonly detail = signal<any | null>(null);
  statuses = ['Draft', 'Posted', 'Overdue', 'Paid', 'Cancelled'];
  filter = 'Draft';
  page = 1;
  readonly pageSize = 50;
  invoiceDate = new Date().toISOString().slice(0, 10);
  notes = '';
  cancelReason = '';
  billingModeOverride = '';
  billingModeOverrideReason = '';
  readonly billingModes = ['PerChallan', 'Daily', 'PerOrder', 'Monthly'];
  selectedIds = new Set<string>();
  private idempotencyKey = '';

  private readonly evidenceUpdated = () => {
    this.load();
    if (this.draftOpen()) this.refreshEligible();
  };

  ngOnInit(): void {
    this.load();
    window.addEventListener('m08:evidence-updated', this.evidenceUpdated);
  }

  ngOnDestroy(): void {
    window.removeEventListener('m08:evidence-updated', this.evidenceUpdated);
  }

  load(): void {
    this.loading.set(true);
    this.api.get<any>(`/m10/invoices?status=${encodeURIComponent(this.filter)}&page=${this.page}&pageSize=${this.pageSize}`).subscribe({
      next: (data) => { this.items.set(data.items); this.loading.set(false); },
      error: () => { this.error.set('Invoices unavailable. Check M10 activation and your role.'); this.loading.set(false); },
    });
  }

  setFilter(status: string): void { this.filter = status; this.page = 1; this.load(); }

  openDraft(): void {
    this.selectedIds.clear();
    this.billingModeOverride = '';
    this.billingModeOverrideReason = '';
    this.idempotencyKey = crypto.randomUUID();
    this.draftOpen.set(true);
    this.refreshEligible();
  }

  refreshEligible(): void {
    this.eligibilityLoading.set(true);
    this.error.set('');
    this.api.get<any>('/m10/eligible-challans').subscribe({
      next: (data) => {
        this.eligible.set(data.items);
        this.selectedIds.clear();
        this.eligibilityLoading.set(false);
      },
      error: () => {
        this.error.set('Eligible challans are unavailable. Refresh after checking M09 and M08 access.');
        this.eligibilityLoading.set(false);
      },
    });
  }

  toggle(id: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) this.selectedIds.add(id); else this.selectedIds.delete(id);
  }

  selectedBillingMode(): string | null {
    const selected = this.eligible().filter(row => this.selectedIds.has(row.challanId));
    if (!selected.length) return null;
    const modes = new Set(selected.map(row => row.billingMode as string | null));
    return modes.size === 1 ? (selected[0].billingMode ?? null) : null;
  }

  requiresBillingModeReason(): boolean {
    const configured = this.selectedBillingMode();
    return !!this.billingModeOverride && this.billingModeOverride !== configured;
  }

  canCreateDraft(): boolean {
    return this.selectedIds.size > 0 && this.selectedBillingMode() !== null &&
      (!this.requiresBillingModeReason() || !!this.billingModeOverrideReason.trim());
  }

  createDraft(): void {
    if (!this.canCreateDraft()) {
      this.error.set(this.selectedBillingMode()
        ? 'Provide a reason for changing the customer billing mode.'
        : 'Configure one consistent customer billing mode before creating an invoice.');
      return;
    }
    this.saving.set(true);
    this.api.post<any, any>('/m10/invoices/drafts', {
      challanIds: [...this.selectedIds], invoiceDate: this.invoiceDate, notes: this.notes,
      idempotencyKey: this.idempotencyKey,
      ...(this.billingModeOverride ? {
        billingModeOverride: this.billingModeOverride,
        billingModeOverrideReason: this.billingModeOverrideReason.trim(),
      } : {}),
    }).subscribe({
      next: (result) => {
        this.saving.set(false); this.draftOpen.set(false);
        this.notice.set('Invoice draft saved. Review the persisted draft and any server-side posting prerequisites.');
        this.load();
        this.openDetail(result.invoiceId);
      },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Invoice draft could not be created.'); },
    });
  }

  openDetail(id: string): void {
    this.api.get<any>(`/m10/invoices/${id}`).subscribe({
      next: (data) => this.detail.set(data),
      error: () => this.error.set('Invoice detail is unavailable.'),
    });
  }

  postInvoice(): void {
    const invoice = this.detail()?.invoice;
    if (!invoice) return;
    this.saving.set(true);
    this.api.post<any, any>(`/m10/invoices/${invoice.invoiceId}/post`, {
      version: invoice.version, reason: 'Posted after invoice preview review',
    }).subscribe({
      next: (result) => { this.saving.set(false); this.notice.set(`Invoice ${result.number} posted to M12.`); this.openDetail(invoice.invoiceId); this.load(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Invoice could not be posted.'); },
    });
  }
  cancelDraft(): void {
    const draft = this.detail()?.invoice;
    if (!draft) return;
    this.saving.set(true);
    this.api.post<any, any>(`/m10/invoices/${draft.invoiceId}/cancel-draft`, {
      version: draft.version, reason: 'Draft cancelled by authorized user',
    }).subscribe({
      next: () => {
        this.saving.set(false); this.detail.set(null);
        this.notice.set('Draft cancelled; linked challans have been released.'); this.load();
      },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Draft cancellation failed.'); },
    });
  }

  cancelPostedInvoice(): void {
    const invoice = this.detail()?.invoice;
    const reason = this.cancelReason.trim();
    if (!invoice || invoice.status !== 'Posted' || !reason) return;
    this.saving.set(true);
    this.api.post<any, any>(`/m10/invoices/${invoice.invoiceId}/cancel`, {
      version: invoice.version, reason,
    }).subscribe({
      next: (result) => {
        this.saving.set(false);
        this.cancelReason = '';
        this.notice.set(`Invoice cancelled. Reversal voucher ${result.reversalVoucherNumber} posted; ${result.releasedChallanCount} challan(s) released.`);
        this.openDetail(invoice.invoiceId);
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.error.set(error?.error?.detail ?? 'Posted invoice cancellation failed.');
      },
    });
  }
}
