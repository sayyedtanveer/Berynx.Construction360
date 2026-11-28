import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m06-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head">
        <div><p class="eyebrow">SELL · M06 SALES ORDERS</p><h1>Sales orders</h1>
          <p class="muted">Customer commitments, controlled confirmation and real fulfilment links.</p></div>
        <div class="head-actions">
          <button class="secondary" type="button" (click)="loadPendingReport()">Pending-order report</button>
          <button *ngIf="canConfigure()" class="secondary" type="button" (click)="loadTolerances()">Delivery tolerances</button>
          <button class="primary" type="button" (click)="openCreate()">＋ New order</button>
        </div>
      </header>
      <section class="snapshot" *ngIf="toleranceSettings() as settings">
        <h3>Tenant delivery tolerances</h3>
        <div class="filters">
          <label>RMC overdelivery %<input [(ngModel)]="settings.rmcPercent" type="number" min="0" max="100" step="0.0001"></label>
          <label>Steel overdelivery %<input [(ngModel)]="settings.steelPercent" type="number" min="0" max="100" step="0.0001"></label>
          <button class="primary" type="button" (click)="saveTolerances()">Save tolerances</button>
          <button class="secondary" type="button" (click)="toleranceSettings.set(null)">Close</button>
        </div>
      </section>
      <div class="toolbar">
        <div class="filters">
          <input aria-label="Search sales orders" placeholder="Order, customer or PO" [(ngModel)]="search" (keyup.enter)="load()">
          <select aria-label="Filter order status" [(ngModel)]="statusFilter" (change)="load()">
            <option value="">All statuses</option><option *ngFor="let status of statuses" [value]="status">{{ status }}</option>
          </select>
          <input aria-label="Filter customer" placeholder="Customer ID" [(ngModel)]="customerFilter">
          <input aria-label="Filter site" placeholder="Site ID" [(ngModel)]="siteFilter">
          <button class="secondary" type="button" (click)="load()">Apply</button>
        </div>
      </div>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p>
      <p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <p class="loading" *ngIf="loading()">Loading sales orders…</p>
      <section class="table-wrap" *ngIf="!loading()">
        <table><thead><tr><th>Sales order</th><th>Customer / site</th><th>Source</th><th>Status</th><th>Order date</th><th>Value</th><th>Actions</th></tr></thead>
          <tbody><tr *ngFor="let row of rows()">
            <td><button class="text-button" type="button" (click)="openDetail(row)">{{ row.number }}</button><small>{{ row.orderType }}</small></td>
            <td>{{ row.customerNameSnapshot }}<small>{{ row.siteId || 'No site linked' }}</small></td>
            <td>{{ row.sourceQuotationNumber || 'Direct' }}</td>
            <td><span class="chip" [attr.data-status]="row.status">{{ row.status }}</span></td>
            <td>{{ row.orderDate | date:'mediumDate' }}</td>
            <td>{{ row.total | currency:'INR':'symbol':'1.2-2' }}</td>
            <td class="actions"><button class="secondary compact" type="button" (click)="openDetail(row)">Open</button>
              <button *ngIf="row.status === 'Draft' || row.status === 'Pending approval'" class="secondary compact" type="button" (click)="confirm(row)">Confirm</button></td>
          </tr></tbody>
        </table>
        <div class="empty" *ngIf="!rows().length">No sales orders match these filters.</div>
      </section>
      <nav class="pager" aria-label="Sales order pages">
        <button class="secondary compact" type="button" (click)="previousPage()" [disabled]="page <= 1">Previous</button>
        <span>Page {{ page }} · {{ total() }} orders</span>
        <button class="secondary compact" type="button" (click)="nextPage()" [disabled]="page * pageSize >= total()">Next</button>
      </nav>

      <div class="backdrop" *ngIf="creating()" (click)="creating.set(false)"></div>
      <aside class="drawer" *ngIf="creating()" aria-label="Sales order form">
        <header><div><p class="eyebrow">SCR-M06-02 · ORDER FORM</p><h2>New sales order</h2></div>
          <button class="icon" type="button" (click)="creating.set(false)" aria-label="Close">×</button></header>
        <div class="tabs"><button type="button" [class.active]="createMode() === 'quote'" (click)="createMode.set('quote')">From accepted quotation</button>
          <button type="button" [class.active]="createMode() === 'direct'" (click)="createMode.set('direct')">Direct order</button></div>
        <section *ngIf="createMode() === 'quote'" class="form-section">
          <p class="notice">Accepted quote lines, price and tax snapshots are copied by the server. Consumed quantity is excluded; each conversion needs an idempotency key.</p>
          <label>Accepted quotation<select [(ngModel)]="selectedQuoteId" name="sourceQuote" (change)="loadQuoteLines()" required>
            <option value="">Select accepted quotation</option>
            <option *ngFor="let quote of acceptedQuotes()" [value]="quote.quotationId">{{ quote.number }} · {{ quote.customerNameSnapshot }} · {{ quote.total | currency:'INR':'symbol':'1.2-2' }}</option>
          </select></label>
          <div class="line" *ngFor="let line of quoteLines">
            <strong>{{ line.description }}</strong>
            <small>Remaining {{ line.remainingQuantity }} {{ line.unitCode }} · {{ line.rate | currency:'INR':'symbol':'1.2-4' }}</small>
            <label>Convert quantity<input [(ngModel)]="line.orderQuantity" [name]="'quoteQty' + line.lineId"
              type="number" min="0.001" [max]="line.remainingQuantity" step="0.001"></label>
          </div>
          <label>Customer PO number<input [(ngModel)]="quotePoNumber" name="quotePo" maxlength="100"></label>
          <label>Customer PO date<input [(ngModel)]="quotePoDate" name="quotePoDate" type="date"></label>
          <label>PO attachment reference<input [(ngModel)]="quotePoAttachment" name="quotePoAttachment" maxlength="500" placeholder="Existing authorized file reference"></label>
          <label>Price validity until<input [(ngModel)]="quoteRateValidity" name="quoteRateValidity" type="date"></label>
          <label>Notes<textarea [(ngModel)]="quoteNotes" name="quoteNotes" maxlength="4000"></textarea></label>
          <button class="primary" type="button" [disabled]="saving()" (click)="convertQuote()">Create from quotation</button>
        </section>
        <form *ngIf="createMode() === 'direct'" (ngSubmit)="createDirect()" class="form-section">
          <p class="notice">Direct creation uses current M03 price and tax masters. Manual rates require server-side price-override permission.</p>
          <label>Customer<select [(ngModel)]="draft.customerPartyId" name="customer" (change)="customerChanged()" required>
            <option value="">Select customer</option><option *ngFor="let customer of customers()" [value]="customer.partyId">{{ customer.tradeName || customer.legalName }}</option>
          </select></label>
          <label>Site<select [(ngModel)]="draft.siteId" name="site"><option value="">No site</option>
            <option *ngFor="let site of customerSites()" [value]="site.siteId">{{ site.name }} · {{ site.city }}</option></select></label>
          <div class="two"><label>Order date<input [(ngModel)]="draft.orderDate" name="orderDate" type="date" required></label>
            <label>Rate valid until<input [(ngModel)]="draft.rateValidityUntil" name="rateValidity" type="date" required></label></div>
          <label>Price basis<select [(ngModel)]="draft.priceBasis" name="priceBasis"><option>ExPlant</option><option>Delivered</option><option>Site</option></select></label>
          <label>Place-of-supply state code<input [(ngModel)]="draft.placeOfSupplyStateCode" name="place" maxlength="2" inputmode="numeric" required></label>
          <div class="two"><label>Customer PO number<input [(ngModel)]="draft.customerPoNumber" name="poNumber" maxlength="100"></label>
            <label>Customer PO date<input [(ngModel)]="draft.customerPoDate" name="poDate" type="date"></label></div>
          <label>Payment terms<textarea [(ngModel)]="draft.paymentTerms" name="paymentTerms" maxlength="2000" required></textarea></label>
          <label>Delivery terms<textarea [(ngModel)]="draft.deliveryTerms" name="deliveryTerms" maxlength="2000" required></textarea></label>
          <label>PO attachment reference<input [(ngModel)]="draft.customerPoAttachmentReference" name="poAttachment" maxlength="500" placeholder="Existing authorized file reference"></label>
          <section class="order-line" *ngFor="let line of draft.lines; let i = index">
            <header><h3>Product line {{ i + 1 }}</h3><button *ngIf="draft.lines.length > 1" type="button" class="secondary compact" (click)="removeLine(i)">Remove</button></header>
            <label>Product<select [name]="'product' + i" [(ngModel)]="line.productId" (change)="productChanged(i)" required>
              <option value="">Select M03 product</option><option *ngFor="let product of products()" [value]="product.productId">{{ product.name }} · {{ product.baseUnitCode }}</option>
            </select></label>
            <div class="three"><label>Quantity<input [name]="'quantity' + i" [(ngModel)]="line.quantity" type="number" min="0.001" step="0.001" required></label>
              <label>Unit<input [name]="'unit' + i" [(ngModel)]="line.unitCode" readonly></label>
              <label>Rate override<input [name]="'rate' + i" [(ngModel)]="line.rate" type="number" min="0" step="0.0001" placeholder="M03 price"></label></div>
            <label>Discount %<input [name]="'discount' + i" [(ngModel)]="line.discountPercent" type="number" min="0" max="100" step="0.0001"></label>
            <div class="four" *ngIf="productFor(line)?.category === 'RMC'">
              <label>Grade<input [name]="'grade' + i" [(ngModel)]="line.rmcGrade" required></label>
              <label>Slump band<input [name]="'slump' + i" [(ngModel)]="line.rmcSlumpBand" required></label>
              <label>Admixture<input [name]="'admixture' + i" [(ngModel)]="line.rmcAdmixtureCode" required></label>
              <label>Pump type<input [name]="'pump' + i" [(ngModel)]="line.rmcPumpType" required></label>
            </div>
          </section>
          <button type="button" class="secondary" (click)="addLine()">＋ Add product line</button>
          <label>Notes<textarea [(ngModel)]="draft.notes" name="notes" maxlength="4000"></textarea></label>
          <button class="primary" type="submit" [disabled]="saving()">Save direct order</button>
        </form>
        <p class="error" *ngIf="error()">{{ error() }}</p>
      </aside>

      <div class="backdrop" *ngIf="detail()" (click)="detail.set(null)"></div>
      <aside class="drawer detail" *ngIf="detail()" aria-label="Sales order detail and fulfilment tracker">
        <header><div><p class="eyebrow">SCR-M06-03 · {{ detail()?.number }}</p><h2>{{ detail()?.customerNameSnapshot }}</h2></div>
          <button class="icon" type="button" (click)="detail.set(null)" aria-label="Close">×</button></header>
        <p><span class="chip" [attr.data-status]="detail()?.status">{{ detail()?.status }}</span>
          · {{ detail()?.orderType }} · {{ detail()?.orderDate | date:'mediumDate' }}</p>
        <p>Customer PO: {{ detail()?.customerPoNumber || 'Not supplied' }} · Site: {{ detail()?.siteAddressSnapshot || 'Not supplied' }}</p>
        <p>Quotation: {{ detail()?.sourceQuotationNumber || 'Direct order' }} · Frozen rates through {{ detail()?.rateValidityUntil || 'Not set' }}</p>
        <section class="snapshot"><h3>Credit and cost controls</h3>
          <p>Credit: <b>{{ detail()?.creditStatus }}</b></p><small>{{ detail()?.creditSnapshot?.decision?.reasons?.join(', ') || 'Credit evaluation is run at confirmation.' }}</small>
          <p>Margin: <b>{{ detail()?.costStatus === 'Unavailable' ? 'Unavailable' : 'Restricted' }}</b></p>
          <small>Purchase-cost facts are pending M07. No margin figure is fabricated.</small>
        </section>
        <h3>Fulfilment tracker</h3>
        <div class="tracker">
          <div><b>{{ detail()?.fulfilment?.orderedQuantity | number:'1.0-3' }} {{ detail()?.fulfilment?.quantityUnitCode || (detail()?.fulfilment?.mixedUnits ? 'Mixed units' : '') }}</b><small>Ordered</small></div>
          <div><b>{{ detail()?.fulfilment?.scheduledQuantity | number:'1.0-3' }} {{ detail()?.fulfilment?.quantityUnitCode || (detail()?.fulfilment?.mixedUnits ? 'Mixed units' : '') }}</b><small>Scheduled</small></div>
          <div><b>{{ detail()?.fulfilment?.deliveredQuantity ?? '—' }}</b><small>Delivered · M08 pending</small></div>
          <div><b>{{ detail()?.fulfilment?.invoicedQuantity ?? '—' }}</b><small>Invoiced · M10 pending</small></div>
          <div><b>{{ detail()?.fulfilment?.paidAmount ?? '—' }}</b><small>Paid · M11 pending</small></div>
        </div>
        <p class="notice">{{ detail()?.fulfilment?.downstreamState }}</p>
        <h3>Order lines</h3>
        <div class="line" *ngFor="let line of detail()?.lines">
          <strong>{{ line.productNameSnapshot }}</strong>
          <small>{{ line.orderedQuantity }} {{ line.unitCode }} × {{ line.rate | number:'1.2-4' }} · GST {{ line.gstPercent }}%</small>
          <small>Delivered: {{ line.deliveredQuantity || 0 }} · Invoiced: {{ line.invoicedQuantity || 0 }} · balance: {{ line.orderedQuantity - line.deliveredQuantity }}</small>
          <b>{{ line.total | currency:'INR':'symbol':'1.2-2' }}</b>
        </div>
        <h3>Delivery schedule</h3>
        <div class="line" *ngFor="let row of detail()?.schedules">
          <strong>{{ row.deliveryDate | date:'mediumDate' }} · {{ row.quantity }} {{ row.timeWindow }}</strong>
          <small>{{ row.status }} · {{ row.operationalNotes || 'No operational note' }}</small>
          <button *ngIf="row.status === 'Pending approval' && row.approvalStatus === 'Approved'"
            class="secondary compact" type="button" (click)="applySchedule(row)">Apply approved schedule</button>
        </div>
        <button class="secondary" *ngIf="['Confirmed','In fulfilment'].includes(detail()?.status)" type="button" (click)="addSchedule()">Schedule delivery</button>
        <button class="secondary" type="button" (click)="showM07Pending()">Create purchase order</button>
        <button class="secondary" *ngIf="['Confirmed','In fulfilment','On hold'].includes(detail()?.status)" type="button" (click)="beginAmendment()">Amend order</button>
        <button class="secondary" *ngIf="['Draft','Pending approval'].includes(detail()?.status)" type="button" (click)="confirm(detail())">Confirm order</button>
        <button class="secondary" *ngIf="['Confirmed','In fulfilment'].includes(detail()?.status)" type="button" (click)="hold(detail())">Put on hold</button>
        <button class="danger" *ngIf="!['Cancelled','Completed','Short-closed'].includes(detail()?.status)" type="button" (click)="cancelOrShortClose(detail())">Cancel / short-close</button>
        <button class="secondary" type="button" (click)="printConfirmation(detail())">Order confirmation</button>
        <section *ngIf="amending()" class="amend-panel">
          <h3>SCR-M06-04 · Amendment comparison</h3>
          <label>Reason<input [(ngModel)]="amendment.reason" maxlength="1000" required></label>
          <label>Site<select [(ngModel)]="amendment.siteId"><option [ngValue]="null">Keep current site</option>
            <option *ngFor="let site of sites()" [ngValue]="site.siteId">{{ site.name }}</option></select></label>
          <label>Line<select [(ngModel)]="amendment.lineId"><option *ngFor="let line of detail()?.lines" [ngValue]="line.lineId">{{ line.productNameSnapshot }}</option></select></label>
          <div class="two"><label>New quantity<input [(ngModel)]="amendment.quantity" type="number" min="0.001" step="0.001"></label>
            <label>New rate<input [(ngModel)]="amendment.rate" type="number" min="0" step="0.0001"></label></div>
          <button class="primary" type="button" (click)="submitAmendment()">Submit for approval</button>
          <button class="secondary" type="button" (click)="amending.set(false)">Close comparison</button>
          <p class="notice">Before and proposed snapshots are kept immutable. Fulfilled lines cannot be amended in place.</p>
        </section>
        <h3>Revision history</h3>
        <div class="line" *ngFor="let amendment of detail()?.amendments">
          <strong>Revision {{ amendment.revisionNumber }} · {{ amendment.status }}</strong>
          <small>{{ amendment.reason }} · {{ amendment.createdAt | date:'medium' }}</small>
          <button class="secondary compact" type="button" (click)="compare(amendment)">Compare before / after</button>
          <button *ngIf="amendment.status === 'Pending approval' && amendment.approvalStatus === 'Approved'" class="secondary compact" type="button" (click)="applyAmendment(amendment)">Apply approved amendment</button>
        </div>
        <section *ngIf="comparison()" class="comparison">
          <h3>Before / proposed</h3>
          <pre>{{ comparison() | json }}</pre>
          <button class="secondary" type="button" (click)="comparison.set(null)">Close comparison</button>
        </section>
        <h3>Linked documents</h3>
        <p *ngFor="let document of detail()?.documents">{{ document.sourceModule }} · {{ document.documentType }} · {{ document.documentNumber }} · {{ document.status }}</p>
        <p *ngIf="!detail()?.documents?.length" class="notice">No downstream PO, dispatch, challan, invoice or receipt is persisted for this order yet.</p>
        <h3>Audit timeline</h3><div class="timeline" *ngFor="let item of detail()?.activities">
          <b>{{ item.action }}</b><small>{{ item.at | date:'medium' }}</small>
        </div>
      </aside>

      <section class="report" *ngIf="report()">
        <header><div><p class="eyebrow">US-M06-08 · PENDING ORDERS</p><h2>Pending-order report</h2></div>
          <button class="secondary" type="button" (click)="exportReport()">Export CSV</button>
          <button class="icon" type="button" (click)="report.set(null)" aria-label="Close report">×</button></header>
        <div class="filters">
          <input aria-label="Report customer filter" placeholder="Customer ID" [(ngModel)]="customerFilter">
          <input aria-label="Report site filter" placeholder="Site ID" [(ngModel)]="siteFilter">
          <input aria-label="Report product filter" placeholder="Product ID" [(ngModel)]="productFilter">
          <button class="secondary" type="button" (click)="loadPendingReport()">Apply filters</button>
        </div>
        <div class="table-wrap"><table><thead><tr><th>Order</th><th>Customer / site</th><th>Product</th><th>Ordered</th><th>Scheduled</th><th>Remaining</th><th>Value</th><th></th></tr></thead>
          <tbody><tr *ngFor="let row of report()?.rows"><td>{{ row.number }}</td><td>{{ row.customerNameSnapshot }}<small>{{ row.siteId || '—' }}</small></td>
            <td>{{ row.productNameSnapshot }}</td><td>{{ row.orderedQuantity }} {{ row.unitCode }}</td><td>{{ row.scheduledQuantity }}</td>
            <td>{{ row.pendingQuantity }}</td><td>{{ row.pendingValue | currency:'INR':'symbol':'1.2-2' }}</td>
            <td><button class="text-button" type="button" (click)="openDetail({ salesOrderId: row.salesOrderId })">Drill down</button></td></tr></tbody>
        </table></div>
        <p class="notice">{{ report()?.deliveredQuantityState }} · The report includes persisted order and schedule data only.</p>
      </section>
    </main>
  `,
  styles: [`
    :host{display:block;color:#252422}.workspace{padding:28px 32px 48px;background:#f6f2ed;min-height:100%;font-family:Inter,Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:center;gap:20px;margin-bottom:24px}.head-actions{display:flex;gap:8px}.eyebrow{font-size:11px;letter-spacing:.14em;font-weight:700;color:#9d573b;margin:0 0 7px}.page-head h1,.drawer h2,.report h2{margin:0;font-size:28px;letter-spacing:-.03em}.muted,small{color:#77736e}.toolbar{display:flex;justify-content:space-between;margin-bottom:16px}.filters{display:flex;gap:8px;flex-wrap:wrap}.filters input,.filters select,.drawer input,.drawer select,.drawer textarea{border:1px solid #d9d1c9;border-radius:7px;background:#fff;padding:10px 12px;color:#252422;min-width:145px}.filters input{width:180px}.primary,.secondary,.danger,.icon{border-radius:7px;padding:10px 14px;font-weight:650;cursor:pointer}.primary{border:1px solid #89452f;background:#914b34;color:white}.secondary{border:1px solid #d6ccc3;background:#fff;color:#393531}.danger{border:1px solid #b64c3d;background:#fff8f6;color:#9e3c30}.compact{padding:6px 10px;font-size:12px}.primary:disabled,.secondary:disabled{opacity:.55;cursor:not-allowed}.table-wrap{background:#fff;border:1px solid #e3ddd7;border-radius:10px;overflow:auto}table{width:100%;border-collapse:collapse;min-width:900px}th{text-align:left;font-size:11px;text-transform:uppercase;letter-spacing:.08em;color:#77736e;background:#faf8f6;padding:13px 16px;border-bottom:1px solid #e8e2dc}td{padding:14px 16px;border-bottom:1px solid #eee9e4;font-size:13px}td small{display:block;margin-top:5px}.actions{display:flex;gap:5px}.chip{display:inline-block;border-radius:999px;background:#f1e8df;padding:5px 9px;font-size:11px;font-weight:700}.chip[data-status="Cancelled"],.chip[data-status="Short-closed"]{background:#f5e5e3;color:#8d3e37}.chip[data-status="Completed"]{background:#e2efe5;color:#28603a}.text-button{border:0;background:transparent;color:#3a3530;font-weight:700;padding:0;cursor:pointer}.empty,.loading{padding:28px;text-align:center;color:#777}.error{color:#9c3028}.success{color:#28603a}.notice{background:#f1e8df;border-radius:7px;padding:10px 12px;color:#63564d;font-size:12px}.backdrop{position:fixed;inset:0;background:#1f1b1880;z-index:20}.drawer{position:fixed;right:0;top:0;bottom:0;z-index:21;width:min(540px,100vw);background:#f9f6f2;box-shadow:-10px 0 35px #0002;padding:24px;overflow:auto}.drawer header,.report header{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:18px}.drawer form,.form-section{display:grid;gap:13px}.drawer label{display:grid;gap:6px;font-size:12px;font-weight:650;color:#605b55}.drawer input,.drawer select,.drawer textarea{width:100%;box-sizing:border-box;font:inherit;font-weight:400}.drawer textarea{min-height:68px;resize:vertical}.icon{background:transparent;border:0;font-size:24px;padding:0 5px}.two{display:grid;grid-template-columns:1fr 1fr;gap:10px}.three{display:grid;grid-template-columns:1fr .75fr 1fr;gap:8px}.four{display:grid;grid-template-columns:repeat(2,1fr);gap:8px}.tabs{display:flex;gap:7px;margin:0 0 18px}.tabs button{border:1px solid #d6ccc3;background:#fff;color:#514a44;padding:9px 12px;border-radius:7px}.tabs button.active{background:#ead9ce;border-color:#c5a38f}.order-line{border:1px solid #e3ddd7;border-radius:8px;padding:12px}.order-line header{display:flex;align-items:center;justify-content:space-between}.order-line h3{margin:0 0 9px}.detail{width:min(650px,100vw)}.detail h3,.amend-panel h3,.comparison h3{font-size:14px;margin:24px 0 10px;border-bottom:1px solid #e2d9d0;padding-bottom:7px}.snapshot{background:#fff;border:1px solid #e3ddd7;border-radius:8px;padding:12px}.snapshot h3{margin-top:0}.tracker{display:grid;grid-template-columns:repeat(5,1fr);gap:6px}.tracker div{background:#fff;border:1px solid #e3ddd7;border-radius:8px;padding:10px;text-align:center}.tracker b,.tracker small{display:block}.tracker b{font-size:16px}.tracker small{font-size:10px;margin-top:4px}.line{display:grid;grid-template-columns:1fr auto;gap:5px;padding:12px 0;border-bottom:1px solid #e6dfd8}.line small{grid-column:1/-1}.amend-panel,.comparison{background:#fff;border:1px solid #e3ddd7;border-radius:8px;padding:12px;margin:12px 0}.amend-panel h3,.comparison h3{margin-top:0}.comparison pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:11px}.timeline{padding:10px 0;border-bottom:1px solid #e3ddd7}.timeline small{float:right}.pager{display:flex;justify-content:center;align-items:center;gap:16px;padding:16px}.report{position:fixed;inset:8vh 5vw;z-index:30;background:#f9f6f2;border:1px solid #e3ddd7;border-radius:12px;box-shadow:0 18px 70px #0003;padding:24px;overflow:auto}.report .filters{margin-bottom:12px}@media(max-width:800px){.workspace{padding:20px 14px}.page-head{align-items:flex-start;flex-direction:column}.head-actions{width:100%}.filters input{flex:1;min-width:130px}.tracker{grid-template-columns:repeat(2,1fr)}.three,.four{grid-template-columns:1fr 1fr}}
  `],
})
export class M06Page implements OnInit {
  private readonly api = inject(ApiClient);
  readonly rows = signal<any[]>([]);
  readonly customers = signal<any[]>([]);
  readonly sites = signal<any[]>([]);
  readonly products = signal<any[]>([]);
  readonly acceptedQuotes = signal<any[]>([]);
  readonly detail = signal<any | null>(null);
  readonly report = signal<any | null>(null);
  readonly comparison = signal<any | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly creating = signal(false);
  readonly amending = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly canConfigure = signal(false);
  readonly toleranceSettings = signal<any | null>(null);
  readonly total = signal(0);
  readonly pageSize = 25;
  readonly statuses = ['Draft', 'Pending approval', 'Confirmed', 'In fulfilment', 'Completed', 'Short-closed', 'On hold', 'Cancelled'];
  page = 1;
  search = '';
  statusFilter = '';
  customerFilter = '';
  siteFilter = '';
  productFilter = '';
  selectedQuoteId = '';
  quoteLines: any[] = [];
  quotePoNumber = '';
  quotePoDate = '';
  quotePoAttachment = '';
  quoteRateValidity = '';
  quoteNotes = '';
  readonly createMode = signal<'quote' | 'direct'>('quote');
  draft = this.emptyDraft();
  amendment: any = { reason: '', siteId: null, lineId: '', quantity: null, rate: null };

  ngOnInit(): void {
    this.load();
    this.api.get<any>('/m02/context').subscribe({
      next: (access) => this.canConfigure.set((access.permissions ?? []).includes('m06.orders.configure')),
    });
    this.api.get<any[]>('/m03/parties?role=Customer').subscribe({
      next: (rows) => this.customers.set(rows), error: () => this.error.set('Customer lookup is unavailable.'),
    });
    this.api.get<any[]>('/m03/sites').subscribe({ next: (rows) => this.sites.set(rows), error: () => undefined });
    this.api.get<any[]>('/m03/products').subscribe({ next: (rows) => this.products.set(rows), error: () => undefined });
    this.api.get<any>('/m05/quotations?status=Accepted&page=1&pageSize=100').subscribe({
      next: (response) => this.acceptedQuotes.set(response.rows ?? []),
      error: () => this.error.set('Accepted quotation lookup is unavailable.'),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    const params = new URLSearchParams({ page: String(this.page), pageSize: String(this.pageSize) });
    if (this.search.trim()) params.set('q', this.search.trim());
    if (this.statusFilter) params.set('status', this.statusFilter);
    if (this.customerFilter.trim()) params.set('customerId', this.customerFilter.trim());
    if (this.siteFilter.trim()) params.set('siteId', this.siteFilter.trim());
    this.api.get<any>(`/m06/orders?${params}`).subscribe({
      next: (result) => { this.rows.set(result.rows ?? []); this.total.set(result.total ?? 0); this.loading.set(false); },
      error: (error) => { this.error.set(error?.error?.detail ?? 'Could not load sales orders. Check M06 access.'); this.loading.set(false); },
    });
  }

  loadTolerances(): void {
    this.api.get<any>('/m06/settings/delivery-tolerances').subscribe({
      next: (settings) => this.toleranceSettings.set(settings),
      error: (error) => this.error.set(error?.error?.detail ?? 'Delivery tolerance settings are unavailable.'),
    });
  }

  saveTolerances(): void {
    const settings = this.toleranceSettings();
    if (!settings) return;
    this.api.put<any, any>('/m06/settings/delivery-tolerances', settings).subscribe({
      next: (saved) => { this.toleranceSettings.set(saved); this.notice.set('Tenant delivery tolerances saved and audited.'); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Delivery tolerances could not be saved.'),
    });
  }

  previousPage(): void { if (this.page > 1) { this.page--; this.load(); } }
  nextPage(): void { if (this.page * this.pageSize < this.total()) { this.page++; this.load(); } }
  openCreate(): void {
    this.error.set(''); this.notice.set(''); this.draft = this.emptyDraft();
    this.createMode.set('quote'); this.selectedQuoteId = ''; this.quoteLines = []; this.creating.set(true);
  }
  customerSites(): any[] { return this.sites().filter((site) => site.customerPartyId === this.draft.customerPartyId); }
  customerChanged(): void {
    const customer = this.customers().find((row) => row.partyId === this.draft.customerPartyId);
    this.draft.placeOfSupplyStateCode = customer?.stateCode ?? '';
    this.draft.siteId = '';
  }
  productFor(line: any): any { return this.products().find((product) => product.productId === line.productId); }
  productChanged(index: number): void {
    const line = this.draft.lines[index];
    line.unitCode = this.productFor(line)?.baseUnitCode ?? '';
    line.rmcGrade = ''; line.rmcSlumpBand = ''; line.rmcAdmixtureCode = ''; line.rmcPumpType = '';
  }
  addLine(): void { if (this.draft.lines.length < 100) this.draft.lines.push(this.emptyLine()); }
  removeLine(index: number): void { this.draft.lines.splice(index, 1); }

  convertQuote(): void {
    if (!this.selectedQuoteId) { this.error.set('Select an accepted quotation.'); return; }
    const lines = this.quoteLines
      .filter((line) => Number(line.orderQuantity) > 0)
      .map((line) => ({ quotationLineId: line.lineId, quantity: Number(line.orderQuantity) }));
    if (!lines.length) { this.error.set('Enter a positive quantity for at least one remaining quotation line.'); return; }
    this.saving.set(true); this.error.set('');
    const body = {
      idempotencyKey: crypto.randomUUID(),
      lines,
      customerPoNumber: this.quotePoNumber || null,
      customerPoDate: this.quotePoDate || null,
      customerPoAttachmentReference: this.quotePoAttachment || null,
      rateValidityUntil: this.quoteRateValidity || null,
      notes: this.quoteNotes,
    };
    this.api.post<any, any>(`/m06/orders/from-quotation/${this.selectedQuoteId}`, body).subscribe({
      next: (result) => this.created(result),
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Quotation conversion failed.'); },
    });
  }

  loadQuoteLines(): void {
    this.quoteLines = [];
    if (!this.selectedQuoteId) return;
    this.api.get<any>(`/m05/quotations/${this.selectedQuoteId}`).subscribe({
      next: (quote) => this.quoteLines = (quote.lines ?? [])
        .filter((line: any) => Number(line.remainingQuantity) > 0)
        .map((line: any) => ({ ...line, orderQuantity: line.remainingQuantity })),
      error: (error) => this.error.set(error?.error?.detail ?? 'Quotation quantities could not be loaded.'),
    });
  }

  createDirect(): void {
    if (!this.draft.customerPartyId || !this.draft.orderDate || !this.draft.rateValidityUntil ||
      !this.draft.paymentTerms.trim() || !this.draft.deliveryTerms.trim() ||
      !/^\d{2}$/.test(this.draft.placeOfSupplyStateCode) ||
      this.draft.lines.some((line: any) => !line.productId || !line.unitCode || Number(line.quantity) <= 0)) {
      this.error.set('Complete the customer, order dates and product lines.'); return;
    }
    this.saving.set(true); this.error.set('');
    const lines = this.draft.lines.map((line: any) => ({
      productId: line.productId, quantity: Number(line.quantity), unitCode: line.unitCode,
      rate: line.rate === '' || line.rate == null ? null : Number(line.rate),
      discountPercent: Number(line.discountPercent || 0),
      rmcGrade: line.rmcGrade || null, rmcSlumpBand: line.rmcSlumpBand || null,
      rmcAdmixtureCode: line.rmcAdmixtureCode || null, rmcPumpType: line.rmcPumpType || null,
    }));
    const categories = lines.map((line: any) => this.productFor(line)?.category);
    const hasRmc = categories.includes('RMC');
    const orderType = hasRmc && categories.some((category: string) => category !== 'RMC')
      ? 'Mixed' : hasRmc ? 'RMC' : 'Material';
    const body = {
      ...this.draft, orderType, lines, charges: [], idempotencyKey: crypto.randomUUID(),
      siteId: this.draft.siteId || null, branchId: null,
      paymentTermId: null,
    };
    this.api.post<any, any>('/m06/orders', body).subscribe({
      next: (result) => this.created(result),
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Direct order could not be saved.'); },
    });
  }

  openDetail(row: any): void {
    if (!row?.salesOrderId) return;
    this.error.set('');
    this.api.get<any>(`/m06/orders/${row.salesOrderId}`).subscribe({
      next: (result) => { this.detail.set(result); this.amending.set(false); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Could not load sales order detail.'),
    });
  }

  confirm(row: any): void {
    const reason = window.prompt('Credit override reason (required only if the server identifies a hold):');
    if (reason === null) return;
    this.api.post<any, any>(`/m06/orders/${row.salesOrderId}/confirm`, {
      version: row.version, overrideReason: reason || null,
    }).subscribe({
      next: (result) => { this.notice.set(result.status === 'Pending approval' ? 'Order is awaiting M02 approval.' : 'Order confirmed.'); this.refreshDetail(row.salesOrderId); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Order confirmation was rejected.'),
    });
  }

  addSchedule(): void {
    const order = this.detail();
    if (!order?.lines?.length) return;
    const line = order.lines[0];
    const quantityText = window.prompt(`Quantity to schedule for ${line.productNameSnapshot}:`, String(line.orderedQuantity));
    if (quantityText === null) return;
    const quantity = Number(quantityText);
    const date = window.prompt('Delivery date (YYYY-MM-DD):', new Date().toISOString().slice(0, 10));
    if (!date || !Number.isFinite(quantity) || quantity <= 0) { this.error.set('Enter a positive quantity and delivery date.'); return; }
    this.api.post<any, any>(`/m06/orders/${order.salesOrderId}/schedules`, {
      version: order.version, rows: [{
        salesOrderLineId: line.lineId, quantity, deliveryDate: date,
        timeWindow: null, siteId: order.siteId, preferredPlantId: null, operationalNotes: '',
      }],
    }).subscribe({
      next: () => { this.notice.set('Schedule row saved. It is not an M08 dispatch record.'); this.refreshDetail(order.salesOrderId); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Delivery schedule could not be saved.'),
    });
  }

  hold(order: any): void { this.transitionWithReason(order, 'hold', 'Reason for placing this order on hold:'); }
  cancelOrShortClose(order: any): void {
    if (!window.confirm('Cancel if no delivery exists; otherwise short-close and release remaining quantity?')) return;
    const reason = window.prompt('Reason (required):');
    if (reason === null || !reason.trim()) return;
    const hasDelivered = (order.lines ?? []).some((line: any) => Number(line.deliveredQuantity) > 0);
    const action = hasDelivered ? 'short-close' : 'cancel';
    this.api.post<any, any>(`/m06/orders/${order.salesOrderId}/${action}`, { version: order.version, reason }).subscribe({
      next: () => { this.notice.set(hasDelivered ? 'Order short-closed; future delivery commitments remain separate.' : 'Order cancelled.'); this.refreshDetail(order.salesOrderId); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Order closure was rejected.'),
    });
  }

  applySchedule(row: any): void {
    const order = this.detail();
    if (!order || !window.confirm('Apply this M02-approved delivery schedule?')) return;
    this.api.post<any, any>(`/m06/orders/${order.salesOrderId}/schedules/${row.scheduleId}/apply`, {
      version: order.version,
    }).subscribe({
      next: () => { this.notice.set('Approved schedule applied; it is not an M08 dispatch record.'); this.refreshDetail(order.salesOrderId); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Approved schedule could not be applied.'),
    });
  }

  showM07Pending(): void {
    this.notice.set('M07 is not implemented. No purchase order was created; the order lines and site remain available for a future pre-filled handoff.');
  }
  printConfirmation(order: any): void {
    this.api.getText(`/m06/orders/${order.salesOrderId}/confirmation`).subscribe({
      next: (html) => {
        const url = URL.createObjectURL(new Blob([html], { type: 'text/html' }));
        const popup = window.open(url, '_blank', 'noopener');
        if (!popup) this.error.set('Allow pop-ups to open the order confirmation print view.');
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (error) => this.error.set(error?.error?.detail ?? 'Order confirmation could not be opened.'),
    });
  }

  beginAmendment(): void {
    const line = this.detail()?.lines?.[0];
    if (!line) return;
    this.amendment = {
      reason: '', siteId: this.detail().siteId,
      lineId: line.lineId, quantity: line.orderedQuantity, rate: line.rate,
    };
    this.amending.set(true);
  }

  submitAmendment(): void {
    const order = this.detail();
    if (!order || !this.amendment.reason.trim()) { this.error.set('An amendment reason is required.'); return; }
    this.api.post<any, any>(`/m06/orders/${order.salesOrderId}/amendments`, {
      version: order.version, reason: this.amendment.reason,
      siteId: this.amendment.siteId || null,
      lines: [{ lineId: this.amendment.lineId, orderedQuantity: Number(this.amendment.quantity), rate: Number(this.amendment.rate) }],
    }).subscribe({
      next: (result) => { this.notice.set(`Amendment ${result.revisionNumber} submitted for M02 approval.`); this.amending.set(false); this.refreshDetail(order.salesOrderId); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Amendment could not be submitted.'),
    });
  }

  compare(amendment: any): void {
    this.comparison.set({
      revision: amendment.revisionNumber, status: amendment.status,
      before: JSON.parse(amendment.beforeSnapshotJson),
      proposed: JSON.parse(amendment.proposedSnapshotJson),
    });
  }

  applyAmendment(amendment: any): void {
    const order = this.detail();
    if (!order || !window.confirm(`Apply approved amendment ${amendment.revisionNumber}?`)) return;
    this.api.post<any, any>(`/m06/orders/${order.salesOrderId}/amendments/${amendment.amendmentId}/apply`, {
      version: order.version,
    }).subscribe({
      next: () => { this.notice.set('Approved amendment applied.'); this.refreshDetail(order.salesOrderId); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Approved amendment could not be applied.'),
    });
  }

  loadPendingReport(): void {
    const params = new URLSearchParams();
    if (this.customerFilter.trim()) params.set('customerId', this.customerFilter.trim());
    if (this.siteFilter.trim()) params.set('siteId', this.siteFilter.trim());
    if (this.productFilter.trim()) params.set('productId', this.productFilter.trim());
    this.api.get<any>(`/m06/reports/pending-orders?${params}`).subscribe({
      next: (result) => this.report.set(result),
      error: (error) => this.error.set(error?.error?.detail ?? 'Pending-order report is unavailable.'),
    });
  }

  exportReport(): void {
    const params = new URLSearchParams({ format: 'csv' });
    if (this.customerFilter.trim()) params.set('customerId', this.customerFilter.trim());
    if (this.siteFilter.trim()) params.set('siteId', this.siteFilter.trim());
    if (this.productFilter.trim()) params.set('productId', this.productFilter.trim());
    this.api.getBlob(`/m06/reports/pending-orders?${params}`).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob); const anchor = document.createElement('a');
        anchor.href = url; anchor.download = 'm06-pending-orders.csv'; anchor.click(); URL.revokeObjectURL(url);
      },
      error: (error) => this.error.set(error?.error?.detail ?? 'Pending-order export failed.'),
    });
  }

  private transitionWithReason(order: any, action: string, promptText: string): void {
    const reason = window.prompt(promptText);
    if (!reason?.trim()) return;
    this.api.post<any, any>(`/m06/orders/${order.salesOrderId}/${action}`, {
      version: order.version, reason,
    }).subscribe({
      next: () => this.refreshDetail(order.salesOrderId),
      error: (error) => this.error.set(error?.error?.detail ?? 'Order status update was rejected.'),
    });
  }

  private refreshDetail(orderId: string): void {
    this.load();
    this.openDetail({ salesOrderId: orderId });
  }
  private created(result: any): void {
    this.saving.set(false); this.creating.set(false); this.load();
    this.notice.set(result.status === 'Pending approval' ? 'Order created and awaiting M02 approval.' : 'Sales order created from persisted source data.');
    this.openDetail(result);
  }
  private emptyLine(): any {
    return { productId: '', quantity: 1, unitCode: '', rate: null, discountPercent: 0,
      rmcGrade: '', rmcSlumpBand: '', rmcAdmixtureCode: '', rmcPumpType: '' };
  }
  private emptyDraft(): any {
    return {
      customerPartyId: '', siteId: '', orderDate: new Date().toISOString().slice(0, 10),
      rateValidityUntil: new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10),
      priceBasis: 'ExPlant', placeOfSupplyStateCode: '', customerPoNumber: '',
      customerPoDate: '', customerPoAttachmentReference: '', paymentTerms: '',
      deliveryTerms: '', notes: '', lines: [this.emptyLine()],
    };
  }
}
