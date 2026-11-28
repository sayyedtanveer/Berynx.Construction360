import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m09-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head">
        <div><p class="eyebrow">CONTROL · M09 DELIVERY PROOF</p><h1>Delivery challans</h1>
          <p class="muted">Persisted trip challans, receiver-verification state, and unbilled ageing.</p></div>
        <button class="secondary" type="button" (click)="load()">Refresh</button>
      </header>
      <p class="notice">Print preview is available in the API as HTML. PDF generation, secure sharing, invoice creation, corrections and daily alerts are not connected yet.</p>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p>
      <nav class="tabs" aria-label="Challan status">
        <button *ngFor="let status of statuses" [class.active]="filter === status" type="button" (click)="setFilter(status)">{{ status }}</button>
      </nav>
      <section class="panel">
        <div class="filters">
          <input aria-label="Customer filter" placeholder="Customer ID" [(ngModel)]="customerId">
          <input aria-label="From date" type="date" [(ngModel)]="from">
          <input aria-label="To date" type="date" [(ngModel)]="to">
          <button class="secondary" type="button" (click)="load()">Apply filters</button>
        </div>
        <p *ngIf="loading()">Loading challans…</p>
        <p class="empty" *ngIf="!loading() && !items().length">No persisted challans match this filter.</p>
        <div class="table-wrap" *ngIf="items().length">
          <table><thead><tr><th>Challan</th><th>Date</th><th>Customer / site</th><th>Order / product</th><th>Quantity</th><th>Value</th><th>Age</th><th>Status</th><th></th></tr></thead>
            <tbody><tr *ngFor="let row of items()">
              <td><strong>{{ row.number }}</strong><small>{{ row.evidenceState }}</small></td>
              <td>{{ row.challanDate | date:'mediumDate' }}</td>
              <td>{{ row.customerNameSnapshot }}<small>{{ row.siteAddressSnapshot }}</small></td>
              <td>{{ row.orderNumberSnapshot }}<small>{{ row.productNameSnapshot }}</small></td>
              <td>{{ row.deliveredQuantity | number:'1.0-3' }}</td>
              <td>{{ row.deliveredValueSnapshot | currency:'INR':'symbol':'1.2-2' }}</td>
              <td><span [class.aged]="row.ageDays > ageingDays">{{ row.ageDays }} days</span></td>
              <td><span class="chip">{{ row.status }}</span></td>
              <td><button class="secondary compact" type="button" (click)="open(row.challanId)">Details</button></td>
            </tr></tbody>
          </table>
        </div>
        <footer *ngIf="total()"><span>{{ total() }} challans · page {{ page }}</span>
          <div><button class="secondary compact" type="button" [disabled]="page <= 1" (click)="changePage(page - 1)">Previous</button>
            <button class="secondary compact" type="button" [disabled]="page * pageSize >= total()" (click)="changePage(page + 1)">Next</button></div></footer>
      </section>
      <div class="backdrop" *ngIf="detail()" (click)="detail.set(null)"></div>
      <aside class="drawer" *ngIf="detail()" aria-label="Challan detail">
        <header><div><p class="eyebrow">SCR-M09-02 · DETAIL</p><h2>{{ detail().challan.number }}</h2></div>
          <button class="icon" type="button" (click)="detail.set(null)" aria-label="Close">×</button></header>
        <p><span class="chip">{{ detail().challan.status }}</span> · {{ detail().challan.evidenceState }}</p>
        <div class="facts">
          <div><small>Customer</small><strong>{{ detail().challan.customerNameSnapshot }}</strong></div>
          <div><small>Site</small><strong>{{ detail().challan.siteAddressSnapshot }}</strong></div>
          <div><small>Order / PO</small><strong>{{ detail().challan.orderNumberSnapshot }} · {{ detail().challan.customerPoNumberSnapshot || '—' }}</strong></div>
          <div><small>Product / quantity</small><strong>{{ detail().challan.productNameSnapshot }} · {{ detail().challan.deliveredQuantity }} {{ detail().challan.unitCodeSnapshot }}</strong></div>
          <div><small>Value snapshot</small><strong>{{ detail().challan.deliveredValueSnapshot | currency:'INR':'symbol':'1.2-2' }}</strong></div>
          <div><small>Delivery</small><strong>{{ detail().challan.deliveredAt | date:'medium' }}</strong></div>
          <div><small>Vehicle / driver</small><strong>{{ detail().challan.vehicleNumberSnapshot }} · {{ detail().challan.driverNameSnapshot }}</strong></div>
          <div><small>Plant / batch</small><strong>{{ detail().challan.plantNameSnapshot }} · {{ detail().challan.batchSlipNumber }}</strong></div>
          <div><small>Receiver</small><strong>{{ detail().challan.receiverName || 'Not recorded' }} · {{ detail().challan.receiverMobile || '—' }}</strong></div>
          <div><small>Invoice</small><strong>{{ detail().challan.invoiceId || 'Pending M10' }}</strong></div>
        </div>
        <h3>History</h3><div class="history" *ngFor="let event of detail().history">
          <strong>{{ event.eventType }}</strong><small>{{ event.occurredAt | date:'medium' }}</small>
        </div>
        <h3>Corrections</h3><div class="empty" *ngIf="!detail().revisions.length">No correction revisions recorded.</div>
        <div class="history" *ngFor="let revision of detail().revisions">
          <strong>Revision {{ revision.revisionNumber }} · {{ revision.status }}</strong>
          <small>{{ revision.originalQuantity }} → {{ revision.correctedQuantity }} · {{ revision.reason }}</small>
        </div>
      </aside>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1500px;margin:auto;color:var(--ink,#292723)}
    .page-head{display:flex;align-items:flex-start;justify-content:space-between;gap:24px}
    .eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}
    h1{font-size:30px;margin:4px 0}.muted,small{color:#77736c}small{display:block;margin-top:4px}
    .notice{background:#f0e8de;padding:12px;border-radius:6px;font-size:13px;line-height:1.5}
    .error{color:#a3342a}.tabs{display:flex;gap:6px;border-bottom:1px solid #d9d3ca;margin-top:20px}
    .tabs button,.secondary{border:0;border-radius:6px;padding:9px 12px;background:#eee9e1;color:#39352f;cursor:pointer}
    .tabs .active{background:#a65d3b;color:white}.panel{margin-top:16px;background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:16px}
    .filters{display:flex;gap:10px;margin-bottom:14px}input{font:inherit;min-width:0;padding:9px 11px;border:1px solid #d7d1c8;border-radius:6px}
    .table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:11px;border-bottom:1px solid #eee9e1;font-size:13px}
    th{color:#77736c;font-weight:600;white-space:nowrap}.chip{background:#eee9e1;border-radius:20px;padding:4px 9px;font-size:11px}
    .aged{color:#a3342a;font-weight:700}.compact{padding:7px 10px;font-size:12px}.empty{padding:18px;color:#77736c}
    footer{display:flex;justify-content:space-between;padding-top:14px}.backdrop{position:fixed;inset:0;background:#19171477;z-index:10}
    .drawer{position:fixed;right:0;top:0;bottom:0;width:min(650px,95vw);overflow:auto;background:#f7f4ee;padding:24px;z-index:11}
    .drawer header{display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid #ddd6cc;padding-bottom:16px}
    .drawer h2{margin:4px 0}.icon{border:0;background:none;font-size:24px;cursor:pointer}.facts{display:grid;gap:13px;margin:18px 0}
    .history{display:flex;justify-content:space-between;gap:12px;padding:12px;background:#fff;border:1px solid #e3ded6;margin:8px 0}
    @media(max-width:740px){.workspace{padding:18px 14px}.filters{flex-direction:column}.page-head{align-items:center}table{min-width:950px}}
  `],
})
export class M09Page implements OnInit, OnDestroy {
  private readonly api = inject(ApiClient);
  readonly items = signal<any[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly detail = signal<any | null>(null);
  readonly ageingDays = 7;
  statuses = ['Unbilled', 'Unverified', 'Billed', 'Cancelled', 'All'];
  filter = 'Unbilled';
  customerId = '';
  from = '';
  to = '';
  page = 1;
  pageSize = 50;

  private readonly evidenceUpdated = () => this.load();

  ngOnInit(): void {
    this.load();
    window.addEventListener('m08:evidence-updated', this.evidenceUpdated);
  }

  ngOnDestroy(): void {
    window.removeEventListener('m08:evidence-updated', this.evidenceUpdated);
  }

  setFilter(status: string): void { this.filter = status; this.page = 1; this.load(); }
  changePage(page: number): void { this.page = page; this.load(); }

  load(): void {
    this.loading.set(true);
    const params = new URLSearchParams({ page: String(this.page), pageSize: String(this.pageSize) });
    if (this.filter !== 'All') {
      params.set('status', this.filter === 'Unbilled' ? 'Delivered' : this.filter === 'Billed' ? 'Invoiced' : this.filter);
    }
    if (this.customerId) params.set('customerId', this.customerId);
    if (this.from) params.set('from', this.from);
    if (this.to) params.set('to', this.to);
    this.api.get<any>(`/m09/challans?${params.toString()}`).subscribe({
      next: (data) => { this.items.set(data.items); this.total.set(data.total); this.loading.set(false); },
      error: () => { this.error.set('Challans unavailable. Check M09 activation and your role.'); this.loading.set(false); },
    });
  }

  open(challanId: string): void {
    this.api.get<any>(`/m09/challans/${challanId}`).subscribe({
      next: (data) => this.detail.set(data),
      error: () => this.error.set('Challan detail is unavailable.'),
    });
  }
}
