import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m05-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head">
        <div><p class="eyebrow">SELL · M05 QUOTATIONS</p><h1>Quotations</h1><p class="muted">Prepare customer offers from approved M03 rates and tax masters.</p></div>
        <button class="primary" type="button" (click)="openNew()">＋ New quotation</button>
      </header>
      <div class="toolbar">
        <div class="filters">
          <input aria-label="Search quotations" placeholder="Search quote or customer" [(ngModel)]="search" (keyup.enter)="load()">
          <select aria-label="Filter quotation status" [(ngModel)]="statusFilter" (change)="load()">
            <option value="">All statuses</option><option *ngFor="let status of statuses" [value]="status">{{ status }}</option>
          </select><button class="secondary" (click)="load()">Apply</button>
        </div>
      </div>
      <p class="error" *ngIf="error()">{{ error() }}</p>
      <p class="loading" *ngIf="loading()">Loading quotations…</p>
      <section class="table-wrap" *ngIf="!loading()">
        <table><thead><tr><th>Quotation</th><th>Customer</th><th>Revision</th><th>Status</th><th>Valid until</th><th>Total</th><th>Actions</th></tr></thead>
          <tbody><tr *ngFor="let row of rows()">
            <td><button class="text-button" (click)="openDetail(row)">{{ row.number || 'Draft' }}</button><small>{{ row.priceBasis }} · {{ row.createdAt | date:'mediumDate' }}</small></td>
            <td>{{ row.customerNameSnapshot }}</td><td>v{{ row.revisionNumber }}</td>
            <td><span class="chip" [attr.data-status]="row.status">{{ row.status }}</span></td>
            <td>{{ row.validUntil | date:'mediumDate' }}</td><td>{{ row.total | currency:'INR':'symbol':'1.2-2' }}</td>
            <td><button *ngIf="row.status === 'Approved'" class="secondary compact" (click)="issue(row)">Issue</button></td>
          </tr></tbody>
        </table>
        <div class="empty" *ngIf="!rows().length">No quotations match these filters.</div>
      </section>
      <div class="backdrop" *ngIf="creating()" (click)="creating.set(false)"></div>
      <aside class="drawer" *ngIf="creating()" aria-label="Quotation form">
        <header><div><p class="eyebrow">{{ fromEnquiryId ? 'ENQUIRY HANDOFF' : 'DIRECT QUOTE' }}</p><h2>{{ fromEnquiryId ? 'Create from enquiry' : 'New quotation' }}</h2></div><button class="icon" (click)="creating.set(false)" aria-label="Close">×</button></header>
        <p class="notice" *ngIf="fromEnquiryId">Enquiry data is copied as a snapshot. The source enquiry is not overwritten.</p>
        <form (ngSubmit)="save()">
          <ng-container *ngIf="!fromEnquiryId">
            <label>Customer<select name="customer" [(ngModel)]="draft.customerPartyId" (change)="customerChanged()" required><option value="">Select customer</option><option *ngFor="let customer of customers()" [value]="customer.partyId">{{ customer.tradeName || customer.legalName }}</option></select></label>
            <label>Site<select name="site" [(ngModel)]="draft.siteId" (change)="siteChanged()"><option value="">No site</option><option *ngFor="let site of customerSites()" [value]="site.siteId">{{ site.name }}</option></select></label>
            <label>Contact<select name="contact" [(ngModel)]="draft.contactId"><option value="">No contact</option><option *ngFor="let contact of customerContacts()" [value]="contact.contactId">{{ contact.name }}</option></select></label>
            <section class="quote-line" *ngFor="let line of draft.lines; let i = index">
              <header><h3>Product line {{ i + 1 }}</h3><button *ngIf="draft.lines.length > 1" type="button" class="secondary compact" (click)="removeLine(i)">Remove</button></header>
              <label>Product<select [name]="'product' + i" [(ngModel)]="line.productId" (change)="lineProductChanged(i)" required><option value="">Select M03 product</option><option *ngFor="let product of products()" [value]="product.productId">{{ product.name }} · {{ product.baseUnitCode }}</option></select></label>
              <ng-container *ngIf="productForLine(line)?.category === 'RMC'">
                <p class="notice">The exact approved M03 grade, slump, admixture and pump rate card is required.</p>
                <label>RMC grade<select [name]="'rmcGrade' + i" [(ngModel)]="line.rmcGrade" (change)="lineRmcGradeChanged(i)"><option value="">Select grade</option><option *ngFor="let grade of lineRmcGrades(i)" [value]="grade">{{ grade }}</option></select></label>
                <label>Slump band<select [name]="'rmcSlump' + i" [(ngModel)]="line.rmcSlumpBand"><option value="">Select slump</option><option *ngFor="let value of lineRmcValues(i, 'slumpBand')" [value]="value">{{ value }}</option></select></label>
                <label>Admixture<select [name]="'rmcAdmixture' + i" [(ngModel)]="line.rmcAdmixtureCode"><option value="">Select admixture</option><option *ngFor="let value of lineRmcValues(i, 'admixtureCode')" [value]="value">{{ value }}</option></select></label>
                <label>Pump type<select [name]="'rmcPump' + i" [(ngModel)]="line.rmcPumpType"><option value="">Select pump type</option><option *ngFor="let value of lineRmcValues(i, 'pumpType')" [value]="value">{{ value }}</option></select></label>
              </ng-container>
              <div class="two"><label>Quantity<input [name]="'quantity' + i" type="number" min="0.001" step="0.001" [(ngModel)]="line.quantity" required></label><label>Unit<input [name]="'unit' + i" [value]="line.unitCode" readonly></label></div>
              <label>Discount %<input [name]="'discount' + i" type="number" min="0" max="100" step="0.0001" [(ngModel)]="line.discountPercent"></label>
            </section>
            <button type="button" class="secondary" (click)="addLine()">＋ Add product line</button>
          </ng-container>
          <section class="charge-editor" aria-label="Quotation charges">
            <h3>Additional charges</h3>
            <div class="charge-row" *ngFor="let charge of draft.charges; let i = index">
              <label>Charge type<select [name]="'chargeType' + i" [(ngModel)]="charge.chargeTypeId"><option value="">Unclassified charge (0% tax)</option><option *ngFor="let type of chargeTypes()" [value]="type.chargeTypeId">{{ type.name }} · {{ type.defaultTaxRatePercent }}%</option></select></label>
              <label>Description<input [name]="'chargeDescription' + i" [(ngModel)]="charge.description" maxlength="200" required></label>
              <label>Amount<input [name]="'chargeAmount' + i" type="number" min="0" step="0.01" [(ngModel)]="charge.amount" required></label>
              <button type="button" class="secondary compact" (click)="removeCharge(i)" aria-label="Remove charge">Remove</button>
            </div>
            <button type="button" class="secondary" (click)="addCharge()">＋ Add charge</button>
          </section>
          <div class="two"><label>Price basis<select name="basis" [(ngModel)]="draft.priceBasis"><option>ExPlant</option><option>Delivered</option><option>Site</option></select></label><label>Valid until<input name="validUntil" type="date" [(ngModel)]="draft.validUntil" required></label></div>
          <label>Place of supply state code<input name="placeOfSupply" [(ngModel)]="draft.placeOfSupplyStateCode" maxlength="2" inputmode="numeric" placeholder="e.g. 27" required></label>
          <label>Payment terms<select name="paymentTerm" [(ngModel)]="draft.paymentTermId"><option value="">Customer default</option><option *ngFor="let term of paymentTerms()" [value]="term.paymentTermId">{{ term.name }} · {{ term.dueDays }} days</option></select></label>
          <label>Delivery terms<textarea name="deliveryTerms" [(ngModel)]="draft.deliveryTerms" maxlength="2000" required></textarea></label>
          <label>Terms and conditions<textarea name="terms" [(ngModel)]="draft.termsAndConditions" maxlength="8000" required></textarea></label>
          <label>Internal notes<textarea name="notes" [(ngModel)]="draft.internalNotes" maxlength="4000"></textarea></label>
          <p class="notice">Rates and GST are resolved server-side from M03. Cost and margin data are not included in this form.</p>
          <p class="error" *ngIf="error()">{{ error() }}</p>
          <footer><button type="button" class="secondary" (click)="creating.set(false)">Cancel</button><button class="primary" type="submit" [disabled]="saving()">Save quotation</button></footer>
        </form>
      </aside>
      <div class="backdrop" *ngIf="detail()" (click)="detail.set(null)"></div>
      <aside class="drawer detail" *ngIf="detail()" aria-label="Quotation details">
        <header><div><p class="eyebrow">{{ detail()?.number || 'DRAFT' }} · REVISION {{ detail()?.revisionNumber }}</p><h2>{{ detail()?.customerNameSnapshot }}</h2></div><button class="icon" (click)="detail.set(null)" aria-label="Close">×</button></header>
        <p><span class="chip" [attr.data-status]="detail()?.status">{{ detail()?.status }}</span> · {{ detail()?.priceBasis }} · Valid to {{ detail()?.validUntil | date:'mediumDate' }}</p>
        <p *ngIf="detail()?.sourceEnquiryId">Source enquiry: {{ detail()?.sourceEnquiryId }}</p>
        <p class="notice" *ngIf="detail()?.approvalStatus === 'Pending'">Awaiting M02 approval. Issue is disabled until approval completes.</p>
        <p class="error" *ngIf="detail()?.approvalStatus === 'Rejected'">M02 approval was rejected. Revise the quotation before issue.</p>
        <h3>Commercial terms</h3><p>{{ detail()?.deliveryTerms }}</p><p>{{ detail()?.termsAndConditions }}</p>
        <h3>Lines</h3><div class="line" *ngFor="let line of detail()?.lines">
          <strong>{{ line.description }}</strong><small>{{ line.quantity }} {{ line.unitCode }} × {{ line.rate | number:'1.2-4' }} · Discount {{ line.discountPercent }}%</small>
          <small>GST {{ line.gstPercent }}% · tax {{ line.taxAmount | currency:'INR':'symbol':'1.2-2' }}</small>
          <small>Converted {{ line.consumedQuantity || 0 }} · remaining {{ line.remainingQuantity ?? line.quantity }} {{ line.unitCode }}</small>
          <b>{{ line.total | currency:'INR':'symbol':'1.2-2' }}</b>
        </div>
        <h3>Charges</h3><div class="line" *ngFor="let charge of detail()?.charges"><strong>{{ charge.description }}</strong><small>{{ charge.amount | currency:'INR':'symbol':'1.2-2' }} · GST {{ charge.taxRatePercent }}% · CGST {{ charge.cgstAmount | currency:'INR':'symbol':'1.2-2' }} · SGST {{ charge.sgstAmount | currency:'INR':'symbol':'1.2-2' }} · IGST {{ charge.igstAmount | currency:'INR':'symbol':'1.2-2' }}</small><b>{{ charge.total | currency:'INR':'symbol':'1.2-2' }}</b></div>
        <div class="summary"><p>Subtotal <b>{{ detail()?.subtotal | currency:'INR':'symbol':'1.2-2' }}</b></p><p>Discount <b>{{ detail()?.discountTotal | currency:'INR':'symbol':'1.2-2' }}</b></p><p>Charges <b>{{ detail()?.chargesTotal | currency:'INR':'symbol':'1.2-2' }}</b></p><p>Tax <b>{{ detail()?.taxTotal | currency:'INR':'symbol':'1.2-2' }}</b></p><p class="grand">Total <b>{{ detail()?.total | currency:'INR':'symbol':'1.2-2' }}</b></p></div>
        <div class="action-row"><button class="secondary" (click)="print(detail())">Print / Save PDF</button><button class="secondary" *ngIf="detail()?.status === 'Approved'" (click)="issue(detail())">Issue quotation</button><button class="secondary" *ngIf="detail()?.status === 'Sent'" (click)="revise(detail())">Revise</button><button class="secondary" *ngIf="detail()?.status !== 'Cancelled' && detail()?.status !== 'Revised'" (click)="clone(detail())">Clone</button><button class="danger" *ngIf="['Draft','Approved','Pending approval','Sent'].includes(detail()?.status)" (click)="cancel(detail())">Cancel</button></div>
        <h3>Sales orders created</h3>
        <p *ngFor="let order of detail()?.createdOrders">{{ order.number }} · {{ order.status }} · {{ order.total | currency:'INR':'symbol':'1.2-2' }}</p>
        <p *ngIf="!detail()?.createdOrders?.length" class="notice">No sales orders have been created from this quotation.</p>
        <h3>Audit timeline</h3><div class="timeline" *ngFor="let item of detail()?.activities"><b>{{ item.action }}</b><small>{{ item.at | date:'medium' }}</small></div>
      </aside>
    </main>
  `,
  styles: [`
    :host{display:block;color:#252422}.workspace{padding:28px 32px 48px;background:#f6f2ed;min-height:100%;font-family:Inter,Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:center;gap:20px;margin-bottom:24px}.eyebrow{font-size:11px;letter-spacing:.14em;font-weight:700;color:#9d573b;margin:0 0 7px}.page-head h1,.drawer h2{margin:0;font-size:28px;letter-spacing:-.03em}.muted,small{color:#77736e}.filters{display:flex;gap:8px;margin-bottom:16px}.filters input,.filters select,.drawer input,.drawer select,.drawer textarea{border:1px solid #d9d1c9;border-radius:7px;background:#fff;padding:10px 12px;color:#252422;min-width:145px}.filters input{width:280px}.primary,.secondary,.danger,.icon{border-radius:7px;padding:10px 14px;font-weight:650;cursor:pointer}.primary{border:1px solid #89452f;background:#914b34;color:white}.secondary{border:1px solid #d6ccc3;background:#fff;color:#393531}.danger{border:1px solid #b64c3d;background:#fff8f6;color:#9e3c30}.compact{padding:6px 10px;font-size:12px}.primary:disabled{opacity:.55}.table-wrap{background:#fff;border:1px solid #e3ddd7;border-radius:10px;overflow:auto}table{width:100%;border-collapse:collapse;min-width:800px}th{text-align:left;font-size:11px;text-transform:uppercase;letter-spacing:.08em;color:#77736e;background:#faf8f6;padding:13px 16px;border-bottom:1px solid #e8e2dc}td{padding:14px 16px;border-bottom:1px solid #eee9e4;font-size:13px}td small{display:block;margin-top:5px}.chip{display:inline-block;border-radius:999px;background:#f1e8df;padding:5px 9px;font-size:11px;font-weight:700}.chip[data-status="Rejected"],.chip[data-status="Expired"],.chip[data-status="Cancelled"]{background:#f5e5e3;color:#8d3e37}.chip[data-status="Accepted"]{background:#e2efe5;color:#28603a}.text-button{border:0;background:transparent;color:#3a3530;font-weight:700;padding:0;cursor:pointer}.empty,.loading{padding:28px;text-align:center;color:#777}.error{color:#9c3028}.notice{background:#f1e8df;border-radius:7px;padding:10px 12px;color:#63564d;font-size:12px}.backdrop{position:fixed;inset:0;background:#1f1b1880;z-index:20}.drawer{position:fixed;right:0;top:0;bottom:0;z-index:21;width:min(520px,100vw);background:#f9f6f2;box-shadow:-10px 0 35px #0002;padding:24px;overflow:auto}.drawer header{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:18px}.drawer form{display:grid;gap:13px}.drawer label{display:grid;gap:6px;font-size:12px;font-weight:650;color:#605b55}.drawer input,.drawer select,.drawer textarea{width:100%;box-sizing:border-box;font:inherit;font-weight:400}.drawer textarea{min-height:68px;resize:vertical}.drawer footer{display:flex;justify-content:flex-end;gap:8px;margin:8px 0}.icon{background:transparent;border:0;font-size:24px;padding:0 5px}.two{display:grid;grid-template-columns:1fr 1fr;gap:10px}.quote-line{border:1px solid #e3ddd7;border-radius:8px;padding:12px;margin:10px 0}.quote-line header{display:flex;justify-content:space-between;align-items:center;margin-bottom:10px}.quote-line h3{margin:0}.detail{width:min(600px,100vw)}.detail h3{font-size:14px;margin:24px 0 10px;border-bottom:1px solid #e2d9d0;padding-bottom:7px}.line{display:grid;grid-template-columns:1fr auto;gap:5px;padding:12px 0;border-bottom:1px solid #e6dfd8}.line small{grid-column:1/-1}.summary{margin:18px 0}.summary p{display:flex;justify-content:space-between;margin:8px 0}.summary .grand{border-top:1px solid #cfc4b9;padding-top:12px;font-size:18px}.action-row{display:flex;gap:8px;flex-wrap:wrap}.timeline{padding:10px 0;border-bottom:1px solid #e3ddd7}.timeline small{float:right}@media(max-width:800px){.workspace{padding:20px 14px}.page-head{align-items:flex-start}.filters{flex-wrap:wrap}.filters input{flex:1;min-width:190px}}
  `],
})
export class M05Page implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly route = inject(ActivatedRoute);
  readonly rows = signal<any[]>([]);
  readonly customers = signal<any[]>([]);
  readonly sites = signal<any[]>([]);
  readonly contacts = signal<any[]>([]);
  readonly products = signal<any[]>([]);
  readonly chargeTypes = signal<any[]>([]);
  private rmcCardsByLine: Record<number, any[]> = {};
  readonly paymentTerms = signal<any[]>([]);
  readonly detail = signal<any | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly creating = signal(false);
  readonly error = signal('');
  readonly statuses = ['Draft', 'Pending approval', 'Approved', 'Sent', 'Accepted', 'Rejected', 'Expired', 'Revised', 'Cancelled'];
  search = '';
  statusFilter = '';
  page = 1;
  fromEnquiryId = '';
  draft: any = this.emptyDraft();

  ngOnInit(): void {
    this.load();
    this.api.get<any[]>('/m03/parties?role=Customer').subscribe({ next: (rows) => this.customers.set(rows), error: () => this.error.set('Customer lookup is unavailable.') });
    this.api.get<any[]>('/m03/sites').subscribe({ next: (rows) => this.sites.set(rows), error: () => undefined });
    this.api.get<any[]>('/m03/contacts').subscribe({ next: (rows) => this.contacts.set(rows), error: () => undefined });
    this.api.get<any[]>('/m03/products').subscribe({ next: (rows) => this.products.set(rows), error: () => undefined });
    this.api.get<any[]>('/m03/payment-terms').subscribe({ next: (rows) => this.paymentTerms.set(rows), error: () => undefined });
    this.api.get<any[]>('/m03/charge-types').subscribe({ next: (rows) => this.chargeTypes.set(rows), error: () => undefined });
    this.route.queryParamMap.subscribe((params) => {
      const id = params.get('fromEnquiry');
      if (id && id !== this.fromEnquiryId) { this.fromEnquiryId = id; this.openNew(); }
    });
  }

  load(): void {
    this.loading.set(true); this.error.set('');
    const params = new URLSearchParams({ page: String(this.page), pageSize: '50' });
    if (this.search.trim()) params.set('q', this.search.trim());
    if (this.statusFilter) params.set('status', this.statusFilter);
    this.api.get<any>(`/m05/quotations?${params}`).subscribe({
      next: (response) => { this.rows.set(response.rows ?? []); this.loading.set(false); },
      error: (error) => { this.error.set(error?.error?.detail ?? 'Could not load quotations. Check M05 access.'); this.loading.set(false); },
    });
  }

  openNew(): void {
    this.error.set('');
    this.draft = this.emptyDraft();
    if (this.fromEnquiryId) {
      this.api.get<any>(`/m04/enquiries/${this.fromEnquiryId}`).subscribe({
        next: (detail) => {
          const enquiry = detail.enquiry;
          this.draft.customerPartyId = enquiry.customerPartyId ?? '';
          this.draft.siteId = enquiry.siteId ?? '';
          this.draft.contactId = enquiry.contactId ?? '';
          this.draft.branchId = enquiry.branchId ?? '';
          const site = this.sites().find((x) => x.siteId === enquiry.siteId);
          const customer = this.customers().find((x) => x.partyId === enquiry.customerPartyId);
          this.draft.placeOfSupplyStateCode = site?.stateCode ?? customer?.stateCode ?? '';
          this.creating.set(true);
        },
        error: (error) => this.error.set(error?.error?.detail ?? 'The source enquiry could not be loaded.'),
      });
    } else {
      this.creating.set(true);
    }
  }

  customerSites(): any[] { return this.sites().filter((x) => x.customerPartyId === this.draft.customerPartyId); }
  customerContacts(): any[] { return this.contacts().filter((x) => x.partyId === this.draft.customerPartyId); }
  customerChanged(): void {
    this.draft.siteId = ''; this.draft.contactId = ''; this.draft.branchId = '';
    const customer = this.customers().find((x) => x.partyId === this.draft.customerPartyId);
    this.draft.placeOfSupplyStateCode = customer?.stateCode ?? '';
  }
  siteChanged(): void {
    const site = this.customerSites().find((x) => x.siteId === this.draft.siteId);
    this.draft.branchId = site?.branchId ?? '';
    this.draft.placeOfSupplyStateCode = site?.stateCode ?? this.draft.placeOfSupplyStateCode;
  }
  addLine(): void {
    if (this.draft.lines.length < 100) this.draft.lines.push(this.emptyLine());
  }

  removeLine(index: number): void {
    this.draft.lines.splice(index, 1);
    this.rmcCardsByLine = {};
  }

  lineProductChanged(index: number): void {
    const line = this.draft.lines[index];
    const product = this.products().find((x) => x.productId === line.productId);
    line.unitCode = product?.baseUnitCode ?? '';
    line.rmcGrade = ''; line.rmcSlumpBand = ''; line.rmcAdmixtureCode = ''; line.rmcPumpType = '';
    this.rmcCardsByLine[index] = [];
    if (product?.category === 'RMC')
      this.api.get<any[]>(`/m03/rmc-rate-cards?productId=${encodeURIComponent(line.productId)}`)
        .subscribe({ next: (rows) => { this.rmcCardsByLine[index] = rows; }, error: () => this.error.set('RMC rate-card lookup is unavailable.') });
  }

  productForLine(line: any): any { return this.products().find((x) => x.productId === line.productId); }
  lineRmcGrades(index: number): string[] { return [...new Set((this.rmcCardsByLine[index] ?? []).map((x) => x.grade))].sort(); }
  lineRmcValues(index: number, field: 'slumpBand' | 'admixtureCode' | 'pumpType'): string[] {
    const line = this.draft.lines[index];
    return [...new Set((this.rmcCardsByLine[index] ?? []).filter((x) => !line.rmcGrade || x.grade === line.rmcGrade).map((x) => x[field]))].filter(Boolean).sort();
  }
  lineRmcGradeChanged(index: number): void {
    const line = this.draft.lines[index];
    const match = (this.rmcCardsByLine[index] ?? []).find((x) => x.grade === line.rmcGrade);
    line.rmcSlumpBand = match?.slumpBand ?? '';
    line.rmcAdmixtureCode = match?.admixtureCode ?? '';
    line.rmcPumpType = match?.pumpType ?? '';
  }

  save(): void {
    const lines = this.draft.lines?.length ? this.draft.lines : [{
      productId: this.draft.productId, quantity: this.draft.quantity, unitCode: this.draft.unitCode,
      discountPercent: this.draft.discountPercent, rmcGrade: this.draft.rmcGrade,
      rmcSlumpBand: this.draft.rmcSlumpBand, rmcAdmixtureCode: this.draft.rmcAdmixtureCode,
      rmcPumpType: this.draft.rmcPumpType,
    }];
    if (!this.draft.validUntil || !this.draft.placeOfSupplyStateCode ||
      (!this.fromEnquiryId && (!this.draft.customerPartyId || lines.length === 0 ||
        lines.some((line: any) => !line.productId || !line.unitCode || Number(line.quantity) <= 0)))) {
      this.error.set('Complete customer, each product line, validity and place-of-supply fields.'); return;
    }
    this.saving.set(true); this.error.set('');
    const terms = {
      priceBasis: this.draft.priceBasis, validUntil: this.draft.validUntil,
      paymentTermId: this.draft.paymentTermId || null,
      placeOfSupplyStateCode: this.draft.placeOfSupplyStateCode,
      deliveryTerms: this.draft.deliveryTerms || 'As mutually agreed',
      termsAndConditions: this.draft.termsAndConditions || 'Prices are valid until the date shown.',
      internalNotes: this.draft.internalNotes || '', idempotencyKey: crypto.randomUUID(),
    };
    const body = this.fromEnquiryId ? { ...terms, discountPercent: Number(this.draft.discountPercent || 0) } : {
      customerPartyId: this.draft.customerPartyId, siteId: this.draft.siteId || null,
      contactId: this.draft.contactId || null, branchId: this.draft.branchId || null,
      ...terms,
      lines: lines.map((line: any) => ({ productId: line.productId, quantity: Number(line.quantity),
        unitCode: line.unitCode, rate: null, discountPercent: Number(line.discountPercent || 0),
        rmcGrade: line.rmcGrade || null, rmcSlumpBand: line.rmcSlumpBand || null,
        rmcAdmixtureCode: line.rmcAdmixtureCode || null, rmcPumpType: line.rmcPumpType || null })),
      charges: this.draft.charges.map((charge: any) => ({
        chargeTypeId: charge.chargeTypeId || null, description: charge.description,
        amount: Number(charge.amount),
      })),
    };
    const path = this.fromEnquiryId
      ? `/m05/quotations/from-enquiry/${this.fromEnquiryId}`
      : '/m05/quotations';
    this.api.post<any, any>(path, body).subscribe({
      next: (result) => { this.saving.set(false); this.creating.set(false); this.load(); this.openDetail({ quotationId: result.quotationId }); },
      error: (error) => { this.saving.set(false); this.error.set(error?.error?.detail ?? 'Quotation could not be saved.'); },
    });
  }

  openDetail(row: any): void {
    if (!row?.quotationId) return;
    this.api.get<any>(`/m05/quotations/${row.quotationId}`).subscribe({
      next: (result) => this.detail.set(result),
      error: (error) => this.error.set(error?.error?.detail ?? 'Could not load quotation details.'),
    });
  }

  issue(row: any): void {
    this.api.post<any, object>(`/m05/quotations/${row.quotationId}/issue`, {}).subscribe({
      next: (result) => { this.error.set(result.deliveryStatus === 'link-created-not-delivered' ? 'Quotation issued. Customer link created; no email or WhatsApp provider is configured.' : 'Quotation issued.'); this.load(); this.openDetail(row); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Quotation issue was rejected.'),
    });
  }

  cancel(row: any): void {
    this.api.post<any, object>(`/m05/quotations/${row.quotationId}/cancel`, {}).subscribe({
      next: () => { this.detail.set(null); this.load(); },
      error: (error) => this.error.set(error?.error?.detail ?? 'Quotation could not be cancelled.'),
    });
  }

  print(row: any): void {
    this.api.getText(`/m05/quotations/${row.quotationId}/print`).subscribe({
      next: (html) => {
        const url = URL.createObjectURL(new Blob([html], { type: 'text/html' }));
        const popup = window.open(url, '_blank', 'noopener');
        if (!popup) this.error.set('Allow pop-ups to open the print view.');
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (error) => this.error.set(error?.error?.detail ?? 'Print view could not be opened.'),
    });
  }

  addCharge(): void {
    if (this.draft.charges.length < 100) this.draft.charges.push({ description: '', amount: 0 });
  }

  removeCharge(index: number): void { this.draft.charges.splice(index, 1); }

  revise(row: any): void {
    const quote = this.detail();
    if (!quote || quote.quotationId !== row.quotationId) { this.openDetail(row); return; }
    if (!window.confirm('Create a new revision? M03 prices and tax will be re-resolved for today.')) return;
    const request = this.quoteWrite(quote);
    this.api.post<any, any>(`/m05/quotations/${quote.quotationId}/revise`, request).subscribe({
      next: (result) => {
        this.error.set('Revision created. Review the new server-resolved rates before issuing.');
        this.load(); this.openDetail({ quotationId: result.quotationId });
      },
      error: (error) => this.error.set(error?.error?.detail ?? 'Quotation revision could not be created.'),
    });
  }

  clone(row: any): void {
    this.api.get<any>(`/m05/quotations/${row.quotationId}`).subscribe({
      next: (quote) => {
        this.error.set('');
        this.detail.set(null);
        this.fromEnquiryId = '';
        this.draft = {
          customerPartyId: quote.customerPartyId, siteId: quote.siteId ?? '',
          contactId: quote.contactId ?? '', branchId: quote.branchId ?? '',
          lines: (quote.lines ?? []).map((line: any) => ({
            productId: line.productId, quantity: line.quantity, unitCode: line.unitCode,
            rmcGrade: line.rmcGrade ?? '', rmcSlumpBand: line.rmcSlumpBand ?? '',
            rmcAdmixtureCode: line.rmcAdmixtureCode ?? '', rmcPumpType: line.rmcPumpType ?? '',
            discountPercent: line.discountPercent ?? 0,
          })), priceBasis: quote.priceBasis,
          validUntil: new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10),
          paymentTermId: quote.paymentTermId ?? '', placeOfSupplyStateCode: quote.placeOfSupplyStateCode,
          deliveryTerms: quote.deliveryTerms, termsAndConditions: quote.termsAndConditions,
          internalNotes: quote.internalNotes, charges: (quote.charges ?? []).map((charge: any) => ({
            chargeTypeId: charge.chargeTypeId ?? null, description: charge.description, amount: charge.amount,
          })),
        };
        this.creating.set(true);
      },
      error: (error) => this.error.set(error?.error?.detail ?? 'Quotation could not be cloned.'),
    });
  }

  private quoteWrite(quote: any): any {
    return {
      customerPartyId: quote.customerPartyId, siteId: quote.siteId ?? null,
      contactId: quote.contactId ?? null, branchId: quote.branchId ?? null,
      priceBasis: quote.priceBasis,
      validUntil: new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10),
      paymentTermId: quote.paymentTermId ?? null,
      placeOfSupplyStateCode: quote.placeOfSupplyStateCode,
      deliveryTerms: quote.deliveryTerms, termsAndConditions: quote.termsAndConditions,
      internalNotes: quote.internalNotes,
      lines: (quote.lines ?? []).map((line: any) => ({
        productId: line.productId, quantity: line.quantity, unitCode: line.unitCode,
        rate: null, discountPercent: line.discountPercent,
        rmcGrade: line.rmcGrade ?? null, rmcSlumpBand: line.rmcSlumpBand ?? null,
        rmcAdmixtureCode: line.rmcAdmixtureCode ?? null, rmcPumpType: line.rmcPumpType ?? null,
      })),
      charges: (quote.charges ?? []).map((charge: any) => ({
        chargeTypeId: charge.chargeTypeId ?? null, description: charge.description, amount: charge.amount,
      })),
      idempotencyKey: crypto.randomUUID(), version: quote.version,
    };
  }

  private emptyLine(): any {
    return { productId: '', quantity: 1, unitCode: '', rmcGrade: '', rmcSlumpBand: '', rmcAdmixtureCode: '', rmcPumpType: '', discountPercent: 0 };
  }

  private emptyDraft(): any {
    return { customerPartyId: '', siteId: '', contactId: '', branchId: '', lines: [this.emptyLine()], discountPercent: 0, charges: [], priceBasis: 'ExPlant', validUntil: new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10), paymentTermId: '', placeOfSupplyStateCode: '', deliveryTerms: '', termsAndConditions: '', internalNotes: '' };
  }
}

@Component({
  selector: 'app-m05-public-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="public-page">
      <p class="eyebrow">BERYNX CONSTRUCTION 360 · CUSTOMER QUOTATION</p>
      <p class="error" *ngIf="error()">{{ error() }}</p>
      <article *ngIf="quote() as item">
        <header><div><h1>{{ item.companyLegalNameSnapshot }}</h1><p>Quotation {{ item.number }} · Revision {{ item.revisionNumber }}</p></div><span class="chip">{{ item.status }}</span></header>
        <section><h2>Prepared for {{ item.customerNameSnapshot }}</h2><p>{{ item.customerAddressSnapshot }}</p><p *ngIf="item.siteAddressSnapshot">Site: {{ item.siteAddressSnapshot }}</p></section>
        <p>Price basis: {{ item.priceBasis }} · Valid through {{ item.validUntil | date:'longDate' }}</p>
        <section class="items"><div class="item heading"><b>Description</b><b>Quantity</b><b>Unit rate</b><b>Tax</b><b>Total</b></div>
          <div class="item" *ngFor="let line of item.lines"><span>{{ line.description }}</span><span>{{ line.quantity }} {{ line.unitCode }}</span><span>{{ line.rate | currency:'INR':'symbol':'1.2-4' }}</span><span>CGST {{ line.cgstAmount | currency:'INR':'symbol':'1.2-2' }} · SGST {{ line.sgstAmount | currency:'INR':'symbol':'1.2-2' }} · IGST {{ line.igstAmount | currency:'INR':'symbol':'1.2-2' }}</span><b>{{ line.total | currency:'INR':'symbol':'1.2-2' }}</b></div>
          <div class="item" *ngFor="let charge of item.charges"><span>{{ charge.description }} (charge)</span><span>1</span><span>{{ charge.amount | currency:'INR':'symbol':'1.2-2' }}</span><span>CGST {{ charge.cgstAmount | currency:'INR':'symbol':'1.2-2' }} · SGST {{ charge.sgstAmount | currency:'INR':'symbol':'1.2-2' }} · IGST {{ charge.igstAmount | currency:'INR':'symbol':'1.2-2' }}</span><b>{{ charge.total | currency:'INR':'symbol':'1.2-2' }}</b></div>
        </section>
        <section class="summary"><p>Subtotal <b>{{ item.subtotal | currency:'INR':'symbol':'1.2-2' }}</b></p><p>Discount <b>{{ item.discountTotal | currency:'INR':'symbol':'1.2-2' }}</b></p><p>Charges <b>{{ item.chargesTotal | currency:'INR':'symbol':'1.2-2' }}</b></p><p>Tax <b>{{ item.taxTotal | currency:'INR':'symbol':'1.2-2' }}</b></p><h2>Total <b>{{ item.total | currency:'INR':'symbol':'1.2-2' }}</b></h2></section>
        <section><h3>Delivery and terms</h3><p>{{ item.deliveryTerms }}</p><p>{{ item.termsAndConditions }}</p></section>
        <section *ngIf="item.status === 'Sent' && item.status !== 'Expired'">
          <h3>Respond to this quotation</h3><p class="notice">A verified customer OTP provider is required. No decision is recorded unless verification succeeds.</p>
          <form (ngSubmit)="sendOtp()"><label>Mobile number<input name="mobile" [(ngModel)]="mobile" autocomplete="tel" required></label><button class="secondary" type="submit" [disabled]="busy()">Request OTP</button></form>
          <form (ngSubmit)="decide()"><label>OTP code<input name="otp" [(ngModel)]="otp" inputmode="numeric" autocomplete="one-time-code" required></label><label>Decision<select name="decision" [(ngModel)]="decision"><option>Accepted</option><option>Rejected</option></select></label><label *ngIf="decision === 'Rejected'">Reason<input name="reason" [(ngModel)]="reason" required></label><button class="primary" type="submit" [disabled]="busy()">Submit decision</button></form>
        </section>
      </article>
      <p *ngIf="!quote() && !error()">Loading quotation…</p>
    </main>
  `,
  styles: [`
    :host{display:block;min-height:100vh;background:#f5f1ec;color:#292622;font-family:Inter,Arial,sans-serif}.public-page{max-width:980px;margin:0 auto;padding:36px 24px 60px}.eyebrow{color:#945037;font-weight:700;font-size:11px;letter-spacing:.13em}article{background:white;border:1px solid #e3ddd7;border-radius:12px;padding:30px;box-shadow:0 8px 24px #4325110c}header{display:flex;justify-content:space-between;gap:20px;align-items:flex-start;border-bottom:1px solid #e5ded7;padding-bottom:20px}h1{margin:0;color:#8d4a32}h2{display:flex;justify-content:space-between;gap:16px;font-size:19px}section{padding:16px 0;border-bottom:1px solid #eee8e2}.items{overflow:auto;margin:18px 0}.item{display:grid;grid-template-columns:2fr 1fr 1fr 1fr 1fr;gap:12px;padding:12px;border-bottom:1px solid #eee8e2;font-size:13px}.heading{background:#faf8f6;font-size:11px;text-transform:uppercase;color:#777}.summary{max-width:380px;margin-left:auto}.summary p{display:flex;justify-content:space-between}.chip{background:#f1e8df;border-radius:20px;padding:7px 12px}.notice{background:#f6eee7;padding:12px;border-radius:7px;font-size:13px}form{display:flex;gap:10px;align-items:end;flex-wrap:wrap;margin:12px 0}label{display:grid;gap:5px;font-size:12px}input,select{padding:10px;border:1px solid #d5ccc3;border-radius:7px;min-width:180px}.primary,.secondary{border-radius:7px;padding:10px 14px;font-weight:650;cursor:pointer}.primary{border:1px solid #89452f;background:#914b34;color:white}.secondary{border:1px solid #d6ccc3;background:#fff;color:#393531}.error{color:#9c3028}@media(max-width:700px){.public-page{padding:22px 12px}article{padding:18px}.item{min-width:680px}}
  `],
})
export class M05PublicPage implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly route = inject(ActivatedRoute);
  readonly quote = signal<any | null>(null);
  readonly error = signal('');
  readonly busy = signal(false);
  mobile = '';
  otp = '';
  decision = 'Accepted';
  reason = '';
  token = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.token = params.get('token') ?? '';
      this.api.get<any>(`/m05/public/quotes/${encodeURIComponent(this.token)}`).subscribe({
        next: (quote) => this.quote.set(quote),
        error: (error) => this.error.set(error?.error?.detail ?? 'This quotation link is unavailable or expired.'),
      });
    });
  }

  sendOtp(): void {
    this.busy.set(true);
    this.api.post<any, any>(`/m05/public/quotes/${encodeURIComponent(this.token)}/otp`, { token: this.token, mobile: this.mobile }).subscribe({
      next: () => { this.error.set('OTP sent.'); this.busy.set(false); },
      error: (error) => { this.error.set(error?.error?.detail ?? 'OTP provider is not configured; no code was sent.'); this.busy.set(false); },
    });
  }

  decide(): void {
    this.busy.set(true);
    this.api.post<any, any>(`/m05/public/quotes/${encodeURIComponent(this.token)}/decision`, {
      token: this.token, mobile: this.mobile, otp: this.otp, decision: this.decision,
      rejectionReason: this.reason || null,
    }).subscribe({
      next: (result) => { this.error.set(`Decision recorded: ${result.status}`); this.busy.set(false); },
      error: (error) => { this.error.set(error?.error?.detail ?? 'Customer decision was not recorded.'); this.busy.set(false); },
    });
  }
}
