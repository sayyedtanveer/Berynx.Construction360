import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

@Component({
  selector: 'app-m15-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head"><div><p class="eyebrow">HOME · M15 OPERATIONS</p><h1>Dashboard</h1>
        <p class="muted">Live counts and totals from persisted transactions; unavailable integrations are labelled below.</p></div>
        <div class="filters"><label>From<input type="date" [(ngModel)]="from" (change)="load()"></label>
          <label>To<input type="date" [(ngModel)]="to" (change)="load()"></label>
          <button class="secondary" type="button" (click)="load()">Refresh</button></div></header>
      <p class="error" role="alert" *ngIf="error()">{{ error() }}</p>
      <p class="muted" *ngIf="data()">Last refreshed {{ data().lastRefreshedAt | date:'mediumTime' }}</p>
      <section class="cards" *ngIf="data() as d">
        <a class="card" routerLink="/m06/orders"><small>Sales orders</small><strong>{{ d.sales.orders }}</strong><span>{{ d.sales.orderValue | currency:'INR':'symbol':'1.0-0' }}</span></a>
        <a class="card" routerLink="/m10/invoices"><small>Posted invoices</small><strong>{{ d.invoices.postedCount }}</strong><span>{{ d.invoices.postedValue | currency:'INR':'symbol':'1.0-0' }}</span></a>
        <a class="card" routerLink="/m10/invoices"><small>Outstanding estimate</small><strong>{{ d.invoices.outstandingEstimate | currency:'INR':'symbol':'1.0-0' }}</strong><span>Posted invoice value less posted allocations</span></a>
        <a class="card" routerLink="/m08/dispatch"><small>Trips in period</small><strong>{{ d.operations.trips }}</strong><span>{{ d.operations.deliveredQuantity }} delivered / {{ d.operations.plannedQuantity }} planned</span></a>
        <a class="card" routerLink="/m09/challans"><small>Unbilled challans</small><strong>{{ d.operations.unbilledChallans }}</strong><span>Persisted challans without an invoice reference</span></a>
        <a class="card" routerLink="/m14/expenses"><small>Posted expenses</small><strong>{{ d.expenses.costVisible ? (d.expenses.postedValue | currency:'INR':'symbol':'1.0-0') : 'Restricted' }}</strong><span>{{ d.expenses.costVisible ? 'M14 posted source entries' : 'Requires view_cost permission' }}</span></a>
      </section>
      <section class="attention" *ngIf="data() as d">
        <div><p class="eyebrow">NEEDS ATTENTION</p><h2>Current work queues</h2></div>
        <a routerLink="/m02/approvals"><strong>{{ d.attention.pendingApprovals }}</strong><span>Pending approvals</span></a>
        <a routerLink="/m10/invoices"><strong>{{ d.attention.pendingInvoicePost }}</strong><span>Invoices pending post</span></a>
        <a routerLink="/m11/receipts"><strong>{{ d.attention.receiptsToAllocate }}</strong><span>Draft receipts to allocate</span></a>
      </section>
      <section class="panel" *ngIf="data() as d"><p class="eyebrow">OPERATIONS</p><h2>Trips by status</h2>
        <p class="empty" *ngIf="!tripStatuses(d).length">No trips found in this period.</p>
        <div class="status-row" *ngFor="let item of tripStatuses(d)"><span>{{ item.name }}</span><strong>{{ item.count }}</strong></div>
      </section>
      <section class="panel"><p class="eyebrow">UNAVAILABLE / PENDING MODULE DATA</p><h2>Not included in this dashboard</h2>
        <p class="notice" *ngFor="let item of data()?.unavailable">{{ item }}</p>
      </section>
    </main>
  `,
  styles: [`
    .workspace{padding:28px 32px;max-width:1450px;margin:auto;color:var(--ink,#292723)}.page-head{display:flex;justify-content:space-between;gap:18px;align-items:flex-start}
    .eyebrow{font-size:11px;letter-spacing:.12em;color:var(--copper,#a65d3b);font-weight:700}h1{font-size:30px;margin:4px 0}.muted{color:#77736c}
    .filters{display:flex;align-items:end;gap:10px}.filters label{display:grid;gap:4px;font-size:11px;color:#706b63}
    input{font:inherit;padding:8px;border:1px solid #d7d1c8;border-radius:6px}.secondary{background:#eee9e1;color:#39352f;border:0;border-radius:6px;padding:9px 13px}
    .error{color:#a3342a}.cards{display:grid;grid-template-columns:repeat(3,1fr);gap:13px;margin:18px 0}
    .card,.panel,.attention{background:#fff;border:1px solid #e3ded6;border-radius:8px;padding:18px}.card{display:grid;gap:7px;color:inherit;text-decoration:none}
    .card:hover,.attention a:hover{border-color:#a65d3b}.card small{color:#77736c}.card strong{font-size:26px}.card span{font-size:12px;color:#77736c}
    .attention{display:grid;grid-template-columns:2fr repeat(3,1fr);gap:12px;align-items:center;margin-bottom:14px}
    .attention a{display:grid;text-decoration:none;color:inherit;border-left:1px solid #eee9e1;padding:8px 14px}.attention a strong{font-size:24px;color:#a65d3b}
    .panel{margin:14px 0}.status-row{display:flex;justify-content:space-between;padding:10px 0;border-bottom:1px solid #eee9e1}
    .notice{background:#f0e8de;border-radius:6px;padding:10px;font-size:13px}.empty{color:#77736c}
    @media(max-width:850px){.cards{grid-template-columns:repeat(2,1fr)}.attention{grid-template-columns:1fr 1fr}.page-head{display:block}.filters{margin-top:12px;flex-wrap:wrap}}
    @media(max-width:520px){.workspace{padding:18px 14px}.cards{grid-template-columns:1fr}.attention{grid-template-columns:1fr}}
  `],
})
export class M15Page implements OnInit {
  private readonly api = inject(ApiClient);
  readonly data = signal<any | null>(null);
  readonly error = signal('');
  from = new Date(Date.UTC(new Date().getUTCFullYear(), new Date().getUTCMonth(), 1)).toISOString().slice(0, 10);
  to = new Date().toISOString().slice(0, 10);

  ngOnInit(): void { this.load(); }
  load(): void {
    this.api.get<any>(`/m15/home?from=${this.from}&to=${this.to}`).subscribe({
      next: (result) => { this.data.set(result); this.error.set(''); },
      error: () => this.error.set('Dashboard data is unavailable. Check M15 activation, permission and branch scope.'),
    });
  }
  tripStatuses(data: any): Array<{ name: string; count: number }> {
    return Object.entries(data.operations.byStatus).map(([name, count]) => ({ name, count: count as number }));
  }
}
