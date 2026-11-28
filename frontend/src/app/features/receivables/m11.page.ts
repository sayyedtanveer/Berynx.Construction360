import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m11-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head"><div><p class="eyebrow">FINANCE · M11 RECEIVABLES</p><h1>Receipts & collections</h1>
        <p class="muted">Draft receipts, posted invoice allocations and customer advances.</p></div>
        <button class="primary" type="button" (click)="openDraft()">Record receipt</button></header>
      <p class="notice">Cash, bank-transfer and UPI drafts can post through M12 when effective mappings and a receipt series exist. Cheque clearance and adjustment workflows remain blocked; draft receipts do not change posted balances.</p>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p><p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <section class="panel">
        <header><h2>Receipt drafts</h2><button class="secondary" type="button" (click)="load()">Refresh</button></header>
        <p class="empty" *ngIf="!items().length">No receipt drafts or posted receipts are available.</p>
        <div class="table-wrap" *ngIf="items().length"><table><thead><tr><th>Date</th><th>Customer</th><th>Method / reference</th><th>Amount</th><th>Allocated</th><th>Advance</th><th>Status</th><th>Action</th></tr></thead>
          <tbody><tr *ngFor="let row of items()"><td>{{ row.receiptDate | date:'mediumDate' }}</td>
            <td>{{ row.customerPartyId }}</td><td>{{ row.paymentMethod }}<small>{{ row.referenceNumber || '—' }}</small></td>
            <td>{{ row.amount | currency:'INR':'symbol':'1.2-2' }}</td><td>{{ row.allocatedAmount | currency:'INR':'symbol':'1.2-2' }}</td>
            <td>{{ row.advanceAmount | currency:'INR':'symbol':'1.2-2' }}</td><td><span class="chip">{{ row.status }}</span></td><td><button *ngIf="row.status === 'Draft'" class="secondary compact" type="button" [disabled]="saving()" (click)="postReceipt(row)">Post</button></td></tr></tbody></table></div>
      </section>
      <div class="backdrop" *ngIf="draftOpen()" (click)="draftOpen.set(false)"></div>
      <aside class="drawer" *ngIf="draftOpen()" aria-label="Receipt draft form">
        <header><div><p class="eyebrow">SCR-M11-02 · RECEIPT DRAFT</p><h2>Record receipt</h2></div>
          <button class="icon" type="button" (click)="draftOpen.set(false)" aria-label="Close">×</button></header>
        <form class="form" (ngSubmit)="saveDraft()">
          <label>Customer<select [(ngModel)]="form.customerPartyId" name="customer" required (ngModelChange)="loadOpenInvoices()">
            <option value="">Choose customer</option>
            <option *ngFor="let customer of customers()" [value]="customer.partyId">{{ customer.tradeName || customer.legalName }}{{ customer.gstin ? ' · ' + customer.gstin : '' }}</option>
          </select></label>
          <label>Branch<select [(ngModel)]="form.branchId" name="branch" required (ngModelChange)="loadOpenInvoices()">
            <option value="">Choose branch</option>
            <option *ngFor="let branch of branches()" [value]="branch.id">{{ branch.name }}{{ branch.gstin ? ' · ' + branch.gstin : '' }}</option>
          </select></label>
          <label>Receipt date<input [(ngModel)]="form.receiptDate" name="date" type="date" required></label>
          <label>Payment method<select [(ngModel)]="form.paymentMethod" name="method">
            <option>Cash</option><option>Bank transfer</option><option>UPI</option><option>Cheque</option><option>Post-dated cheque</option><option>Adjustment</option>
          </select></label>
          <label>Reference number<input [(ngModel)]="form.referenceNumber" name="reference"></label>
          <label>Amount<input [(ngModel)]="form.amount" name="amount" type="number" min="0.01" step="0.01" required></label>
          <div class="two"><label>TDS deducted<input [(ngModel)]="form.tdsAmount" name="tds" type="number" min="0" step="0.01"></label>
            <label>Bank charges<input [(ngModel)]="form.bankCharges" name="charges" type="number" min="0" step="0.01"></label></div>
          <div class="section-head"><h3>Posted invoices</h3><button class="secondary compact" type="button" (click)="autoAllocate()">Auto-allocate oldest first</button></div>
          <p class="empty" *ngIf="!openInvoices().length">No posted invoice balance is available for this customer.</p>
          <div class="invoice" *ngFor="let invoice of openInvoices()">
            <div><strong>{{ invoice.number }}</strong><small>{{ invoice.invoiceDate | date:'mediumDate' }} · outstanding {{ invoice.outstanding | currency:'INR':'symbol':'1.2-2' }}</small></div>
            <input type="number" min="0" step="0.01" [max]="invoice.outstanding" [(ngModel)]="allocations[invoice.invoiceId]" [name]="'allocation-' + invoice.invoiceId" aria-label="Allocation amount">
          </div>
          <p class="notice">Allocated {{ allocatedTotal() | currency:'INR':'symbol':'1.2-2' }} · advance {{ advanceAmount() | currency:'INR':'symbol':'1.2-2' }}</p>
          <label>Notes<textarea [(ngModel)]="form.notes" name="notes"></textarea></label>
          <button class="primary" type="submit" [disabled]="saving() || optionsLoading() || !customers().length || !branches().length">Save draft receipt</button>
        </form>
      </aside>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1400px;margin:auto;color:var(--ink,#292723)}
    .page-head{display:flex;justify-content:space-between;align-items:flex-start}.eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}
    h1{font-size:30px;margin:4px 0}.muted,small{color:#77736c}small{display:block;margin-top:4px}
    .primary,.secondary{border:0;border-radius:6px;padding:10px 14px;cursor:pointer}.primary{background:#a65d3b;color:#fff}.secondary{background:#eee9e1;color:#39352f}
    .notice{background:#f0e8de;padding:12px;border-radius:6px;font-size:13px;line-height:1.5}.error{color:#a3342a}.success{color:#24734f}
    .panel{margin-top:18px;background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:16px}.panel header,.section-head{display:flex;justify-content:space-between;align-items:center}
    .empty{padding:16px;color:#77736c}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:11px;border-bottom:1px solid #eee9e1;font-size:13px}
    .chip{background:#eee9e1;border-radius:20px;padding:4px 9px;font-size:11px}.backdrop{position:fixed;inset:0;background:#19171477;z-index:10}
    .drawer{position:fixed;right:0;top:0;bottom:0;width:min(620px,95vw);overflow:auto;background:#f7f4ee;padding:24px;z-index:11}
    .drawer header{display:flex;justify-content:space-between;border-bottom:1px solid #ddd6cc;padding-bottom:16px}.drawer h2{margin:4px 0}
    .icon{border:0;background:none;font-size:24px;cursor:pointer}.form{display:grid;gap:12px;padding:18px 0}.form label{display:grid;gap:6px;font-size:13px}
    input,select,textarea{font:inherit;padding:9px 11px;border:1px solid #d7d1c8;border-radius:6px}.two{display:flex;gap:10px}.two>*{flex:1}
    .invoice{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:10px;background:#fff;border:1px solid #e3ded6;border-radius:6px}
    .invoice input{width:140px}.compact{padding:7px 10px;font-size:12px}
    @media(max-width:740px){.workspace{padding:18px 14px}.page-head{align-items:center}.two{flex-direction:column}}
  `],
})
export class M11Page implements OnInit {
  private readonly api = inject(ApiClient);
  readonly items = signal<any[]>([]);
  readonly customers = signal<any[]>([]);
  readonly branches = signal<any[]>([]);
  readonly openInvoices = signal<any[]>([]);
  readonly optionsLoading = signal(false);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly draftOpen = signal(false);
  allocations: Record<string, number> = {};
  idempotencyKey = '';
  form: any = {};

  ngOnInit(): void { this.load(); }
  load(): void {
    this.loading.set(true);
    this.api.get<any[]>('/m11/receipts').subscribe({
      next: (rows) => { this.items.set(rows); this.loading.set(false); },
      error: () => { this.error.set('Receipts unavailable. Check M11 activation and your role.'); this.loading.set(false); },
    });
  }

  openDraft(): void {
    this.idempotencyKey = crypto.randomUUID();
    this.form = {
      customerPartyId: '', branchId: '', receiptDate: new Date().toISOString().slice(0, 10),
      paymentMethod: 'Bank transfer', referenceNumber: '', amount: 0,
      tdsAmount: 0, bankCharges: 0, notes: '',
    };
    this.allocations = {};
    this.openInvoices.set([]);
    this.draftOpen.set(true);
    this.loadReceiptOptions();
  }

  loadReceiptOptions(): void {
    this.optionsLoading.set(true);
    this.api.get<any>('/m11/receipt-options').subscribe({
      next: (data) => {
        this.customers.set(data.customers);
        this.branches.set(data.branches);
        if (data.branches.length === 1) this.form.branchId = data.branches[0].id;
        this.optionsLoading.set(false);
      },
      error: (error) => {
        this.optionsLoading.set(false);
        this.error.set(error?.error?.detail ?? 'Receipt customers and branches are unavailable for your current M11 scope.');
      },
    });
  }

  loadOpenInvoices(): void {
    if (!this.form.customerPartyId || !this.form.branchId) {
      this.openInvoices.set([]);
      return;
    }
    this.api.get<any>(`/m11/customers/${this.form.customerPartyId}/open-invoices?branchId=${encodeURIComponent(this.form.branchId)}`).subscribe({
      next: (data) => this.openInvoices.set(data.items),
      error: () => this.error.set('Posted invoice balances are unavailable.'),
    });
  }

  allocatedTotal(): number { return Object.values(this.allocations).reduce((sum, value) => sum + Number(value || 0), 0); }
  advanceAmount(): number { return Math.max(0, Number(this.form.amount || 0) - this.allocatedTotal()); }

  autoAllocate(): void {
    let remaining = Number(this.form.amount || 0);
    for (const invoice of this.openInvoices()) {
      const applied = Math.min(remaining, invoice.outstanding);
      this.allocations[invoice.invoiceId] = applied;
      remaining -= applied;
    }
  }

  postReceipt(receipt: any): void {
    this.saving.set(true);
    this.api.post<any, any>(`/m11/receipts/${receipt.receiptId}/post`, {
      version: receipt.version, reason: 'Posted after receipt evidence review',
    }).subscribe({
      next: (result) => { this.saving.set(false); this.notice.set(`Receipt ${result.number} posted to accounting.`); this.load(); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Receipt could not be posted.'); },
    });
  }
  saveDraft(): void {
    const selected = Object.entries(this.allocations)
      .filter(([, amount]) => Number(amount) > 0)
      .map(([invoiceId, amount]) => ({ invoiceId, amount: Number(amount) }));
    this.saving.set(true);
    this.api.post<any, any>('/m11/receipts/drafts', {
      ...this.form, allocations: selected, idempotencyKey: this.idempotencyKey,
    }).subscribe({
      next: (receipt) => {
        this.saving.set(false); this.draftOpen.set(false);
        this.notice.set(`Receipt draft saved. Advance ${receipt.advanceAmount}. Posting will recheck mappings, period controls and settlement eligibility.`);
        this.load();
      },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Receipt draft could not be saved.'); },
    });
  }
}
