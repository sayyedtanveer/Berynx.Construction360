import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m16-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head"><div><p class="eyebrow">INSIGHTS · M16 REPORTS</p><h1>Reports</h1>
        <p class="muted">Supported reports read operational source records; unavailable reports are not simulated.</p></div>
        <button class="secondary" (click)="loadCatalog()">Refresh catalogue</button></header>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p><p class="success" role="status" *ngIf="notice()">{{ notice() }}</p>
      <section class="panel"><p class="eyebrow">SUPPORTED REPORTS</p>
        <div class="form">
          <label>Report<select [(ngModel)]="code"><option *ngFor="let item of catalog()?.available" [value]="item">{{ item }} · {{ reportName(item) }}</option></select></label>
          <label>From<input type="date" [(ngModel)]="from"></label><label>To<input type="date" [(ngModel)]="to"></label>
          <label>Branch ID (optional)<input [(ngModel)]="branchId"></label>
          <label>Customer ID (optional)<input [(ngModel)]="customerId"></label>
          <label>Site ID (optional)<input [(ngModel)]="siteId"></label>
          <label>Product ID (optional)<input [(ngModel)]="productId"></label>
        </div>
        <div class="actions"><button class="primary" (click)="run()">Run report</button>
          <button class="secondary" [disabled]="!report()" (click)="exportCsv()">Export CSV (audited)</button></div>
      </section>
      <section class="panel" *ngIf="report() as result">
        <div class="report-head"><div><p class="eyebrow">{{ result.reportCode }}</p><h2>Report results</h2>
          <small>{{ result.from }} to {{ result.to }} · Refreshed {{ result.lastRefreshedAt | date:'medium' }}</small></div></div>
        <div class="table-wrap"><table><thead><tr><th *ngFor="let column of result.columns">{{ column }}</th></tr></thead>
          <tbody><tr *ngFor="let row of result.rows"><td *ngFor="let column of result.columns">{{ format(row[column]) }}</td></tr></tbody>
          <tfoot><tr><th *ngFor="let column of result.columns">{{ result.totals[column] ?? (column === result.columns[0] ? 'Total' : '') }}</th></tr></tfoot>
        </table></div>
        <p class="empty" *ngIf="!result.rows.length">No matching source transactions in this period.</p>
      </section>
      <section class="panel"><p class="eyebrow">PENDING CATALOGUE</p><h2>Not yet implemented</h2>
        <p class="notice" *ngFor="let item of catalog()?.pending">{{ item }}</p>
      </section>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1450px;margin:auto;color:var(--ink,#292723)}.page-head{display:flex;justify-content:space-between;gap:18px}
    .eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}h1{font-size:30px;margin:4px 0}.muted,small{color:#77736c}
    .panel{background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:18px;margin:14px 0}.form{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px}
    label{font-size:12px;display:grid;gap:5px;color:#57524a}input,select{font:inherit;padding:9px;border:1px solid #d7d1c8;border-radius:6px;min-width:0}
    .actions{display:flex;gap:10px;margin-top:14px}.primary,.secondary{border:0;border-radius:6px;padding:9px 13px;cursor:pointer}
    .primary{background:#a65d3b;color:white}.secondary{background:#eee9e1;color:#39352f}.error{color:#a3342a}.success{color:#24734f}
    .notice{background:#f0e8de;border-radius:6px;padding:10px;font-size:13px}.table-wrap{overflow:auto}table{border-collapse:collapse;width:100%;font-size:13px}th,td{text-align:left;padding:10px;border-bottom:1px solid #eee9e1;white-space:nowrap}tfoot{background:#f7f4ef}
    .empty{color:#77736c}.report-head{margin-bottom:14px}
    @media(max-width:760px){.workspace{padding:18px 14px}.form{grid-template-columns:repeat(2,minmax(0,1fr))}.page-head{display:block}}
  `],
})
export class M16Page implements OnInit {
  private readonly api = inject(ApiClient);
  readonly catalog = signal<any | null>(null);
  readonly report = signal<any | null>(null);
  readonly error = signal('');
  readonly notice = signal('');
  code = 'R-S03';
  from = new Date(Date.UTC(new Date().getUTCFullYear(), new Date().getUTCMonth(), 1)).toISOString().slice(0, 10);
  to = new Date().toISOString().slice(0, 10);
  branchId = ''; customerId = ''; siteId = ''; productId = '';

  ngOnInit(): void { this.loadCatalog(); }
  loadCatalog(): void {
    this.api.get<any>('/m16/catalog').subscribe({
      next: (value) => { this.catalog.set(value); if (!value.available.includes(this.code)) this.code = value.available[0] ?? ''; },
      error: () => this.error.set('Report catalogue unavailable. Check M16 module activation and permissions.'),
    });
  }
  reportName(code: string): string {
    return ({ 'R-S03': 'Pending orders', 'R-D01': 'RMC delivery summary', 'R-D04': 'Unbilled challans ageing', 'R-X01': 'Expense analytics' } as Record<string, string>)[code] ?? code;
  }
  private query(): string {
    const params = new URLSearchParams({ from: this.from, to: this.to });
    if (this.branchId) params.set('branchId', this.branchId);
    if (this.customerId) params.set('customerId', this.customerId);
    if (this.siteId) params.set('siteId', this.siteId);
    if (this.productId) params.set('productId', this.productId);
    return params.toString();
  }
  run(): void {
    this.api.get<any>(`/m16/reports/${this.code}?${this.query()}`).subscribe({
      next: (value) => { this.report.set(value); this.error.set(''); },
      error: () => this.error.set('Report unavailable or filters exceed your permitted data scope.'),
    });
  }
  exportCsv(): void {
    this.api.getBlob(`/m16/reports/${this.code}/export.csv?${this.query()}`).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a'); link.href = url; link.download = `${this.code.toLowerCase()}-${this.from}.csv`; link.click();
        URL.revokeObjectURL(url); this.notice.set('CSV downloaded and export audit recorded.');
      },
      error: () => this.error.set('CSV export failed. Check export permission and cost access.'),
    });
  }
  format(value: unknown): string { return value == null ? '' : String(value); }
}
