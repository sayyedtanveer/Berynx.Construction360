import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m07-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head">
        <div><p class="eyebrow">BUY · M07 PROCUREMENT</p><h1>Purchase orders</h1>
          <p class="muted">Supplier commitments and purchase-order-matched goods receipts.</p></div>
        <div class="head-actions">
          <button class="secondary" type="button" (click)="load()">Refresh</button>
          <button class="secondary" type="button" (click)="loadTolerances()">Receipt tolerances</button>
          <button class="primary" type="button" (click)="openCreate()">＋ New purchase order</button>
        </div>
      </header>
      <section class="snapshot" *ngIf="tolerances() as settings">
        <h3>Goods-receipt overage limits</h3>
        <div class="filters">
          <label>Steel tolerance %<input [(ngModel)]="settings.steelPercent" type="number" min="0" max="100" step="0.0001"></label>
          <label>Cement tolerance %<input [(ngModel)]="settings.cementPercent" type="number" min="0" max="100" step="0.0001"></label>
          <button class="primary" type="button" (click)="saveTolerances()">Save tolerances</button>
          <button class="secondary" type="button" (click)="tolerances.set(null)">Close</button>
        </div>
      </section>
      <div class="toolbar"><div class="filters">
        <select aria-label="Filter purchase order status" [(ngModel)]="statusFilter" (change)="applyFilters()">
          <option value="">All statuses</option><option *ngFor="let status of statuses" [value]="status">{{ status }}</option>
        </select>
        <input aria-label="Supplier ID filter" placeholder="Supplier ID" [(ngModel)]="supplierFilter">
        <input aria-label="Sales order ID filter" placeholder="Sales order ID" [(ngModel)]="salesOrderFilter">
        <button class="secondary" type="button" (click)="applyFilters()">Apply</button>
      </div></div>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p>
      <p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <p class="loading" *ngIf="loading()">Loading purchase orders…</p>
      <section class="table-wrap" *ngIf="!loading()">
        <table><thead><tr><th>Purchase order</th><th>Supplier</th><th>Sales order</th><th>Status</th><th>Expected</th><th>Value</th><th>Actions</th></tr></thead>
          <tbody><tr *ngFor="let row of rows()">
            <td><button class="text-button" type="button" (click)="openDetail(row)">{{ row.number }}</button>
              <small>{{ row.orderDate | date:'mediumDate' }}</small></td>
            <td>{{ row.supplierNameSnapshot }}<small>{{ row.supplierPartyId }}</small></td>
            <td>{{ row.sourceSalesOrderId || 'Direct purchase' }}</td>
            <td><span class="chip" [attr.data-status]="row.status">{{ row.status }}</span></td>
            <td>{{ row.expectedDate ? (row.expectedDate | date:'mediumDate') : '—' }}</td>
            <td>{{ row.total === null ? 'Restricted' : (row.total | currency:'INR':'symbol':'1.2-2') }}</td>
            <td class="actions">
              <button *ngIf="row.status === 'Draft'" class="secondary compact" type="button" (click)="requestApproval(row)">Request approval</button>
              <button *ngIf="row.status === 'Pending approval'" class="secondary compact" type="button" (click)="release(row)">Release if approved</button>
              <button *ngIf="row.status === 'Approved'" class="secondary compact" type="button" (click)="markSent(row)">Record supplier dispatch</button>
              <button *ngIf="canReceive(row)" class="secondary compact" type="button" (click)="openReceipt(row)">Receive goods</button>
              <button class="secondary compact" type="button" (click)="openDetail(row)">Open</button>
            </td>
          </tr></tbody>
        </table>
        <div class="empty" *ngIf="!rows().length">No purchase orders match these filters.</div>
      </section>
      <nav class="pager" aria-label="Purchase order pages">
        <button class="secondary compact" type="button" (click)="previousPage()" [disabled]="loading() || page <= 1">Previous</button>
        <span>Page {{ page }} · {{ total() }} orders</span>
        <button class="secondary compact" type="button" (click)="nextPage()" [disabled]="loading() || page * pageSize >= total()">Next</button>
      </nav>

      <div class="backdrop" *ngIf="creating()" (click)="creating.set(false)"></div>
      <aside class="drawer" *ngIf="creating()" aria-label="Purchase order form">
        <header><div><p class="eyebrow">SCR-M07 · PURCHASE ORDER</p><h2>New purchase order</h2></div>
          <button class="icon" type="button" (click)="creating.set(false)" aria-label="Close">×</button></header>
        <form class="form-section" (ngSubmit)="create()">
          <label>Branch ID<input [(ngModel)]="draft.branchId" name="branchId" required></label>
          <label>Supplier<select [(ngModel)]="draft.supplierPartyId" name="supplierPartyId" required>
            <option value="">Select supplier</option>
            <option *ngFor="let supplier of suppliers()" [value]="supplier.partyId">{{ supplier.tradeName || supplier.legalName }}</option>
          </select></label>
          <label>Source sales order ID <span class="muted">(optional)</span><input [(ngModel)]="draft.sourceSalesOrderId" name="salesOrderId"></label>
          <label>Delivery site ID <span class="muted">(optional)</span><input [(ngModel)]="draft.siteId" name="siteId"></label>
          <div class="two"><label>Order date<input [(ngModel)]="draft.orderDate" name="orderDate" type="date" required></label>
            <label>Expected date<input [(ngModel)]="draft.expectedDate" name="expectedDate" type="date"></label></div>
          <label>Payment terms<input [(ngModel)]="draft.paymentTerms" name="paymentTerms" maxlength="1000" required></label>
          <label>Delivery terms<input [(ngModel)]="draft.deliveryTerms" name="deliveryTerms" maxlength="1000" required></label>
          <label>Notes<textarea [(ngModel)]="draft.notes" name="notes" maxlength="4000"></textarea></label>
          <h3>Items</h3>
          <div class="line" *ngFor="let line of draft.lines; let i = index">
            <label>Product<select [(ngModel)]="line.productId" [name]="'product' + i" required>
              <option value="">Select product</option><option *ngFor="let product of products()" [value]="product.productId">{{ product.code }} · {{ product.name }}</option>
            </select></label>
            <label>Source sales-order line ID <span class="muted">(required for linked orders)</span>
              <input [(ngModel)]="line.sourceSalesOrderLineId" [name]="'sourceLine' + i"></label>
            <div class="two"><label>Quantity<input [(ngModel)]="line.quantity" [name]="'quantity' + i" type="number" min="0.001" step="0.001" required></label>
              <label>Unit rate<input [(ngModel)]="line.rate" [name]="'rate' + i" type="number" min="0.0001" step="0.0001" required></label></div>
          </div>
          <button class="secondary" type="button" (click)="draft.lines.push(newLine())">＋ Add line</button>
          <p class="notice">Rates and tax are visible to authorized procurement users only. M03 effective tax rates are snapshotted; supplier-invoice tax matching is not yet available.</p>
          <button class="primary" type="submit" [disabled]="saving()">Create draft PO</button>
        </form>
      </aside>

      <div class="backdrop" *ngIf="detail()" (click)="detail.set(null)"></div>
      <aside class="drawer" *ngIf="detail()" aria-label="Purchase order details">
        <header><div><p class="eyebrow">PURCHASE ORDER</p><h2>{{ detail().number }}</h2></div>
          <button class="icon" type="button" (click)="detail.set(null)" aria-label="Close">×</button></header>
        <p><span class="chip">{{ detail().status }}</span> · {{ detail().supplierNameSnapshot }}</p>
        <p>Source sales order: {{ detail().sourceSalesOrderId || 'None' }}</p>
        <p *ngIf="detail().total !== null">Total: {{ detail().total | currency:'INR':'symbol':'1.2-2' }}</p>
        <p *ngIf="detail().total === null" class="notice">Cost totals are restricted by your M07 view-cost permission.</p>
        <h3>Lines</h3><div class="line" *ngFor="let line of detail().lines">
          <strong>{{ line.productNameSnapshot }}</strong>
          <small>Ordered {{ line.orderedQuantity }} {{ line.unitCode }} · received {{ line.receivedQuantity }}</small>
          <small *ngIf="line.rate !== null">Rate {{ line.rate | currency:'INR':'symbol':'1.2-4' }} · GST {{ line.gstPercent }}%</small>
        </div>
        <h3>Goods receipts</h3>
        <p *ngIf="!detail().receipts.length" class="empty">No goods receipts are recorded.</p>
        <div class="line" *ngFor="let receipt of detail().receipts">{{ receipt.number }} · {{ receipt.status }} · {{ receipt.receiptDate | date:'mediumDate' }}</div>
        <p class="notice">Supplier invoice posting, payment allocation and ledger balances are not yet implemented.</p>
      </aside>

      <div class="backdrop" *ngIf="receiving()" (click)="receiving.set(null)"></div>
      <aside class="drawer" *ngIf="receiving()" aria-label="Goods receipt form">
        <header><div><p class="eyebrow">SCR-M07 · GOODS RECEIPT</p><h2>Receive against {{ receiving().number }}</h2></div>
          <button class="icon" type="button" (click)="receiving.set(null)" aria-label="Close">×</button></header>
        <form class="form-section" (ngSubmit)="createReceipt()">
          <label>Receipt date<input [(ngModel)]="receiptDraft.receiptDate" name="receiptDate" type="date" required></label>
          <label>Vehicle number<input [(ngModel)]="receiptDraft.vehicleNumber" name="vehicle" maxlength="32"></label>
          <label>Attachment reference<input [(ngModel)]="receiptDraft.attachmentReference" name="attachment"></label>
          <div class="line" *ngFor="let line of receiptDraft.lines; let i = index">
            <strong>{{ line.productNameSnapshot }}</strong>
            <small>Open order balance {{ line.orderedQuantity - line.receivedQuantity }} {{ line.unitCode }}</small>
            <label>Received quantity<input [(ngModel)]="line.receivedQuantity" [name]="'receiptQty' + i" type="number" min="0.001" step="0.001" required></label>
            <label>Lot / heat number<input [(ngModel)]="line.lotOrHeatNumber" [name]="'lot' + i"></label>
            <label>MTC reference<input [(ngModel)]="line.mtcReference" [name]="'mtc' + i"></label>
            <label>Exception reason<input [(ngModel)]="line.exceptionReason" [name]="'reason' + i"></label>
          </div>
          <p class="notice">Over-receipt beyond configured material tolerance requires M02 approval. Posting a GRN updates quantities only; it does not post stock or accounting entries.</p>
          <button class="primary" type="submit" [disabled]="saving()">Create and post GRN</button>
        </form>
      </aside>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1440px;margin:0 auto;color:var(--ink,#292723)}
    .page-head{display:flex;justify-content:space-between;align-items:flex-start;gap:24px;margin-bottom:24px}
    .eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}
    h1{font-size:30px;margin:4px 0}.muted,small{color:var(--muted,#77736c)}small{display:block;margin-top:4px}
    .head-actions,.filters,.actions,.two{display:flex;gap:10px;align-items:center}.toolbar{margin-bottom:16px}
    input,select,textarea{font:inherit;padding:10px 12px;border:1px solid #d7d1c8;border-radius:6px;background:#fff;min-width:0}
    button{font:inherit;border:0;border-radius:6px;padding:10px 14px;cursor:pointer}
    .primary{background:var(--copper,#a65d3b);color:white}.secondary{background:#eee9e1;color:#39352f}
    .compact{padding:7px 10px;font-size:12px}.text-button{background:none;color:var(--copper,#a65d3b);padding:0;font-weight:700;text-align:left}
    .table-wrap{overflow:auto;background:#fff;border:1px solid #e3ded6;border-radius:8px}
    table{width:100%;border-collapse:collapse;min-width:1000px}th,td{text-align:left;padding:13px 14px;border-bottom:1px solid #eee9e1;vertical-align:top}
    th{font-size:12px;color:#77736c;background:#f8f6f2}td{font-size:13px}.chip{display:inline-block;background:#eee9e1;border-radius:20px;padding:4px 9px;font-size:11px}
    .pager{display:flex;align-items:center;justify-content:center;gap:16px;padding:18px}.empty,.loading{padding:20px;color:#77736c}
    .error{color:#a3342a}.success{color:#24734f}.backdrop{position:fixed;inset:0;background:#19171477;z-index:10}
    .drawer{position:fixed;right:0;top:0;bottom:0;width:min(640px,95vw);overflow:auto;background:#f7f4ee;padding:24px;z-index:11;box-shadow:-8px 0 30px #0002}
    .drawer header{display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid #ddd6cc;padding-bottom:16px}
    .drawer h2{margin:4px 0}.icon{background:transparent;font-size:24px}.form-section{display:grid;gap:14px;padding:18px 0}
    label{display:grid;gap:6px;font-size:13px}.two>*{flex:1}.line{border:1px solid #e1dbd1;border-radius:7px;padding:12px;display:grid;gap:9px;background:#fff}
    .notice{background:#f0e8de;padding:12px;border-radius:6px;font-size:13px;line-height:1.5}
    @media(max-width:760px){.workspace{padding:18px 14px}.page-head{display:block}.head-actions{margin-top:16px}.filters{align-items:stretch;flex-direction:column}.two{flex-direction:column}}
  `],
})
export class M07Page implements OnInit {
  private readonly api = inject(ApiClient);
  readonly rows = signal<any[]>([]);
  readonly suppliers = signal<any[]>([]);
  readonly products = signal<any[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly creating = signal(false);
  readonly detail = signal<any | null>(null);
  readonly receiving = signal<any | null>(null);
  readonly tolerances = signal<any | null>(null);
  readonly statuses = ['Draft', 'Pending approval', 'Approved', 'Sent', 'Partially received', 'Received', 'Closed', 'Cancelled'];
  statusFilter = '';
  supplierFilter = '';
  salesOrderFilter = '';
  page = 1;
  pageSize = 25;
  draft = this.emptyDraft();
  receiptDraft: any = {};

  ngOnInit(): void {
    this.load();
    this.api.get<any[]>('/m03/parties?role=Supplier').subscribe({
      next: (rows) => this.suppliers.set(rows),
      error: () => this.error.set('Supplier lookup is unavailable.'),
    });
    this.api.get<any[]>('/m03/products').subscribe({
      next: (rows) => this.products.set(rows),
      error: () => this.error.set('Product lookup is unavailable.'),
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  previousPage(): void {
    if (this.loading() || this.page <= 1) return;
    this.page = Math.max(1, this.page - 1);
    this.load();
  }

  nextPage(): void {
    if (this.loading()) return;
    const lastPage = Math.max(1, Math.ceil(this.total() / this.pageSize));
    if (this.page >= lastPage) return;
    this.page = Math.min(lastPage, this.page + 1);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const query = new URLSearchParams({ page: String(this.page), pageSize: String(this.pageSize) });
    if (this.statusFilter) query.set('status', this.statusFilter);
    if (this.supplierFilter.trim()) query.set('supplierPartyId', this.supplierFilter.trim());
    if (this.salesOrderFilter.trim()) query.set('salesOrderId', this.salesOrderFilter.trim());
    this.api.get<any>(`/m07/purchase-orders?${query}`).subscribe({
      next: (response) => {
        const total = response.total ?? 0;
        const lastPage = Math.max(1, Math.ceil(total / this.pageSize));
        this.total.set(total);
        if (this.page > lastPage) {
          this.page = lastPage;
          this.loading.set(false);
          this.load();
          return;
        }
        this.rows.set(response.rows ?? []);
        this.loading.set(false);
      },
      error: () => { this.error.set('Purchase orders could not be loaded. Check M07 activation and your role.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.draft = this.emptyDraft();
    this.creating.set(true);
  }

  newLine(): any { return { productId: '', sourceSalesOrderLineId: '', quantity: 1, rate: 0.01 }; }
  canReceive(row: any): boolean { return ['Approved', 'Sent', 'Partially received'].includes(row.status); }

  loadTolerances(): void {
    this.api.get<any>('/m07/settings/receipt-tolerances').subscribe({
      next: (value) => this.tolerances.set(value),
      error: () => this.error.set('Receipt tolerances are unavailable; check your M07 configuration permission.'),
    });
  }

  saveTolerances(): void {
    const settings = this.tolerances();
    if (!settings) return;
    this.api.put<any, any>('/m07/settings/receipt-tolerances', settings).subscribe({
      next: (value) => { this.tolerances.set(value); this.notice.set('Goods-receipt tolerances updated.'); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Receipt tolerances could not be saved.'),
    });
  }

  create(): void {
    this.saving.set(true);
    const body = {
      ...this.draft,
      sourceSalesOrderId: this.draft.sourceSalesOrderId || null,
      siteId: this.draft.siteId || null,
      expectedDate: this.draft.expectedDate || null,
      idempotencyKey: crypto.randomUUID(),
      lines: this.draft.lines.map((line: any) => ({
        ...line, sourceSalesOrderLineId: line.sourceSalesOrderLineId || null,
        quantity: Number(line.quantity), rate: Number(line.rate),
      })),
      charges: [],
    };
    this.api.post<any, any>('/m07/purchase-orders', body).subscribe({
      next: (response) => {
        this.saving.set(false); this.creating.set(false);
        this.notice.set(`Draft ${response.number} created. Submit it for approval before supplier release.`);
        this.load();
      },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Purchase order could not be created.'); },
    });
  }

  requestApproval(row: any): void {
    this.transition(`/m07/purchase-orders/${row.purchaseOrderId}/request-approval`, row.version,
      'Approval request submitted.');
  }

  release(row: any): void {
    this.transition(`/m07/purchase-orders/${row.purchaseOrderId}/release`, row.version,
      'Purchase order released after M02 approval.');
  }

  markSent(row: any): void {
    this.transition(`/m07/purchase-orders/${row.purchaseOrderId}/supplier-acknowledgement`, row.version,
      'Status updated. External supplier communication is not connected.');
  }

  private transition(path: string, version: number, success: string): void {
    this.api.post<any, any>(path, { version }).subscribe({
      next: (response) => { this.notice.set(response?.communicationState ?? success); this.load(); },
      error: (error) => this.error.set(error?.error?.detail ?? 'The purchase-order action failed.'),
    });
  }

  openDetail(row: any): void {
    this.api.get<any>(`/m07/purchase-orders/${row.purchaseOrderId}`).subscribe({
      next: (value) => this.detail.set(value),
      error: () => this.error.set('Purchase order details are unavailable.'),
    });
  }

  openReceipt(row: any): void {
    this.api.get<any>(`/m07/purchase-orders/${row.purchaseOrderId}`).subscribe({
      next: (value) => {
        this.receiving.set(value);
        this.receiptDraft = {
          receiptDate: new Date().toISOString().slice(0, 10),
          vehicleNumber: '',
          attachmentReference: '',
          lines: value.lines.filter((line: any) => line.orderedQuantity > line.receivedQuantity)
            .map((line: any) => ({
              purchaseOrderLineId: line.purchaseOrderLineId,
              productNameSnapshot: line.productNameSnapshot,
              unitCode: line.unitCode,
              orderedQuantity: line.orderedQuantity,
              receivedQuantity: Math.max(0.001, line.orderedQuantity - line.receivedQuantity),
              damagedQuantity: 0,
              shortQuantity: 0,
              lotOrHeatNumber: '',
              mtcReference: '',
              exceptionReason: '',
            })),
        };
      },
      error: () => this.error.set('Purchase order lines could not be loaded for receipt.'),
    });
  }

  createReceipt(): void {
    const order = this.receiving();
    if (!order) return;
    this.saving.set(true);
    const lines = this.receiptDraft.lines.map((line: any) => ({
      ...line,
      receivedQuantity: Number(line.receivedQuantity),
      exceptionReason: line.exceptionReason || null,
      lotOrHeatNumber: line.lotOrHeatNumber || null,
      mtcReference: line.mtcReference || null,
      manufacturerName: null,
    }));
    this.api.post<any, any>(`/m07/purchase-orders/${order.purchaseOrderId}/goods-receipts`, {
      ...this.receiptDraft, idempotencyKey: crypto.randomUUID(), lines,
    }).subscribe({
      next: (receipt) => this.api.post<any, any>(`/m07/goods-receipts/${receipt.goodsReceiptId}/post`,
        { version: receipt.version }).subscribe({
          next: (result) => {
            this.saving.set(false); this.receiving.set(null);
            this.notice.set(result?.status === 'Pending approval'
              ? 'GRN is awaiting M02 over-receipt approval.'
              : 'GRN posted; quantities updated. Stock and accounting effects remain unavailable.');
            this.load();
          },
          error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'GRN posting failed.'); },
        }),
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Goods receipt could not be created.'); },
    });
  }

  private emptyDraft(): any {
    return {
      branchId: '', supplierPartyId: '', sourceSalesOrderId: '', siteId: '',
      orderDate: new Date().toISOString().slice(0, 10), expectedDate: '',
      paymentTerms: 'As agreed', deliveryTerms: 'As agreed', notes: '', lines: [this.newLine()],
    };
  }
}
