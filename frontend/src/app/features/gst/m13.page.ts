import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m13-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head"><div><p class="eyebrow">TAX · M13 COMPLIANCE</p><h1>GST workspace</h1>
        <p class="muted">Month-end view from posted invoices; filing remains with the tenant and tax advisor.</p></div>
        <div class="controls"><input aria-label="GST period" type="month" [(ngModel)]="month" (change)="load()">
          <button class="secondary" type="button" (click)="load()">Re-check</button></div></header>
      <p class="notice">This is an internal extract, not a verified GST Offline Tool payload. External portal filing is not performed. Current sales invoice posting and supplier ITC integrations are pending.</p>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p><p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <p *ngIf="loading()">Checking posted source documents…</p>
      <section class="hero" *ngIf="workspace() as data">
        <div><small>Readiness</small><strong>{{ data.readinessPercent }}%</strong></div>
        <div><small>Blocking errors</small><strong>{{ data.blockingErrors }}</strong></div>
        <div><small>Posted invoices</small><strong>{{ data.invoices.length }}</strong></div>
        <div><small>Period</small><strong>{{ data.status }}</strong></div>
      </section>
      <div class="actions">
        <button class="secondary" type="button" (click)="exportJson()">Export internal JSON</button>
        <button class="primary" type="button" [disabled]="!workspace() || workspace().blockingErrors > 0 || workspace().status === 'Filed'" (click)="markFiled()">Record manually filed</button>
      </div>
      <section class="grid">
        <article class="panel"><p class="eyebrow">GSTR-1 OUTWARD</p><h2>Posted sales register</h2>
          <p class="empty" *ngIf="!workspace()?.invoices?.length">No posted invoices in this period.</p>
          <div class="row" *ngFor="let invoice of workspace()?.invoices">
            <div><strong>{{ invoice.number || 'Unnumbered' }} · {{ invoice.customerNameSnapshot }}</strong>
              <small>{{ invoice.invoiceDate | date:'mediumDate' }} · GSTIN {{ invoice.customerGstinSnapshot || 'missing' }} · Place {{ invoice.placeOfSupplyStateCode || 'missing' }}</small></div>
            <span>{{ invoice.taxableAmount | currency:'INR':'symbol':'1.2-2' }}</span>
          </div>
          <footer><strong>Taxable {{ workspace()?.outwardTax?.taxable | currency:'INR':'symbol':'1.2-2' }}</strong>
            <span>Output GST {{ workspace()?.outwardTax?.total | currency:'INR':'symbol':'1.2-2' }}</span></footer>
        </article>
        <article class="panel"><p class="eyebrow">GSTR-3B WORKING</p><h2>Source totals</h2>
          <div class="row"><span>Output tax</span><strong>{{ workspace()?.gstr3b?.outputTax | currency:'INR':'symbol':'1.2-2' }}</strong></div>
          <div class="row"><span>Input tax credit</span><strong>{{ workspace()?.itc?.eligible | currency:'INR':'symbol':'1.2-2' }}</strong></div>
          <div class="row"><span>Net payable estimate</span><strong>{{ workspace()?.gstr3b?.netPayable | currency:'INR':'symbol':'1.2-2' }}</strong></div>
          <p class="notice">{{ workspace()?.itc?.status }}</p>
        </article>
      </section>
      <section class="panel"><p class="eyebrow">EXCEPTIONS</p><h2>Pre-export checks</h2>
        <p class="empty" *ngIf="!workspace()?.exceptions?.length">No posted-invoice exceptions for the current period.</p>
        <div class="row" *ngFor="let exception of workspace()?.exceptions">
          <div><strong>{{ exception.code }}</strong><small>Invoice {{ exception.invoiceId }}</small></div>
          <span class="chip">{{ exception.severity }}</span>
        </div>
      </section>
      <section class="panel"><p class="eyebrow">ITC / HSN</p><h2>Supplier data and product summary</h2>
        <p class="notice">Supplier invoice ITC and 180-day reversal are not available until M07 supplier invoice posting is connected. Tax rates are maintained in M03 effective-dated masters.</p>
        <div class="row" *ngFor="let line of workspace()?.lines">
          <span>{{ line.hsnCodeSnapshot || 'Missing HSN' }} · {{ line.productNameSnapshot }}</span>
          <span>{{ line.taxableAmount | currency:'INR':'symbol':'1.2-2' }} · {{ line.taxPercent }}%</span>
        </div>
      </section>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1450px;margin:auto;color:var(--ink,#292723)}
    .page-head{display:flex;justify-content:space-between;align-items:flex-start;gap:20px}.eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}
    h1{font-size:30px;margin:4px 0}.muted,small{color:#77736c}small{display:block;margin-top:4px}
    .controls,.actions{display:flex;gap:10px}button{border:0;border-radius:6px;padding:10px 14px;cursor:pointer}
    input{font:inherit;padding:9px;border:1px solid #d7d1c8;border-radius:6px}.primary{background:#a65d3b;color:#fff}.secondary{background:#eee9e1;color:#39352f}
    .notice{background:#f0e8de;padding:12px;border-radius:6px;font-size:13px;line-height:1.5}.error{color:#a3342a}.success{color:#24734f}
    .hero{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:18px 0}.hero div,.panel{background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:16px}
    .hero small{display:block}.hero strong{display:block;font-size:24px;margin-top:5px}.actions{margin:10px 0 18px}
    .grid{display:grid;grid-template-columns:1.4fr 1fr;gap:14px}.panel{margin-bottom:14px}.row,footer{display:flex;justify-content:space-between;align-items:center;gap:14px;padding:11px 0;border-bottom:1px solid #eee9e1;font-size:13px}
    footer{border:0}.empty{padding:14px;color:#77736c}.chip{background:#eee9e1;border-radius:20px;padding:4px 9px;font-size:11px}
    @media(max-width:760px){.workspace{padding:18px 14px}.page-head{display:block}.controls{margin-top:12px}.hero{grid-template-columns:repeat(2,1fr)}.grid{grid-template-columns:1fr}.actions{flex-direction:column}}
  `],
})
export class M13Page implements OnInit {
  private readonly api = inject(ApiClient);
  readonly workspace = signal<any | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  month = new Date().toISOString().slice(0, 7);

  ngOnInit(): void { this.load(); }
  private periodStart(): string { return `${this.month}-01`; }

  load(): void {
    this.loading.set(true);
    this.api.get<any>(`/m13/workspace?periodStart=${this.periodStart()}`).subscribe({
      next: (data) => { this.workspace.set(data); this.loading.set(false); },
      error: () => { this.error.set('GST workspace unavailable. Check M13 activation and your role.'); this.loading.set(false); },
    });
  }

  exportJson(): void {
    this.api.get<any>(`/m13/gstr1/export?periodStart=${this.periodStart()}`).subscribe({
      next: (data) => {
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url; link.download = `gstr1-internal-${this.month}.json`; link.click();
        URL.revokeObjectURL(url);
        this.notice.set('Internal JSON extract downloaded; it is not validated for portal upload.');
      },
      error: () => this.error.set('GST export could not be generated.'),
    });
  }

  markFiled(): void {
    this.api.post<any, any>('/m13/periods/filed', {
      periodStart: this.periodStart(), externalAcknowledgement: '',
    }).subscribe({
      next: () => { this.notice.set('Period marked filed in the application. External portal filing was not performed.'); this.load(); },
      error: (error) => this.error.set(error?.error?.detail ?? 'GST period could not be marked filed.'),
    });
  }
}
